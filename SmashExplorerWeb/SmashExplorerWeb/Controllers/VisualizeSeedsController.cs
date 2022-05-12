using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class VisualizeSeedsController : Controller
    {
        public VisualizeSeedsController()
        {

        }

        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Smash Explorer - Visualize Seeds {id}";

            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            IEnumerable<VisualizeSeedDataPoint> dataPoints = db_event.Standings.Where(x => x.Entrant.InitialSeedNum != null).Select(x =>
            {
                return new VisualizeSeedDataPoint()
                {
                    Placement = FlattenPlacement(x.Placement),
                    SPR = GetSPR(x),
                    Name = x.Entrant.Name
                };
            });

			return View(new VisualizeSeedsModel() { Event = db_event, DataPoints = dataPoints });
        }

        private int GetSPR(Standing standing)
        {
            int seedingRoundsPlacement;
            SmashExplorerDatabase.PlacementToRounds.TryGetValue(standing.Entrant.InitialSeedNum ?? -1, out seedingRoundsPlacement);

            int actualRoundsPlacement;
            SmashExplorerDatabase.PlacementToRounds.TryGetValue(standing.Placement, out actualRoundsPlacement);

            return seedingRoundsPlacement - actualRoundsPlacement;
        }

        private int FlattenPlacement(int placement)
        {
            int roundsPlacement;
            SmashExplorerDatabase.PlacementToRounds.TryGetValue(placement, out roundsPlacement);

            return roundsPlacement;
        }
    }
}
