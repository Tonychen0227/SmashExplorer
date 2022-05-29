using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class UpsetsController : Controller
    {
        public async Task<ActionResult> Index(string id)
        {
            ViewBag.Title = $"Smash Explorer - Upsets {id}";

            if (string.IsNullOrWhiteSpace(id)) return View();

            UpsetsModel model = new UpsetsModel();

            var upsets = await SmashExplorerDatabase.Instance.GetUpsetsAndNotableAsync(id);
            var db_event = await SmashExplorerDatabase.Instance.GetEventAsync(id);

            if (upsets.Count() == 0)
            {
                model.Event = db_event;
                model.Message = "No upsets returned! Come back later.";
                return View(model);
            }

            model = OrganizeUpsets(upsets);
            model.Event = db_event;
            model.DQEntrants = await SmashExplorerDatabase.Instance.GetDQdEntrantsAsync(id);
            model.MaxAvailableUpseteeSeed = upsets.SelectMany(x => x.Set.Entrants.Select(e => e.InitialSeedNum ?? -1)).Max();
            if (model.MaxAvailableUpseteeSeed == -1)
            {
                model.MaxAvailableUpseteeSeed = int.MaxValue;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Index(string id, UpsetsModel model)
        {
            ViewBag.Title = $"Smash Explorer - Upsets {id}";

            if (string.IsNullOrWhiteSpace(id)) return View();

            var maxUpseteeSeed = model.MaximumUpseteeSeed;
            var minUpsetFactor = model.MinimumUpsetFactor;

            var upsets = await SmashExplorerDatabase.Instance.GetUpsetsAndNotableAsync(id);
            model = OrganizeUpsets(upsets, model.MinimumUpsetFactor ?? 1, model.MaximumUpseteeSeed ?? int.MaxValue, model.SelectedPhases);
            model.Event = await SmashExplorerDatabase.Instance.GetEventAsync(id);
            model.DQEntrants = await SmashExplorerDatabase.Instance.GetDQdEntrantsAsync(id);
            model.MaxAvailableUpseteeSeed = upsets.SelectMany(x => x.Set.Entrants.Select(e => e.InitialSeedNum ?? -1)).Max();
            if (model.MaxAvailableUpseteeSeed == -1)
            {
                model.MaxAvailableUpseteeSeed = int.MaxValue;
            }
            model.MaximumUpseteeSeed = maxUpseteeSeed;
            model.MinimumUpsetFactor = minUpsetFactor;

            return View(model);
        }

        private UpsetsModel OrganizeUpsets(IEnumerable<Upset> upsets, int minimumUpsetFactor = 1, int maximumUpseteeSeed = int.MaxValue, List<string> selectedPhases = null)
        {
            selectedPhases = selectedPhases ?? upsets.Select(x => x.Set.PhaseName).Distinct().ToList();
            upsets = upsets.OrderByDescending(x => x.Set.PhaseOrder);

            var upsetsModel = new UpsetsModel()
            {
                WinnersNotable = new Dictionary<string, List<Upset>>(),
                WinnersUpsets = new Dictionary<string, List<Upset>>(),
                LosersNotable = new Dictionary<string, List<Upset>>(),
                LosersUpsets = new Dictionary<string, List<Upset>>(),
                MaximumUpsetFactor = upsets.Max(x => x.UpsetFactor),
                AvailablePhases = upsets.Select(x => x.Set.PhaseName).Distinct().Select(x => new SelectListItem { Text = x, Value = x }).ToList(),
                SelectedPhases = selectedPhases
            };

            foreach (var upset in upsets.Where(x => x.UpsetFactor >= minimumUpsetFactor)
                .Where(x => x.Set.Entrants.Select(k => k.InitialSeedNum ?? -1).Min() <= maximumUpseteeSeed)
                .Where(x => selectedPhases.Contains(x.Set.PhaseName)))
            {
                if (upset.Set.Round > 0)
                {
                    if (upset.CompletedUpset)
                    {
                        if (!upsetsModel.WinnersUpsets.ContainsKey(upset.Set.PhaseName))
                            upsetsModel.WinnersUpsets.Add(upset.Set.PhaseName, new List<Upset>());
                        
                        upsetsModel.WinnersUpsets[upset.Set.PhaseName].Add(upset);
                    } else
                    {
                        if (!upsetsModel.WinnersNotable.ContainsKey(upset.Set.PhaseName))
                            upsetsModel.WinnersNotable.Add(upset.Set.PhaseName, new List<Upset>());

                        upsetsModel.WinnersNotable[upset.Set.PhaseName].Add(upset);
                    }
                } else if (upset.Set.Round < 0)
                {
                    if (upset.CompletedUpset)
                    {
                        if (!upsetsModel.LosersUpsets.ContainsKey(upset.Set.PhaseName))
                            upsetsModel.LosersUpsets.Add(upset.Set.PhaseName, new List<Upset>());

                        upsetsModel.LosersUpsets[upset.Set.PhaseName].Add(upset);
                    }
                    else
                    {
                        if (!upsetsModel.LosersNotable.ContainsKey(upset.Set.PhaseName))
                            upsetsModel.LosersNotable.Add(upset.Set.PhaseName, new List<Upset>());

                        upsetsModel.LosersNotable[upset.Set.PhaseName].Add(upset);
                    }
                } else
                {
                    continue;
                }
            }

            return upsetsModel;
        }
    }
}
