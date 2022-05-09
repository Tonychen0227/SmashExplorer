using System.Collections.Generic;

public class TournamentFilterModel
{
    public string Slug { get; set; }
    public string StartAtAfter { get; set; }
    public string StartAtBefore { get; set; }
    public string ErrorMessage { get; set; }
    public string StartTrackingDate { get; set; }
    public string EndTrackingDate { get; set; }
    public List<Event> Events { get; set; }
    public string ChosenEventId { get; set; }
}