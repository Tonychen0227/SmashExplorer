using System.Collections.Generic;

public class UpsetsModel
{
    public Dictionary<string, List<Upset>> WinnersUpsets { get; set; }
    public Dictionary<string, List<Upset>> WinnersNotable { get; set; }
    public Dictionary<string, List<Upset>> LosersUpsets { get; set; }
    public Dictionary<string, List<Upset>> LosersNotable { get; set; }
    public Event Event { get; set; }
}