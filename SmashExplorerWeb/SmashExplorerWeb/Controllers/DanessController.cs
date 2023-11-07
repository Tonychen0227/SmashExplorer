using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class DanessController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var eventId = await StartGGDatabase.Instance.GetEventId(id);
            var phaseId = await StartGGDatabase.Instance.GetPhaseId(eventId, "Swiss");
            var httpResponse = await StartGGDatabase.Instance.GetSetsInPhase(eventId, phaseId);

            var sets = httpResponse.Data.Event.Sets.Nodes;
            var setsByRound = new Dictionary<int, List<StartGGSet>>();
            var entrantsWins = new Dictionary<string, (StartGGEntrant Entrant, int Wins, List<string> Opponents)>();

            var cachedEntrantSeeding = await SmashExplorerDatabase.Instance.GetCachedEntrantSeeding(eventId);

            if (cachedEntrantSeeding == null)
            {
                cachedEntrantSeeding = new Dictionary<string, (string Name, int Seeding)>();

                foreach (var roundOneSet in httpResponse.Data.Event.Sets.Nodes.Where(x => x.Round == 1))
                {
                    foreach (var setSlot in roundOneSet.Slots)
                    {
                        cachedEntrantSeeding[setSlot.Entrant.Id] = (setSlot.Entrant.Name, setSlot.Entrant.Seeds.First().SeedNum);
                    }
                }
            }

            foreach (var set in sets)
            {
                if (!setsByRound.ContainsKey(set.Round))
                {
                    setsByRound[set.Round] = new List<StartGGSet>();
                }

                set.Slots = set.Slots.Select(x => new StartGGSetSlot()
                {
                    Entrant = new StartGGEntrant()
                    {
                        Name = x.Entrant.Name,
                        Id = x.Entrant.Id,
                        Seeds = new List<StartGGEntrantSeed>()
                        {
                            new StartGGEntrantSeed()
                            {
                                SeedNum = cachedEntrantSeeding[x.Entrant.Id].Seeding
                            }
                        }
                    }
                }).ToList();

                setsByRound[set.Round].Add(set);
            }

            var lastCompletedSwissRound = 0;

            while (true)
            {
                var currentSwissRound = lastCompletedSwissRound + 1;
                
                if (!setsByRound.ContainsKey(currentSwissRound))
                {
                    break;
                }

                if (setsByRound[currentSwissRound].Any(x => x.WinnerId == null))
                {
                    break;
                }

                lastCompletedSwissRound++;
            }

            var currentProcessingRound = 1;

            while (currentProcessingRound <= lastCompletedSwissRound)
            {
                var setsInRound = setsByRound[currentProcessingRound];

                foreach (var set in setsInRound)
                {
                    foreach (var slot in set.Slots)
                    {
                        if (!entrantsWins.ContainsKey(slot.Entrant.Id))
                        {
                            entrantsWins[slot.Entrant.Id] = (slot.Entrant, 0, new List<string>());
                        }

                        entrantsWins[slot.Entrant.Id].Opponents.AddRange(set.Slots.Where(x => x.Entrant.Id != slot.Entrant.Id).Select(x => x.Entrant.Id));
                    }

                    entrantsWins[set.WinnerId] = (entrantsWins[set.WinnerId].Entrant, entrantsWins[set.WinnerId].Wins + 1, entrantsWins[set.WinnerId].Opponents);
                }

                currentProcessingRound++;
            }

            var entrantsByWins = new Dictionary<int, List<StartGGEntrant>>();

            foreach (var entrantWin in entrantsWins)
            {
                if (!entrantsByWins.ContainsKey(entrantWin.Value.Wins))
                {
                    entrantsByWins[entrantWin.Value.Wins] = new List<StartGGEntrant>();
                }

                entrantsByWins[entrantWin.Value.Wins].Add(entrantWin.Value.Entrant);
            }

            var pairings = new Dictionary<int, List<(StartGGEntrant, StartGGEntrant)>>();

            if (lastCompletedSwissRound < 5)
            {
                foreach (var winCount in entrantsByWins.Keys.OrderByDescending(x => x))
                {
                    pairings[winCount] = GetSwissPairings(
                        entrantsByWins[winCount].OrderBy(x => x.Seeds.First().SeedNum).ToList(),
                        entrantsWins.Select(
                            (kvPair) => new KeyValuePair<string, List<string>>(kvPair.Key, kvPair.Value.Opponents)).ToDictionary(x => x.Key, x => x.Value));
                }
            }
            else
            {
                var mainBracket = new List<StartGGEntrant>();
                var redemption = new List<StartGGEntrant>();
                var entrantOpponents = new Dictionary<string, List<string>>();

                foreach (var entrantWins in entrantsWins)
                {
                    entrantOpponents[entrantWins.Key] = entrantWins.Value.Opponents;

                    if (entrantWins.Value.Wins >= 3)
                    {
                        mainBracket.Add(entrantWins.Value.Entrant);
                    } else
                    {
                        redemption.Add(entrantWins.Value.Entrant);
                    }
                }

                mainBracket = mainBracket
                    .OrderByDescending(x => entrantsWins[x.Id].Wins)
                    .ThenByDescending(x => entrantOpponents[x.Id].Sum(o => entrantsWins[o].Wins))
                    .ToList();

                redemption = redemption
                    .OrderByDescending(x => entrantsWins[x.Id].Wins)
                    .ThenByDescending(x => entrantOpponents[x.Id].Sum(o => entrantsWins[o].Wins))
                    .ToList();

                pairings[420] = GetBracketPairings(mainBracket, entrantOpponents);
                pairings[69] = GetBracketPairings(redemption, entrantOpponents);
            }

            if (lastCompletedSwissRound == 1)
            {
                await SmashExplorerDatabase.Instance.UpsertDanessSeeding(eventId, cachedEntrantSeeding);
            }

            return View(new DanessModel()
            {
                StartGGEventResponse = httpResponse.Data,
                LastCompletedSwissRound = lastCompletedSwissRound,
                Pairings = pairings,
                CachedEntrantSeeding = cachedEntrantSeeding
            });
        }

        private List<(StartGGEntrant, StartGGEntrant)> GetSwissPairings(List<StartGGEntrant> entrants, Dictionary<string, List<string>> entrantOpponents)
        {
            var entrantsToOrderMap = new Dictionary<string, int>();

            for (var currentIndex = 0; currentIndex < entrants.Count; currentIndex++)
            {
                entrantsToOrderMap[entrants[currentIndex].Id] = currentIndex;
            }

            var firstHalfEntrants = entrants.OrderBy(x => x.Seeds.First().SeedNum).Take(entrants.Count / 2);
            var secondHalfEntrants = entrants.OrderByDescending(x => x.Seeds.First().SeedNum).Take(entrants.Count / 2);

            var count = entrants.Count();
            var averageDistance = count / 2;
            var random = new Random();

            var validPairingSets = new List<List<(StartGGEntrant, StartGGEntrant)>>();

            for (var x = 0; x < 500; x++)
            {
                var currentPairings = new List<(StartGGEntrant, StartGGEntrant)>();
                var eligiblePairings = new Dictionary<string, List<StartGGEntrant>>();

                foreach (var firstHalfEntrant in firstHalfEntrants)
                {
                    eligiblePairings[firstHalfEntrant.Id] = secondHalfEntrants.Where(e => !entrantOpponents[firstHalfEntrant.Id].Contains(e.Id)).ToList();
                    if (eligiblePairings[firstHalfEntrant.Id].Count == 0)
                    {
                        eligiblePairings[firstHalfEntrant.Id] = firstHalfEntrants.Where(e => !entrantOpponents[firstHalfEntrant.Id].Contains(e.Id) && e.Id != firstHalfEntrant.Id).ToList();
                    }
                }

                foreach (var secondHalfEntrant in secondHalfEntrants)
                {
                    eligiblePairings[secondHalfEntrant.Id] = firstHalfEntrants.Where(e => !entrantOpponents[secondHalfEntrant.Id].Contains(e.Id)).ToList();
                    if (eligiblePairings[secondHalfEntrant.Id].Count == 0)
                    {
                        eligiblePairings[secondHalfEntrant.Id] = secondHalfEntrants.Where(e => !entrantOpponents[secondHalfEntrant.Id].Contains(e.Id) && e.Id != secondHalfEntrant.Id).ToList();
                    }
                }

                while (eligiblePairings.Count > 0)
                {
                    var leftEntrantId = eligiblePairings.Keys.ToList()[random.Next(eligiblePairings.Keys.Count)];

                    if (eligiblePairings.Any(pairings => pairings.Value.Count == 1))
                    {
                        leftEntrantId = eligiblePairings.First(pairings => pairings.Value.Count == 1).Key;
                    }

                    var leftEntrantEligiblePairings = eligiblePairings[leftEntrantId];

                    if (leftEntrantEligiblePairings.Count == 0)
                    {
                        break;
                    }

                    var rightEntrantId = leftEntrantEligiblePairings[random.Next(leftEntrantEligiblePairings.Count)].Id;

                    var leftEntrant = entrants.Where(e => e.Id == leftEntrantId).First();
                    var rightEntrant = entrants.Where(e => e.Id == rightEntrantId).First();

                    if (leftEntrant.Seeds.First().SeedNum > rightEntrant.Seeds.First().SeedNum)
                    {
                        currentPairings.Add((rightEntrant, leftEntrant));
                    } else
                    {
                        currentPairings.Add((leftEntrant, rightEntrant));
                    }

                    eligiblePairings.Remove(leftEntrant.Id);
                    eligiblePairings.Remove(rightEntrant.Id);

                    foreach (var eligiblePairing in eligiblePairings)
                    {
                        eligiblePairing.Value.Remove(leftEntrant);
                        eligiblePairing.Value.Remove(rightEntrant);
                    }
                }

                if (eligiblePairings.Count == 0)
                {
                    validPairingSets.Add(currentPairings);
                }
            }

            Func<List<(StartGGEntrant, StartGGEntrant)>, double> minimumGap = (pairingSet) =>
            {
                return pairingSet.Select(x => Math.Abs(entrantsToOrderMap[x.Item1.Id] - entrantsToOrderMap[x.Item2.Id])).ToList().Min();
            };

            return validPairingSets
                .OrderByDescending(set => minimumGap(set))
                .First()
                .OrderBy(x => x.Item1.Seeds.First().SeedNum)
                .ToList();
        }

        private List<(StartGGEntrant, StartGGEntrant)> GetBracketPairings(List<StartGGEntrant> entrants, Dictionary<string, List<string>> entrantOpponents)
        {
            var currentPairings = new List<(StartGGEntrant, StartGGEntrant)>();
            var entrantIds = entrants.Select(x => x.Id).ToList();

            var entrantsToOrderMap = new Dictionary<string, int>();
            for (var currentIndex = 0; currentIndex < entrants.Count; currentIndex++)
            {
                entrantsToOrderMap[entrants[currentIndex].Id] = currentIndex;
            }

            var eligiblePairings = new Dictionary<string, List<string>>();

            foreach (var entrantId in entrantIds)
            {
                eligiblePairings[entrantId] = entrantIds.Where(x => !entrantOpponents[entrantId].Contains(x)).ToList();
            };

            while (entrantIds.Count > 0) {
                var currentId = entrantIds[0];
                if (eligiblePairings.Any(x => x.Value.Count <= 2))
                {
                    currentId = eligiblePairings.OrderBy(x => x.Value.Count).First().Key;
                }

                var opponentId = eligiblePairings[currentId].OrderByDescending(x => Math.Abs(entrantsToOrderMap[currentId] - entrantsToOrderMap[x])).First();

                currentPairings.Add((entrants.First(x => x.Id == currentId), entrants.First(x => x.Id == opponentId)));
                
                entrantIds.Remove(currentId);
                entrantIds.Remove(opponentId);
                eligiblePairings.Remove(currentId);
                eligiblePairings.Remove(opponentId);

                foreach (var eligiblePairing in eligiblePairings)
                {
                    eligiblePairing.Value.Remove(currentId);
                    eligiblePairing.Value.Remove(opponentId);
                }
            }

            currentPairings.OrderBy(x => Math.Min(entrantsToOrderMap[x.Item1.Id], entrantsToOrderMap[x.Item2.Id]));

            var finalPairings = new List<(StartGGEntrant, StartGGEntrant)>();

            while (currentPairings.Count > 0)
            {
                finalPairings.Add(currentPairings.First());
                finalPairings.Add(currentPairings.Last());

                currentPairings.Remove(currentPairings.First());
                currentPairings.Remove(currentPairings.Last());
            }

            return finalPairings;
        }
    }
}
