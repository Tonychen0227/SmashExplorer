using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class TournamentAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id, string userSlug)
        {
            dynamic ret = new System.Dynamic.ExpandoObject();
            var tournamentEvents = (await StartGGDatabase.Instance.GetTournamentEvents(id)).Tournament.Events;

            var tournamentUpsetTasks = tournamentEvents.Select(async x => await SmashExplorerDatabase.Instance.GetUpsetsAndNotableAsync(x.Id));
            var tournamentUpsets = (await Task.WhenAll(tournamentUpsetTasks)).SelectMany(x => x).Where(x => x.CompletedUpset);

            ret.Events = tournamentEvents;
            
            foreach (var retEvent in ret.Events)
            {
                var eventId = retEvent.Id;
                retEvent.TournamentUpsets = tournamentUpsets.Where(x => x.Set.EventId == retEvent.Id).ToList();
            }

            if (userSlug != null)
            {
                var eventEntrantsTasks = tournamentEvents.Select(async x => await SmashExplorerDatabase.Instance.GetEntrantBySlugAndEventAsync(userSlug, x.Id)).ToList();
                var eventEntrants = await Task.WhenAll(eventEntrantsTasks);

                var userRegisteredEvents = eventEntrants.Where(x => x != null).Select(x =>
                {
                    return new UserRegisteredEvent()
                    {
                        EventId = x.EventId,
                        Name = tournamentEvents.Where(e => e.Id == x.EventId).First().Name,
                        StartAt = tournamentEvents.Where(e => e.Id == x.EventId).First().StartAt,
                        UserSeeding = x.Seeding ?? -1,
                        UserStanding = x.Standing ?? -1,
                        NumEntrants = tournamentEvents.Where(e => e.Id == x.EventId).First().NumEntrants,
                        UserEntrantDisplayName = x.Name,
                        EntrantId = x.Id
                    };
                }).ToList();

                ret.UserRegisteredEvents = userRegisteredEvents;
            }

            return Content(JsonConvert.SerializeObject(ret), "application/json");
        }
    }

    internal class UserRegisteredEvent
    {
        public string EventId { get; set; }
        public string Name { get; set; }
        public long StartAt { get; set; }
        public int UserSeeding { get; set; }
        public int UserStanding { get; set; }
        public int NumEntrants { get; set; }
        public string UserEntrantDisplayName { get; set; }
        public string EntrantId { get; set; }
    }
}

