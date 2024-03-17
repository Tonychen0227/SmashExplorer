using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class CurrentTournamentAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Current Tournament API";

            var retrievedEvent = await SmashExplorerDatabase.Instance.GetCurrentTournamentsAsync();

            if (retrievedEvent == null)
            {
                return new HttpNotFoundResult("Event not found");
            }

            return Content(JsonConvert.SerializeObject(retrievedEvent), "application/json");
        }
    }
}