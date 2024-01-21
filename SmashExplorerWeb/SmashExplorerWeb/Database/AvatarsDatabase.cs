using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AvatarsDatabase
{
    private static readonly Lazy<AvatarsDatabase> lazy = new Lazy<AvatarsDatabase>(() => new AvatarsDatabase());

    private Dictionary<string, Tuple<DateTime, Dictionary<string, (string Name, List<Image>)>>> AvatarsCache
        = new Dictionary<string, Tuple<DateTime, Dictionary<string, (string Name, List<Image>)>>>();
    private static readonly int AvatarsCacheTTLSeconds = 86400;

    private AvatarsDatabase() { }

    public static AvatarsDatabase Instance { get { return lazy.Value; } }

    public async Task<Dictionary<string, (string Name, List<Image>)>> GetAvatars(string id)
    {
        if (!AvatarsCache.ContainsKey(id))
        {
            AvatarsCache.Add(id, Tuple.Create(DateTime.MinValue, new Dictionary<string, (string Name, List<Image>)>()));
        }

        var cachedAvatars = AvatarsCache[id];

        if (DateTime.UtcNow - cachedAvatars.Item1 < TimeSpan.FromSeconds(AvatarsCacheTTLSeconds))
        {
            return cachedAvatars.Item2;
        }

        var entrants = await SmashExplorerDatabase.Instance.GetEntrantsAsync(id);

        var ret = new Dictionary<string, (string Name, List<Image>)>();

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

            var entrantInfoImages = (entrant.Name, images.ToList());
            ret[entrant.Id] = entrantInfoImages;
        }

        AvatarsCache[id] = new Tuple<DateTime, Dictionary<string, (string Name, List<Image>)>>(DateTime.UtcNow, ret);

        return ret;
    }
}