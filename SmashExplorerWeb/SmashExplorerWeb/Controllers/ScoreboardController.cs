using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class ScoreboardController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id, bool shouldRefresh = false)
        {
            ViewBag.Title = "Scoreboard";

            var scoreboard = await SmashExplorerDatabase.Instance.GetScoreboardAsync(id);

            return View(new ScoreboardModel(scoreboard) { ShouldRefresh = shouldRefresh });
        }

        [HttpPost]
        public async Task<ActionResult> Index(string id, ScoreboardModel scoreboardModel)
        {
            await SmashExplorerDatabase.Instance.AddLogToScoreboardAsync(id, scoreboardModel.NextLog);

            return RedirectToAction("Index", "Scoreboard", new { id = id });
        }
    }
}