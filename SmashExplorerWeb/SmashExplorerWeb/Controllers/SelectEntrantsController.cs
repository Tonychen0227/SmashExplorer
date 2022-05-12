using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SelectEntrantsController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Smash Explorer - Select Entrants {id}";

            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            return View();
        }
    }
}
