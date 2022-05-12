using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SmashExplorerDatabase
{
    private readonly CosmosClient Client;
    private readonly Container EntrantsContainer;
    private readonly Container SetsContainer;
    private readonly Container VanityLinksContainer;
    private readonly Container EventsContainer;

    public static Dictionary<int, int> PlacementToRounds;

    private static readonly Lazy<SmashExplorerDatabase> lazy = new Lazy<SmashExplorerDatabase>(() => new SmashExplorerDatabase());

    private Tuple<DateTime, List<Event>> UpcomingEventsCache = new Tuple<DateTime, List<Event>>(DateTime.MinValue, new List<Event>());
    private static readonly int UpcomingEventsCacheTTLSeconds = 60;

    private Dictionary<string, Tuple<DateTime, List<Entrant>>> EntrantsCache = new Dictionary<string, Tuple<DateTime, List<Entrant>>>();
    private static readonly int EntrantsCacheTTLSeconds = 60;

    private Dictionary<string, Tuple<DateTime, List<Set>>> SetsCache = new Dictionary<string, Tuple<DateTime, List<Set>>>();
    private static readonly int SetsCacheTTLSeconds = 30;

    private Dictionary<string, Tuple<DateTime, IEnumerable<Upset>>> UpsetsCache = new Dictionary<string, Tuple<DateTime, IEnumerable<Upset>>>();
    private static readonly int UpsetsCacheTTLSeconds = 120;

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
        EventsContainer = GetContainer("Events");

        PlacementToRounds = new Dictionary<int, int>();

        var keyPlacements = new List<int>() { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097 };

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

    public static SmashExplorerDatabase Instance { get { return lazy.Value; } }

    public async Task<List<Event>> GetUpcomingEventsAsync()
    {
        if (DateTime.UtcNow - UpcomingEventsCache.Item1 < TimeSpan.FromSeconds(UpcomingEventsCacheTTLSeconds))
        {
            return UpcomingEventsCache.Item2;
        }

        var results = new List<Event>();

        int secondsSinceEpoch = (int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        using (var iterator = EventsContainer.GetItemQueryIterator<Event>($"SELECT * FROM c WHERE c.startAt > {secondsSinceEpoch} ORDER BY c.numEntrants DESC OFFSET 0 LIMIT 10"))
        {
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
        }

        UpcomingEventsCache = Tuple.Create(DateTime.UtcNow, results);

        return results;
    }

    public async Task<Event> GetEventAsync(string eventId)
    {
        return await EventsContainer.ReadItemAsync<Event>(eventId, new PartitionKey(eventId));
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
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
        }

        return results;
    }

    public async Task<List<Entrant>> GetEntrantsAsync(string eventId)
    {
        if (!EntrantsCache.ContainsKey(eventId))
        {
            EntrantsCache.Add(eventId, Tuple.Create(DateTime.MinValue, new List<Entrant>()));
        }

        Tuple<DateTime, List<Entrant>> cachedEntrants = EntrantsCache[eventId];

        if (DateTime.UtcNow - cachedEntrants.Item1 < TimeSpan.FromSeconds(EntrantsCacheTTLSeconds))
        {
            return cachedEntrants.Item2;
        }

        var results = new List<Entrant>();

        using (var iterator = EntrantsContainer.GetItemQueryIterator<Entrant>($"select * from t where t.eventId = \"{eventId}\""))
        {
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
        }

        EntrantsCache[eventId] = Tuple.Create(DateTime.UtcNow, results);

        return results;
    }

    public async Task<List<Set>> GetSetsAsync(string eventId)
    {
        if (!SetsCache.ContainsKey(eventId))
        {
            SetsCache.Add(eventId, Tuple.Create(DateTime.MinValue, new List<Set>()));
        }

        Tuple<DateTime, List<Set>> cachedSets = SetsCache[eventId];

        if (DateTime.UtcNow - cachedSets.Item1 < TimeSpan.FromSeconds(SetsCacheTTLSeconds))
        {
            return cachedSets.Item2;
        }

        var results = new List<Set>();

        using (var iterator = SetsContainer.GetItemQueryIterator<Set>($"select * from t where t.eventId = \"{eventId}\""))
        {
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
        }

        SetsCache[eventId] = Tuple.Create(DateTime.UtcNow, results);

        return results;
    }

    public async Task<VanityLink> CreateVanityLinkAsync(string eventId, string name, List<string> entrantIds)
    {
        var newVanityLink = new VanityLink(eventId, name, entrantIds);
        string generatedId = newVanityLink.Id;
        var currentLinkSize = 8;

        while (true)
        {
            try
            {
                newVanityLink.Id = generatedId.Substring(0, currentLinkSize);
                return await VanityLinksContainer.CreateItemAsync(newVanityLink, new PartitionKey(eventId));
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

    public async Task<VanityLink> GetDataForVanityLinkAsync(string id, string eventId)
    {
        return await VanityLinksContainer.ReadItemAsync<VanityLink>(id, new PartitionKey(eventId));
    }

    public async Task<IEnumerable<Upset>> GetUpsetsAndNotableAsync(string eventId)
    {
        if (!UpsetsCache.ContainsKey(eventId))
        {
            UpsetsCache.Add(eventId, Tuple.Create(DateTime.MinValue, new List<Upset>().AsEnumerable()));
        }

        Tuple<DateTime, IEnumerable<Upset>> cachedUpsets = UpsetsCache[eventId];

        if (DateTime.UtcNow - cachedUpsets.Item1 < TimeSpan.FromSeconds(UpsetsCacheTTLSeconds))
        {
            return cachedUpsets.Item2;
        }

        var results = new List<Set>();

        using (var iterator = SetsContainer.GetItemQueryIterator<Set>($"select * from t where t.eventId = \"{eventId}\" and t.isUpsetOrNotable"))
        {
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
        }

        var ret = results.Select(set => MapToUpset(set));

        UpsetsCache[eventId] = Tuple.Create(DateTime.UtcNow, ret);

        return ret;
    }

    public string GetStringOrdinal(int num)
    {
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

        var newDisplayScore = $"{winner.Name} ({winner.InitialSeedNum}) {set.DetailedScore[winner.Id]}-{set.DetailedScore[loser.Id]} " +
                              $"{loser.Name} ({loser.InitialSeedNum})";

        if (set.Round < 0)
        {
            newDisplayScore += $" (out @ {GetStringOrdinal(set.LPlacement ?? -1)})";
        }

        return new Upset()
        {
            Set = set,
            CompletedUpset = winnerRoundSeedingPlacement > loserRoundSeedingPlacement,
            UpsetFactor = upsetFactor,
            NewDisplayScore = newDisplayScore
        };
    }
}