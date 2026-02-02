using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Transcendence.Data;
using Transcendence.Data.Models.LoL.Static;
using Transcendence.Service.Core.Services.Cache;
using Transcendence.Service.Core.Services.StaticData.DTOs;
using Transcendence.Service.Core.Services.StaticData.Interfaces;

namespace Transcendence.Service.Core.Services.StaticData.Implementations;

public class StaticDataService(
    TranscendenceContext context,
    IHttpClientFactory httpClientFactory,
    ICacheService cacheService)
    : IStaticDataService
{
    public async Task UpdateStaticDataAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();

        var patches = await FetchPatchesAsync(client, cancellationToken);
        if (patches is null || patches.Count == 0) return;

        // Only use the latest patch (first in the list) and trim last decimal (e.g., 15.20.1 -> 15.20)
        var latestFullPatch = patches[0].Patch;
        var latestCdragonPatch = TrimPatch(latestFullPatch);

        await EnsureStaticDataForPatchAsync(latestCdragonPatch, cancellationToken);
    }

    public async Task DetectAndRefreshAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();
        var patches = await FetchPatchesAsync(client, cancellationToken);
        if (patches is null || patches.Count == 0) return;

        var latestFullPatch = patches[0].Patch;
        var latestCdragonPatch = TrimPatch(latestFullPatch);

        // Check if this is a new patch
        var currentPatch = await context.Patches
            .FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

        if (currentPatch == null || currentPatch.Version != latestCdragonPatch)
        {
            // New patch detected
            if (currentPatch != null)
            {
                currentPatch.IsActive = false;
            }

            var newPatch = new Patch
            {
                Version = latestCdragonPatch,
                ReleaseDate = DateTime.UtcNow,
                DetectedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.Patches.Add(newPatch);
            await context.SaveChangesAsync(cancellationToken);

            // Invalidate all patch-dependent cache
            if (currentPatch != null)
            {
                await cacheService.RemoveByTagAsync($"patch-{currentPatch.Version}", cancellationToken);
            }

            // Fetch fresh static data
            await EnsureStaticDataForPatchAsync(latestCdragonPatch, cancellationToken);
        }
    }

    public async Task EnsureStaticDataForPatchAsync(string patchVersion, CancellationToken cancellationToken = default)
    {
        // Ensure Patch row exists
        if (!await context.Patches.AnyAsync(p => p.Version == patchVersion, cancellationToken))
        {
            context.Patches.Add(new Patch
            {
                Version = patchVersion,
                ReleaseDate = DateTime.UtcNow,
                DetectedAt = DateTime.UtcNow,
                IsActive = false  // Will be set true by DetectAndRefreshAsync if this is the latest
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        // Use cache for runes
        await cacheService.GetOrCreateAsync(
            $"static:runes:{patchVersion}",
            async ct => await FetchAndStoreRunesAsync(patchVersion, ct),
            expiration: TimeSpan.FromDays(30),
            localExpiration: TimeSpan.FromMinutes(5),
            tags: new[] { $"patch-{patchVersion}" },
            cancellationToken: cancellationToken);

        // Use cache for items
        await cacheService.GetOrCreateAsync(
            $"static:items:{patchVersion}",
            async ct => await FetchAndStoreItemsAsync(patchVersion, ct),
            expiration: TimeSpan.FromDays(30),
            localExpiration: TimeSpan.FromMinutes(5),
            tags: new[] { $"patch-{patchVersion}" },
            cancellationToken: cancellationToken);
    }

    private async Task<bool> FetchAndStoreRunesAsync(string patchVersion, CancellationToken cancellationToken)
    {
        // Check if already in database
        var existingRuneIds = await context.RuneVersions
            .Where(rv => rv.PatchVersion == patchVersion)
            .Select(rv => rv.RuneId)
            .ToListAsync(cancellationToken);

        if (existingRuneIds.Count > 0)
        {
            return true;  // Already stored
        }

        // Fetch from API
        var client = httpClientFactory.CreateClient();
        var runes = await FetchRunesForPatchAsync(client, patchVersion, cancellationToken);
        await context.RuneVersions.AddRangeAsync(runes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> FetchAndStoreItemsAsync(string patchVersion, CancellationToken cancellationToken)
    {
        // Check if already in database
        var existingItemIds = await context.ItemVersions
            .Where(iv => iv.PatchVersion == patchVersion)
            .Select(iv => iv.ItemId)
            .ToListAsync(cancellationToken);

        if (existingItemIds.Count > 0)
        {
            return true;  // Already stored
        }

        // Fetch from API
        var client = httpClientFactory.CreateClient();
        var items = await FetchItemsForPatchAsync(client, patchVersion, cancellationToken);
        await context.ItemVersions.AddRangeAsync(items, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string TrimPatch(string patch)
    {
        // Converts "15.20.1" -> "15.20" by removing the last dot segment
        var parts = patch.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : patch;
    }

    private async Task<List<DataDragonPatch>?> FetchPatchesAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response =
            await client.GetAsync("https://ddragon.leagueoflegends.com/api/versions.json", cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var versions = JsonSerializer.Deserialize<List<string>>(content);
        return versions?.Select(v => new DataDragonPatch
        {
            Patch = v
        }).ToList();
    }

    private async Task<List<RuneVersion>> FetchRunesForPatchAsync(HttpClient client, string patch,
        CancellationToken cancellationToken)
    {
        var response =
            await client.GetAsync(
                $"https://raw.communitydragon.org/{patch}/plugins/rcp-be-lol-game-data/global/default/v1/perks.json",
                cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var communityDragonRunes = JsonSerializer.Deserialize<List<CommunityDragonRune>>(content);

        return communityDragonRunes.Select(r => new RuneVersion
        {
            RuneId = r.Id,
            PatchVersion = patch,
            Name = r.Name,
            Description = r.ShortDesc
        }).ToList();
    }

    private async Task<List<ItemVersion>> FetchItemsForPatchAsync(HttpClient client, string patch,
        CancellationToken cancellationToken)
    {
        var response =
            await client.GetAsync(
                $"https://raw.communitydragon.org/{patch}/plugins/rcp-be-lol-game-data/global/default/v1/items.json",
                cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var communityDragonItems = JsonSerializer.Deserialize<List<CommunityDragonItem>>(content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (communityDragonItems == null || communityDragonItems.Count == 0)
            return new List<ItemVersion>();

        return communityDragonItems.Select(i => new ItemVersion
        {
            ItemId = i.Id,
            PatchVersion = patch,
            Name = i.Name,
            Description = i.Description,
            Tags = i.Categories ?? new List<string>()
        }).ToList();
    }
}