using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

public class SmashExplorerDatabase
{
    private readonly CosmosClient Client;
    private readonly Container EntrantsContainer;
    private readonly Container SetsContainer;
    private readonly Container VanityLinksContainer;
    private readonly Container CurrentTournamentsContainer;
    private readonly Container EventsContainer;
    private readonly Container ScoreboardsContainer;
    private readonly Container DanessSeedingContainer;
    private readonly Container PRDataContainer;
    private readonly Container GalintAuthenticationContainer;

    public static Dictionary<int, int> PlacementToRounds;

    private static readonly Lazy<SmashExplorerDatabase> lazy = new Lazy<SmashExplorerDatabase>(() => new SmashExplorerDatabase());

    private static readonly List<int> BannedOwners = new List<int>() { 1819468 };

    private Container GetContainer(string containerName)
    {
        var dbName = "smash-explorer-database";

        return Client.GetContainer(dbName, containerName);
    }

    private SmashExplorerDatabase()
    {
        var url = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT"); 
        var key = Environment.GetEnvironmentVariable("COSMOS_KEY");

        Client = new CosmosClient(url, key);

        EntrantsContainer = GetContainer("Entrants");
        SetsContainer = GetContainer("Sets");
        VanityLinksContainer = GetContainer("VanityLinks");
        CurrentTournamentsContainer = GetContainer("CurrentTournaments");
        EventsContainer = GetContainer("Events");
        ScoreboardsContainer = GetContainer("Scoreboards");
        DanessSeedingContainer = GetContainer("DanessSeedingContainer");
        PRDataContainer = GetContainer("PRData");
        GalintAuthenticationContainer = GetContainer("GalintAuthenticationTokens");

        PlacementToRounds = new Dictionary<int, int>();

        var keyPlacements = new List<int>() { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193 };

        for (var index = 0; index < keyPlacements.Count - 1; index++)
        {
            var nextKeyPlacement = keyPlacements[index + 1];

            var currentPlacement = keyPlacements[index];

            for (var placement = currentPlacement; placement < nextKeyPlacement; placement++)
            {
                PlacementToRounds[placement] = index;
            }
        }
    }

    public Dictionary<string, ReportScoreAPIRequestBody> GetEventReportedSets(string eventId)
    {
        return CacheManager.Instance.GetEventReportedSets(eventId);
    }

    public static SmashExplorerDatabase Instance { get { return lazy.Value; } }

    public async Task<StartGGUser> GetGalintAuthenticatedUserAsync(string token)
    {
        var results = new List<StartGGUser>();

        using (var iterator = GalintAuthenticationContainer.GetItemQueryIterator<StartGGUser>($"select * from t where t.token = \"{token}\"",
                                                                              requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(token) }))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }
        
        return results.FirstOrDefault();
    }

    public async Task UpsertGalintAuthenticationTokenAsync(StartGGUser user)
    {
        try
        {
            await GalintAuthenticationContainer.UpsertItemAsync(user, new PartitionKey(user.Token));
        }
        catch (Exception ce)
        {
            return;
        }

        return;
    }

    public async Task<List<Event>> GetUpcomingEventsAsync()
    {
        var cached = CacheManager.Instance.GetUpcomingEvents();

        if (cached != null)
        {
            return cached;
        }

        var results = new List<Event>();

        int secondsSinceEpoch = (int) (DateTime.UtcNow.AddDays(-5) - new DateTime(1970, 1, 1)).TotalSeconds;
        using (var iterator = EventsContainer.GetItemQueryIterator<Event>($"SELECT * FROM c WHERE c.startAt > {secondsSinceEpoch} ORDER BY c.numEntrants DESC OFFSET 0 LIMIT 10"))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        CacheManager.Instance.SetUpcomingEvents(results);

        return results.Where(x => x.TournamentOwner?.Id == null || !BannedOwners.Contains(x.TournamentOwner.Id)).ToList();
    }

    public async Task<List<Tournament>> GetCurrentTournamentsAsync()
    {
        var cached = CacheManager.Instance.GetCurrentTournaments();

        if (cached != null)
        {
            return cached;
        }

        var results = new List<Tournament>();

        using (var iterator = CurrentTournamentsContainer.GetItemQueryIterator<Tournament>($"SELECT * FROM c WHERE c.isActive OFFSET 0 LIMIT 10"))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        CacheManager.Instance.SetCurrentTournaments(results);

        return results.ToList();
    }

    public async Task<Event> GetEventAsync(string eventId, bool useLongerCache = false)
    {
        var cached = CacheManager.Instance.GetEvent(eventId);
        if (cached != null)
        {
            return cached;
        }

        Event result;
        try
        {
            result = await EventsContainer.ReadItemAsync<Event>(eventId, new PartitionKey(eventId));
        } catch (CosmosException ce)
        {
            return null;
        }

        if (result.TournamentOwner?.Id != null && BannedOwners.Contains(result.TournamentOwner.Id))
        {
            result = null;
        }

        CacheManager.Instance.SetEvent(eventId, result);

        return result;
    }

    public async Task<Dictionary<string, (string Name, int Seeding)>> GetCachedEntrantSeeding(string eventId)
    {
        var cached = CacheManager.Instance.GetDanessEntrantSeeding(eventId);

        if (cached != null)
        {
            return cached;
        }

        DanessEntrantSeeding result;
        try
        {
            result = await DanessSeedingContainer.ReadItemAsync<DanessEntrantSeeding>(eventId, new PartitionKey(eventId));
        }
        catch (CosmosException)
        {
            return null;
        }

        CacheManager.Instance.SetDanessEntrantSeeding(eventId, result.CachedEntrantSeeding);

        return result.CachedEntrantSeeding;
    }

    public async Task<Dictionary<string, (string Name, int Seeding)>> UpsertDanessSeeding(string eventId, Dictionary<string, (string Name, int Seeding)> cachedEntrantSeeding)
    {
        DanessEntrantSeeding result;
        try
        {
            var danessSeeding = new DanessEntrantSeeding()
            {
                Id = eventId,
                CachedEntrantSeeding = cachedEntrantSeeding
            };

            result = await DanessSeedingContainer.CreateItemAsync(danessSeeding, new PartitionKey(eventId));
        }
        catch (CosmosException)
        {
            return cachedEntrantSeeding;
        }

        CacheManager.Instance.SetDanessEntrantSeeding(eventId, cachedEntrantSeeding);

        return cachedEntrantSeeding;
    }

    public async Task<Event> GetEventBySlugAndDatesAsync(string slug, DateTime startAt, DateTime endAt)
    {
        return (await GetEventsBySlugAndDatesAsync(slug, startAt, endAt)).FirstOrDefault();
    }

    public async Task<List<Event>> GetEventsBySlugAndDatesAsync(string slug, DateTime startAt, DateTime endAt)
    {
        var results = new List<Event>();

        int startAtEpoch = (int) (startAt - new DateTime(1970, 1, 1)).TotalSeconds;
        int endAtEpoch = (int) (endAt - new DateTime(1970, 1, 1)).TotalSeconds;

        using (var iterator = EventsContainer.GetItemQueryIterator<Event>($"SELECT * FROM c WHERE CONTAINS(c.slug, \"{slug}\") " +
                                                                          $"AND c.startAt > {startAtEpoch} AND c.startAt < {endAtEpoch} " +
                                                                          $"ORDER BY c.numEntrants DESC OFFSET 0 LIMIT 50"))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        return results.Where(x => x.TournamentOwner?.Id == null || !BannedOwners.Contains(x.TournamentOwner.Id)).ToList();
    }

    public async Task<Entrant> GetEntrantBySlugAndEventAsync(string slug, string eventId)
    {
        var matchingEntrants = new List<Entrant>();
        var options = new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey(eventId)
        };

        using (var iterator = EntrantsContainer.GetItemQueryIterator<Entrant>($"SELECT * FROM c WHERE ARRAY_CONTAINS(c.userSlugs, \"{slug}\")", requestOptions: options))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                matchingEntrants.AddRange(next.Resource);
            }
        }

        return matchingEntrants.FirstOrDefault();
    }

    public async Task<List<Entrant>> GetDQdEntrantsAsync(string eventId)
    {
        var entrants = await GetEntrantsAsync(eventId);

        return entrants.Where(x => x.IsDisqualified == true).ToList();
    }

    public async Task<Entrant> GetEntrantAsync(string entrantId)
    {
        var cached = CacheManager.Instance.GetEntrant(entrantId);
        if (cached != null)
        {
            return cached;
        }

        var entrantsList = new List<Entrant>();
        using (var iterator = EntrantsContainer.GetItemQueryIterator<Entrant>($"SELECT * FROM c WHERE c.id = \"{entrantId}\""))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                entrantsList.AddRange(next.Resource);
            }
        }

        var retEntrant = entrantsList.FirstOrDefault();

        CacheManager.Instance.SetEntrant(entrantId, retEntrant);

        return retEntrant;
    }

    public async Task<List<Entrant>> GetEntrantsAsync(string eventId, bool useLongerCache = false)
    {
        var cached = CacheManager.Instance.GetEventEntrants(eventId);
        if (cached != null)
        {
            return cached;
        }

        List<Entrant> results = new List<Entrant>();

        using (var iterator = EntrantsContainer.GetItemQueryIterator<Entrant>($"select * from t where t.eventId = \"{eventId}\"",
                                                                              requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(eventId) }))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        List<Entrant> resultsList = results.OrderBy(x => x.Seeding == null).ThenBy(x => x.Seeding).ToList();

        long? ttl = (eventId == "864717" || useLongerCache) ? (long?)86400 : null;
        CacheManager.Instance.SetEventEntrants(eventId, results, ttl);

        return resultsList;
    }

    public async Task<Set> GetSetAsync(string setId)
    {
        var results = new List<Set>();

        using (var iterator = SetsContainer.GetItemQueryIterator<Set>($"select * from t where t.id = \"{setId}\""))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        return results.FirstOrDefault();
    }

    public async Task<List<Set>> GetSetsAsync(string eventId, bool useLongerCache = false)
    {
        var cached = CacheManager.Instance.GetEventSets(eventId);
        if (cached != null)
        {
            return cached;
        }

        var results = new List<Set>();

        using (var iterator = SetsContainer.GetItemQueryIterator<Set>($"select * from t where t.eventId = \"{eventId}\"",
                                                                      requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(eventId) }))
        {
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                results.AddRange(next.Resource);
            }
        }

        long? ttl = (eventId == "864717" || useLongerCache) ? (long?) 86400 : null;
        CacheManager.Instance.SetEventSets(eventId, results, ttl);

        return results;
    }

    public async Task<VanityLink> CreateVanityLinkAsync(string eventId, string name, List<string> entrantIds)
    {
        var newVanityLink = new VanityLink(eventId, name, entrantIds);
        string generatedId = newVanityLink.Id;
        var currentLinkSize = 8;
        newVanityLink.Id = generatedId.Substring(0, currentLinkSize);

        while (int.TryParse(newVanityLink.Id, out var foo))
        {
            currentLinkSize++;
            newVanityLink.Id = generatedId.Substring(0, currentLinkSize);
        }

        while (true)
        {
            try
            {
                newVanityLink.Id = generatedId.Substring(0, currentLinkSize);
                return await VanityLinksContainer.CreateItemAsync(newVanityLink, new PartitionKey(newVanityLink.Id));
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.Conflict)
                {
                    throw ex;
                }

                currentLinkSize++;
            }
        };
    }

    public async Task<VanityLink> GetVanityLinkAsync(string id)
    {
        try
        {
            return await VanityLinksContainer.ReadItemAsync<VanityLink>(id, new PartitionKey(id));
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw ex;
            }

            return null;
        }
    }

    public async Task<IEnumerable<Upset>> GetUpsetsAndNotableAsync(string eventId)
    {
        var cached = CacheManager.Instance.GetEventUpsets(eventId);
        if (cached != null)
        {
            return cached;
        }

        var results = await GetSetsAsync(eventId);

        var ret = results.Where(x => x.IsUpsetOrNotable).Select(set => MapToUpset(set));

        long? ttl = eventId == "864717" ? (long?)86400 : null;
        CacheManager.Instance.SetEventUpsets(eventId, ret.ToList(), ttl);

        return ret;
    }

    public async Task<Scoreboard> GetScoreboardAsync(string id)
    {
        try
        {
            return await ScoreboardsContainer.ReadItemAsync<Scoreboard>(id, new PartitionKey(id));
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw ex;
            }

            return null;
        }
    }

    public async Task<Scoreboard> CreateScoreboardAsync(CreateScoreboardModel model)
    {
        var newScoreboard = new Scoreboard(model.Player1Name, model.Player2Name, model.BestOf);

        string generatedId = newScoreboard.Id;
        var currentLinkSize = 8;

        while (true)
        {
            try
            {
                newScoreboard.Id = generatedId.Substring(0, currentLinkSize);
                return await ScoreboardsContainer.CreateItemAsync(newScoreboard, new PartitionKey(newScoreboard.Id));
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.Conflict)
                {
                    throw ex;
                }

                currentLinkSize++;
            }
        };
    }

    public async Task<Scoreboard> AddLogToScoreboardAsync(string id, ScoreboardLog log)
    {
        var scoreboard = await GetScoreboardAsync(id);
        var lastLog = scoreboard.ScoreboardLogs.LastOrDefault();

        scoreboard.ScoreboardLogs.Add(log);
        
        switch (log.ScoreboardAction)
        {
            case ScoreboardAction.AgreeGentleman:
                scoreboard.ScoreboardState = ScoreboardState.Gentlemaning;
                break;
            case ScoreboardAction.StarterPick:
            case ScoreboardAction.GentlemanPick:
            case ScoreboardAction.Pick:
                scoreboard.ScoreboardState = ScoreboardState.StageSelected;
                break;
            case ScoreboardAction.Ban:
                if (lastLog.ScoreboardAction == ScoreboardAction.Ban)
                {
                    scoreboard.ScoreboardState = ScoreboardState.Counterpicking;
                } else
                {
                    scoreboard.ScoreboardState = ScoreboardState.Banning;
                }
                break;
            case ScoreboardAction.Win:
                scoreboard.ScoreboardState = ScoreboardState.Banning;
                if (log.PlayerName == scoreboard.Player1Name)
                {
                    scoreboard.Player1Score += 1;
                } else
                {
                    scoreboard.Player2Score += 1;
                }

                if (Math.Max(scoreboard.Player1Score, scoreboard.Player2Score) > scoreboard.BestOf / 2)
                {
                    scoreboard.ScoreboardState = ScoreboardState.Completed;
                }
                break;
            default:
                break;
        }

        return await ScoreboardsContainer.UpsertItemAsync(scoreboard);
    }

    public string GetStringOrdinal(int? num)
    {
        if (num == null)
        {
            return "None";
        }

        if (num <= 0) return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1:
                return num + "st";
            case 2:
                return num + "nd";
            case 3:
                return num + "rd";
            default:
                return num + "th";
        }
    }

    private Upset MapToUpset(Set set)
    {
        Entrant winner = set.Entrants.Where(x => x.Id == set.WinnerId).Single();
        Entrant loser = set.Entrants.Where(x => x.Id != set.WinnerId).Single();

        int winnerRoundSeedingPlacement;
        PlacementToRounds.TryGetValue(winner.InitialSeedNum ?? -1, out winnerRoundSeedingPlacement);

        int loserRoundSeedingPlacement;
        PlacementToRounds.TryGetValue(loser.InitialSeedNum ?? -1, out loserRoundSeedingPlacement);

        int upsetFactor = Math.Abs(winnerRoundSeedingPlacement - loserRoundSeedingPlacement);

        if (set.DisplayScore == "DQ")
        {
            set.DetailedScore = new Dictionary<string, string>()
            {
                { winner.Id, "W" },
                { loser.Id, "DQ" }
            };
        }

        var display = set.DetailedScore == null ? ">" : $"{set.DetailedScore[winner.Id]}-{set.DetailedScore[loser.Id]}";

        var newDisplayScore = $"{winner.Name} ({winner.InitialSeedNum}) {display} " +
                              $"{loser.Name} ({loser.InitialSeedNum})";

        return new Upset()
        {
            Set = set,
            CompletedUpset = winnerRoundSeedingPlacement > loserRoundSeedingPlacement,
            UpsetFactor = upsetFactor,
            NewDisplayScore = newDisplayScore
        };
    }

    public async Task<PRDataSet> GetPRDataset(string id)
    {
        PRDataSet result;
        try
        {
            result = await PRDataContainer.ReadItemAsync<PRDataSet>(id, new PartitionKey(id));
        }
        catch (CosmosException ce)
        {
            return null;
        }

        return result;
    }

    public async Task UpsertPRDataSet(PRDataSet dataSet)
    {
        try
        {
            await PRDataContainer.UpsertItemAsync(dataSet, new PartitionKey(dataSet.Id));
        }
        catch (Exception ce)
        {
            return;
        }

        return;
    }
}