using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class OnboardNewGalintTournamentAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = "Onboard Galint Tournament API";

            var startGGData = await StartGGDatabase.Instance.GetTournament(id);

            await SmashExplorerDatabase.Instance.AddCurrentTournamentAndSetAsActiveAsync(startGGData.Data.Tournament);

            return new HttpStatusCodeResult(System.Net.HttpStatusCode.Created);
        }
    }
}