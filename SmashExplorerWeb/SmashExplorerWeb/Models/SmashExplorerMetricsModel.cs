using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public class SmashExplorerMetricsModel
{
    [JsonProperty("id")]
    public string Id => BeginTime.ToUnixTimeMilliseconds().ToString();

    public Dictionary<string, Dictionary<string, SetReportModel>> SetsReported { get; set; }
        = new Dictionary<string, Dictionary<string, SetReportModel>>();

    public Dictionary<string, Dictionary<string, string>> SetsFailed { get; set; } = new Dictionary<string, Dictionary<string, string>>();
 
    public Dictionary<string, int> Logins { get; set; } = new Dictionary<string, int>();

    public Dictionary<string, Dictionary<string, int>> TournamentAPIVisits { get; set; } = new Dictionary<string, Dictionary<string, int>>();

    public Dictionary<string, Dictionary<string, int>> PageViews { get; set; } = new Dictionary<string, Dictionary<string, int>>();

    public DateTimeOffset BeginTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public SmashExplorerMetricsModel()
    {
        BeginTime = DateTimeOffset.UtcNow;
    }

    public SmashExplorerMetricsModel Consolidate(SmashExplorerMetricsModel additionalModel)
    {
        foreach (var key in additionalModel.SetsReported.Keys.ToList())
        {
            if (!SetsReported.ContainsKey(key))
            {
                SetsReported[key] = new Dictionary<string, SetReportModel>();
            }

            foreach (var subKey in additionalModel.SetsReported[key].Keys.ToList())
            {
                if (!SetsReported[key].ContainsKey(subKey))
                {
                    SetsReported[key][subKey] = new SetReportModel();
                }

                SetsReported[key][subKey] = new SetReportModel(
                    SetsReported[key][subKey].Started + additionalModel.SetsReported[key][subKey].Started,
                    SetsReported[key][subKey].Completed + additionalModel.SetsReported[key][subKey].Completed);
            }
        }

        foreach (var key in additionalModel.SetsFailed.Keys.ToList())
        {
            if (!SetsFailed.ContainsKey(key))
            {
                SetsFailed[key] = new Dictionary<string, string>();
            }

            foreach (var subKey in additionalModel.SetsFailed[key].Keys.ToList())
            {
                SetsFailed[key][subKey] = additionalModel.SetsFailed[key][subKey];
            }
        }

        foreach (var key in additionalModel.Logins.Keys.ToList())
        {
            if (!Logins.ContainsKey(key))
            {
                Logins[key] = 0;
            }

            Logins[key] = Logins[key] + additionalModel.Logins[key];
        }

        foreach (var key in additionalModel.TournamentAPIVisits.Keys.ToList())
        {
            if (!TournamentAPIVisits.ContainsKey(key))
            {
                TournamentAPIVisits[key] = new Dictionary<string, int>();
            }

            foreach (var subKey in additionalModel.TournamentAPIVisits[key].Keys.ToList())
            {
                if (!TournamentAPIVisits[key].ContainsKey(subKey))
                {
                    TournamentAPIVisits[key][subKey] = 0;
                }

                TournamentAPIVisits[key][subKey] = TournamentAPIVisits[key][subKey] + additionalModel.TournamentAPIVisits[key][subKey];
            }
        }

        foreach (var key in additionalModel.PageViews.Keys.ToList())
        {
            if (!PageViews.ContainsKey(key))
            {
                PageViews[key] = new Dictionary<string, int>();
            }

            foreach (var subKey in additionalModel.PageViews[key].Keys.ToList())
            {
                if (!PageViews[key].ContainsKey(subKey))
                {
                    PageViews[key][subKey] = 0;
                }

                PageViews[key][subKey] = PageViews[key][subKey] + additionalModel.PageViews[key][subKey];
            }
        }

        if (additionalModel.BeginTime < BeginTime)
        {
            BeginTime = additionalModel.BeginTime;
        }

        if (additionalModel.EndTime > EndTime)
        {
            EndTime = additionalModel.EndTime;
        }

        return this;
    }
}

public class SetReportModel
{
    public int Started { get; set; }
    public int Completed { get; set; }

    public SetReportModel()
    {
        Started = 0;
        Completed = 0;
    }

    public SetReportModel(int started, int completed)
    {
        Started = started;
        Completed = completed;
    }

    public void IncrementStarted()
    {
        Started++;
    }

    public void IncrementCompleted()
    {
        Completed++;
    }
}