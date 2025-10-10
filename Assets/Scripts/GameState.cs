using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class GameState
{ 
    public enum State
    {
        overworld, battle

    }
    //public enum OverworldState
    //{
    //    disabled, regular

    //}
    public enum BattleState
    {
        disabled, playerTurn, enemyTurn

    }

    public static State state = State.overworld;
    //public static OverworldState overworldState = OverworldState.regular;
    public static BattleState battleState = BattleState.disabled;

    public static bool paused = false;

    public static void startBattle(UnityEngine.Transform player, UnityEngine.Transform enemy)
    {
        state = State.battle;
        battleState = BattleState.playerTurn;
        enemy.SetPositionAndRotation(player.position + new Vector3(4, 0, 0), enemy.rotation);
    }


}
