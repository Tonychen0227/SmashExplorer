using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{

    public class FTPController : Controller
    {
        private static Dictionary<string, string> Players = new Dictionary<string, string>()
    {
        {"b2120d43", "jdv"},
        {"eedb9647", "ludo"},
        {"ccb48fc0", "embo_z"},
        {"ab9d60c7", "jojodahobo"},
        {"ed48b9e1", "Despa"},
        {"6f31adf1", "Mystery Sol"},
        {"a627d015", "Eddawg"},
        {"5684916c", "nanoash"},
        {"024d257c", "Seesaw"},
        {"54730be9", "Raiyihn"},
        {"fbaaca3e", "ditto"},
        {"9f578cc6", "Konga"},
        {"9a5a9d6a", "Solo"},
        {"58103b74", "Bradoof"},
        {"c3b9ec6d", "h4"},
        {"40a11c5b", "Charlie D."},
        {"1d6d6fbf", "FireThePyro"},
        {"7fd0bb23", "Clune"},
        {"fcf9f270", "Corncycle"},
        {"400b9341", "Lumeckos"},
        {"7eab6592", "Spair"},
        {"b0ce876c", "HalfaTeaspoon"},
        {"c76189c1", "Cyan"},
        {"0978a9fc", "capsize"},
        {"02381d72", "pokepen"},
        {"4abc29d3", "Aloha"},
        {"acb04a70", "Mio!"},
        {"af599011", "archer"},
        {"ab0a9e36", "Safzai"},
        {"09a650e3", "Cattail"}
    };

        private static List<string> Tournaments = new List<string>()
    {
        "tournament/stairway-to-heaven-37/event/stairway-to-heaven",
        "tournament/cascadia-clash-2023/event/cascadia-clash-singles",
        "tournament/smashed-to-pieces-48/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-september-6th-10th-2023/event/ultimate-thursday-108",
        "tournament/protostar-16/event/ultimate-singles",
        "tournament/bsc-notcade-summer-series-8/event/ultimate-singles",
        "tournament/stairway-to-heaven-38/event/stairway-to-heaven",
        "tournament/the-retro-collection-september-13th-17th-2023/event/ultimate-thursday-109",
        "tournament/sinistar-saga-56-1/event/smash-ultimate-singles",
        "tournament/roc-smash-100-100-pot-bonus/event/ultimate-singles",
        "tournament/eclipse-6/event/ultimate-singles-sunday",
        "tournament/smashed-to-pieces-49/event/super-smash-bros-ultimate-singles",
        "tournament/bsc-notcade-summer-series-9-100-pot-bonus/event/ultimate-singles",
        "tournament/the-retro-collection-september-20th-24th-2023/event/ultimate-thursday-110",
        "tournament/keep-it-chill-5-fight/event/ultimate-singles-2000-prize-pool",
        "tournament/sea-salt-offline-7/event/super-smash-bros-ultimate-ns-2pm",
        "tournament/the-retro-collection-september-27th-october-1st-2023/event/ultimate-thursday-111",
        "tournament/stairway-to-heaven-39/event/stairway-to-heaven",
        "tournament/evergreen-rising-8/event/ultimate-singles",
        "tournament/smashed-to-pieces-49-5/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-october-4th-5th-2023/event/ultimate-thursday-112",
        "tournament/protostar-17/event/ultimate-singles",
        "tournament/the-retro-collection-october-11th-15th-2023/event/ultimate-thursday-113",
        "tournament/stairway-to-heaven-40/event/stairway-to-heaven",
        "tournament/rise-n-grind-2023/event/super-smash-bros-ultimate-singles",
        "tournament/trc-overnight-donation-drive-help-us-break-the-wall/event/ultimate-singles-cash-entry-cash-prizing",
        "tournament/smashed-to-pieces-50/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-october-18th-22nd-2023/event/ultimate-thursday-114",
        "tournament/protostar-18/event/ultimate-singles",
        "tournament/sinistar-saga-59/event/smash-ultimate-singles",
        "tournament/the-retro-collection-october-25th-29th-2023/event/ultimate-thursday-115",
        "tournament/stairway-to-heaven-41/event/stairway-to-heaven",
        "tournament/octobair-2023/event/ultimate-singles",
        "tournament/final-judgment-2-rise-from-the-grave/event/ultimate-singles",
        "tournament/smashed-to-pieces-51/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-october-31st-november-5th-2023/event/ultimate-thursday-116",
        "tournament/protostar-19/event/ultimate-singles",
        "tournament/pinnacle-2023/event/super-smash-bros-ultimate-singles",
        "tournament/games-unlimited-smash-ultimate-118/event/games-unlimited-singles-118",
        "tournament/wga-colosseum-52/event/ultimate-singles",
        "tournament/smashed-to-pieces-pp8-preparation/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-november-8th-12th-2023/event/ultimate-thursday-117",
        "tournament/little-league-port-priority-pre-local/event/ultimate-singles",
        "tournament/port-priority-8-5/event/ultimate-singles",
        "tournament/smashed-to-pieces-52/event/super-smash-bros-ultimate-singles",
        "tournament/the-retro-collection-november-15th-16th-2023/event/ultimate-thursday-118",
        "tournament/protostar-20/event/ultimate-singles",
        "tournament/grandslam-14/event/ultimate-singles"
    };

        private static List<(Event, List<Entrant>, List<Set>)> _goods = null;

        public async Task<ActionResult> Index()
        {
            var path = @"C:\Users\TonyC\Downloads\FTP_results.json";

            List<(Event, List<Entrant>, List<Set>)> goods;

            if (System.IO.File.Exists(path))
            {
                var data = System.IO.File.ReadAllText(@"C:\Users\TonyC\Downloads\FTP_results.json");

                goods = JsonConvert.DeserializeObject<List<(Event, List<Entrant>, List<Set>)>>(data);

                _goods = goods;
            }
            else if (_goods != null)
            {
                goods = _goods;
            }
            else
            {
                var tasks = Tournaments.Select(slug => SmashExplorerDatabase.Instance.GetEventsBySlugAndDatesAsync(slug, DateTime.UtcNow.Subtract(TimeSpan.FromDays(120)), DateTime.UtcNow));
                var tournaments = await Task.WhenAll(tasks);

                var tournamentsList = tournaments.Select(x => x.First());

                goods = (await Task.WhenAll(
                    tournamentsList.Select(async tournament => (
                        tournament,
                        (await SmashExplorerDatabase.Instance.GetEntrantsAsync(tournament.Id)).Where(e => Players.Keys.Any(k => e.UserSlugs.Contains($"user/{k}"))).ToList(),
                        await SmashExplorerDatabase.Instance.GetSetsAsync(tournament.Id))))).ToList();

                _goods = goods;
            }

            var model = GetRankingsStructFromGoods();

            return View(model);
        }

        private static PRDataModel GetRankingsStructFromGoods()
        {
            var rankingEvents = _goods.Select(good => new RankingEvent()
            {
                Date = DateTimeOffset.FromUnixTimeSeconds(good.Item1.StartAt).DateTime.ToShortDateString(),
                EventName = good.Item1.TournamentName,
                RankingConsideredPlayers = good.Item2.Count(),
                NumEntrants = good.Item1.NumEntrants,
                Link = $"https://start.gg/{good.Item1.Slug}",
                Placements = good.Item2.Where(x => x.IsDisqualified == null || !x.IsDisqualified.Value).Select(x => (x.UserSlugs.First(), x.Standing)).ToDictionary(x => x.Item1, x => x.Standing.Value).OrderBy(x => x.Value).ToDictionary(x => Players[x.Key.Split('/')[1]], x => x.Value)
            });

            var headToHead = Players.ToDictionary(x => x.Key, x => Players.ToDictionary(k => k.Key, k => new HeadToHead()));

            foreach (var tournament in _goods)
            {
                foreach (var entrant in tournament.Item2)
                {
                    var entrantSlug = entrant.UserSlugs.First().Split('/')[1];

                    foreach (var set in tournament.Item3.Where(x => x.EntrantIds.Contains(entrant.Id) && !x.Id.Contains("preview")))
                    {
                        var opponent = set.Entrants.Where(x => x.Id != entrant.Id).FirstOrDefault();

                        if (opponent?.Id == null)
                        {
                            continue;
                        }

                        var opponentEntrant = tournament.Item2.Where(x => x.Id == opponent.Id).FirstOrDefault();

                        if (opponentEntrant == null)
                        {
                            continue;
                        }

                        var opponentSlug = opponentEntrant.UserSlugs.First().Split('/')[1];

                        if (set.WinnerId == entrant.Id)
                        {
                            headToHead[entrantSlug][opponentSlug].Wins += 1;
                            headToHead[entrantSlug][opponentSlug].WinsDetails.Add($"https://start.gg/{tournament.Item1.Slug}/set/{set.Id}");
                        } else if (set.WinnerId == opponentEntrant.Id)
                        {
                            headToHead[entrantSlug][opponentSlug].Losses += 1;
                            headToHead[entrantSlug][opponentSlug].LossesDetails.Add($"https://start.gg/{tournament.Item1.Slug}/set/{set.Id}");
                        }
                    }
                }
            }

            var headToHeadFixed = headToHead.ToDictionary(x => Players[x.Key], x => x.Value.ToDictionary(y => Players[y.Key], y => y.Value));

            foreach (var key in headToHeadFixed.Keys)
            {
                var playerOpponentList = headToHeadFixed[key].Keys.ToList();

                headToHeadFixed[key]["Overall"] = new HeadToHead();

                foreach (var opp in playerOpponentList)
                {
                    headToHeadFixed[key]["Overall"].Wins += headToHeadFixed[key][opp].Wins;
                    headToHeadFixed[key]["Overall"].Losses += headToHeadFixed[key][opp].Losses;
                }
            }

            headToHeadFixed.OrderBy(x => x.Value["Overall"].Wins / (x.Value["Overall"].Wins + x.Value["Overall"].Losses));

            var headToHeadKeysOrder = headToHeadFixed.Keys.ToList();

            foreach (var key in headToHeadKeysOrder)
            {
                var temp = new Dictionary<string, HeadToHead>();

                foreach (var oppKey in headToHeadKeysOrder)
                {
                    temp[oppKey] = headToHeadFixed[key][oppKey];
                }

                temp["Overall"] = headToHeadFixed[key]["Overall"];

                headToHeadFixed[key] = temp;
            }

            var playerEventPerformances = Players.ToDictionary(x => x.Key, x => new List<PlayerEventPerformance>());

            foreach (var tournament in _goods)
            {
                foreach (var entrant in tournament.Item2)
                {
                    var entrantSlug = entrant.UserSlugs.First().Split('/')[1];
                    var entrantPerformance = new PlayerEventPerformance()
                    {
                        Seed = entrant.Seeding ?? entrant.InitialSeedNum.Value,
                        TournamentName = tournament.Item1.TournamentName,
                        Placement = entrant.Standing ?? -1,
                        EntrantTournamentUrl = $"https://start.gg/{tournament.Item1.Slug}/entrant/{entrant.Id}"
                    };

                    foreach (var set in tournament.Item3.Where(x => x.EntrantIds.Contains(entrant.Id) && !x.Id.Contains("preview")))
                    {
                        var opponent = set.Entrants.Where(x => x.Id != entrant.Id).FirstOrDefault();

                        if (opponent?.Id == null)
                        {
                            continue;
                        }

                        var eventOpponent = new PlayerEventOpponent()
                        {
                            PlayerName = opponent.Name,
                            Seed = opponent.InitialSeedNum.Value
                        };

                        if (set.WinnerId == entrant.Id)
                        {
                            entrantPerformance.Wins.Add(eventOpponent);
                        }
                        else if (set.WinnerId == opponent.Id)
                        {
                            entrantPerformance.Losses.Add(eventOpponent);
                        }
                    }

                    entrantPerformance.Wins = entrantPerformance.Wins.OrderBy(x => x.Seed).ToList();
                    entrantPerformance.Losses = entrantPerformance.Losses.OrderBy(x => x.Seed).ToList();

                    playerEventPerformances[entrantSlug].Add(entrantPerformance);
                }
            }

            return new PRDataModel()
            {
                RankingEvents = rankingEvents,
                HeadToHead = headToHeadFixed,
                PlayerEventPerformances = playerEventPerformances.ToDictionary(x => Players[x.Key], x => x.Value)
            };
        }
    }
}

public class HeadToHead
{
    public int Wins { get; set; } = 0;
    public List<string> WinsDetails { get; set; } = new List<string>();
    public int Losses { get; set; } = 0;
    public List<string> LossesDetails { get; set; } = new List<string>();
}

public class RankingEvent
{
    public string Date { get; set; }
    public string EventName { get; set; }
    public int RankingConsideredPlayers { get; set; }
    public int NumEntrants { get; set; }
    public string Link { get; set; }
    public Dictionary<string, int> Placements { get; set; }
}

public class PlayerEventPerformance
{
    public int Seed { get; set; }
    public int Placement { get; set; }
    public string TournamentName { get; set; }
    public string EntrantTournamentUrl { get; set; }
    public List<PlayerEventOpponent> Wins { get; set; } = new List<PlayerEventOpponent> { };
    public List<PlayerEventOpponent> Losses { get; set; } = new List<PlayerEventOpponent> { };
}

public class PlayerEventOpponent
{
    public string PlayerName { get; set; }
    public int Seed { get; set; }
}