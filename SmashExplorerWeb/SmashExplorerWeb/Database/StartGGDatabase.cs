using GraphQL;
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

    private StartGGDatabase()
    {
        var keys = Environment.GetEnvironmentVariable("STARTGG_KEYS");
        var clients = new List<GraphQLHttpClient>();

        foreach (var key in keys.Split(' '))
        {
            var graphQLClient = new GraphQLHttpClient("https://api.smash.gg/gql/alpha", new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
            clients.Add(graphQLClient);
        }

        Clients = clients;
    }

    public static StartGGDatabase Instance { get { return lazy.Value; } }

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

    private async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest query)
    {
        while (true)
        {
            try
            {
                var response = await GetClient().SendQueryAsync<T>(query, new System.Threading.CancellationToken());
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