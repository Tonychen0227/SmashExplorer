using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class UpsetsController : Controller
    {
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Upsets";

            var events = await SmashExplorerDatabase.Instance.GetUpcomingEventsAsync();
            var vanityLink = await SmashExplorerDatabase.Instance.CreateVanityLinkAsync("20230", "Redx Vanity Link", new List<string>() { "Test" });
            vanityLink = await SmashExplorerDatabase.Instance.GetDataForVanityLinkAsync(vanityLink.Id, vanityLink.EventId);

            return View();
        }
    }
}
