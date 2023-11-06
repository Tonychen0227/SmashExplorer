using System.Collections.Generic;

public class DanessModel
{
    public StartGGEventResponse StartGGEventResponse { get; set; }
    public int LastCompletedSwissRound { get; set; }
    public Dictionary<int, List<(StartGGEntrant, StartGGEntrant)>> Pairings { get; set; }
    public Dictionary<string, (string Name, int Seeding)> CachedEntrantSeeding { get; set; }
} 