using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class EventSetsController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var sets = await SmashExplorerDatabase.Instance.GetSetsAsync(id);

            Dictionary<(string, string, string), Set> foo = new Dictionary<(string, string, string), Set>();

            foreach (var set in sets)
            {
                var key = (set.PhaseGroupId, set.PhaseId, set.Identifier);

                if (foo.ContainsKey(key))
                {
                    if (foo[key].Timestamp < set.Timestamp)
                    {
                        foo[key] = set;
                    }
                }
                else
                {
                    foo[key] = set;
                }
            }

            var curatedSets = foo.Values.ToList();

            curatedSets = curatedSets
                .OrderBy(x => x.PhaseOrder)
                .ThenBy(x => x.PhaseIdentifier)
                .ThenBy(x => Math.Abs(x.Round ?? -1)).ToList();

            var ret = new Dictionary<string, List<EmergencySet>>();

            foreach (var set in curatedSets)
            {
                var key = $"{set.PhaseName} - {set.PhaseIdentifier}";

                if (!ret.ContainsKey(key))
                {
                    ret[key] = new List<EmergencySet>();
                }

                var actualSet = new EmergencySet()
                {
                    Id = set.Id,
                    Round = set.Round,
                    WinnerId = set.WinnerId,
                    DisplayScore = set.DisplayScore,
                    Identifier = set.Identifier,
                    PhaseIdentifier = set.PhaseIdentifier,
                    FullRoundText = set.FullRoundText,
                    DetailedScore = set.DetailedScore,
                    Entrants = set.Entrants.Select(x => new EmergencyEntrant()
                    {
                        Id = x.Id,
                        InitialSeedNum = x.InitialSeedNum,
                        Name = x.Name,
                        PreReqId = x.PrereqId,
                        PreReqType = x.PrereqType
                    }).ToList(),
                    PhaseId = set.PhaseId,
                    PhaseName = set.PhaseName
                };

                ret[key].Add(actualSet);
            }

            return View(new EventSetsModel()
            {
                Sets = ret,
                TournamentEvent = await SmashExplorerDatabase.Instance.GetEventAsync(id)
            });
        }
    }
}

