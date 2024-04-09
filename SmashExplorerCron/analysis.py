import json

f = open("C:\\Users\\TonyC\\Downloads\\SmashExplorerStats.json")

metrics = json.load(f)

bobc_event_ids_mapping = {
  "999585": "Ultimate Singles",
  "999586": "Melee Singles",
  "1000213": "Tekken Singles",
  "1000212": "SF6 Singles",
  "1000214": "Ultimate Doubles",
  "1000215": "Melee Doubles",
  "1104552": "Melee Ladder",
  "1104051": "Ultimate Ladder"
}

final_payload = {}

final_payload["Reports"] = {}

for eventId in metrics["SetsReported"]:
  if eventId not in bobc_event_ids_mapping.keys():
    continue
  completed = 0
  started = 0
  for userKey in metrics["SetsReported"][eventId]:
    completed += metrics["SetsReported"][eventId][userKey]["Completed"]
    started += metrics["SetsReported"][eventId][userKey]["Started"]

  final_payload["Reports"][bobc_event_ids_mapping[eventId]] = {
    "Completed": completed,
    "Failed": started - completed
  }

final_payload["Logins"] = len(metrics["Logins"].keys())

for tournamentId in metrics["TournamentAPIVisits"]:
  if tournamentId != "593513":
    continue

  num_users = 0
  num_visits = 0

  num_non_regged_visits = 0
  for user in metrics["TournamentAPIVisits"][tournamentId].keys():
    if user == "":
      num_non_regged_visits += metrics["TournamentAPIVisits"][tournamentId][user]
      continue

    num_users += 1
    num_visits += metrics["TournamentAPIVisits"][tournamentId][user]

  final_payload["TournamentPageVisits"] = {
    "Non-Competitor visits": num_non_regged_visits,
    "Competitors (# users)": num_users,
    "Competitors (# visits)": num_visits
  }


print(final_payload)