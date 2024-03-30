using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

public class MetricsManager
{
    private static readonly Lazy<MetricsManager> lazy = new Lazy<MetricsManager>(() => new MetricsManager());
    private readonly Timer _emitTimer;

    private List<SmashExplorerMetricsModel> _metricsModels;

    public SmashExplorerMetricsModel CurrentModel => _metricsModels.Last();

    private readonly object _lock = new object();

    private MetricsManager()
    {
        _emitTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds)
        {
            Enabled = true,
            AutoReset = true
        };

        _metricsModels = new List<SmashExplorerMetricsModel>() { new SmashExplorerMetricsModel() };

        _emitTimer.Elapsed += FireMetricsAndDoCleanup;
        _emitTimer.Start();
    }

    public static MetricsManager Instance { get { return lazy.Value; } }

    public void AddStartReportSet(string eventId, string hashedToken)
    {
        lock (_lock)
        {
            if (!CurrentModel.SetsReported.ContainsKey(eventId))
            {
                CurrentModel.SetsReported[eventId] = new Dictionary<string, SetReportModel>();
            }

            if (!CurrentModel.SetsReported[eventId].ContainsKey(hashedToken))
            {
                CurrentModel.SetsReported[eventId][hashedToken] = new SetReportModel();
            }

            CurrentModel.SetsReported[eventId][hashedToken].IncrementStarted();
        }
    }

    public void AddSuccessReportSet(string eventId, string hashedToken)
    {
        lock (_lock)
        {
            if (!CurrentModel.SetsReported.ContainsKey(eventId))
            {
                CurrentModel.SetsReported[eventId] = new Dictionary<string, SetReportModel>();
            }

            if (!CurrentModel.SetsReported[eventId].ContainsKey(hashedToken))
            {
                CurrentModel.SetsReported[eventId][hashedToken] = new SetReportModel();
            }

            CurrentModel.SetsReported[eventId][hashedToken].IncrementCompleted();
        }
    }

    public void AddFailReportSet(string eventId, string setId, string reason)
    {
        lock (_lock)
        {
            if (!CurrentModel.SetsFailed.ContainsKey(eventId))
            {
                CurrentModel.SetsFailed[eventId] = new Dictionary<string, string>();
            }

            CurrentModel.SetsFailed[eventId][setId] = reason;
        }
    }

    public void AddCorrelatedSet((string, string) item)
    {
        lock (_lock)
        {
            if (!CurrentModel.SetsCorrelated.Contains(item))
            {
                CurrentModel.SetsCorrelated.Add(item);
            }
        }
    }

    public void AddLogin(string hashedToken)
    {
        lock (_lock)
        {
            if (!CurrentModel.Logins.ContainsKey(hashedToken))
            {
                CurrentModel.Logins[hashedToken] = 0;
            }

            CurrentModel.Logins[hashedToken]++;
        }
    }

    public void AddTournamentAPIVisit(string tournamentId, string userSlug)
    {
        lock (_lock)
        {
            if (!CurrentModel.TournamentAPIVisits.ContainsKey(tournamentId))
            {
                CurrentModel.TournamentAPIVisits[tournamentId] = new Dictionary<string, int>();
            }

            userSlug = userSlug ?? string.Empty;

            if (!CurrentModel.TournamentAPIVisits[tournamentId].ContainsKey(userSlug))
            {
                CurrentModel.TournamentAPIVisits[tournamentId].Add(userSlug, 0);
            }

            CurrentModel.TournamentAPIVisits[tournamentId][userSlug]++;
        }
    }

    public void AddPageView(string pageName, string subKey)
    {
        lock (_lock)
        {
            if (!CurrentModel.PageViews.ContainsKey(pageName))
            {
                CurrentModel.PageViews[pageName] = new Dictionary<string, int>();
            }

            subKey = subKey ?? string.Empty;

            if (!CurrentModel.PageViews[pageName].ContainsKey(subKey))
            {
                CurrentModel.PageViews[pageName].Add(subKey, 0);
            }

            CurrentModel.PageViews[pageName][subKey]++;
        }
    }

    private async void FireMetricsAndDoCleanup(object sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            _metricsModels.Add(new SmashExplorerMetricsModel());
        }

        var oldModel = _metricsModels.First();
        oldModel.EndTime = DateTimeOffset.UtcNow;
        await SmashExplorerDatabase.Instance.UpsertMetricsAsync(oldModel);

        lock (_lock)
        {
            _metricsModels.RemoveAt(0);
        }
    }
}