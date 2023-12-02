using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class TournamentAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var retObject = await StartGGDatabase.Instance.GetUserTokenDetails(id);
            retObject.Token = id.GetSha256Hash();

            await SmashExplorerDatabase.Instance.UpsertGalintAuthenticationTokenAsync(retObject);

            return Content(JsonConvert.SerializeObject(retObject), "application/json");
        }
    }
}