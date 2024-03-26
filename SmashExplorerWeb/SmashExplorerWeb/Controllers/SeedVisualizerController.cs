using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SeedVisualizerController : Controller
    {
        public SeedVisualizerController()
        {

        }

        public async Task<ActionResult> Index(string id)
        {
            MetricsManager.Instance.AddPageView(nameof(SeedVisualizerController), id);

            ViewBag.Title = $"Smash Explorer - Visualize Seeds {id}";

            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            IEnumerable<VisualizeSeedDataPoint> dataPoints = db_event.Standings
                .Where(x => x.Entrant.InitialSeedNum != null && !(x.Entrant.IsDisqualified ?? false)).Select(x =>
            {
                return new VisualizeSeedDataPoint()
                {
                    NormalizedPlacement = FlattenPlacement(x.Placement),
                    Placement = x.Placement,
                    SPR = GetSPR(x),
                    Name = x.Entrant.Name,
                    Seed = x.Entrant.InitialSeedNum ?? -1,
                    PlacementOrdinal = SmashExplorerDatabase.Instance.GetStringOrdinal(x.Placement)
                };
            });

			return View(new VisualizeSeedsModel() { Event = db_event, DataPoints = dataPoints, 
                Message = dataPoints.Count() == 0 ? "No standings published. Come back later!" : ""});
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
