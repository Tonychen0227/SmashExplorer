﻿using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StartGGDatabase
{
    private readonly List<GraphQLHttpClient> Clients;

    private static readonly Lazy<StartGGDatabase> lazy = new Lazy<StartGGDatabase>(() => new StartGGDatabase());

    private Dictionary<string, Tuple<DateTime, StartGGTournamentResponse>> TournamentEventsCache = new Dictionary<string, Tuple<DateTime, StartGGTournamentResponse>>();
    private static readonly int TournamentEventsCacheTTLSeconds = 600;

    private StartGGDatabase()
    {
        var keys = Environment.GetEnvironmentVariable("STARTGG_KEYS");
        var mutationKeys = Environment.GetEnvironmentVariable("STARTGG_KEYS_MUTATION");
        var clients = new List<GraphQLHttpClient>();

        foreach (var key in keys.Split(' '))
        {
            clients.Add(GetClient(key));
        }

        Clients = clients;
    }

    private GraphQLHttpClient GetClient(string token)
    {
        var graphQLClient = new GraphQLHttpClient("https://api.smash.gg/gql/alpha", new NewtonsoftJsonSerializer());
        graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return graphQLClient;
    }

    public static StartGGDatabase Instance { get { return lazy.Value; } }

    public async Task<StartGGUser> GetUserTokenDetails(string token)
    {
        var client = GetClient(token);

        var query = new GraphQLRequest
        {
            Query = @"
query {
  currentUser{
    id
    tournaments(query:{page:1, perPage:500, filter:{upcoming:true}}){
      nodes{
        name
        id
      }
    }
    genderPronoun
    name
    slug
    email
    discriminator
  }
}"
        };

        var response = await SendQueryAsync<StartGGUserResponse>(query, client);

        return response.Data.CurrentUser;
    }

    private static readonly Dictionary<string, StartGGVideogame> VideoGameCache = new Dictionary<string, StartGGVideogame>();

    public async Task<StartGGVideogame> GetVideogameDetails(string videoGameId)
    {
        if (VideoGameCache.ContainsKey(videoGameId))
        {
            return VideoGameCache[videoGameId];
        }

        var query = new GraphQLRequest
        {
            Query = @"
query VideoGameQuery($id: ID) {
  videogame(id:$id){
    id 
    name
    characters {
      id
      name
      images {
        url
        height
        width
        ratio
        type
      }
    }
    stages {
      id
      name
    }
  }
}",
            Variables = new
            {
                id = videoGameId
            }
        };

        var response = await SendQueryAsync<StartGGVideogameResponse>(query);

        VideoGameCache[videoGameId] = response.Data.Videogame;

        return response.Data.Videogame;
    }

    public async Task<string> GetEventId(string slug)
    {
        var query = new GraphQLRequest
        {
            Query = @"
query TournamentSlugQuery($slug: String) {
  tournament(slug:$slug){
    id
    events {
      name
      id
    }
  }
}",
            Variables = new
            {
                slug = slug
            }
        };

        var response = await SendQueryAsync<StartGGTournamentResponse>(query);
        var eventId = response.Data.Tournament.Events.First().Id;

        return eventId;
    }

    public async Task<StartGGTournamentResponse> GetTournamentEvents(string id)
    {
        var entryExists = TournamentEventsCache.TryGetValue(id, out var cachedData);

        if (entryExists && DateTime.UtcNow - cachedData.Item1 < TimeSpan.FromSeconds(TournamentEventsCacheTTLSeconds))
        {
            return cachedData.Item2;
        }

        var query = new GraphQLRequest
        {
            Query = @"
query TournamentIdQuery($id: ID) {
  tournament(id:$id){
    id
    events(filter: {videogameId: [1, 1386]}) {
      name
      id
      startAt
      slug
      numEntrants
    }
  }
}",
            Variables = new
            {
                id = id
            }
        };

        var response = await SendQueryAsync<StartGGTournamentResponse>(query);

        return response.Data;
    }

    public async Task<string> GetPhaseId(string eventId, string stringToSearch)
    {
        var query = new GraphQLRequest
        {
            Query = @"
query EventPhaseQuery($eventId: ID) {
  event(id:$eventId){
    phases {
      name
      id
    }
  }
}",
            Variables = new
            {
                eventId = eventId
            }
        };

        var response = await SendQueryAsync<StartGGEventResponse>(query);
        var swissPhaseId = response.Data.Event.Phases.Where(x => x.Name == stringToSearch).Select(x => x.Id).First();

        return swissPhaseId;
    }

    private async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest query, GraphQLHttpClient overrideClient = null)
    {
        while (true)
        {
            try
            {
                var response = await (overrideClient ?? GetClient()).SendQueryAsync<T>(query, new System.Threading.CancellationToken());
                return response;
            } catch (Exception)
            {
                await Task.Delay(1000);
            }
        }
    }

    public async Task<GraphQLHttpResponse<StartGGEventResponse>> GetSetsInPhase(string eventId, string phaseId)
    {
        var query = new GraphQLRequest
        {
            Query = @"
query EventSetsQuery($eventId: ID, $phaseId: ID) {
  event(id:$eventId){
    sets (page:1, perPage:500, filters:{phaseIds:[$phaseId]}) {
      nodes{
        id
        winnerId
        round
        slots {
          seed{
            id
          }
          entrant{
            name
            id
            seeds{
              seedNum
            }
          }
        }
      }
    }
  }
}",
            Variables = new
            {
                phaseId = phaseId,
                eventId = eventId
            }
        };

        var response = await SendQueryAsync<StartGGEventResponse>(query);
        var httpResponse = response.AsGraphQLHttpResponse();

        return httpResponse;
    }

    private GraphQLHttpClient GetClient()
    {
        return Clients[new Random().Next(Clients.Count)];
    }
}