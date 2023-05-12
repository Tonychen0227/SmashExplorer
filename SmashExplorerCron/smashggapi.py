import time
from copy import copy
from datetime import datetime
import logging

from graphqlclient import GraphQLClient, json

SMASH_MELEE_GAME_ID = 1
RIVALS_OF_AETHER_ID = 24
SMASH_ULTIMATE_GAME_ID = 1386

KEY_PLACEMENTS = [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097]


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
        self.placement_to_round = {}

        for index in range(0, len(KEY_PLACEMENTS) - 1):
            bottom_placement = KEY_PLACEMENTS[index]
            top_placement = KEY_PLACEMENTS[index + 1]
            for placement in range(bottom_placement, top_placement):
                self.placement_to_round[placement] = index

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
        total_pages = 1
        current_page = 1

        all_results = []

        while total_pages is None or current_page <= total_pages:
            params = copy(params_base)
            params["page"] = current_page

            results = self.__call_api(f"Get Upcoming Ult {keys_array}", query_string, params)

            if results is None:
                break

            for key in keys_array:
                results = results[key]
                if results is None:
                    self.logger.log(f"WTF: Breaking early on Paginated Calls {query_string.replace(' ', '')[:20]} with {params}")
                    return all_results

            total_pages = results["pageInfo"]["totalPages"]

            all_results.extend(results["nodes"])

            current_page += 1

        return all_results

    def get_game_characters(self, video_game_id):
        query_string = '''
            query Characters($videoGameId: ID) {
                videogame(id: $videoGameId){
                    characters {
                        id
                        images {
                          url
                        }
                        name
                    }
                }
            }
            '''

        params = {"videoGameId": video_game_id}
        return self.__call_api(f"Characters, for {video_game_id}", query_string, params)

    def get_upcoming_ult_events(self, start_time: datetime, end_time: datetime):
        after_date = int(start_time.timestamp())
        before_date = int(end_time.timestamp())
        query_string = '''
            query TournamentEvents($beforeDate: Timestamp, $afterDate: Timestamp, $page: Int) {
              tournaments(query:{page: $page, perPage: 200, filter:{afterDate:$afterDate, beforeDate:$beforeDate, videogameIds: [1, 24, 1386], published:true, publiclySearchable:true}}) {
                pageInfo{
                  perPage
                  totalPages
                }
                pageInfo{
                  perPage
                  totalPages
                }
                nodes {
                  events(filter:{published:true, videogameId: [1, 24, 1386]}) {
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
                owner {
                  id
                  slug
                  player {
                    gamerTag
                  }
                  name
                }
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
                  isFinal
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

        if result is None:
            return None
        
        event = result["event"]

        if event is None:
            return None

        tournament = result["event"]["tournament"]

        tournament_owner = {
            "id": tournament["owner"]["id"],
            "slug": tournament["owner"]["slug"],
            "gamerTag": None
        }

        if tournament["owner"]["player"] is not None and "gamerTag" in tournament["owner"]["player"]:
            tournament_owner["gamerTag"] = tournament["owner"]["player"]["gamerTag"]

        standings = None

        if event["standings"] is not None and "nodes" in event["standings"]:
            standings = event["standings"]["nodes"]

        return {
                "tournamentSlug": tournament["slug"],
                "tournamentName": tournament["name"],
                "tournamentOwner": tournament_owner,
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
                "standings": standings
            }

    def get_ult_tournament_events(self, tournament_slug):
        query_string = '''
            query TournamentEventsQuery($slug: String) {
              tournament(slug: $slug) {
                events(filter:{published:true, videogameId: [1, 24, 1386]}) {
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
                  authorizations(types: [TWITTER, TWITCH]) {
                    url
                  }
                  slug
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
                    isDisqualified
                    initialSeedNum
                    id
                    standing {
                      placement
                    }
                    participants {
                      user {
                        authorizations(types: [TWITTER, TWITCH]) {
                          url
                        }
                        slug
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
                "isDisqualified": entrant["isDisqualified"],
                "seeding": entrant["initialSeedNum"],
                "additionalInfo": [
                    {
                        "urls": [x for x in participant["user"]["authorizations"] if x is not None] if participant["user"]["authorizations"] is not None else [],
                        "location": participant["user"]["location"]
                    } if participant["user"] is not None else None
                    for participant in entrant["participants"]
                ],
                "userSlugs": [x for x in [
                    participant["user"]["slug"] if participant["user"] is not None else None
                    for participant in entrant["participants"]
                ] if x is not None],
            } for entrant in entrants
        ]

    def __process_set(self, event_id, tournament_set):
        if "phaseGroup" not in tournament_set or tournament_set["phaseGroup"] is None:
            tournament_set["phaseGroup"] = {
                "id": "NONE",
                "displayIdentifier": "NONE"
            }

        if "phase" not in tournament_set["phaseGroup"] or tournament_set["phaseGroup"]["phase"] is None:
            tournament_set["phaseGroup"]["phase"] = {
                "bracketType": "NONE",
                "phaseOrder": 1,
                "name": "NONE",
                "id": "NONE"
            }
            
        return_set = {
            "id": str(tournament_set["id"]),
            "eventId": str(event_id),
            "fullRoundText": tournament_set["fullRoundText"],
            "displayScore": tournament_set["displayScore"],
            "winnerId": str(tournament_set["winnerId"]),
            "round": tournament_set["round"],
            "createdAt": tournament_set["createdAt"],
            "completedAt": tournament_set["completedAt"],
            "wPlacement": tournament_set["wPlacement"],
            "lPlacement": tournament_set["lPlacement"],
            "bracketType": tournament_set["phaseGroup"]["phase"]["bracketType"],
            "phaseIdentifier": tournament_set["phaseGroup"]["displayIdentifier"],
            "phaseGroupId": tournament_set["phaseGroup"]["id"],
            "phaseOrder": tournament_set["phaseGroup"]["phase"]["phaseOrder"],
            "phaseName": tournament_set["phaseGroup"]["phase"]["name"],
            "phaseId": tournament_set["phaseGroup"]["phase"]["id"],
            "games": tournament_set["games"],
            "entrants":
                [
                    {
                        "name": None if x["entrant"] is None else x["entrant"]["name"],
                        "id": None if x["entrant"] is None else str(x["entrant"]["id"]),
                        "initialSeedNum": None if x["entrant"] is None else x["entrant"]["initialSeedNum"],
                        "prereqId": x["prereqId"],
                        "prereqType": x["prereqType"]
                    } for x in tournament_set["slots"]
                ],
            "entrantIds": [str(x["entrant"]["id"]) for x in tournament_set["slots"] if x["entrant"] is not None],
            "stream": tournament_set["stream"],
            "isUpsetOrNotable": False,
            "detailedScore": None
        }
        
        try:
            if return_set["displayScore"] is None or return_set["winnerId"] is None \
                    or return_set["displayScore"] == "Bye" or return_set["displayScore"] == "DQ":
                return return_set

            if len(return_set["entrants"]) != 2:
                self.logger.log(f"WTF: Not 2 entrants {return_set}")
                return return_set

            winner = [x for x in return_set["entrants"] if str(return_set["winnerId"]) == str(x["id"])][0]
            loser = [x for x in return_set["entrants"] if str(return_set["winnerId"]) != str(x["id"])][0]

            if winner["initialSeedNum"] is None or loser["initialSeedNum"] is None:
                return return_set

            winner_round_seed = self.placement_to_round[winner["initialSeedNum"]]
            loser_round_seed = self.placement_to_round[loser["initialSeedNum"]]

            return_set["upsetFactor"] = abs(winner_round_seed - loser_round_seed)

            if winner_round_seed > loser_round_seed:
                return_set["isUpsetOrNotable"] = True

            display_score = return_set["displayScore"]
            display_score_end = display_score[:-2]

            test_1 = (display_score.startswith(winner['name']) and not display_score.startswith(loser['name'])) or \
                     (display_score_end.endswith(loser['name']) and not display_score_end.endswith(winner['name']))
            test_2 = (display_score.startswith(loser['name']) and not display_score.startswith(winner['name'])) or \
                     (display_score_end.endswith(winner['name']) and not display_score_end.endswith(loser['name']))

            try:
                if test_1 and test_2:
                    self.logger.log(f"WTF: Both Regex Matched {return_set}")
                    return return_set
                elif test_1:
                    display_score = display_score.replace(f"{winner['name']} ", "", 1)
                    winner_score = display_score[:1]
                    loser_score = display_score[-1:]
                    return_set["detailedScore"] = {
                        winner["id"]: winner_score,
                        loser["id"]: loser_score
                    }
                elif test_2:
                    display_score = display_score.replace(f"{loser['name']} ", "", 1)
                    loser_score = display_score[:1]
                    winner_score = display_score[-1:]
                    return_set["detailedScore"] = {
                        winner["id"]: winner_score,
                        loser["id"]: loser_score
                    }
                else:
                    raise IndexError()
            except IndexError:
                self.logger.log(f"WTF: No Regex Matching {return_set}")
                return return_set

            if winner_round_seed == loser_round_seed:
                return return_set
            try:
                if abs(int(winner_score) - int(loser_score)) == 1:
                    return_set["isUpsetOrNotable"] = True
                    return return_set
            except ValueError:
                return return_set

            return return_set
        except:
            return return_set

    def get_event_sets_updated_after_timestamp(self, event_id: str, start_timestamp: int = None):
        query_string = '''
            query EventSetsQuery($eventId: ID, $page: Int, $updatedAfter: Timestamp) {
              event(id:$eventId) {
                id
                sets(page: $page, perPage: 15, sortType:NONE, 
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
                    completedAt
                    createdAt
                    round
                    wPlacement
                    lPlacement
                    games {
                      orderNum
                      winnerId
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
                      id
                      displayIdentifier
                      phase {
                        name
                        phaseOrder
                        bracketType
                        id
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
                      streamName
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

        return [self.__process_set(event_id, tournament_set) for tournament_set in sets]
