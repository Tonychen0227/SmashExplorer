using System;
using System.Collections.Generic;
using System.Timers;

public class CacheManager
{
    private static readonly Lazy<CacheManager> lazy = new Lazy<CacheManager>(() => new CacheManager());
    private readonly Timer _cleanupTimer;
    private static readonly string _defaultKey = string.Empty;

    private readonly Cache<Dictionary<string, ReportScoreAPIRequestBody>> ReportedSetsCache = new Cache<Dictionary<string, ReportScoreAPIRequestBody>>(6000);
    private readonly Cache<Dictionary<string, (string Name, int Seeding)>> CachedEntrantSeedingCache = new Cache<Dictionary<string, (string Name, int Seeding)>>(86400);
    private readonly Cache<List<Event>> UpcomingEventsCache = new Cache<List<Event>>(60);
    private readonly Cache<List<Tournament>> CurrentTournamentsCache = new Cache<List<Tournament>>(600);
    private readonly Cache<List<Set>> SetsCache = new Cache<List<Set>>(30);
    private readonly Cache<List<Upset>> UpsetsCache = new Cache<List<Upset>>(120);
    private readonly Cache<List<Entrant>> EventEntrantsCache = new Cache<List<Entrant>>(60);
    private readonly Cache<Entrant> EntrantsCache = new Cache<Entrant>(60);
    private readonly Cache<Event> EventsCache = new Cache<Event>(60);
    private readonly Cache<ExploreModel> FullEventsCache = new Cache<ExploreModel>(30);
    private readonly Cache<StartGGTournamentResponse> TournamentEventsCache = new Cache<StartGGTournamentResponse>(600);
    private readonly Cache<Dictionary<string, (string Name, List<Image>)>> TournamentAvatarsCache = new Cache<Dictionary<string, (string Name, List<Image>)>>(86400);

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

    public List<Set> GetEventSets(string eventId)
    {
        if (SetsCache.ContainsKey(eventId))
        {
            return SetsCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void SetEventSets(string eventId, List<Set> eventSets, long? overrideTTLSeconds)
    {
        SetsCache.SetCacheObject(eventId, eventSets, overrideTTLSeconds);
    }

    public List<Upset> GetEventUpsets(string eventId)
    {
        if (UpsetsCache.ContainsKey(eventId))
        {
            return UpsetsCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void SetEventUpsets(string eventId, List<Upset> eventUpsets, long? overrideTTLSeconds)
    {
        UpsetsCache.SetCacheObject(eventId, eventUpsets, overrideTTLSeconds);
    }

    public List<Entrant> GetEventEntrants(string eventId)
    {
        if (EventEntrantsCache.ContainsKey(eventId))
        {
            return EventEntrantsCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void SetEventEntrants(string eventId, List<Entrant> eventEntrants, long? overrideTTLSeconds)
    {
        EventEntrantsCache.SetCacheObject(eventId, eventEntrants, overrideTTLSeconds);
    }

    public Entrant GetEntrant(string entrantId)
    {
        if (EntrantsCache.ContainsKey(entrantId))
        {
            return EntrantsCache.GetFromCache(entrantId);
        }
        else
        {
            return null;
        }
    }

    public void SetEntrant(string entrantId, Entrant entrant)
    {
        EntrantsCache.SetCacheObject(entrantId, entrant);
    }

    public Event GetEvent(string eventId)
    {
        if (EventsCache.ContainsKey(eventId))
        {
            return EventsCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void SetEvent(string eventId, Event eventObject)
    {
        EventsCache.SetCacheObject(eventId, eventObject);
    }

    public ExploreModel GetFullEvent(string eventId)
    {
        if (FullEventsCache.ContainsKey(eventId))
        {
            return FullEventsCache.GetFromCache(eventId);
        }
        else
        {
            return null;
        }
    }

    public void SetFullEvent(string eventId, ExploreModel fullEvent)
    {
        FullEventsCache.SetCacheObject(eventId, fullEvent);
    }

    public StartGGTournamentResponse GetTournamentEvents(string tournamentId)
    {
        if (TournamentEventsCache.ContainsKey(tournamentId))
        {
            return TournamentEventsCache.GetFromCache(tournamentId);
        }
        else
        {
            return null;
        }
    }

    public void SetTournamentEvents(string tournamentId, StartGGTournamentResponse fullEvent)
    {
        TournamentEventsCache.SetCacheObject(tournamentId, fullEvent);
    }

    public Dictionary<string, (string Name, List<Image>)> GetTournamentAvatars(string tournamentId)
    {
        if (TournamentAvatarsCache.ContainsKey(tournamentId))
        {
            return TournamentAvatarsCache.GetFromCache(tournamentId);
        }
        else
        {
            return null;
        }
    }

    public void SetTournamentEvents(string tournamentId, Dictionary<string, (string Name, List<Image>)> tournamentAvatars)
    {
        TournamentAvatarsCache.SetCacheObject(tournamentId, tournamentAvatars);
    }

    private void CleanupCaches(object sender, ElapsedEventArgs e)
    {
        ReportedSetsCache.CleanupCache();
        CachedEntrantSeedingCache.CleanupCache();
        UpcomingEventsCache.CleanupCache();
        CurrentTournamentsCache.CleanupCache();
        SetsCache.CleanupCache();
        UpsetsCache.CleanupCache();
        EventEntrantsCache.CleanupCache();
        EntrantsCache.CleanupCache();
        EventsCache.CleanupCache();
        FullEventsCache.CleanupCache();
        TournamentEventsCache.CleanupCache();
        TournamentAvatarsCache.CleanupCache();
    }

    public void InvalidateCaches(string eventId, string tournamentId)
    {
        ReportedSetsCache.InvalidateCache(eventId);
        CachedEntrantSeedingCache.InvalidateCache(eventId);
        UpcomingEventsCache.InvalidateCache(_defaultKey);
        CurrentTournamentsCache.InvalidateCache(_defaultKey);
        SetsCache.InvalidateCache(eventId);
        UpsetsCache.InvalidateCache(eventId);
        EventEntrantsCache.InvalidateCache(eventId);
        //EntrantsCache.InvalidateCache we almost never have to invalidate this
        EventsCache.InvalidateCache(eventId);
        FullEventsCache.InvalidateCache(eventId);
        TournamentAvatarsCache.InvalidateCache(tournamentId);
        //TournamentEventsCache.InvalidateCache we almost never have to invalidate this
    }

    public void InvalidateSetsAndShortenTTL(string eventId, long shortenTTLDurationSeconds)
    {
        UpsetsCache.InvalidateCacheAndOverrideTTL(eventId, shortenTTLDurationSeconds);
        SetsCache.InvalidateCacheAndOverrideTTL(eventId, shortenTTLDurationSeconds);
    }
}