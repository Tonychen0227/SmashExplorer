using System;
using System.Collections.Generic;
using System.Timers;

public class CacheManager
{
    private static readonly Lazy<CacheManager> lazy = new Lazy<CacheManager>(() => new CacheManager());
    private readonly Timer _cleanupTimer;

    private readonly Cache<string, Dictionary<string, ReportScoreAPIRequestBody>> ReportedSetsCache = new Cache<string, Dictionary<string, ReportScoreAPIRequestBody>>(6000);
    private readonly Cache<string, Dictionary<string, (string Name, int Seeding)>> CachedEntrantSeedingCache = new Cache<string, Dictionary<string, (string Name, int Seeding)>>(86400);
    private readonly Cache<string, List<Event>> UpcomingEventsCache = new Cache<string, List<Event>>(60);

    private CacheManager() {
        _cleanupTimer = new Timer(TimeSpan.FromMinutes(2).TotalMilliseconds)
        {
            Enabled = true,
            AutoReset = true
        };

        _cleanupTimer.Elapsed += CleanupCaches;
        _cleanupTimer.Start();
    }

    public static CacheManager Instance { get { return lazy.Value; } }

    public Dictionary<string, ReportScoreAPIRequestBody> GetEventReportedSets(string eventId)
    {
        return ReportedSetsCache.ContainsKey(eventId) ? ReportedSetsCache.GetFromCache(eventId) : null;
    }

    public void AddReportedSetToCache(string eventId, string setId, ReportScoreAPIRequestBody reportedSet)
    {
        ReportedSetsCache.AddToCacheObject(eventId, (Dictionary<string, ReportScoreAPIRequestBody> cachedObject) =>
        {
            cachedObject[setId] = reportedSet;
        });
    }

    public List<Event> GetCachedUpcomingEvents()
    {
        var key = string.Empty;

        if (UpcomingEventsCache.ContainsKey(key))
        {
            return UpcomingEventsCache.GetFromCache(key);
        }
        else
        {
            return null;
        }
    }

    public void SetUpcomingEvents(List<Event> events)
    {
        UpcomingEventsCache.SetCacheObject(string.Empty, events);
    }

    public Dictionary<string, (string Name, int Seeding)> GetCachedEntrantSeeding(string eventId)
    {
        if (CachedEntrantSeedingCache.ContainsKey(eventId))
        {
            return CachedEntrantSeedingCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void UpdateCachedEntrantSeeding(string eventId, Dictionary<string, (string Name, int Seeding)> cachedSeeding)
    {
        CachedEntrantSeedingCache.SetCacheObject(eventId, cachedSeeding);
    }

    private void CleanupCaches(object sender, ElapsedEventArgs e)
    {
        ReportedSetsCache.CleanupCache();
        CachedEntrantSeedingCache.CleanupCache();
        UpcomingEventsCache.CleanupCache();
    }
}