using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SelectEntrantsController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Select Entrants for event {id}";

            return View();
        }
    }
}
