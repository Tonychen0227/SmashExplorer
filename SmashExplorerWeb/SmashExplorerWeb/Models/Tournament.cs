using System.Collections.Generic;

public class Tournament
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<TournamentEvent> Events { get; set; }
}

public class TournamentEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<TournamentEventPhase> Phases { get; set; }
}

public class TournamentEventPhase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int BestOf { get; set; }
}