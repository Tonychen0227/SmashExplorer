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
            var retObject = await StartGGDatabase.Instance.GetUserTokenDetails(id);

            return Content(JsonConvert.SerializeObject(retObject), "application/json");
        }
    }
}