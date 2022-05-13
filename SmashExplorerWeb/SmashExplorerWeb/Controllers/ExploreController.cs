using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class ExploreController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            VanityLink vanityLink = await SmashExplorerDatabase.Instance.GetVanityLinkAsync(id);

            if (vanityLink == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = $"Smash Explorer - Explore {id} @ {vanityLink.EventId}";

            return View();
        }

    }
}
