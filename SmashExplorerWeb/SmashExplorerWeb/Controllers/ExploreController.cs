using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class ExploreController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Smash Explorer - Explore {id}";

            return View();
        }
    }
}
