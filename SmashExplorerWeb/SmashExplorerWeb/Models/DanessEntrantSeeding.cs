using Newtonsoft.Json;
using System.Collections.Generic;

public class DanessEntrantSeeding
{
    [JsonProperty("id")]
    public string Id { get; set; } // eventId
    [JsonProperty("cachedEntrantSeeding")]
    public Dictionary<string, (string Name, int Seeding)> CachedEntrantSeeding { get; set; }
}