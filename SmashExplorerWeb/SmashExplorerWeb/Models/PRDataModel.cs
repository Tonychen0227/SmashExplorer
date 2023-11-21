using System.Collections.Generic;

public class PRDataModel
{
    public string Title { get; set; }
    public IEnumerable<RankingEvent> RankingEvents { get; set; }

    public Dictionary<string, Dictionary<string, HeadToHead>> HeadToHead { get; set; }
    public Dictionary<string, List<PlayerEventPerformance>> PlayerEventPerformances { get; set; }
}