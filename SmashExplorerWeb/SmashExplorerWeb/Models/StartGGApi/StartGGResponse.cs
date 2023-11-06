using System.Collections.Generic;

public class StartGGTournamentResponse
{
    public StartGGTournament Tournament { get; set; }
}

public class StartGGTournament
{
    public string Id { get; set; }
    public List<StartGGEvent> Events { get; set; }
}

public class StartGGEventResponse
{
    public StartGGEvent Event { get; set; }
}

public class StartGGEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<StartGGEventPhase> Phases { get; set; }
    public StartGGSetConnection Sets { get; set; }
}

public class StartGGSetConnection
{
    public List<StartGGSet> Nodes { get; set; }
}

public class StartGGSet
{
    public string Id { get; set; }
    public string WinnerId { get; set; }
    public int Round { get; set; }

    public List<StartGGSetSlot> Slots { get; set; }
}

public class StartGGSetSlot
{
    public StartGGSetSeed Seed { get; set; }

    public StartGGEntrant Entrant { get; set; }
}

public class StartGGEntrant
{
    public string Name { get; set; }
    public string Id { get; set; }
    public List<StartGGEntrantSeed> Seeds { get; set; }
}

public class StartGGSetSeed
{
    public string Id { get; set; }
}

public class StartGGEntrantSeed
{
    public int SeedNum { get; set; }
}

public class StartGGEventPhase
{
    public string Name { get; set; }
    public string Id { get; set; }
}