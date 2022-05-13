using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SelectEntrantsController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Smash Explorer - Select Entrants {id}";

            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            return View(new SelectEntrantsModel()
            {
                Entrants = entrants,
                SelectedEntrants = new List<Entrant>(),
                SelectedEntrantIds = new List<string>(),
                Event = db_event,
                EventId = id,
                IsAddEntrant = false,
                IsFinal = false
            });
        }

        [HttpPost]
        public async Task<ActionResult> Index(SelectEntrantsModel model)
        {
            if (model.IsFinal)
            {
                VanityLink vanityLink 
                    = await SmashExplorerDatabase.Instance.CreateVanityLinkAsync(model.EventId, model.Title, model.SelectedEntrantIds);


            }

            ViewBag.Title = $"Smash Explorer - Select Entrants {model.EventId}";

            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(model.EventId);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(model.EventId);

            model.SelectedEntrantIds = model.SelectedEntrantIds ?? new List<string>();

            if (!string.IsNullOrEmpty(model.ToModifyEntrantId))
            {
                if (model.IsAddEntrant)
                {
                    model.SelectedEntrantIds.Add(model.ToModifyEntrantId);
                } else
                {
                    model.SelectedEntrantIds = model.SelectedEntrantIds.Where(x => x != model.ToModifyEntrantId).ToList();
                }
            }

            var viewModel = new SelectEntrantsModel()
            {
                Entrants = entrants,
                SelectedEntrants = entrants.Where(x => model.SelectedEntrantIds.Contains(x.Id)).ToList(),
                SelectedEntrantIds = model.SelectedEntrantIds,
                Event = db_event,
                EventId = model.EventId,
                IsAddEntrant = false,
                IsFinal = false,
                Title = model.Title
            };

            return View(viewModel);
        }
    }
}
