import time
from copy import copy
from datetime import datetime
import logging

from graphqlclient import GraphQLClient, json

SMASH_MELEE_GAME_ID = 1
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
                logging.exception("")
                self.logger.log(f"Error {result} occured calling query {description} with {params}")
                num_retries += 1

        return None

    def make_paginated_calls(self, query_string, keys_array, params_base):
        total_pages = None
        current_page = 1

        all_results = []

        while total_pages is None or current_page <= total_pages:
            params = copy(params_base)
            params["page"] = current_page

            results = self.__call_api(f"Get Upcoming Ult {keys_array}", query_string, params)

            for key in keys_array:
                results = results[key]

            total_pages = results["pageInfo"]["totalPages"]

            all_results.extend(results["nodes"])

            current_page += 1

        return all_results

    def get_upcoming_ult_events(self, start_time: datetime, end_time: datetime):
        after_date = int(start_time.timestamp())
        before_date = int(end_time.timestamp())
        query_string = '''
            query TournamentsQuery($beforeDate: Timestamp, $afterDate: Timestamp, $page: Int) {
              tournaments(query:{page: $page, perPage: 200, filter:{afterDate:$afterDate, beforeDate:$beforeDate, videogameIds: [1, 1386], published:true, publiclySearchable:true}}) {
                pageInfo{
                  perPage
                  totalPages
                }
                pageInfo{
                  perPage
                  totalPages
                }
                nodes {
                  events(filter:{published:true, videogameId: [1, 1386]}) {
                    id
                    numEntrants
                  }
                }
              }
            }
        '''

        params = {"beforeDate": before_date, "afterDate": after_date}

        return [x["events"] for x in self.make_paginated_calls(query_string, ["tournaments"], params)]

    def get_event(self, event_id):
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
              tournament {
                addrState
                city
                countryCode
                slug
                name
                images {
                  url
                  ratio
                }
              }
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

        result = self.__call_api("Get single event", query_string, {"eventId": event_id})
        tournament = result["event"]["tournament"]
        event = result["event"]

        return {
                "tournamentSlug": tournament["slug"],
                "tournamentName": tournament["name"],
                "tournamentLocation": {
                    "city": tournament["city"],
                    "countryCode": tournament["countryCode"],
                    "addrState": tournament["addrState"]
                },
                "tournamentImages": tournament["images"],
                "state": event["state"],
                "id": str(event["id"]),
                "name": event["name"],
                "startAt": event["startAt"],
                "createdAt": event["createdAt"],
                "updatedAt": event["updatedAt"],
                "slug": event["slug"],
                "numEntrants": event["numEntrants"],
                "standings": event["standings"]["nodes"]
            }

    def get_ult_tournament_events(self, tournament_slug):
        query_string = '''
            query TournamentQuery($slug: String) {
              tournament(slug: $slug) {
                events(filter:{published:true, videogameId: [1, 1386]}) {
                  id
              }}}
            '''

        params = {"slug": tournament_slug}

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
                entrants(query:{page: $page, perPage: 50}) {
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

        params = {"eventId": event_id}
        entrants = self.make_paginated_calls(query_string, ["event", "entrants"], params)

        return [
            {
                "eventId": event_id,
                "name": entrant["name"],
                "standing": None if entrant["standing"] is None else entrant["standing"]["placement"],
                "id": str(entrant["id"]),
                "seeding": entrant["initialSeedNum"],
                "additionalInfo": [
                    {
                        "urls": [x for x in participant["user"]["authorizations"] if x is not None],
                        "location": participant["user"]["location"]
                    } if participant["user"] is not None else None
                    for participant in entrant["participants"]
                ]
            } for entrant in entrants
        ]

    def get_event_sets_updated_after_timestamp(self, event_id: str, start_timestamp: int = None):
        query_string = '''
            query EventSetsQuery($eventId: ID, $page: Int, $updatedAfter: Timestamp) {
              event(id:$eventId) {
                id
                slug
                sets(page: $page, perPage: 25, sortType:ROUND, 
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
                    wPlacement
                    lPlacement
                    games {
                      orderNum
                      stage {
                        name
                      }
                      selections{
                        entrant {
                          id
                        }
                        selectionType
                        selectionValue
                      }
                    }
                    phaseGroup{
                      phase {
                        name
                        phaseOrder
                        bracketType
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

        params = {"eventId": event_id, "updatedAfter": start_timestamp}

        sets = self.make_paginated_calls(query_string, ["event", "sets"], params)

        return [
            {
                "id": str(tournament_set["id"]),
                "eventId": event_id,
                "fullRoundText": tournament_set["fullRoundText"],
                "displayScore": tournament_set["displayScore"],
                "winnerId": tournament_set["winnerId"],
                "round": tournament_set["round"],
                "wPlacement": tournament_set["wPlacement"],
                "lPlacement": tournament_set["lPlacement"],
                "bracketType": tournament_set["phaseGroup"]["phase"]["bracketType"],
                "phaseOrder": tournament_set["phaseGroup"]["phase"]["phaseOrder"],
                "phaseName": tournament_set["phaseGroup"]["phase"]["name"],
                "games": tournament_set["games"],
                "entrants":
                [
                    {
                        "name": None if x["entrant"] is None else x["entrant"]["name"],
                        "id": None if x["entrant"] is None else x["entrant"]["id"],
                        "seeding": None if x["entrant"] is None else x["entrant"]["initialSeedNum"],
                        "prereqId": x["prereqId"],
                        "prereqType": x["prereqType"]
                    } for x in tournament_set["slots"]
                ]
            } for tournament_set in sets
        ]
