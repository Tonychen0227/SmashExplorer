﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

public class MetricsManager
{
    private static readonly Lazy<MetricsManager> lazy = new Lazy<MetricsManager>(() => new MetricsManager());
    private readonly Timer _emitTimer;

    private List<SmashExplorerMetricsModel> _metricsModels;

    private SmashExplorerMetricsModel CurrentModel => _metricsModels.Last();

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

    public void AddReportedSet(string eventId)
    {
        lock (_lock)
        {
            if (!CurrentModel.SetsReported.ContainsKey(eventId))
            {
                CurrentModel.SetsReported[eventId] = 0;
            }
            
            CurrentModel.SetsReported[eventId]++;
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