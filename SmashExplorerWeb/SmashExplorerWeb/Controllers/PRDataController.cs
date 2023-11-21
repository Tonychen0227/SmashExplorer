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

    public class PRDataController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            id = id.ToLower();

            var existingData = await SmashExplorerDatabase.Instance.GetPRDataset(id);
            List<(Event Event, List<Entrant> RankingConsideredPlayers, List<Entrant> TopPlacingPlayers, List<Set> Sets)> data = null;

            if (existingData == null)
            {
                return new HttpNotFoundResult();
            }

            var tasks = existingData.Events.Select(slug => SmashExplorerDatabase.Instance.GetEventsBySlugAndDatesAsync(slug, DateTime.UtcNow.Subtract(TimeSpan.FromDays(120)), DateTime.UtcNow));
            var tournaments = await Task.WhenAll(tasks);

            var tournamentsList = tournaments.Select(x => x.First());

            data = (await Task.WhenAll(
                tournamentsList.Select(async tournament => (
                    tournament,
                    (await SmashExplorerDatabase.Instance.GetEntrantsAsync(tournament.Id, true)).ToList(),
                    await SmashExplorerDatabase.Instance.GetSetsAsync(tournament.Id, true))))).Select(x => (
                        x.tournament,
                        x.Item2.Where(e => existingData.Players.Keys.Any(k => e.UserSlugs.Contains($"user/{k}"))).Where(e => e.IsDisqualified == null || !e.IsDisqualified.Value).ToList(),
                        x.Item2,
                        x.Item3)).ToList();

            var model = GetRankingsStructFromData(existingData, data);

            return View(model);
        }

        private static PRDataModel GetRankingsStructFromData(PRDataSet dataSet, List<(Event Event, List<Entrant> RankingConsideredPlayers, List<Entrant> TopPlacingPlayers, List<Set> Sets)> data)
        {
            var rankingEvents = data.Select((good) => {
                var topEntrantStanding = GetTournamentTopEntrantsMinimumStanding(good);
                var players = good.TopPlacingPlayers
                    .Where(x => x.Standing <= GetTournamentTopEntrantsMinimumStanding(good)
                           && (x.UserSlugs.Count == 0 || !dataSet.Players.ContainsKey(x.UserSlugs.First().Split('/')[1])))
                    .ToDictionary(x => x.Name.Split('|').Last(), x => x.Standing.Value);

                good.RankingConsideredPlayers
                    .Select(x => (x.UserSlugs.First(), x.Standing))
                    .ToDictionary(x => dataSet.Players[x.Item1.Split('/')[1]], x => x.Standing.Value)
                    .ToList().ForEach(x => players.Add(x.Key, x.Value));

                return new RankingEvent()
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(good.Event.StartAt).DateTime.ToShortDateString(),
                    EventName = good.Event.TournamentName,
                    RankingConsideredPlayers = good.RankingConsideredPlayers.Count(),
                    NumEntrants = good.Event.NumEntrants,
                    Link = $"https://start.gg/{good.Event.Slug}",
                    Placements = players,
                    ColorCode = GetTournamentColorCodeAndScore(good).Color,
                    Score = GetTournamentColorCodeAndScore(good).Score
                };
            });

            var headToHead = dataSet.Players.ToDictionary(x => x.Key, x => dataSet.Players.ToDictionary(k => k.Key, k => new HeadToHead()));

            foreach (var tournament in data)
            {
                foreach (var entrant in tournament.RankingConsideredPlayers)
                {
                    var entrantSlug = entrant.UserSlugs.First().Split('/')[1];

                    foreach (var set in tournament.Sets.Where(x => x.EntrantIds.Contains(entrant.Id) && !x.Id.Contains("preview")))
                    {
                        var opponent = set.Entrants.Where(x => x.Id != entrant.Id).FirstOrDefault();

                        if (opponent?.Id == null)
                        {
                            continue;
                        }

                        var opponentEntrant = tournament.RankingConsideredPlayers.Where(x => x.Id == opponent.Id).FirstOrDefault();

                        if (opponentEntrant == null)
                        {
                            continue;
                        }

                        var opponentSlug = opponentEntrant.UserSlugs.First().Split('/')[1];

                        if (set.WinnerId == entrant.Id)
                        {
                            headToHead[entrantSlug][opponentSlug].Wins += 1;
                            headToHead[entrantSlug][opponentSlug].WinsDetails.Add($"https://start.gg/{tournament.Event.Slug}/set/{set.Id}");
                        }
                        else if (set.WinnerId == opponentEntrant.Id)
                        {
                            headToHead[entrantSlug][opponentSlug].Losses += 1;
                            headToHead[entrantSlug][opponentSlug].LossesDetails.Add($"https://start.gg/{tournament.Event.Slug}/set/{set.Id}");
                        }
                    }
                }
            }

            var headToHeadFixed = headToHead.ToDictionary(x => dataSet.Players[x.Key], x => x.Value.ToDictionary(y => dataSet.Players[y.Key], y => y.Value));

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

            var orderedByOverallWinRate = headToHeadFixed
                .ToDictionary(x => x.Key, x => x.Value["Overall"])
                .ToDictionary(x => x.Key, x => (x.Value.Wins, x.Value.Losses))
                .OrderByDescending(x => x.Value.Wins + x.Value.Wins > 5 ? 5 : 0)
                .ThenByDescending(x => (double) x.Value.Wins == 0 ? 0 : (double) x.Value.Wins / ((double) x.Value.Wins + (double) x.Value.Losses))
                .Select(x => x.Key);

            var finalHeadToHead = new Dictionary<string, Dictionary<string, HeadToHead>>();

            foreach (var key in orderedByOverallWinRate)
            {
                var temp = new Dictionary<string, HeadToHead>();

                foreach (var oppKey in orderedByOverallWinRate)
                {
                    temp[oppKey] = headToHeadFixed[key][oppKey];
                    temp[oppKey].ColorCode = GetHeadToHeadColorCode(temp[oppKey]);
                }

                temp["Overall"] = headToHeadFixed[key]["Overall"];

                finalHeadToHead[key] = temp;
            }

            var playerEventPerformances = dataSet.Players.ToDictionary(x => x.Key, x => new List<PlayerEventPerformance>());

            foreach (var tournament in data)
            {
                foreach (var entrant in tournament.RankingConsideredPlayers)
                {
                    var entrantSlug = entrant.UserSlugs.First().Split('/')[1];
                    var entrantPerformance = new PlayerEventPerformance()
                    {
                        Seed = entrant.Seeding ?? entrant.InitialSeedNum.Value,
                        TournamentName = tournament.Event.TournamentName,
                        Placement = entrant.Standing ?? -1,
                        TournamentColorCode = GetTournamentColorCodeAndScore(tournament).Color,
                        EntrantTournamentUrl = $"https://start.gg/{tournament.Event.Slug}/entrant/{entrant.Id}"
                    };

                    foreach (var set in tournament.Sets.Where(x => x.EntrantIds.Contains(entrant.Id) && !x.Id.Contains("preview")))
                    {
                        var opponent = set.Entrants.Where(x => x.Id != entrant.Id).FirstOrDefault();

                        if (opponent?.Id == null)
                        {
                            continue;
                        }

                        var eventOpponent = new PlayerEventOpponent()
                        {
                            PlayerName = opponent.Name.Split('|').Last(),
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
                Title = dataSet.Title,
                RankingEvents = rankingEvents,
                HeadToHead = finalHeadToHead,
                PlayerEventPerformances = playerEventPerformances.ToDictionary(x => dataSet.Players[x.Key], x => x.Value)
            };
        }

        private static (int Score, string Color) GetTournamentColorCodeAndScore((Event Event, List<Entrant> RankingConsideredPlayers, List<Entrant> TopPlacingPlayers, List<Set> Sets) entry)
        {
            var score = entry.Event.NumEntrants >= 40 ? (entry.Event.NumEntrants >= 54 ? 2 : 1) : 0;

            score += entry.RankingConsideredPlayers.Where(x => x.IsDisqualified == null || !x.IsDisqualified.Value).Count();

            var color = "#ffffff";

            if (score >= 10)
            {
                color = "#b592de";
            }
            else if (score >= 7)
            {
                color = "#2596be";
            }
            else if (score >= 4)
            {
                color = "#96be25";
            }

            return (score, color);
        }

        private static int GetTournamentTopEntrantsMinimumStanding((Event Event, List<Entrant> RankingConsideredPlayers, List<Entrant> TopPlacingPlayers, List<Set> Sets) entry)
        {
            var score = entry.Event.NumEntrants > 17 ? (entry.Event.NumEntrants > 54 ? 2 : 1) : 0;
            score = 0;

            score += entry.RankingConsideredPlayers.Where(x => x.IsDisqualified == null || !x.IsDisqualified.Value).Count();

            var topCut = 3;

            if (score >= 10)
            {
                topCut = 12;
            }
            else if (score >= 7)
            {
                topCut = 6;
            }
            else if (score >= 4)
            {
                topCut = 4;
            }

            return topCut;
        }

        private static string GetHeadToHeadColorCode(HeadToHead headToHead)
        {
            string backgroundColor;

            if ((headToHead.Wins + headToHead.Losses) == 0)
            {
                backgroundColor = "#b2beb5";
            }
            else if (headToHead.Wins == headToHead.Losses)
            {
                backgroundColor = "#f8de7e";
            }
            else if (headToHead.Wins < headToHead.Losses)
            {
                var diff = headToHead.Losses - headToHead.Wins;

                if (diff >= 3)
                {
                    backgroundColor = "#e42800";
                }
                else if (diff == 2)
                {
                    backgroundColor = "#f96241";
                }
                else
                {
                    backgroundColor = "#ff927a";
                }
            }
            else
            {
                var diff = headToHead.Wins - headToHead.Losses;

                if (diff >= 3)
                {
                    backgroundColor = "#2bed00";
                }
                else if (diff == 2)
                {
                    backgroundColor = "#7cff60";
                }
                else
                {
                    backgroundColor = "#baffab";
                }
            }

            return backgroundColor;
        }
    }
}

public class HeadToHead
{
    public int Wins { get; set; } = 0;
    public List<string> WinsDetails { get; set; } = new List<string>();
    public int Losses { get; set; } = 0;
    public string ColorCode { get; set; } = "ffffff";
    public List<string> LossesDetails { get; set; } = new List<string>();
}

public class RankingEvent
{
    public string Date { get; set; }
    public string EventName { get; set; }
    public int RankingConsideredPlayers { get; set; }
    public int NumEntrants { get; set; }
    public string Link { get; set; }
    public string ColorCode { get; set; }
    public int Score { get; set; }
    public Dictionary<string, int> Placements { get; set; }
}

public class PlayerEventPerformance
{
    public int Seed { get; set; }
    public int Placement { get; set; }
    public string TournamentName { get; set; }
    public string EntrantTournamentUrl { get; set; }
    public string TournamentColorCode { get; set; }
    public List<PlayerEventOpponent> Wins { get; set; } = new List<PlayerEventOpponent> { };
    public List<PlayerEventOpponent> Losses { get; set; } = new List<PlayerEventOpponent> { };
}

public class PlayerEventOpponent
{
    public string PlayerName { get; set; }
    public int Seed { get; set; }
}

public class PRDataSet
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string Title { get; set; }
    public Dictionary<string, string> Players { get; set; }
    public List<string> Events { get; set; }
}