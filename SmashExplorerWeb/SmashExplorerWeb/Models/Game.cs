using Newtonsoft.Json;
using System.Collections.Generic;

public class Game
{
    public int? WinnerId { get; set; }
    public int? OrderNum { get; set; }
    public Stage Stage { get; set; }
    public List<GameSelection> Selections { get; set; }
}