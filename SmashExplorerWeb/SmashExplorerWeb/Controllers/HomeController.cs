using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string DATE_FORMAT = "yyyy-MM-dd";
        private static DateTime DefaultEventsFetched = DateTime.UtcNow;
        private static List<Event> DefaultEvents;

        public async Task<ActionResult> Index()
        {
            if (DefaultEvents == null || DefaultEventsFetched.AddMinutes(1) < DateTime.UtcNow)
            {
                DefaultEvents = await SmashExplorerDatabase.Instance.GetUpcomingEventsAsync();
            }

            ViewBag.Title = "Home Page";

            var startingModel = new TournamentFilterModel()
            {
                StartAtBefore = DateTime.UtcNow.AddDays(7).ToString(DATE_FORMAT),
                StartAtAfter = DateTime.UtcNow.ToString(DATE_FORMAT),
                Events = DefaultEvents,
                StartTrackingDate = new DateTime(2022, 4, 10).ToString(DATE_FORMAT),
                EndTrackingDate = DateTime.UtcNow.AddDays(7).ToString(DATE_FORMAT)
            };

            return View(startingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Index(TournamentFilterModel filterModel)
        {
            if (filterModel.ChosenEventId != null)
            {
                filterModel.Events = new List<Event>() { await SmashExplorerDatabase.Instance.GetEvent(filterModel.ChosenEventId) };
                return View(filterModel);
            }

            var StartTrackingDate = DateTime.ParseExact(filterModel.StartTrackingDate, DATE_FORMAT, null);
            var EndTrackingDate = DateTime.ParseExact(filterModel.EndTrackingDate, DATE_FORMAT, null);

            var startAtAfter = DateTime.ParseExact(filterModel.StartAtAfter, DATE_FORMAT, null);
            var startAtBefore = DateTime.ParseExact(filterModel.StartAtBefore, DATE_FORMAT, null);

            filterModel.Events = DefaultEvents;
            filterModel.Slug = string.Join("-", filterModel.Slug.Split(' ')) ?? String.Empty;

            if (filterModel.Slug.Contains("smash.gg"))
            {
                try
                {
                    var slugArray = filterModel.Slug.Split('/').ToList();
                    var index = slugArray.FindIndex(x => x.Contains("tournament"));

                    if (index + 1 >= slugArray.Count)
                        throw new ArgumentNullException();

                    filterModel.Slug = slugArray[index + 1];
                    startAtAfter = StartTrackingDate;
                    startAtBefore = EndTrackingDate;
                } catch (ArgumentNullException)
                {
                    filterModel.ErrorMessage = "Invalid smash.gg URL detected. Returning default events.";
                    return View(filterModel);
                }
            }

            if (startAtAfter > EndTrackingDate || startAtBefore < StartTrackingDate)
            {
                filterModel.ErrorMessage = "Filter dates must be between tracking dates. Returning default events.";
                return View(filterModel);
            }

            var queriedEvents = await SmashExplorerDatabase.Instance.GetEventsBySlugAndDatesAsync(filterModel.Slug?.ToLower() ?? "", startAtAfter, startAtBefore);

            if (queriedEvents.Count == 0)
            {
                filterModel.ErrorMessage = "Filter returned no entries. Returning default events.";
                return View(filterModel);
            }

            filterModel.Events = queriedEvents;
            return View(filterModel);
        }
    }
}
