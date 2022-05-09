using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class UpsetsController : Controller
    {
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Upsets";

            return View();
        }
    }
}
