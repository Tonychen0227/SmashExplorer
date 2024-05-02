using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class OnboardNewGalintTournamentAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id, string bannerOverride, string twitter)
        {
            ViewBag.Title = "Onboard Galint Tournament API";

            var startGGData = await StartGGDatabase.Instance.GetTournament(id);
            var existingCurrentTournaments = await SmashExplorerDatabase.Instance.GetCurrentTournamentsAsync();
            var existingCurrentTournament = existingCurrentTournaments.FirstOrDefault(x => x.Id == id);

            var tournament = startGGData.Data.Tournament;

            if (!string.IsNullOrEmpty(bannerOverride))
            {
                var bannerImage = tournament.Images.FirstOrDefault(x => x.type.ToLower() == "banner");

                if (bannerImage == null)
                {
                    tournament.Images.Add(new Image()
                    {
                        type = "banner",
                        ratio = 1,
                        url = bannerOverride
                    });
                } 
                else
                {
                    bannerImage.url = bannerOverride;
                }
            }

            foreach (var tournamentEvent in tournament.Events)
            {
                foreach (var phase in tournamentEvent.Phases)
                {
                    phase.BestOf = existingCurrentTournament?
                        .Events?
                        .FirstOrDefault(x => x.Id == tournamentEvent.Id)?
                        .Phases?
                        .FirstOrDefault(x => x.Id == phase.Id)?
                        .BestOf ?? 3;
                }

                tournamentEvent.ShowUpsets = existingCurrentTournament?
                        .Events?
                        .FirstOrDefault(x => x.Id == tournamentEvent.Id)?
                        .ShowUpsets ?? true;
            }

            tournament.Twitter = string.IsNullOrEmpty(twitter) ? 
                existingCurrentTournament?.Twitter ?? "https://twitter.com/galintgaming"
                : twitter;

            await SmashExplorerDatabase.Instance.AddCurrentTournamentAndSetAsActiveAsync(tournament);

            CacheManager.Instance.InvalidateCurrentTournamentCache();

            return new HttpStatusCodeResult(System.Net.HttpStatusCode.Created);
        }
    }
}