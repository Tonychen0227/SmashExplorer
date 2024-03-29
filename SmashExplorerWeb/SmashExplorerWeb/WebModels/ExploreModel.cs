﻿using System.Collections.Generic;

public class ExploreModel
{
    public VanityLink VanityLink { get; set; }
    public Event Event { get; set; }
    public Dictionary<Entrant, List<Set>> EntrantsSets { get; set; }
    public List<Entrant> DictKeys { get; set; }
    public List<Set> AllSets { get; set; }
}