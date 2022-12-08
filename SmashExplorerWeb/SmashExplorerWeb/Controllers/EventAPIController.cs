using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class EventAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = "Event API";

            var retrievedEvent = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            if (retrievedEvent == null)
            {
                return new HttpNotFoundResult("Event not found");
            }

            return Content(JsonConvert.SerializeObject(retrievedEvent), "application/json");
        }
    }
}