using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class ZookaloozaController : Controller
    {
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "I believe in Zookalooza Supremacy";

            return View();
        }
    }
}
