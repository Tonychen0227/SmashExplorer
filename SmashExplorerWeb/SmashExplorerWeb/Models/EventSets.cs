using System.Collections.Generic;

public class EventSetsModel
{
    public Event TournamentEvent { get; set; }
    public Dictionary<string, List<EmergencySet>> Sets { get; set; }
}

public class EmergencySet
{
    public string Id { get; set; }
    public string DisplayScore { get; set; }
    public string Identifier { get; set; }
    public string PhaseIdentifier { get; set; }
    public string FullRoundText { get; set; }
    public Dictionary<string, string> DetailedScore { get; set; }
    public List<EmergencyEntrant> Entrants { get; set; }
    public string PhaseId { get; set; }
    public string PhaseName { get; set; }
    public string WinnerId { get; set; }
    public int? Round { get; set; }
}

public class EmergencyEntrant
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int? InitialSeedNum { get; set; }
    public string PreReqId { get; set; }
    public string PreReqType { get; set; }
}