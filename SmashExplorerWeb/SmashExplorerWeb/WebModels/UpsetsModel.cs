using System.Collections.Generic;
using System.Web.Mvc;

public class UpsetsModel
{
    public Dictionary<string, List<Upset>> WinnersUpsets { get; set; }
    public Dictionary<string, List<Upset>> WinnersNotable { get; set; }
    public Dictionary<string, List<Upset>> LosersUpsets { get; set; }
    public Dictionary<string, List<Upset>> LosersNotable { get; set; }
    public List<Entrant> DQEntrants { get; set; }
    public Event Event { get; set; }
    public int MinimumUpsetFactor { get; set; }
    public int MaximumUpsetFactor { get; set; }
    public List<SelectListItem> AvailablePhases { get; set; }
    public List<string> SelectedPhases { get; set; }
    public string Message { get; set; }
}