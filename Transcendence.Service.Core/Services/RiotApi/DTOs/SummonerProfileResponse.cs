namespace Transcendence.Service.Core.Services.RiotApi.DTOs;

public class SummonerProfileResponse
{
    public string Puuid { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string TagLine { get; set; } = string.Empty;
    public int SummonerLevel { get; set; }
    public int ProfileIconId { get; set; }

    // Rank data
    public RankInfo? SoloRank { get; set; }
    public RankInfo? FlexRank { get; set; }

    // Data freshness
    public DataAgeMetadata ProfileAge { get; set; } = new();
    public DataAgeMetadata RankAge { get; set; } = new();
}

public class RankInfo
{
    public string Tier { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public int LeaguePoints { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}
