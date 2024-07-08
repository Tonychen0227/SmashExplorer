using Newtonsoft.Json;
using System.Collections.Generic;

public class BackendTournament
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public bool IsActive { get; set; }
    public long StartAt { get; set; }
    public string Name { get; set; }
    public bool ShowUpsets { get; set; } = true;
    public List<TournamentEvent> Events { get; set; }
    public List<Image> Images { get; set; }
    public string Slug { get; set; }
    public string Twitter { get; set; }
    public List<Stream> Streams { get; set; }
}

public class Tournament
{
    public string Id { get; set; }
    public long StartAt { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; }
    public List<TournamentEvent> Events { get; set; }
    public List<Image> Images { get; set; }
    public string Slug { get; set; }
    public string Twitter { get; set; }
    public List<Stream> Streams { get; set; }
}

public class TournamentEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool? ShowUpsets { get; set; }
    public long StartAt { get; set; }
    public int NumEntrants { get; set; }
    public List<Upset> TournamentUpsets { get; set; }
    public List<Image> Images { get; set; }
    public List<TournamentEventPhase> Phases { get; set; }
}

public class TournamentEventPhase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int BestOf { get; set; }
}