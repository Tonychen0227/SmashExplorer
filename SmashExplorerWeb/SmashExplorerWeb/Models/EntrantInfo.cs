using System.Collections.Generic;
using System.Linq;

public class EntrantInfo
{
    public List<UrlObject> Urls { get; set; }
    public List<Image> Images { get; set; }
    public EntrantLocation Location { get; set; }
    public string LocationString { get
        {
            List<string> elements = new List<string>();

            if (!string.IsNullOrEmpty(Location?.City))
            {
                elements.Add(Location.City);
            }

            if (!string.IsNullOrEmpty(Location?.State))
            {
                elements.Add(Location.State);
            }

            if (!string.IsNullOrEmpty(Location?.Country))
            {
                elements.Add(Location.Country);
            }

            if (elements.Count == 0)
            {
                elements = new List<string> { "No Location" };
            }

            return string.Join(", ", elements);
        } }
}