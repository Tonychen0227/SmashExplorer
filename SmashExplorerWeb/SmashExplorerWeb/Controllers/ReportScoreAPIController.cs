using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    MetricsManager.Instance.AddFailReportSet(string.Empty, id, "Set not found");
                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.SetNotFound)), (int)HttpStatusCode.NotFound);
                }

                if (retrievedSet.IsFakeSet ?? false && !string.IsNullOrEmpty(retrievedSet.Identifier))
                {
                    var tournamentSets = await SmashExplorerDatabase.Instance.GetSetsAsync(retrievedSet.EventId);
                    var target = tournamentSets
                        .FirstOrDefault(x => !(x.IsFakeSet ?? false)
                            && x.PhaseGroupId == retrievedSet.PhaseGroupId
                            && x.PhaseId == retrievedSet.PhaseId
                            && !string.IsNullOrEmpty(x.Identifier)
                            && !string.IsNullOrEmpty(x.FullRoundText)
                            && !string.IsNullOrEmpty(x.PhaseIdentifier)
                            && x.EntrantIds?.Count == 2
                            && x.Identifier == retrievedSet.Identifier
                            && x.FullRoundText == retrievedSet.FullRoundText
                            && x.PhaseIdentifier == retrievedSet.PhaseIdentifier
                            && x.EntrantIds.OrderBy(k => k) == retrievedSet.EntrantIds.OrderBy(k => k)); 

                    if (target != null)
                    {
                        System.Diagnostics.Trace.TraceInformation($"Correlating fake set {retrievedSet.Id} with {target.Id}");
                        MetricsManager.Instance.AddCorrelatedSet((retrievedSet.Id, target.Id));
                        retrievedSet = target;
                    }
                }

                System.Diagnostics.Trace.TraceInformation($"Reporting set {id} with token {body.AuthUserToken}");
                MetricsManager.Instance.AddStartReportSet(retrievedSet.EventId, body.AuthUserToken.GetSha256Hash() ?? string.Empty);

                if (body.AuthUserToken == null)
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to no token passed");
                    MetricsManager.Instance.AddFailReportSet(retrievedSet.EventId, retrievedSet.Id, "Token not passed");
                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.Unauthorized)), (int)HttpStatusCode.Unauthorized);
                }

                body.AuthUserToken = body.AuthUserToken.GetSha256Hash();

                var retrievedAuth = await SmashExplorerDatabase.Instance.GetGalintAuthenticatedUserAsync(body.AuthUserToken);

                if (retrievedAuth == null)
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to token not being in server");
                    MetricsManager.Instance.AddFailReportSet(retrievedSet.EventId, retrievedSet.Id, "Token not stored");
                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.Unauthorized)), (int)HttpStatusCode.Unauthorized);
                }

                var eventEntrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(retrievedSet.EventId);
                var authenticatedEntrant = eventEntrants.Where(x => x.UserSlugs != null && x.UserSlugs.Contains(retrievedAuth.Slug)).FirstOrDefault();

                if (authenticatedEntrant == null || !retrievedSet.EntrantIds.Contains(authenticatedEntrant.Id))
                {
                    System.Diagnostics.Trace.TraceError($"Failing set report due to token not correctly matching an entrant");
                    MetricsManager.Instance.AddFailReportSet(retrievedSet.EventId, retrievedSet.Id, "Token mismatch");
                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.Unauthorized)), (int)HttpStatusCode.Unauthorized);
                }

                if (!(retrievedSet.WinnerId == null || retrievedSet.WinnerId == "None"))
                {
                    System.Diagnostics.Trace.TraceError($"Set is already finished");
                    MetricsManager.Instance.AddFailReportSet(retrievedSet.EventId, retrievedSet.Id, "Set already done");

                    var completedSet = new CompletedSetInformation()
                    {
                        Id = retrievedSet.Id,
                        WinnerId = retrievedSet.WinnerId,
                        DetailedScore = retrievedSet.DetailedScore
                    };

                    var error = new FailedReportSetModel(FailedReportSetErrorMessages.SetAlreadyCompleted, completedSet);

                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(error), (int)HttpStatusCode.Conflict);
                }

                var reportedSets = SmashExplorerDatabase.Instance.GetEventReportedSets(retrievedSet.EventId);

                try
                {
                    await StartGGDatabase.Instance.ReportSet(retrievedSet.Id, body);
                } 
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"Exception from StartGG: {ex.Message}");
                    MetricsManager.Instance.AddFailReportSet(retrievedSet.EventId, retrievedSet.Id, ex.Message);
                    if (ex.Message.Contains("Cannot report completed set"))
                    {
                        var completedSet = await GetCompletedSetFromStartGG(retrievedSet.Id);

                        if (completedSet != null)
                        {
                            var error = new FailedReportSetModel(FailedReportSetErrorMessages.SetAlreadyCompleted, completedSet);
                            return new NonOKWithMessageResult(JsonConvert.SerializeObject(error), (int)HttpStatusCode.Conflict);
                        }
                    }

                    return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.InternalServerError)), (int)HttpStatusCode.InternalServerError);
                }

                CacheManager.Instance.AddEventReportedSet(retrievedSet.EventId, retrievedSet.Id, body);

                // Invalidate Sets Cache and shorten TTL for 60 seconds
                CacheManager.Instance.InvalidateSetsAndShortenTTL(retrievedSet.EventId, 60);

                MetricsManager.Instance.AddSuccessReportSet(retrievedSet.EventId, body.AuthUserToken ?? string.Empty);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            } catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError($"Exception reporting set: {e.Message}");
                return new NonOKWithMessageResult(JsonConvert.SerializeObject(new FailedReportSetModel(FailedReportSetErrorMessages.InternalServerError)), (int)HttpStatusCode.InternalServerError);
            }
        }

        private async Task<CompletedSetInformation> GetCompletedSetFromStartGG(string setId)
        {
            var finishedSet = await StartGGDatabase.Instance.GetSet(setId);

            if (finishedSet.WinnerId != null)
            {
                var completedSet = new CompletedSetInformation()
                {
                    Id = finishedSet.Id.ToString(),
                    WinnerId = finishedSet.WinnerId.ToString(),
                    DetailedScore = new Dictionary<string, string>()
                };

                var winner = finishedSet.Slots.First(x => x.Entrant.Id == finishedSet.WinnerId.ToString()).Entrant;
                var loser = finishedSet.Slots.First(x => x.Entrant.Id != finishedSet.WinnerId.ToString()).Entrant;

                var winnerScore = 0;
                var loserScore = 0;

                if (finishedSet.DisplayScore == "DQ")
                {
                    loserScore = -1;
                }
                else
                {
                    var displayScore = finishedSet.DisplayScore;

                    if (displayScore.StartsWith(winner.Name) && !displayScore.StartsWith(loser.Name))
                    {
                        displayScore = displayScore.Substring(winner.Name.Length + 1);
                        int.TryParse(displayScore.Substring(0, 1), out winnerScore);

                        displayScore = displayScore.Remove(0, 4);
                        displayScore = displayScore.Substring(loser.Name.Length + 1);
                        int.TryParse(displayScore.Substring(0, 1), out loserScore);
                    }
                    else
                    {
                        displayScore = displayScore.Substring(loser.Name.Length + 1);
                        int.TryParse(displayScore.Substring(0, 1), out loserScore);

                        displayScore = displayScore.Remove(0, 4);
                        displayScore = displayScore.Substring(winner.Name.Length + 1);
                        int.TryParse(displayScore.Substring(0, 1), out winnerScore);
                    }
                }

                completedSet.DetailedScore[winner.Id] = winnerScore.ToString();
                completedSet.DetailedScore[loser.Id] = loserScore.ToString();

                return completedSet;
            }

            return null;
        }
    }
}