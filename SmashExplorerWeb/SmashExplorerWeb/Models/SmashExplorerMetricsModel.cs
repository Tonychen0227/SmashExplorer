using System;
using System.Collections.Generic;

public class SmashExplorerMetricsModel
{
    public string Id => BeginTime.ToUnixTimeMilliseconds().ToString();

    public Dictionary<string, int> SetsReported { get; set; } = new Dictionary<string, int>();

    public Dictionary<string, int> Logins { get; set; } = new Dictionary<string, int>();

    public Dictionary<string, Dictionary<string, int>> TournamentAPIVisits { get; set; } = new Dictionary<string, Dictionary<string, int>>();

    public Dictionary<string, Dictionary<string, int>> PageViews { get; set; } = new Dictionary<string, Dictionary<string, int>>();

    public DateTimeOffset BeginTime { get; }

    public DateTimeOffset EndTime { get; set; }

    public SmashExplorerMetricsModel()
    {
        BeginTime = DateTimeOffset.UtcNow;
    }
}