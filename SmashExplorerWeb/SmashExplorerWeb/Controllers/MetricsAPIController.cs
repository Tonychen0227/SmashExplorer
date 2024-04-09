using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class MetricsAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id, bool prettify = false)
        {
            var hoursBack = string.IsNullOrEmpty(id) ? 1 : int.Parse(id);

            if (hoursBack > 144)
            {
                hoursBack = 144;
            }

            var metrics = await SmashExplorerDatabase.Instance.GetMetricsAsync(hoursBack);

            var summaryMetric = metrics.Aggregate(metrics.First(), (acc, x) => acc.Consolidate(x));

            summaryMetric.Consolidate(MetricsManager.Instance.CurrentModel);

            if (prettify)
            {
                return View(summaryMetric);
            }
            else
            {
                return Content(JsonConvert.SerializeObject(summaryMetric), "application/json");
            }
        }
    }
}
