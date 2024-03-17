using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class HistoricalMatchesAPIController : Controller
    {
        private readonly Cache<Event> EventCache = new Cache<Event>(int.MaxValue);
        private readonly Cache<List<Entrant>> EntrantCache = new Cache<List<Entrant>>(int.MaxValue);
        private readonly Cache<List<Set>> SetCache = new Cache<List<Set>>(int.MaxValue);

        private readonly static List<string> EventIds = new List<string>()
        {
            #region Melee 2022
            // Scuffed World Tour
            "831001",
            // Mainstage 2022
            "719555",
            // Apex 2022
            "692989",
            // Smash Summit 14
            "792141",
            // Ludwig Smash Invitational
            "772879",
            // The Big House 10
            "634112",
            // Lost Tech City 2022
            "719950",
            // Riptide 2022
            "700420",
            // Shine 2022
            "630208",
            // Super Smash Con 2022
            "637610",
            // Double Down 2022
            "657298",
            // Get On My Level 2022
            "690635",
            // Battle of BC 4
            "684275",
            // Smash Summit 13
            "721901",
            // Pound 2022
            "679049",
            // Genesis 8
            "400200",
            // Ludwig Ahgren Championship Series 4
            "670531",
            // Smash Factor 9
            "692413",
            // CEO 2022
            "678464",
            // Low Tide City 2022
            "622417",
            #endregion Melee 2022


            #region Melee 2023
            // MAJOR UPSET
            "816678",
            // Collision 2023
            "790934",
            // GENESIS 9
            "769490",
            // KOWLOON 5
            "850304",
            // LVL UP EXPO 2023
            "799917",
            #endregion Melee 2023


            #region Ultimate 2022
            // JAPAN 24
            "834678",
            // Scuffed World Tour
            "831002",
            // Mainstage 2022
            "715155",
            // Seibugeki 12
            "802437",
            // Port Priority 7
            "757796",
            // Ultimate Fighting Arena 2022
            "750059",
            // Ludwig Smash Invitational
            "772884",
            // L"Odyssee
            "795765",
            // MaesumaTOP 10
            "777607",
            // The Big House 10
            "634120",
            // Vienna Challengers Arena 2022
            "735271",
            // Apex 2022
            "692991",
            // Lost Tech City 2022
            "719947",
            // Riptide 2022
            "700422",
            // Shine 2022
            "630206",
            // Smash Ultimate Summit 5
            "751186",
            // Ultimate Wanted 4
            "751778",
            // Rise N Grind
            "591286",
            // Super Smash Con 2022
            "637617",
            // Kagaribi 8
            "731197",
            // Smash Factor 9
            "692411",
            // Double Down 2022
            "657322",
            // Colossel 2022
            "425950",
            // Get On My Level 2022
            "690637",
            // 95 Kings of Fields 2
            "699201",
            // CEO 2022
            "672288",
            // e-Caribana 2022 Invitational
            "735352",
            // e-Caribana 2022 Open 
            "735350",
            // Crown 2
            "589054",
            // Gimvitational
            "718327",
            // MaesumaTOP 8
            "724817",
            // Battle of BC 4
            "684278",
            // MomoCon 2022
            "691230",
            // Kagaribi 7
            "658860",
            // MaesumaTOP 7
            "696442",
            // Low Tide City 2022
            "622416",
            // Pound 2022
            "679057",
            // GENESIS 8
            "400198",
            // Collision 2022
            "598666",
            // Smash Ultimate Summit 4
            "671601",
            // Glitch: Infinite
            "620048",
            // Kagaribi 6
            "649141",
            // Let"s make Big Moves 2022
            "582812",
            #endregion Ultimate 2022

            #region Ultimate 2023
            // Kagaribi 10
            "892118",
            // Delta 4
            "891100",
            // MaesumaTOP 12
            "881721",
            // WAVE 4
            "870180",
            // KOWLOON 5
            "850303",
            // MAJOR UPSET
            "805941",
            // Smash Ultimate Summit 6
            "848958",
            // Seibugeki 13
            "859296",
            // Collision 2023
            "790932",
            // MaesumaTOP 11
            "856949",
            // LVL UP EXPO 2023
            "799749",
            // Kagaribi 9
            "841253",
            // GENESIS 9
            "769488",
            // Umebura SP9
            "813968",
            // Let"s make big moves 2023
            "728892"
            #endregion Ultimate 2023
        };

        private readonly static List<string> BattleOfBC6EventIds = new List<string>() { "999586", "999585" };

        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = "Production Thing API";

            var isBOBC = BattleOfBC6EventIds.Contains(id);

            Event eventData;
            IEnumerable<Entrant> entrants;
            IEnumerable<Set> sets;

            if (isBOBC)
            {
                eventData = await SmashExplorerDatabase.Instance.GetEventAsync(id);
                eventData.Standings = null;

                entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);
                sets = await SmashExplorerDatabase.Instance.GetSetsAsync(id);
            }
            else
            {
                if (EventCache.ContainsKey(id))
                {
                    eventData = EventCache.GetFromCache(id);
                }

                var apiCall = await SmashExplorerDatabase.Instance.GetEventAsync(id);

                apiCall.Standings = null;

                EventCache.SetCacheObject(id, apiCall);

                eventData = apiCall;

                if (EntrantCache.ContainsKey(id))
                {
                    entrants = EntrantCache.GetFromCache(id);
                }

                var entrantsApiCall = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);

                EntrantCache.SetCacheObject(id, entrantsApiCall);

                entrants = entrantsApiCall;

                if (SetCache.ContainsKey(id))
                {
                    sets = SetCache.GetFromCache(id);
                }

                var setsApiCall = await SmashExplorerDatabase.Instance.GetSetsAsync(id);

                SetCache.SetCacheObject(id, setsApiCall);

                sets = setsApiCall;
            }

            sets = sets.Where(x => x.DisplayScore != "DQ" && !string.IsNullOrEmpty(x.WinnerId) && x.WinnerId != "None");

            List<Set> goodSets = new List<Set>();

            foreach (var set in sets)
            {
                try
                {
                    if (set.EntrantIds.Any(x => !entrants.Any(entrant => entrant.Id == x)))
                    {
                        continue;
                    }

                    var entrantSlugs = set.EntrantIds.Select(x => entrants.First(e => e.Id == x)).Select(x => x.UserSlugs?.FirstOrDefault());

                    if (entrantSlugs.Any(x => string.IsNullOrEmpty(x)))
                    {
                        continue;
                    }

                    goodSets.Add(set);
                } catch (Exception e)
                {
                    continue;
                }
            }

            var ret = new
            {
                Events = new
                {
                    Id = eventData.Id,
                    Name = eventData.Name,
                    TournamentLocation = eventData.TournamentLocation,
                    StartAt = eventData.StartAt,
                    CreatedAt = eventData.CreatedAt,
                    NumEntrants = eventData.NumEntrants
                },
                Entrants = entrants.Select(entrant =>
                {
                    return new
                    {
                        Id = entrant.Id,
                        Slugs = entrant.UserSlugs,
                        Name = entrant.Name,
                        Standing = entrant.Standing,
                        Seeding = entrant.Seeding
                    };
                }),
                Sets = goodSets.Select(set =>
                {
                    return new
                    {
                        set.Id,
                        set.DisplayScore,
                        set.WinnerId,
                        set.CompletedAt,
                        Entrants = set.Entrants.Select(se =>
                        {
                            return new
                            {
                                se.Id,
                                se.InitialSeedNum,
                                se.Name
                        };
                        }),
                        set.EntrantIds,
                        EntrantSlugs = set.EntrantIds.Select(x => entrants.First(e => e.Id == x)).Select(x => x.UserSlugs?.FirstOrDefault()),
                        set.DetailedScore,
                        set.IsUpsetOrNotable
                    };
                })
            };

            return Content(JsonConvert.SerializeObject(ret), "application/json");
        }
    }
}