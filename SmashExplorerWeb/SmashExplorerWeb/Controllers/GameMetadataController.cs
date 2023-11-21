using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class GameMetadataController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            return Content(JsonConvert.SerializeObject(await StartGGDatabase.Instance.GetVideogameDetails(id)), "application/json");
        }
    }
}
