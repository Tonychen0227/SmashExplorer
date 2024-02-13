using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class FullEventAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var cached = CacheManager.Instance.GetFullEvent(id);
            if (cached != null)
            {
                return Content(JsonConvert.SerializeObject(cached), "application/json");
            }

            string eventId = id;
            var retEvent = await SmashExplorerDatabase.Instance.GetEventAsync(eventId);
            var retEntrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(eventId);
            var vanityLink = new VanityLink(retEvent.Id, $"Admin panel for {retEvent.Name} @ {retEvent.TournamentName}", retEntrants.Select(x => x.Id).ToList());

            if (vanityLink == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = $"Smash Explorer - Explore {vanityLink.Name}";

            var sets = await SmashExplorerDatabase.Instance.GetSetsAsync(vanityLink.EventId);
            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(vanityLink.EventId);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(vanityLink.EventId);

            Dictionary<Entrant, List<Set>> setsByEntrants = new Dictionary<Entrant, List<Set>>();

            var vanityLinkEntrants = entrants.Where(x => vanityLink.EntrantIds.Contains(x.Id)).ToList();

            sets.ForEach(x =>
            {
                foreach (var entrant in x.Entrants)
                {
                    if (!vanityLink.EntrantIds.Contains(entrant.Id))
                    {
                        continue;
                    }

                    var actualEntrant = vanityLinkEntrants.Where(y => y.Id == entrant.Id).Single();

                    if (!setsByEntrants.ContainsKey(actualEntrant))
                    {
                        setsByEntrants.Add(actualEntrant, new List<Set>());
                    }

                    setsByEntrants[actualEntrant].Add(x);
                }
            });

            vanityLinkEntrants.ForEach(x =>
            {
                if (!setsByEntrants.ContainsKey(x))
                {
                    setsByEntrants.Add(x, new List<Set>());
                }
            });

            var returnModel = new ExploreModel()
            {
                Event = db_event,
                DictKeys = entrants,
                AllSets = sets
            };

            CacheManager.Instance.SetFullEvent(id, returnModel);

            return Content(JsonConvert.SerializeObject(returnModel), "application/json");
        }
    }
}