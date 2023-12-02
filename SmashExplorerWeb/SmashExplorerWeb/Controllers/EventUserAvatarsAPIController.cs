using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmashExplorerWeb.Controllers
{
    public class EventUserAvatarsAPIController : Controller
    {
        private Dictionary<string, Tuple<DateTime, Dictionary<string, (string Name, List<(string Url, double Ratio)>)>>> AvatarsCache 
            = new Dictionary<string, Tuple<DateTime, Dictionary<string, (string Name, List<(string Url, double Ratio)>)>>>();
        private static readonly int AvatarsCacheTTLSeconds = 86400;

        [HttpGet]
        public async Task<ActionResult> Index(string id)
        {
            if (!AvatarsCache.ContainsKey(id))
            {
                AvatarsCache.Add(id, Tuple.Create(DateTime.MinValue, new Dictionary<string, (string Name, List<(string Url, double Ratio)>)>()));
            }

            var cachedAvatars = AvatarsCache[id];

            if (DateTime.UtcNow - cachedAvatars.Item1 < TimeSpan.FromSeconds(AvatarsCacheTTLSeconds))
            {
                return Content(JsonConvert.SerializeObject(cachedAvatars.Item2), "application/json");
            }

            var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);

            var ret = new Dictionary<string, (string Name, List<(string Url, double Ratio)>)>();

            foreach (var entrant in entrants)
            {
                if (entrant.AdditionalInfo == null || !entrant.AdditionalInfo.Any(x => x != null) || !entrant.AdditionalInfo.Any(x => x.Images != null))
                {
                    continue;
                }

                var images = entrant.AdditionalInfo.Select(x => x?.Images).SelectMany(x => x).Where(x => x != null);

                if (images == null || images.Count() == 0)
                {
                    continue;
                }

                var entrantInfoImages = (entrant.Name, images.Select(x => (x.url, x.ratio)).ToList());
                ret[entrant.Id] = entrantInfoImages;
            }

            AvatarsCache[id] = new Tuple<DateTime, Dictionary<string, (string Name, List<(string Url, double Ratio)>)>>(DateTime.UtcNow, ret);

            return Content(JsonConvert.SerializeObject(ret), "application/json");
        }
    }
}