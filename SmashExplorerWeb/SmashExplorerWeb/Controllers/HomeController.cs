using System;
using System.Collections.Generic;
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
                return View(filterModel);
            }

            if (filterModel.Slug.Length < 4)
            {
                filterModel.ErrorMessage = "Slug must be at least size 4";
                return View(filterModel);
            }

            DateTime StartTrackingDate = DateTime.ParseExact(filterModel.StartTrackingDate, DATE_FORMAT, null);
            DateTime EndTrackingDate = DateTime.ParseExact(filterModel.EndTrackingDate, DATE_FORMAT, null);

            DateTime startAtAfter = DateTime.ParseExact(filterModel.StartAtAfter, DATE_FORMAT, null);
            DateTime startAtBefore = DateTime.ParseExact(filterModel.StartAtBefore, DATE_FORMAT, null);

            if (startAtAfter > EndTrackingDate || startAtBefore < StartTrackingDate)
            {
                filterModel.ErrorMessage = "Filter dates must be between tracking dates";
                return View(filterModel);
            }

            filterModel.Events = await SmashExplorerDatabase.Instance.GetEventsBySlugAndDatesAsync(filterModel.Slug.ToLower(), startAtAfter, startAtBefore);

            return View(filterModel);
        }
    }
}
