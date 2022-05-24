using System.Collections.Generic;

public class SelectEntrantsModel
{
    public Event Event { get; set; }
    public string EventId { get; set; }
    public List<Entrant> Entrants { get; set; }
    public List<Entrant> SelectedEntrants { get; set; }
    public List<string> SelectedEntrantIds { get; set; }
    public string Title { get; set; }
    public string EntrantsFilterText { get; set; }
    public bool IsFinal { get; set; }
    public bool IsAddEntrant { get; set; }
    public string EntrantsAnchorId { get; set; }
    public string ToModifyEntrantId { get; set; }
    public string ErrorMessage { get; set; }
}