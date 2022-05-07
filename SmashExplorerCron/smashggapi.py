import time
from datetime import datetime

from graphqlclient import GraphQLClient, json

SMASH_ULTIMATE_GAME_ID = 1386


class API:
    def __get_client(self):
        ret = self.clients.pop(0)
        self.clients.append(ret)
        return ret

    def __init__(self, tokens):
        self.clients = []

        for token in tokens.split(" "):
            client = GraphQLClient('https://api.smash.gg/gql/alpha')
            client.inject_token(f'Bearer {token}')
            self.clients.append(client)

    def __call_api(self, description, input, params):
        num_retries = 0

        while num_retries < 10:
            result = None
            try:
                result = self.__get_client().execute(input, params)
                return json.loads(result)["data"]
            except:
                time.sleep(2)
                print(f"Error {json.loads(result)} occured calling query {description} with {params}")
                num_retries += 1

    def get_ult_tournament_events(self, slug):
        query_string = '''
            query TournamentsQuery($slug: String) {
              tournament(slug: $slug) {
                city
                countryCode
                slug
                name
                events(filter:{published:true, videogameId: [1386]}) {
                  id
                  state
                  startAt
                  name
                  slug
                  numEntrants
                  standings(query: {perPage: 128}) {
                    nodes {
                      placement
                      entrant {
                        name
                        initialSeedNum
                      }
                    }
                  }
              }}}
            '''

        params = {"slug": slug}

        return self.__call_api("Get Ult Tournament Events", query_string, params)

    def get_ult_entrant(self, entrant_id):
        query_string = '''
          query EntrantQuery($entrantId: ID!) {
            entrant(id: $entrantId) {
              name
              initialSeedNum
              id
              standing {
                placement
              }
              event {
                id
                name
                slug
              }
            }
          }
          '''

        params = {"entrantId": entrant_id}

        result = self.__call_api("Get Entrants For Event", query_string, params)

        return result["entrant"]["event"], result["entrant"]

    def get_ult_event_entrants(self, event_slug):
        query_string = '''
            query EventEntrantsQuery($slug: String, $page: Int) {
              event(slug: $slug) {
                id
                slug
                entrants(query:{page: $page, perPage: 100}) {
                  pageInfo {
                    totalPages
                    page
                  }
                  nodes {
                    name
                    initialSeedNum
                    id
                    standing {
                      placement
                    }
                  }
                }
              }
            }
            '''

        current_page = 1
        total_pages = 2

        entrants = []

        while current_page <= total_pages:
            params = {"slug": event_slug, "page": current_page}
            results = self.__call_api("Get Entrants For Event", query_string, params)
            entrants.extend(results["event"]["entrants"]["nodes"])
            total_pages = results["event"]["entrants"]["pageInfo"]["totalPages"]

            current_page += 1

        return entrants

    def get_event_sets_updated_after_timestamp(self, event_slug: str, start_timestamp: datetime):
        query_string = '''
            query EventSetsQuery($slug: String, $page: Int) {
              event(slug: $slug) {
                id
                slug
                sets(query:{page: $page, perPage: 100}) {
                  pageInfo {
                    totalPages
                    page
                  }
                  nodes {
                    name
                    initialSeedNum
                    id
                    standing {
                      placement
                    }
                  }
                }
              }
            }
            '''

