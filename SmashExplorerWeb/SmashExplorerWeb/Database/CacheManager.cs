using System;
using System.Collections.Generic;
using System.Timers;

public class CacheManager
{
    private static readonly Lazy<CacheManager> lazy = new Lazy<CacheManager>(() => new CacheManager());
    private readonly Timer _cleanupTimer;
    private static readonly string _defaultKey = string.Empty;

    private readonly Cache<string, Dictionary<string, ReportScoreAPIRequestBody>> ReportedSetsCache = new Cache<string, Dictionary<string, ReportScoreAPIRequestBody>>(6000);
    private readonly Cache<string, Dictionary<string, (string Name, int Seeding)>> CachedEntrantSeedingCache = new Cache<string, Dictionary<string, (string Name, int Seeding)>>(86400);
    private readonly Cache<string, List<Event>> UpcomingEventsCache = new Cache<string, List<Event>>(60);
    private readonly Cache<string, List<Tournament>> CurrentTournamentsCache = new Cache<string, List<Tournament>>(600);

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

    public void AddEventReportedSet(string eventId, string setId, ReportScoreAPIRequestBody reportedSet)
    {
        ReportedSetsCache.AddToCacheObject(eventId, (Dictionary<string, ReportScoreAPIRequestBody> cachedObject) =>
        {
            cachedObject[setId] = reportedSet;
        });
    }

    public List<Event> GetUpcomingEvents()
    {
        if (UpcomingEventsCache.ContainsKey(_defaultKey))
        {
            return UpcomingEventsCache.GetFromCache(_defaultKey);
        }
        else
        {
            return null;
        }
    }

    public void SetUpcomingEvents(List<Event> events)
    {
        UpcomingEventsCache.SetCacheObject(_defaultKey, events);
    }

    public Dictionary<string, (string Name, int Seeding)> GetDanessEntrantSeeding(string eventId)
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

    public void SetDanessEntrantSeeding(string eventId, Dictionary<string, (string Name, int Seeding)> cachedSeeding)
    {
        CachedEntrantSeedingCache.SetCacheObject(eventId, cachedSeeding);
    }

    public List<Tournament> GetCurrentTournaments()
    {
        if (CurrentTournamentsCache.ContainsKey(_defaultKey))
        {
            return CurrentTournamentsCache.GetFromCache(_defaultKey);
        }
        else
        {
            return null;
        }
    }

    public void SetCurrentTournaments(List<Tournament> currentTournaments)
    {
        CurrentTournamentsCache.SetCacheObject(_defaultKey, currentTournaments);
    }

    private void CleanupCaches(object sender, ElapsedEventArgs e)
    {
        ReportedSetsCache.CleanupCache();
        CachedEntrantSeedingCache.CleanupCache();
        UpcomingEventsCache.CleanupCache();
        CurrentTournamentsCache.CleanupCache();
    }

    public void InvalidateCaches(string eventId)
    {
        ReportedSetsCache.InvalidateCache(eventId);
        CachedEntrantSeedingCache.InvalidateCache(eventId);
        UpcomingEventsCache.InvalidateCache(_defaultKey);
        CurrentTournamentsCache.InvalidateCache(_defaultKey);
    }
}