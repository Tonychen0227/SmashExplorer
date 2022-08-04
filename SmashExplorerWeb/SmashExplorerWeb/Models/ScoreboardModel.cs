using System.Collections.Generic;
using System.Linq;

public class ScoreboardModel
{
    public Scoreboard Scoreboard { get; set; }
    public ScoreboardLog NextLog { get; set; }
    public List<ScoreboardStage> SelectableStages { get; set; }
    public List<ScoreboardStage> DSRStages { get; set; }
    public List<ScoreboardStage> BannedStages { get; set; }
    public bool ShouldRefresh { get; set; }

    private static readonly List<ScoreboardStage> StarterStages = new List<ScoreboardStage>()
    {
        ScoreboardStage.Smashville,
        ScoreboardStage.Battlefield,
        ScoreboardStage.PokemonStadium2,
        ScoreboardStage.FinalDestination,
        ScoreboardStage.SmallBattlefield
    };

    private static readonly List<ScoreboardStage> CounterpickStages = new List<ScoreboardStage>()
    {
        ScoreboardStage.HollowBastion,
        ScoreboardStage.TownAndCity
    };

    public ScoreboardModel() { }

    public ScoreboardModel(Scoreboard scoreboard)
    {
        Scoreboard = scoreboard;
        NextLog = null;
        DSRStages = new List<ScoreboardStage>();
        BannedStages = new List<ScoreboardStage>();
        SelectableStages = new List<ScoreboardStage>();

        List<ScoreboardStage> AllStages = StarterStages.Select(x => x).ToList();
        AllStages.AddRange(CounterpickStages);

        if (Scoreboard.ScoreboardState == ScoreboardState.Completed)
        {
            return;
        }

        if (Scoreboard.ScoreboardState == ScoreboardState.StageSelected)
        {
            SelectableStages = AllStages.Select(x => x).ToList();

            var lastLog = scoreboard.ScoreboardLogs.Last();

            NextLog = new ScoreboardLog()
            {
                ScoreboardAction = ScoreboardAction.Win,
                Stage = lastLog.Stage
            };

            return;
        }

        if (Scoreboard.ScoreboardState == ScoreboardState.StarterStagePicking)
        {
            SelectableStages = StarterStages.Select(x => x).ToList();

            NextLog = new ScoreboardLog()
            {
                ScoreboardAction = ScoreboardAction.StarterPick
            };

            return;
        }

        if (Scoreboard.ScoreboardState == ScoreboardState.Gentlemaning)
        {
            SelectableStages = StarterStages.Select(x => x).ToList();
            SelectableStages.AddRange(CounterpickStages);

            NextLog = new ScoreboardLog()
            {
                ScoreboardAction = ScoreboardAction.GentlemanPick
            };

            return;
        }

        string counterpickingPlayer = null;

        Dictionary<string, List<ScoreboardStage>> DSR = new Dictionary<string, List<ScoreboardStage>>()
        {
            { Scoreboard.Player1Name, new List<ScoreboardStage>() },
            { Scoreboard.Player2Name, new List<ScoreboardStage>() }
        };

        foreach (var log in Scoreboard.ScoreboardLogs)
        {
            if (log.ScoreboardAction == ScoreboardAction.Win)
            {
                BannedStages = new List<ScoreboardStage>();
                DSR[log.PlayerName].Add(log.Stage);
                NextLog = new ScoreboardLog() { 
                    PlayerName = log.PlayerName == Scoreboard.Player1Name ? Scoreboard.Player1Name : Scoreboard.Player2Name, 
                    ScoreboardAction = ScoreboardAction.Ban 
                };
                counterpickingPlayer = log.PlayerName != Scoreboard.Player1Name ? Scoreboard.Player1Name : Scoreboard.Player2Name;
            } else if (log.ScoreboardAction == ScoreboardAction.Ban)
            {
                BannedStages.Add(log.Stage);

                if (BannedStages.Count == 2)
                {
                    NextLog = new ScoreboardLog()
                    {
                        PlayerName = log.PlayerName != Scoreboard.Player1Name ? Scoreboard.Player1Name : Scoreboard.Player2Name,
                        ScoreboardAction = ScoreboardAction.Pick
                    };
                } else
                {
                    NextLog = new ScoreboardLog()
                    {
                        PlayerName = log.PlayerName == Scoreboard.Player1Name ? Scoreboard.Player1Name : Scoreboard.Player2Name,
                        ScoreboardAction = ScoreboardAction.Ban
                    };
                }
            } else if (log.ScoreboardAction == ScoreboardAction.Pick)
            {
                NextLog = new ScoreboardLog()
                {
                    ScoreboardAction = ScoreboardAction.Win,
                    Stage = log.Stage
                };
            } else if (log.ScoreboardAction == ScoreboardAction.GentlemanPick)
            {
                NextLog = new ScoreboardLog()
                {
                    ScoreboardAction = ScoreboardAction.Win,
                    Stage = log.Stage
                };
            }
        }

        SelectableStages = AllStages;
        DSRStages = DSR[counterpickingPlayer];
    }
}