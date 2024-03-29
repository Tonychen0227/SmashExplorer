using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Mvc.HttpGetAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;

namespace SmashExplorerWeb.Controllers
{
    public class ReportScoreAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            return Content("", "application/json");
        }

        [HttpPost]
        public async Task<ActionResult> Index(string id, [FromBody] ReportScoreAPIRequestBody body)
        {
            try
            {
                ViewBag.Title = "Set API";

                var retrievedSet = await SmashExplorerDatabase.Instance.GetSetAsync(id);

                MetricsManager.Instance.AddPageView(nameof(ReportScoreAPIController), id);

                if (retrievedSet == null)
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to set not found {id}");
                    return new HttpNotFoundResult("Set not found");
                }

                System.Diagnostics.Trace.TraceInformation($"Reporting set {id} with token {body.AuthUserToken}");
                MetricsManager.Instance.AddStartReportSet(retrievedSet.EventId, body.AuthUserToken.GetSha256Hash() ?? string.Empty);

                if (body.AuthUserToken == null)
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to no token passed");
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "No token passed!");
                }

                body.AuthUserToken = body.AuthUserToken.GetSha256Hash();

                var retrievedAuth = await SmashExplorerDatabase.Instance.GetGalintAuthenticatedUserAsync(body.AuthUserToken);

                if (retrievedAuth == null)
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to token not being in server");
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Unauthenticated user!");
                }

                var eventEntrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(retrievedSet.EventId);
                var authenticatedEntrant = eventEntrants.Where(x => x.UserSlugs != null && x.UserSlugs.Contains(retrievedAuth.Slug)).FirstOrDefault();

                if (authenticatedEntrant == null || !retrievedSet.EntrantIds.Contains(authenticatedEntrant.Id))
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to token not correctly matching an entrant");
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Unauthenticated user!");
                }

                if (!(retrievedSet.WinnerId == null || retrievedSet.WinnerId == "None"))
                {
                    System.Diagnostics.Trace.TraceError($"Set is already finished");
                    return new HttpStatusCodeResult(HttpStatusCode.Conflict, "Set is already completed!");
                }

                var reportedSets = SmashExplorerDatabase.Instance.GetEventReportedSets(retrievedSet.EventId);

                try
                {
                    await StartGGDatabase.Instance.ReportSet(id, body);
                } catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"Exception from StartGG: {ex.Message}");
                    if (ex.Message.Contains("Cannot report completed set"))
                    {
                        if (reportedSets != null && reportedSets.ContainsKey(retrievedSet.Id))
                        {
                            return new NonOKWithMessageResult(JsonConvert.SerializeObject(reportedSets[retrievedSet.Id]), (int)HttpStatusCode.Conflict);
                        }
                    }

                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
                }

                CacheManager.Instance.AddEventReportedSet(retrievedSet.EventId, retrievedSet.Id, body);

                // Invalidate Sets Cache and shorten TTL for 60 seconds
                CacheManager.Instance.InvalidateSetsAndShortenTTL(retrievedSet.EventId, 60);

                MetricsManager.Instance.AddSuccessReportSet(retrievedSet.EventId, body.AuthUserToken ?? string.Empty);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            } catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError($"Exception reporting set: {e.Message}");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}