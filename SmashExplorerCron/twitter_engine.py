import tweepy
import os


class TwitterClient:
    def __init__(self, logger):
        self.logger = logger

        if "TWITTER_AUTH" not in os.environ:
            self.api = None
            return

        twitter_keys = os.environ["TWITTER_AUTH"].split(" ")
        self.api = tweepy.Client(consumer_key=twitter_keys[0],
                                 consumer_secret=twitter_keys[1],
                                 access_token=twitter_keys[2],
                                 access_token_secret=twitter_keys[3],
                                 wait_on_rate_limit=True)

    def make_root_tweet(self, event, minimum_upset_factor):
        if minimum_upset_factor is None:
            minimum_upset_factor = 3

        text = f"Upset Thread for {event['name']} @ {event['tournamentName']} (minimum Upset Factor {minimum_upset_factor})\n\nFor full upsets, check https://smashexplorer.gg/Upsets/{event['id']}"

        print(text)

        if self.api is None:
            return None

        response = self.api.create_tweet(text=text)
        return response.data["id"]

    def make_tweet(self, args, root):
        text = '\n\n'.join(args)

        print(text)

        if self.api is None:
            return None

        response = self.api.create_tweet(text=text, in_reply_to_tweet_id=root)
        return response.data["id"]
