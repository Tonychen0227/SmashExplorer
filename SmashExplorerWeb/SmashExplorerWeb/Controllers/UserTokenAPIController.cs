using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class UserTokenAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            MetricsManager.Instance.AddLogin(id);

            var retObject = await StartGGDatabase.Instance.GetUserTokenDetails(id);
            retObject.Token = id.GetSha256Hash();

            System.Diagnostics.Trace.TraceInformation($"Saved token details for user with token {id}");

            await SmashExplorerDatabase.Instance.UpsertGalintAuthenticationTokenAsync(retObject);

            return Content(JsonConvert.SerializeObject(retObject), "application/json");
        }
    }
}