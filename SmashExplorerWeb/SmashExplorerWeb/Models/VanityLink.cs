using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class VanityLink
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> EntrantIds { get; set; }
    [JsonProperty("eventId")]
    public string EventId { get; set; }

    public VanityLink(string eventId, string name, List<string> entrantIds)
    {
        EventId = eventId;
        Name = name;
        EntrantIds = entrantIds;
        Id = Guid.NewGuid().ToString().Replace("-", "");
    }
}