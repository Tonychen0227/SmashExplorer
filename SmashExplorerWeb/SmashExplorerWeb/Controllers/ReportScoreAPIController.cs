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

                if (retrievedSet == null)
                {
                    return new HttpNotFoundResult("Set not found");
                }

                if (body.AuthUserToken == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "No token passed!");
                }

                body.AuthUserToken = body.AuthUserToken.GetSha256Hash();

                var retrievedAuth = await SmashExplorerDatabase.Instance.GetGalintAuthenticatedUserAsync(body.AuthUserToken);

                if (retrievedAuth == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Unauthenticated user!");
                }

                var eventEntrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(retrievedSet.EventId);
                var authenticatedEntrant = eventEntrants.Where(x => x.UserSlugs.Contains(retrievedAuth.Slug)).FirstOrDefault();

                if (authenticatedEntrant == null || !retrievedSet.EntrantIds.Contains(authenticatedEntrant.Id))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Unauthenticated user!");
                }

                if (!(retrievedSet.WinnerId == null || retrievedSet.WinnerId == "None"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Conflict, "Set is already completed!");
                }

                var reportedSets = SmashExplorerDatabase.Instance.GetEventReportedSets(retrievedSet.EventId);

                try
                {
                    await StartGGDatabase.Instance.ReportSet(id, body);
                } catch (Exception ex)
                {
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
                CacheManager.Instance.InvalidateSetsAndShortenTTL(retrievedSet.EventId, 120);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            } catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}