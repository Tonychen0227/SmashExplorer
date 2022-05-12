using System.Collections.Generic;

public class VisualizeSeedsModel
{
    public Event Event { get; set; }
    public string Message { get; set; }
    public IEnumerable<VisualizeSeedDataPoint> DataPoints { get; set; }
}