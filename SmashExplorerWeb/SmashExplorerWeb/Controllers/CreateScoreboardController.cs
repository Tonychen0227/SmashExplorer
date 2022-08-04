using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class CreateScoreboardController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Create Scoreboard";

            return View(new CreateScoreboardModel());
        }

        [HttpPost]
        public async Task<ActionResult> Index(CreateScoreboardModel model)
        {
            var newModel = await SmashExplorerDatabase.Instance.CreateScoreboardAsync(model);

            return RedirectToAction("Index", "Scoreboard", new { id = newModel.Id, shouldRefresh = true });
        }
    }
}