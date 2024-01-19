using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class EntrantMatchesAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            dynamic ret = new System.Dynamic.ExpandoObject();

            var entrant = await SmashExplorerDatabase.Instance.GetEntrantAsync(id);

            if (entrant != null)
            {
                var entrantMatches = await SmashExplorerDatabase.Instance.GetSetsAsync(entrant.EventId);
                var eventReportedMatches = SmashExplorerDatabase.Instance.GetEventReportedSets(entrant.EventId);

                foreach (var entrantMatch in entrantMatches)
                {
                    if (eventReportedMatches != null && eventReportedMatches.Keys.Contains(entrantMatch.Id))
                    {
                        entrantMatch.ReportedScoreViaAPI = eventReportedMatches[entrantMatch.Id].Item1;
                    }
                }

                ret = entrantMatches.Where(x => x.EntrantIds.Contains(id)).OrderBy(x => x.PhaseOrder).ThenBy(x => Math.Abs(x.Round ?? 999)).ToList();
            }

            return Content(JsonConvert.SerializeObject(ret), "application/json");
        }
    }
}

