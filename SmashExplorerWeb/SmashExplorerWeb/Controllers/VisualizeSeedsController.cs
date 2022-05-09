using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class VisualizeSeedsController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Visualize Seeds for event {id}";

            return View();
        }
    }
}
