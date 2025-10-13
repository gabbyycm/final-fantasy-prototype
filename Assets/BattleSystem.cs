using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleSystem : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text partyText;
    public TMP_Text enemyText;
    public TMP_Text logText;
    public TMP_Dropdown targetDropdown;
    public Button fightButton;
    public Button runButton;

    [Header("Optional Icon Dots (UI Images)")]
    public Image[] partyIcons;   // size 4 in Inspector (P1..P4)
    public Image[] enemyIcons;   // size 3 in Inspector (E1..E3)
    public Color aliveParty = Color.white;
    public Color deadParty = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    public Color aliveEnemy = Color.red;
    public Color deadEnemy = new Color(0.6f, 0f, 0f, 0.6f);

    // data
    List<UnitStats> party = new();
    List<UnitStats> enemies = new();
    List<QueuedAction> partyChoices = new();
    System.Random rng = new System.Random();
    int currentActor = 0;
    bool roundResolving = false;

    // party-wide run
    bool runChosenThisRound = false;   // set if any member selected Run
    bool runResolvedThisRound = false; // ensure we resolve at most once

    void Awake()
    {
        // temporary party & enemies so the scene runs standalone
        party.Add(UnitStats.Hero("Fighter", 36, 10, 6));
        party.Add(UnitStats.Hero("Thief", 28, 8, 5));
        party.Add(UnitStats.Hero("W. Mage", 24, 5, 4));
        party.Add(UnitStats.Hero("B. Mage", 22, 4, 3));

        enemies.Add(UnitStats.Enemy("Imp", 18, 6, 2));
        enemies.Add(UnitStats.Enemy("Imp", 18, 6, 2));
        enemies.Add(UnitStats.Enemy("Wolf", 34, 9, 4));
    }

    void Start()
    {
        fightButton.onClick.AddListener(() => OnChoose(CommandType.Fight));
        runButton.onClick.AddListener(() => OnChoose(CommandType.Run));

        RefreshHUD();
        RefreshIcons();
        RefreshTargets();
        Log("A battle begins!");
        BeginRoundSelection();
    }

    // round flow
    void BeginRoundSelection()
    {
        partyChoices.Clear();
        currentActor = 0;
        roundResolving = false;

        runChosenThisRound = false;
        runResolvedThisRound = false;

        SkipDeadActorsForSelection();
    }

    void SkipDeadActorsForSelection()
    {
        while (currentActor < party.Count && party[currentActor].IsDead)
            currentActor++;
        SetChoiceUIInteractable(currentActor < party.Count);
    }

    void SetChoiceUIInteractable(bool interactable)
    {
        if (fightButton) fightButton.interactable = interactable;
        if (runButton) runButton.interactable = interactable;
        if (targetDropdown) targetDropdown.interactable = interactable;
    }

    // UI updates
    void RefreshHUD()
    {
        partyText.text = "";
        for (int i = 0; i < party.Count; i++)
        {
            var u = party[i];
            var ko = u.IsDead ? " (KO)" : "";
            partyText.text += $"{i + 1}. {u.Name}{ko}  HP {u.HP}/{u.MaxHP}\n";
        }

        enemyText.text = "";
        for (int i = 0; i < enemies.Count; i++)
        {
            var u = enemies[i];
            var ko = u.IsDead ? " (KO)" : "";
            enemyText.text += $"{i + 1}. {u.Name}{ko}  HP {u.HP}/{u.MaxHP}\n";
        }
    }

    void RefreshIcons()
    {
        // party dots/bars
        for (int i = 0; i < (partyIcons != null ? partyIcons.Length : 0); i++)
        {
            if (partyIcons[i] == null) continue;
            if (i < party.Count)
            {
                bool dead = party[i].IsDead;
                partyIcons[i].gameObject.SetActive(true);
                partyIcons[i].color = dead ? deadParty : aliveParty;
            }
            else partyIcons[i].gameObject.SetActive(false);
        }

        // enemy dots/bars
        for (int i = 0; i < (enemyIcons != null ? enemyIcons.Length : 0); i++)
        {
            if (enemyIcons[i] == null) continue;
            if (i < enemies.Count)
            {
                bool dead = enemies[i].IsDead;
                enemyIcons[i].gameObject.SetActive(true);
                enemyIcons[i].color = dead ? deadEnemy : aliveEnemy;
            }
            else enemyIcons[i].gameObject.SetActive(false);
        }
    }

    void RefreshTargets()
    {
        targetDropdown.ClearOptions();
        var opts = new List<string>();
        for (int i = 0; i < enemies.Count; i++)
            opts.Add($"{i + 1}. {enemies[i].Name}");
        targetDropdown.AddOptions(opts);
        if (opts.Count > 0) targetDropdown.value = 0;
    }

    // choosing actions
    void OnChoose(CommandType choice)
    {
        if (roundResolving || currentActor >= party.Count) return;

        // record the player's choice
        int sel = (targetDropdown != null && targetDropdown.options.Count > 0) ? targetDropdown.value : 0;
        sel = Mathf.Clamp(sel, 0, Mathf.Max(0, enemies.Count - 1));

        var qa = new QueuedAction
        {
            actorIsPlayer = true,
            actorIndex = currentActor,
            type = choice,
            targetIndex = sel
        };
        partyChoices.Add(qa);

        if (choice == CommandType.Run)
            runChosenThisRound = true; // one shared flee attempt for the party

        Log($"{party[currentActor].Name} chooses {choice}.");

        currentActor++;
        SkipDeadActorsForSelection();

        if (currentActor >= party.Count)
        {
            SetChoiceUIInteractable(false);
            StartCoroutine(ResolveRound());
        }
    }

    // resolution
    System.Collections.IEnumerator ResolveRound()
    {
        roundResolving = true;

        //if anyone chose Run, attempt ONCE for the party now
        if (runChosenThisRound && !runResolvedThisRound)
        {
            runResolvedThisRound = true;

            // simple success chance (feel free to tune or base on stats)
            bool success = rng.NextDouble() < 0.60;

            if (success)
            {
                Log("Party ran away!");
                yield return new WaitForSeconds(0.4f);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // or "Title"/"Map"
                yield break; // everyone escapes immediately
            }
            else
            {
                Log("Couldn't run!");
                // continue the round: only characters who chose FIGHT will act
            }
        }

        // player actions (skip any that chose Run if the attempt failed)
        foreach (var act in partyChoices)
        {
            if (AllEnemiesDead() || AllPartyDead()) break;
            if (act.type == CommandType.Run) continue; // failed flee, they lose their turn

            yield return ResolveAction(act);
            RefreshHUD();
            RefreshIcons();
            yield return new WaitForSeconds(0.15f);
        }

        // enemy actions
        if (!AllEnemiesDead() && !AllPartyDead())
        {
            for (int e = 0; e < enemies.Count; e++)
            {
                if (enemies[e].IsDead) continue;
                int target = PickRandomLivingIndex(party);
                if (target < 0) break;

                var qa = new QueuedAction
                {
                    actorIsPlayer = false,
                    actorIndex = e,
                    type = CommandType.Fight,
                    targetIndex = target
                };

                yield return ResolveAction(qa);
                RefreshHUD();
                RefreshIcons();
                yield return new WaitForSeconds(0.15f);
                if (AllPartyDead()) break;
            }
        }

        FinishBattleIfOver();
        if (!AllEnemiesDead() && !AllPartyDead())
            BeginRoundSelection();
    }

    System.Collections.IEnumerator ResolveAction(QueuedAction qa)
    {
        // Only FIGHT actions reach here for players; enemies always Fight
        if (qa.type == CommandType.Fight)
        {
            if (qa.actorIsPlayer)
            {
                // revalidate target at resolution time
                int t = qa.targetIndex;
                if (!TargetAlive(enemies, t))
                    t = FirstLivingIndex(enemies);

                if (t < 0)
                {
                    Log("Ineffective."); // nothing left to hit
                    yield break;
                }

                DealDamage(party[qa.actorIndex], enemies[t]);
                Log($"{party[qa.actorIndex].Name} hits {enemies[t].Name}!");
            }
            else
            {
                int t = qa.targetIndex;
                if (!TargetAlive(party, t))
                    t = FirstLivingIndex(party);

                if (t < 0) yield break;

                DealDamage(enemies[qa.actorIndex], party[t]);
                Log($"{enemies[qa.actorIndex].Name} hits {party[t].Name}!");
            }
        }
    }

    void DealDamage(UnitStats attacker, UnitStats target)
    {
        int baseDmg = Mathf.Max(1, attacker.Attack - target.Defense);
        int variance = Mathf.Max(1, Mathf.RoundToInt(baseDmg * 0.2f));
        int dmg = baseDmg + rng.Next(-variance, variance + 1);
        if (rng.NextDouble() < 0.05) dmg = Mathf.RoundToInt(dmg * 1.5f); // crit
        dmg = Mathf.Max(1, dmg);
        target.HP = Mathf.Max(0, target.HP - dmg);
    }

    // helpers
    bool TargetAlive(List<UnitStats> list, int idx)
    {
        return idx >= 0 && idx < list.Count && !list[idx].IsDead;
    }

    int FirstLivingIndex(List<UnitStats> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (!list[i].IsDead) return i;
        return -1;
    }

    int PickRandomLivingIndex(List<UnitStats> list)
    {
        var alive = new List<int>();
        for (int i = 0; i < list.Count; i++)
            if (!list[i].IsDead) alive.Add(i);
        if (alive.Count == 0) return -1;
        return alive[rng.Next(0, alive.Count)];
    }

    bool AllEnemiesDead()
    {
        foreach (var e in enemies) if (!e.IsDead) return false;
        return true;
    }

    bool AllPartyDead()
    {
        foreach (var p in party) if (!p.IsDead) return false;
        return true;
    }

    void FinishBattleIfOver()
    {
        if (AllEnemiesDead())
        {
            Log("Victory!");
            SetChoiceUIInteractable(false);
            // StartCoroutine(GoVictoryAfter(0.7f));
        }
        else if (AllPartyDead())
        {
            Log("Party wiped...");
            SetChoiceUIInteractable(false);
        }
    }

    // optional victory transition
    System.Collections.IEnumerator GoVictoryAfter(float secs)
    {
        yield return new WaitForSeconds(secs);
        SceneManager.LoadScene("Victory");
    }

    void Log(string msg)
    {
        if (logText != null) logText.text = msg; // overwrite (no stacking)
        // Debug.Log(msg);
    }
}

// Companion types
public enum CommandType { Fight, Run }

public struct QueuedAction
{
    public bool actorIsPlayer;
    public int actorIndex;
    public CommandType type;
    public int targetIndex;
}