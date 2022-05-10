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

    private Dictionary<int, int> PlacementToRounds;

    private static readonly Lazy<SmashExplorerDatabase> lazy = new Lazy<SmashExplorerDatabase>(() => new SmashExplorerDatabase());

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
        var results = new List<Event>();

        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
        int secondsSinceEpoch = (int)t.TotalSeconds;

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

        return results;
    }

    public async Task<Event> GetEvent(string eventId)
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

        return results;
    }

    public async Task<List<Set>> GetSetsAsync(string eventId)
    {
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

        return results;
    }

    public async Task<List<Set>> GetUpsetsAndNotableAsync(string eventId)
    {
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

    private Upset IsUpset(Set set)
    {
        if (set.DisplayScore == "DQ" || set.WinnerId == null || set.Entrants.Count != 2) return null;

        int winnerSeeding = set.Entrants.Where(x => x.Id == set.WinnerId.ToString()).Select(x => x.Seeding).Single() ?? -1;
        int winnerRoundSeedingPlacement;
        PlacementToRounds.TryGetValue(winnerSeeding, out winnerRoundSeedingPlacement);

        int loserSeeding = set.Entrants.Where(x => x.Id != set.WinnerId.ToString()).Select(x => x.Seeding).Single() ?? -1;
        int loserRoundSeedingPlacement;
        PlacementToRounds.TryGetValue(loserSeeding, out loserRoundSeedingPlacement);

        if (winnerRoundSeedingPlacement == loserRoundSeedingPlacement)
            return null;

        int upsetFactor = Math.Abs(winnerRoundSeedingPlacement - loserRoundSeedingPlacement);

        var entrantScores = set.DisplayScore;
        entrantScores = entrantScores.Replace(set.Entrants[0].Name, set.Entrants[0].Id + ": ");
        entrantScores = entrantScores.Replace(set.Entrants[1].Name, set.Entrants[1].Id + ": ");
        entrantScores = "{" + entrantScores.Replace(" - ", ", ") + "}";
        entrantScores = entrantScores.Replace("W", "\"W\"");
        entrantScores = entrantScores.Replace("L", "\"L\"");

        Dictionary<string, string> scoresMappingString;

        try
        {
            scoresMappingString = JsonConvert.DeserializeObject<Dictionary<string, string>>(entrantScores);
        } catch (JsonReaderException ex)
        {
            return null;
        }

        var newDisplayScore = $"{set.Entrants.Where(x => x.Id == set.WinnerId.ToString()).Select(x => x.Name).Single()} {scoresMappingString[set.WinnerId]}-" +
                                $"{scoresMappingString[scoresMappingString.Keys.Where(x => x != set.WinnerId).Single()]} " +
                                $"{set.Entrants.Where(x => x.Id != set.WinnerId.ToString()).Select(x => x.Name).Single()}";

        if (winnerRoundSeedingPlacement > loserRoundSeedingPlacement)
            return new Upset()
            {
                Set = set,
                CompletedUpset = true,
                UpsetFactor = upsetFactor,
                NewDisplayScore = newDisplayScore
            };

        try
        {
            var scoresMapping = JsonConvert.DeserializeObject<Dictionary<string, int>>(entrantScores);

            if (Math.Abs(scoresMapping[set.Entrants[0].Id] - scoresMapping[set.Entrants[1].Id]) != 1)
                return null;
        }
        catch (JsonReaderException ex)
        {
            if (ex.Message.Contains("Unexpected character encountered while parsing value: W") ||
                ex.Message.Contains("Unexpected character encountered while parsing value: L"))
                return null;

            return null;
        }

        return new Upset()
        {
            Set = set,
            CompletedUpset = false,
            UpsetFactor = upsetFactor,
            NewDisplayScore = newDisplayScore
        };
    }

    public async Task<List<Upset>> GetUpsetsAsync(string eventId)
    {
        DateTime start = DateTime.UtcNow;

        var sets = await GetSetsAsync(eventId);

        TimeSpan timespan1 = DateTime.UtcNow - start; 

        var upsets = await GetUpsetsAndNotableAsync(eventId);

        TimeSpan timespan2 = DateTime.UtcNow - start - timespan1;

        return sets.Select(set => IsUpset(set)).Where(x => x != null).ToList();
    }
}