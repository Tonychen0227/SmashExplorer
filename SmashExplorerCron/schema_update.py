import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("schema_update")
    operations = OperationsManager(logger)
    logger.log("Starting Schema Upgrade")

    event_count = 0
    event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    total_events = len(event_ids)

    def is_notable(set):
        if set["winnerId"] is None or set["displayScore"] is None or len(set["entrants"]) != 2\
                or set["displayScore"] == "Bye" or set["displayScore"] == "DQ":
            return False

        winner = [x for x in set["entrants"] if str(set["winnerId"]) == str(x["id"])][0]
        loser = [x for x in set["entrants"] if str(set["winnerId"]) != str(x["id"])][0]

        winner_round_seed = operations.api.placement_to_round[winner["initialSeedNum"]]
        loser_round_seed = operations.api.placement_to_round[loser["initialSeedNum"]]

        display_score = set["displayScore"]
        display_score_end = display_score[:-2]

        test_1 = (display_score.startswith(winner['name']) and not display_score.startswith(loser['name'])) or \
                 (display_score_end.endswith(loser['name']) and not display_score_end.endswith(winner['name']))
        test_2 = (display_score.startswith(loser['name']) and not display_score.startswith(winner['name'])) or \
                 (display_score_end.endswith(winner['name']) and not display_score_end.endswith(loser['name']))

        try:
            if test_1 and test_2:
                return False
            elif test_1:
                display_score = display_score.replace(f"{winner['name']} ", "", 1)
                winner_score = display_score[:1]
                loser_score = display_score[-1:]
                set["detailedScore"] = {
                    winner["id"]: winner_score,
                    loser["id"]: loser_score
                }
            elif test_2:
                display_score = display_score.replace(f"{loser['name']} ", "", 1)
                loser_score = display_score[:1]
                winner_score = display_score[-1:]
                set["detailedScore"] = {
                    winner["id"]: winner_score,
                    loser["id"]: loser_score
                }
            else:
                return False
        except IndexError:
            return False

        if winner_round_seed == loser_round_seed:
            return False
        try:
            if abs(int(winner_score) - int(loser_score)) == 1:
                return True
        except ValueError:
            return False

        return False

    for event_id in [str(x) for x in event_ids]:
        event_count += 1
        try:
            cosmos_sets = list(operations.cosmos.get_event_sets(event_id))
            sets = [x for x in cosmos_sets if not x["id"].startswith("preview") and (not x["isUpsetOrNotable"] and is_notable(x))]

            logger.log(f"Schema upgrade progress ({event_id}): {event_count} of {total_events}, found {len(sets)} sets - {[x['id'] for x in sets]}")

            total_sets = len(sets)
            for db_set in sets:
                try:
                    db_set["isUpsetOrNotable"] = True
                    operations.cosmos.create_set(db_set)
                except:
                    logger.log(f"Failed to upgrade set {db_set['id']}")
        except:
            logging.exception("")
            logger.log(f"Failed to update sets for {event_id}")
