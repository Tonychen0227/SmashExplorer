using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

public class CacheDatabase
{
    private static readonly Lazy<CacheDatabase> lazy = new Lazy<CacheDatabase>(() => new CacheDatabase());

    private Timer _cleanupTimer;

    private Dictionary<string, Dictionary<string, (ReportScoreAPIRequestBody, DateTime)>> ReportedSetsCache = new Dictionary<string, Dictionary<string, (ReportScoreAPIRequestBody, DateTime)>>();
    private static readonly int ReportedSetsCacheTTLSeconds = 6000;
    private readonly object _reportedSetsCacheLock = new object();


    private CacheDatabase() {
        _cleanupTimer = new Timer(TimeSpan.FromMinutes(2).TotalMilliseconds)
        {
            Enabled = true,
            AutoReset = true
        };

        _cleanupTimer.Elapsed += CleanupCaches;
        _cleanupTimer.Start();
    }

    public static CacheDatabase Instance { get { return lazy.Value; } }

    public Dictionary<string, (ReportScoreAPIRequestBody, DateTime)> GetEventReportedSets(string eventId)
    {
        lock (_reportedSetsCacheLock)
        {
            return ReportedSetsCache.ContainsKey(eventId) ? ReportedSetsCache[eventId] : null;
        }
    }

    public void AddReportedSetToCache(string eventId, string setId, ReportScoreAPIRequestBody reportedSet)
    {
        var expiry = DateTime.UtcNow.AddSeconds(ReportedSetsCacheTTLSeconds);

        lock (_reportedSetsCacheLock)
        {
            if (!ReportedSetsCache.ContainsKey(eventId))
            {
                ReportedSetsCache.Add(eventId, new Dictionary<string, (ReportScoreAPIRequestBody, DateTime)>());
            }

            var eventCache = ReportedSetsCache[eventId];

            eventCache[setId] = (reportedSet, expiry);
        }
    }

    private void CleanupCaches(object sender, ElapsedEventArgs e)
    {
        lock (_reportedSetsCacheLock)
        {
            foreach (var eventId in ReportedSetsCache.Keys)
            {
                var eventReportedSetsCache = ReportedSetsCache[eventId];

                foreach (var reportedSetId in eventReportedSetsCache.Keys)
                {
                    var reportedSet = eventReportedSetsCache[reportedSetId];

                    if (reportedSet.Item2 < DateTime.UtcNow)
                    {
                        ReportedSetsCache.Remove(reportedSetId);
                    }
                }

                if (ReportedSetsCache[eventId].Keys.Count == 0)
                {
                    ReportedSetsCache.Remove(eventId);
                }
            }
        }
    }
}