using System;
using System.Collections.Generic;

public class Event
{
    public string Id { get; set; }
    public ActivityState State { get; set; }
    public string TournamentName { get; set; }
    public string TournamentSlug { get; set; }
    public Location TournamentLocation { get; set; }
    public List<Image> TournamentImages { get; set; }
    public string Name { get; set; }
    public long StartAt { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public string Slug { get; set; }
    public int NumEntrants { get; set; }
    public long SetsLastUpdated { get; set; }
    public List<Standing> Standings { get; set; }
}