using Newtonsoft.Json;
using System;
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

                if (!(retrievedSet.WinnerId == null || retrievedSet.WinnerId == "None"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Set is already completed!");
                }

                await StartGGDatabase.Instance.ReportSet(id, body);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            } catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}