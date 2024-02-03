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
    slug
    name
    images {
      ratio
      type
      url
    }
    events{
      id
      images {
        ratio
        type
        url
      }
      name
      phases{
        name
        id
      }
    }
    streams {
      streamName
      streamSource
    }
  }
}
         */
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