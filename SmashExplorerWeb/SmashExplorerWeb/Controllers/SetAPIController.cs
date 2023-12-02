using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class SetAPIController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            return Content(JsonConvert.SerializeObject(await SmashExplorerDatabase.Instance.GetSetAsync(id)), "application/json");
        }
    }
}

