using System.Collections.Generic;

public class Entrant
{
    public string Name { get; set; }
    public string Id { get; set; }
    public bool? IsDisqualified { get; set; }
    public int? InitialSeedNum { get; set; }
    public int? Seeding { get; set; }
    public int? Standing { get; set; }
    public string EventId { get; set; }
    public List<string> UserSlugs { get; set; }
    public List<EntrantInfo> AdditionalInfo { get; set; }
    public string PrereqId { get; set; }
    public string PrereqType { get; set; }
}