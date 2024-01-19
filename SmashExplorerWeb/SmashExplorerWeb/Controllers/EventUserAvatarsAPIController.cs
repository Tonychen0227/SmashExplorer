using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class EventUserAvatarsAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            return Content(JsonConvert.SerializeObject(await AvatarsDatabase.Instance.GetAvatars(id)), "application/json");
        }
    }
}