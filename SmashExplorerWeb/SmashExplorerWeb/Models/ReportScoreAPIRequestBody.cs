using Newtonsoft.Json;
using System.Collections.Generic;

public class ReportScoreAPIRequestBody
{
    [JsonProperty("winnerId")]
    public int WinnerId { get; set; }
    [JsonProperty("gameData")]
    public List<GameData> GameData { get; set; }
    [JsonProperty("AuthUserSlug")]
    public string AuthUserSlug { get; set; }
    [JsonProperty("AuthUserToken")]
    public string AuthUserToken { get; set; }
} 

public class GameData
{
    [JsonProperty("gameNum")]
    public int GameNum { get; set; }
    [JsonProperty("winnerId")]
    public int WinnerId { get; set; }
    [JsonProperty("stageId")]
    public int? StageId { get; set; }
    [JsonProperty("selections")]
    public List<CharacterSelection> Selections { get; set; }
}

public class CharacterSelection
{
    [JsonProperty("entrantId")]
    public int EntrantId { get; set; }
    [JsonProperty("characterId")]
    public int CharacterId { get; set; }
}