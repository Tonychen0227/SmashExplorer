using Newtonsoft.Json;
using System.Collections.Generic;

public class Set
{
    public string Id { get; set; }
    public string VideoGameName { get; set; }
    public string Identifier { get; set; }
    public string VideoGameId { get; set; }
    public string EventId { get; set; }
    public string FullRoundText { get; set; }
    public string DisplayScore { get; set; }
    public string WinnerId { get; set; }
    public bool? IsFakeSet { get; set; }
    public int? Round { get; set; }
    public int? WPlacement { get; set; }
    public int? LPlacement { get; set; }
    public int? CompletedAt { get; set; }
    public int? CreatedAt { get; set; }
    public List<Game> Games { get; set; }
    public List<Entrant> Entrants { get; set; }
    public List<string> EntrantIds { get; set; }
    public string BracketType { get; set; }
    public string PhaseGroupId { get; set; }
    public string PhaseIdentifier { get; set; }
    public int? PhaseOrder { get; set; }
    public string PhaseName { get; set; }
    public string PhaseId { get; set; }
    public Stream Stream { get; set; }
    public Dictionary<string, string> DetailedScore { get; set; }
    public bool IsUpsetOrNotable { get; set; }
    public int? SetGamesType { get; set; }
    public int? TotalGames { get; set; }
    public Station Station { get; set; }
    public ReportScoreAPIRequestBody ReportedScoreViaAPI { get; set; }

    [JsonProperty("_ts")]
    public int Timestamp { get; set; }
}