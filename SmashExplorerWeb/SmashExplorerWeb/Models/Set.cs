using System.Collections.Generic;

public class Set
{
    public string Id { get; set; }
    public string FullRoundText { get; set; }
    public string DisplayScore { get; set; }
    public string WinnerId { get; set; }
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
}