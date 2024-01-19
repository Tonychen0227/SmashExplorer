using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SetAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var set = await SmashExplorerDatabase.Instance.GetSetAsync(id);
            var eventReportedMatches = SmashExplorerDatabase.Instance.GetEventReportedSets(set.EventId);

            if (eventReportedMatches != null && eventReportedMatches.Keys.Contains(set.Id))
            {
                set.ReportedScoreViaAPI = eventReportedMatches[set.Id].Item1;
            }

            return Content(JsonConvert.SerializeObject(await SmashExplorerDatabase.Instance.GetSetAsync(id)), "application/json");
        }
    }
}

