using System.Collections.Generic;

public class ExploreModel
{
    public Event Event { get; set; }
    public Dictionary<Entrant, List<Set>> EntrantsSets { get; set; }
}