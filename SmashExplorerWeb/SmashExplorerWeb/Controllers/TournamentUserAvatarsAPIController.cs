using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class TournamentUserAvatarsAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            var thing = await StartGGDatabase.Instance.GetTournamentEvents(id);
            var tournamentEvents = thing.Tournament.Events;

            var ret = new Dictionary<string, Dictionary<string, (string Name, List<(string Url, double Ratio)>)>>();

            foreach (var tournamentEvent in tournamentEvents)
            {
                var avatars = await AvatarsDatabase.Instance.GetAvatars(tournamentEvent.Id);
                ret[tournamentEvent.Id] = avatars;
            }

            return Content(JsonConvert.SerializeObject(ret), "application/json");
        }
    }
}