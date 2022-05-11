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

            var model = OrganizeUpsets(await SmashExplorerDatabase.Instance.GetUpsetsAndNotableAsync(id));
            model.Event = await SmashExplorerDatabase.Instance.GetEvent(id);

            return View(model);
        }

        private UpsetsModel OrganizeUpsets(List<Upset> upsets)
        {
            var upsetsModel = new UpsetsModel()
            {
                WinnersNotable = new Dictionary<string, List<Upset>>(),
                WinnersUpsets = new Dictionary<string, List<Upset>>(),
                LosersNotable = new Dictionary<string, List<Upset>>(),
                LosersUpsets = new Dictionary<string, List<Upset>>()
            };

            foreach (var upset in upsets.OrderBy(x => x.Set.PhaseOrder))
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
