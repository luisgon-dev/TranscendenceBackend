using Microsoft.EntityFrameworkCore;
using Transcendence.Data.Models.LoL.Match;
using Transcendence.Data.Repositories.Interfaces;

namespace Transcendence.Data.Repositories.Implementations;

public class MatchRepository(TranscendenceContext transcendenceContext) : IMatchRepository
{
    public async Task AddMatchAsync(Match match, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(match.MatchId))
            throw new ArgumentException("Match.MatchId must be populated before persistence.", nameof(match));

        if (transcendenceContext.Matches.Local.Any(x => x.MatchId == match.MatchId))
            return;

        var exists = await transcendenceContext.Matches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.MatchId == match.MatchId, cancellationToken);

        if (exists)
            return;

        await transcendenceContext.Matches.AddAsync(match, cancellationToken);
    }

    public Task<Match?> GetMatchByIdAsync(string matchId, CancellationToken cancellationToken)
    {
        return transcendenceContext.Matches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.MatchId == matchId, cancellationToken);
    }
}
