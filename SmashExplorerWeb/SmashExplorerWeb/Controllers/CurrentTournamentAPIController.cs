using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class CurrentTournamentAPIController : Controller
    {
        /*QUERY FROM START GG
query ReportBracketSet {
  tournament(slug:"tournament/tony-chen-test-2-1"){
    id
    events{
      id
      name
      phases{
        name
        id
      }
    }
  }
}
         */
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Current Tournament API";

            var retrievedEvent = await SmashExplorerDatabase.Instance.GetTournamentsAsync();

            if (retrievedEvent == null)
            {
                return new HttpNotFoundResult("Event not found");
            }

            return Content(JsonConvert.SerializeObject(retrievedEvent), "application/json");
        }
    }
}