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

        public async Task<ActionResult> Index()
        {
            MetricsManager.Instance.AddPageView(nameof(HomeController), string.Empty);

            ViewBag.Title = "Smash Explorer";

            var startingModel = new TournamentFilterModel()
            {
                StartAtBefore = DateTime.UtcNow.AddDays(14).ToString(DATE_FORMAT),
                StartAtAfter = DateTime.UtcNow.AddDays(-5).ToString(DATE_FORMAT),
                Events = await SmashExplorerDatabase.Instance.GetUpcomingEventsAsync(),
                StartTrackingDate = new DateTime(2019, 1, 1).ToString(DATE_FORMAT),
                EndTrackingDate = DateTime.UtcNow.AddDays(14).ToString(DATE_FORMAT)
            };

            return View(startingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Index(TournamentFilterModel filterModel)
        {
            ViewBag.Title = "Smash Explorer";

            if (filterModel.ChosenEventId != null)
            {
                filterModel.Events = new List<Event>() { await SmashExplorerDatabase.Instance.GetEventAsync(filterModel.ChosenEventId) };
                return View(filterModel);
            }

            var StartTrackingDate = DateTime.ParseExact(filterModel.StartTrackingDate, DATE_FORMAT, null);
            var EndTrackingDate = DateTime.ParseExact(filterModel.EndTrackingDate, DATE_FORMAT, null);

            var startAtAfter = DateTime.ParseExact(filterModel.StartAtAfter, DATE_FORMAT, null);
            var startAtBefore = DateTime.ParseExact(filterModel.StartAtBefore, DATE_FORMAT, null);

            filterModel.Events = await SmashExplorerDatabase.Instance.GetUpcomingEventsAsync();
            filterModel.Slug = !string.IsNullOrWhiteSpace(filterModel.Slug) ? string.Join("-", filterModel.Slug.Split(' ')) : string.Empty;

            if (filterModel.Slug.Contains("start.gg"))
            {
                try
                {
                    var slugArray = filterModel.Slug.Split('/').ToList();
                    var index = slugArray.FindIndex(x => x.Contains("tournament"));

                    if (index + 1 >= slugArray.Count)
                        throw new ArgumentNullException();

                    filterModel.Slug = slugArray[index + 1];
                    startAtAfter = new DateTime(1970, 1, 1);
                    startAtBefore = new DateTime(2030, 1, 1);
                } catch (ArgumentNullException)
                {
                    filterModel.ErrorMessage = "Invalid start.gg URL detected. Returning default events.";
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
