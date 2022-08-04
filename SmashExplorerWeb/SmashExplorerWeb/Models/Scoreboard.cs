using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class Scoreboard
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("player1Name")]
    public string Player1Name { get; set; }
    [JsonProperty("player1Score")]
    public int Player1Score { get; set; }
    [JsonProperty("player2Name")]
    public string Player2Name { get; set; }
    [JsonProperty("player2Score")]
    public int Player2Score { get; set; }
    [JsonProperty("scoreboardLogs")]
    public List<ScoreboardLog> ScoreboardLogs { get; set; }
    [JsonProperty("scoreboardState")]
    public ScoreboardState ScoreboardState { get; set; }
    [JsonProperty("bestOf")]
    public int BestOf { get; set; }

    public Scoreboard() { }

    public Scoreboard(string player1Name, string player2Name, int bestOf)
    {
        Id = Guid.NewGuid().ToString().Replace("-", "");
        Player1Name = player1Name;
        Player1Score = 0;
        Player2Name = player2Name;
        Player2Score = 0;
        BestOf = bestOf;
        ScoreboardLogs = new List<ScoreboardLog>();
        ScoreboardState = ScoreboardState.StarterStagePicking;
    }
}