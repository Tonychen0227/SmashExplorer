import requests
import datetime
import time

from logger import Logger

if __name__ == '__main__':
    logger = Logger(f"GalintApp")
    unique_tournament_responses = set()
    unique_entrant_matches_responses = set()
    unique_sets_responses = dict()

    while True:
        start = datetime.datetime.now()
        result = requests.get("https://smashexplorer.gg/TournamentAPI/603943?userSlug=user/de74e51d")
        end = datetime.datetime.now()
        elapsed = end - start
        elapsed_seconds = elapsed.total_seconds()
        result_string = str(result.json())
        logger.log(f"Tournament API Call Elapsed {elapsed_seconds} seconds")
        if result_string not in unique_tournament_responses:
            unique_tournament_responses.add(result_string)
            logger.log(f"NEW TOURNAMENT API RESPONSE DROPPED!")
            logger.log(result_string)

        start = datetime.datetime.now()
        result = requests.get("https://smashexplorer.gg/EntrantMatchesAPI/15285789")
        end = datetime.datetime.now()
        elapsed = end - start
        elapsed_seconds = elapsed.total_seconds()
        result_string = str(result.json())
        logger.log(f"Entrant Matches API Call Elapsed {elapsed_seconds} seconds")
        if result_string not in unique_entrant_matches_responses:
            unique_entrant_matches_responses.add(result_string)
            logger.log(f"NEW ENTRANT MATCHES API RESPONSE DROPPED!")
            logger.log(result_string)

        for x in result.json():
            set_id = x["Id"]

            if set_id not in unique_sets_responses:
                unique_sets_responses[set_id] = set()

            start = datetime.datetime.now()
            result = requests.get(f"https://smashexplorer.gg/SetAPI/{set_id}")
            end = datetime.datetime.now()
            elapsed = end - start
            elapsed_seconds = elapsed.total_seconds()
            result_string = str(result.json())
            logger.log(f"Set API Call for {set_id} Elapsed {elapsed_seconds} seconds")

            if result_string not in unique_sets_responses[set_id]:
                unique_sets_responses[set_id].add(result_string)
                logger.log(f"NEW SET API RESPONSE DROPPED!")
                logger.log(result_string)

        time.sleep(5)