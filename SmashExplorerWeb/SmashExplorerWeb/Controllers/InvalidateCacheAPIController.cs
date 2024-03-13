using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Mvc.HttpGetAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;

namespace SmashExplorerWeb.Controllers
{
    public class InvalidateCacheAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var retrievedEvent = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            CacheManager.Instance.InvalidateCaches(retrievedEvent.Id, retrievedEvent.TournamentId);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}