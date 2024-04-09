using Newtonsoft.Json;
using System.Collections.Generic;

public class StartGGGalintTournamentResponse
{
    public BackendTournament Tournament { get; set; }
}

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
    public string Slug { get; set; }
    public long StartAt { get; set; }
    public int NumEntrants { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<Upset> TournamentUpsets { get; set; }
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

public class StartGGVideogameResponse
{
    public StartGGVideogame Videogame { get; set; }
}

public class StartGGVideogame
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<StartGGCharacter> Characters { get; set; }
    public List<StartGGStage> Stages { get; set; }
}

public class StartGGCharacter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<StartGGImage> Images { get; set; }
}

public class StartGGStage
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class StartGGImage
{
    public string Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public double Ratio { get; set; }
    public string Type { get; set; }
}

public class StartGGUserResponse
{
    public StartGGUser CurrentUser { get; set; }
}

public class StartGGSetResponse
{
    public StartGGAPISet Set { get; set; }
}

public class StartGGAPISet
{
    public int? Id { get; set; }
    public int? WinnerId { get; set; }
    public string DisplayScore { get; set; }
    public List<StartGGAPISetSlot> Slots { get; set; }
}

public class StartGGAPISetSlot
{
    public StartGGAPIEntrant Entrant { get; set; }
}

public class StartGGAPIEntrant
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class StartGGUser
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string GenderPronoun { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Email { get; set; }
    public string Discriminator { get; set; }
    [JsonProperty("token")]
    public string Token { get; set; }
    public StartGGUserTournamentsConnection Tournaments { get; set; }
}

public class StartGGUserTournamentsConnection
{
    public List<StartGGUserTournament> Nodes { get; set; } 
}

public class StartGGUserTournament
{
    public string Id { get; set; }
    public string Name { get; set; }
}