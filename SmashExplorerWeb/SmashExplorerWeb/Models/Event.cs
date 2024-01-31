using System;
using System.Collections.Generic;

public class Event
{
    public User TournamentOwner { get; set; }
    public string Id { get; set; }
    public ActivityState State { get; set; }
    public string TournamentName { get; set; }
    public string TournamentId { get; set; }
    public string TournamentSlug { get; set; }
    public Location TournamentLocation { get; set; }
    private List<Image> tournamentImages { get; set; }
    public List<Image> TournamentImages
    {
        get
        {
            return tournamentImages;
        }

        set
        {
            tournamentImages = value ?? new List<Image>();
        }
    }
    public string Name { get; set; }
    public long StartAt { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public string Slug { get; set; }
    public string VideoGameId { get; set; }
    public string VideoGameName { get; set; }
    public int NumEntrants { get; set; }
    public long SetsLastUpdated { get; set; }
    public List<Standing> standings { get; set; }
    public List<Standing> Standings
    {
        get
        {
            return standings;
        }

        set
        {
            standings = value ?? new List<Standing>();
        }
    }
}