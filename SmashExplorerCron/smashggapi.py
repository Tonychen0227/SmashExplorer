import time
from copy import copy
from datetime import datetime

from graphqlclient import GraphQLClient, json

SMASH_ULTIMATE_GAME_ID = 1386


class API:
    def __get_client(self):
        ret = self.clients.pop(0)
        self.clients.append(ret)
        return ret

    def __init__(self, tokens, logger):
        self.clients = []

        for token in tokens.split(" "):
            client = GraphQLClient('https://api.smash.gg/gql/alpha')
            client.inject_token(f'Bearer {token}')
            self.clients.append(client)

        self.logger = logger

    def __call_api(self, description, input, params):
        num_retries = 0

        while num_retries < 10:
            result = None
            try:
                result = self.__get_client().execute(input, params)
                return json.loads(result)["data"]
            except:
                time.sleep(2)
                self.logger.log(f"Error {json.loads(result)} occured calling query {description} with {params}")
                num_retries += 1

    def get_upcoming_ult_tournaments(self, start_after: datetime, start_before: datetime):
        start_after = int(start_after.timestamp())
        start_before = int(start_before.timestamp())
        query_string = '''
            query TournamentsQuery($beforeDate: Timestamp, $afterDate: Timestamp, $page: Int) {
              tournaments(query:{page: $page, perPage: 500, filter:{afterDate:$afterDate, beforeDate:$beforeDate, videogameIds: [1386], published:true, publiclySearchable:true}}) {
                pageInfo{
                  perPage
                  totalPages
                }
                nodes {
                  city
                  countryCode
                  slug
                  name
                  startAt
                  numAttendees
                }
              }
            }
        '''

        params = {"beforeDate": start_before, "afterDate": start_after}

        current_page = 1
        total_pages = 2

        tournaments = []

        while current_page <= total_pages:
            params_temp = copy(params)
            params_temp["page"] = current_page

            results = self.__call_api("Get Upcoming Ult Tournaments", query_string, params_temp)

            def filter_events(tournament):
                return tournament["numAttendees"] is not None and tournament["numAttendees"] >= 16

            tournaments_filtered = list(filter(filter_events, results["tournaments"]["nodes"]))

            tournaments.extend(tournaments_filtered)
            total_pages = results["tournaments"]["pageInfo"]["totalPages"]

            current_page += 1

        return tournaments

    def get_ult_events_one_by_one(self, event_ids):
        query_string = '''
          query EventQuery($eventId: ID) {
            event(id: $eventId) {
              id
              state
              createdAt
              updatedAt
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
                    isDisqualified
                  }
                }
              }
            }
          }
          '''

        results = []

        for event_id in event_ids:
            query_result = self.__call_api("Get single event", query_string, {"eventId": event_id})
            results.append(query_result["event"])

        return results

    def get_ult_tournament_events(self, tournament_slug):
        backup_query_string = '''
            query TournamentQuery($slug: String) {
              tournament(slug: $slug) {
                city
                countryCode
                slug
                name
                events(filter:{published:true, videogameId: [1386]}) {
                  id
              }}}
        '''

        query_string = '''
            query TournamentQuery($slug: String) {
              tournament(slug: $slug) {
                city
                countryCode
                slug
                name
                events(filter:{published:true, videogameId: [1386]}) {
                  id
                  state
                  createdAt
                  updatedAt
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
                        isDisqualified
                      }
                    }
                  }
              }}}
            '''

        params = {"slug": tournament_slug}

        ret = self.__call_api("Get Ult Tournament Events", query_string, params)

        if ret is None:
            result = self.__call_api("Get Ult Tournament Events BACKUP", backup_query_string, params)
            ret = {
                "tournament": {
                    "city": result["tournament"]["city"],
                    "countryCode": result["tournament"]["countryCode"],
                    "slug": result["tournament"]["slug"],
                    "name": result["tournament"]["name"],
                    "events": self.get_ult_events_one_by_one([x["id"] for x in result["tournament"]["events"]])
                }
            }

        return ret

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
              participants {
                user {
                  authorizations(types: [TWITCH, DISCORD]) {
                    url
                  }
                  address {
                    city
                    state
                    country
                  }
                }
              }
            }
          }
          '''

        params = {"entrantId": entrant_id}

        result = self.__call_api("Get Entrants For Event", query_string, params)

        return result["entrant"]["event"], result["entrant"]

    def get_ult_event_entrants(self, event_id):
        query_string = '''
            query EventEntrantsQuery($eventId: ID, $page: Int) {
              event(id: $eventId) {
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
                    participants {
                      user {
                        authorizations(types: [TWITCH, DISCORD]) {
                          url
                        }
                        location {
                          city
                          state
                          country
                        }
                      }
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
            params = {"eventId": event_id, "page": current_page}
            results = self.__call_api("Get Entrants For Event", query_string, params)
            entrants.extend(results["event"]["entrants"]["nodes"])
            total_pages = results["event"]["entrants"]["pageInfo"]["totalPages"]

            current_page += 1

        return entrants

    def get_event_sets_updated_after_timestamp(self, event_id: str, start_timestamp: int = None):
        query_string = '''
            query EventSetsQuery($eventId: ID, $page: Int, $updatedAfter: Timestamp) {
              event(id:$eventId) {
                id
                slug
                sets(page: $page, perPage: 50, sortType:ROUND, 
                  filters:{showByes:false, hideEmpty:true, updatedAfter: $updatedAfter}) {
                  pageInfo {
                    totalPages
                    perPage
                    page
                  }
                  nodes {
                    id
                    fullRoundText
                    displayScore
                    winnerId
                    round
                    phaseGroup{
                      phase {
                        name
                        phaseOrder
                      }
                    }
                    slots {
                      entrant{
                        id
                        name
                        initialSeedNum
                      }
                      prereqId
                      prereqType
                    }
                    stream {
                      streamSource
                      streamId
                    }
                  }
                }
              }
            }
            '''

        if start_timestamp is None:
            start_timestamp = int(datetime.fromisocalendar(1971, 1, 1).timestamp())

        params_root = {"eventId": event_id, "updatedAfter": start_timestamp}
        current_page = 1
        total_pages = 2

        sets = []

        while current_page <= total_pages:
            params = copy(params_root)
            params["page"] = current_page

            results = self.__call_api("Get Sets For Event", query_string, params)
            sets.extend(results["event"]["sets"]["nodes"])
            total_pages = results["event"]["sets"]["pageInfo"]["totalPages"]

            current_page += 1

        return sets
