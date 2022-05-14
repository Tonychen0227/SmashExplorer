using System.Collections.Generic;
using System.Linq;
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

            ViewBag.Title = $"Smash Explorer - Explore {vanityLink.Name}";

            var sets = await SmashExplorerDatabase.Instance.GetSetsAsync(vanityLink.EventId);
            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(vanityLink.EventId);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(vanityLink.EventId);

            Dictionary<Entrant, List<Set>> setsByEntrants = new Dictionary<Entrant, List<Set>>();

            sets.ForEach(x =>
            {
                foreach (var entrant in x.Entrants)
                {
                    if (!vanityLink.EntrantIds.Contains(entrant.Id))
                    {
                        continue;
                    }

                    var actualEntrant = entrants.Where(y => y.Id == entrant.Id).Single();

                    if (!setsByEntrants.ContainsKey(actualEntrant))
                    {
                        setsByEntrants.Add(actualEntrant, new List<Set>());
                    }

                    setsByEntrants[actualEntrant].Add(x);
                }
            });

            return View(new ExploreModel()
            {
                Event = db_event,
                DictKeys = new List<Entrant>(setsByEntrants.Keys),
                EntrantsSets = setsByEntrants,
                AllSets = sets,
                VanityLink = vanityLink
            });
        }
    }
}
