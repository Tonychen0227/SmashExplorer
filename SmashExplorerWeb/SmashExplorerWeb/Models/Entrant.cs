public class Entrant
{
    public string Name { get; set; }
    public string Id { get; set; }
    public bool? IsDisqualified { get; set; }
    public int? InitialSeedNum { get; set; }
    public int? Standing { get; set; }
    public string EventId { get; set; }
    public EntrantInfo AdditionalInfo { get; set; }
}