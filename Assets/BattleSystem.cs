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
    public Button magicButton;   // NEW
    public Button itemButton;    // NEW

    [Header("Icon Dots (UI Images)")]
    public Image[] partyIcons;   // size 4 in Inspector (P1..P4)
    public Image[] enemyIcons;   // size 3 in Inspector (E1..E3)

    [Header("Icon Colors")]
    public bool useOverrideAliveColors = false;        // OFF = keep editor colors
    public Color aliveParty = Color.white;             // only used if toggle is ON
    public Color aliveEnemy = Color.red;               // only used if toggle is ON
    public Color deadParty = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    public Color deadEnemy = new Color(0.6f, 0f, 0f, 0.6f);

    [Header("Tiny Attack Anim")]
    public float bopPixels = 20f;       // how far icon moves
    public float bopDuration = 0.12f;   // time for each half (forward/back)
    public float hitFlashTime = 0.10f;  // flash duration

    // data
    List<UnitStats> party = new();
    List<UnitStats> enemies = new();
    List<QueuedAction> partyChoices = new();
    System.Random rng = new System.Random();
    int currentActor = 0;
    bool roundResolving = false;

    // party-wide run
    bool runChosenThisRound = false;     // set if anyone picked Run during selection
    bool runResolvedThisRound = false;   // resolved once per round

    // cache icon base colors so we can restore them
    Color[] partyIconBaseColors = System.Array.Empty<Color>();
    Color[] enemyIconBaseColors = System.Array.Empty<Color>();

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
        // capture initial icon colors from the editor so we can keep them
        if (partyIcons != null)
        {
            partyIconBaseColors = new Color[partyIcons.Length];
            for (int i = 0; i < partyIcons.Length; i++)
                partyIconBaseColors[i] = partyIcons[i] ? partyIcons[i].color : Color.white;
        }
        if (enemyIcons != null)
        {
            enemyIconBaseColors = new Color[enemyIcons.Length];
            for (int i = 0; i < enemyIcons.Length; i++)
                enemyIconBaseColors[i] = enemyIcons[i] ? enemyIcons[i].color : Color.white;
        }

        fightButton.onClick.AddListener(() => OnChoose(CommandType.Fight));
        runButton.onClick.AddListener(() => OnChoose(CommandType.Run));
        if (magicButton) magicButton.onClick.AddListener(() => OnChoose(CommandType.Magic));
        if (itemButton) itemButton.onClick.AddListener(() => OnChoose(CommandType.Item));

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
        if (magicButton) magicButton.interactable = interactable;
        if (itemButton) itemButton.interactable = interactable;
        if (targetDropdown) targetDropdown.interactable = interactable;
    }

    // ----- UI updates -----
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
            var img = partyIcons[i];
            if (img == null) continue;

            if (i < party.Count)
            {
                bool dead = party[i].IsDead;
                img.gameObject.SetActive(true);

                if (dead)
                {
                    img.color = deadParty; // dead tint
                }
                else
                {
                    // keep editor color unless override toggle is ON
                    img.color = useOverrideAliveColors ? aliveParty
                                                       : (i < partyIconBaseColors.Length ? partyIconBaseColors[i] : img.color);
                }
            }
            else img.gameObject.SetActive(false);
        }

        // enemy dots/bars
        for (int i = 0; i < (enemyIcons != null ? enemyIcons.Length : 0); i++)
        {
            var img = enemyIcons[i];
            if (img == null) continue;

            if (i < enemies.Count)
            {
                bool dead = enemies[i].IsDead;
                img.gameObject.SetActive(true);

                if (dead)
                {
                    img.color = deadEnemy; // dead tint
                }
                else
                {
                    img.color = useOverrideAliveColors ? aliveEnemy
                                                       : (i < enemyIconBaseColors.Length ? enemyIconBaseColors[i] : img.color);
                }
            }
            else img.gameObject.SetActive(false);
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

        // if anyone chose Run, attempt once for the party now
        if (runChosenThisRound && !runResolvedThisRound)
        {
            runResolvedThisRound = true;

            bool success = rng.NextDouble() < 0.60;
            if (success)
            {
                Log("Party ran away!");
                yield return new WaitForSeconds(0.4f);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // or "Map"/"Title"
                yield break;
            }
            else
            {
                Log("Couldn't run!");
                // continue the round: only characters who chose FIGHT/MAGIC/ITEM act
            }
        }

        // player actions
        foreach (var act in partyChoices)
        {
            if (AllEnemiesDead() || AllPartyDead()) break;

            if (act.type == CommandType.Run) continue; // failed flee: they lose their turn
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

        // final refresh + end of round HP summary in log
        RefreshHUD();
        RefreshIcons();
        LogRoundSummary();

        FinishBattleIfOver();
        if (!AllEnemiesDead() && !AllPartyDead())
            BeginRoundSelection();
    }

    System.Collections.IEnumerator ResolveAction(QueuedAction qa)
    {
        switch (qa.type)
        {
            case CommandType.Fight:
                if (qa.actorIsPlayer)
                {
                    int t = qa.targetIndex;
                    if (!TargetAlive(enemies, t)) t = FirstLivingIndex(enemies);
                    if (t < 0) { Log("Ineffective."); yield break; }

                    // --- ANIMATIONS: party bops forward, enemy flashes ---
                    Image attackerIcon = (partyIcons != null && qa.actorIndex >= 0 && qa.actorIndex < partyIcons.Length) ? partyIcons[qa.actorIndex] : null;
                    Image targetIcon = (enemyIcons != null && t >= 0 && t < enemyIcons.Length) ? enemyIcons[t] : null;

                    yield return AttackBop(attackerIcon, fromLeft: true);          // party moves right
                    yield return HitFlash(targetIcon, Color.white, hitFlashTime);   // target flash

                    int dmg = DealDamage(party[qa.actorIndex], enemies[t]);
                    Log($"{party[qa.actorIndex].Name} hits {enemies[t].Name} for {dmg}!");
                }
                else
                {
                    int t = qa.targetIndex;
                    if (!TargetAlive(party, t)) t = FirstLivingIndex(party);
                    if (t < 0) yield break;

                    // --- ANIMATIONS: enemy bops forward, party flashes ---
                    Image attackerIcon = (enemyIcons != null && qa.actorIndex >= 0 && qa.actorIndex < enemyIcons.Length) ? enemyIcons[qa.actorIndex] : null;
                    Image targetIcon = (partyIcons != null && t >= 0 && t < partyIcons.Length) ? partyIcons[t] : null;

                    yield return AttackBop(attackerIcon, fromLeft: false);         // enemy moves left
                    yield return HitFlash(targetIcon, Color.white, hitFlashTime);   // target flash

                    int dmg = DealDamage(enemies[qa.actorIndex], party[t]);
                    Log($"{enemies[qa.actorIndex].Name} hits {party[t].Name} for {dmg}!");
                }
                break;

            case CommandType.Magic:
                if (qa.actorIsPlayer)
                    Log("No magic available.");   // placeholder behavior
                else
                    Log($"{enemies[qa.actorIndex].Name} prepares magic... but nothing happens.");
                break;

            case CommandType.Item:
                if (qa.actorIsPlayer)
                    Log("No items available.");   // placeholder behavior
                else
                    Log($"{enemies[qa.actorIndex].Name} searches for an item... nothing.");
                break;
        }
        yield break;
    }

    // --- tiny UI animations ---
    System.Collections.IEnumerator AttackBop(Image icon, bool fromLeft)
    {
        if (icon == null) yield break;
        var rt = icon.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + new Vector2((fromLeft ? +1f : -1f) * bopPixels, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bopDuration;
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bopDuration;
            rt.anchoredPosition = Vector2.Lerp(end, start, t);
            yield return null;
        }
        rt.anchoredPosition = start;
    }

    System.Collections.IEnumerator HitFlash(Image icon, Color flashColor, float time)
    {
        if (icon == null) yield break;
        var original = icon.color;
        icon.color = flashColor;
        yield return new WaitForSeconds(time);
        icon.color = original; // restores whatever color it had (editor color or override)
    }

    int DealDamage(UnitStats attacker, UnitStats target)
    {
        int baseDmg = Mathf.Max(1, attacker.Attack - target.Defense);
        int variance = Mathf.Max(1, Mathf.RoundToInt(baseDmg * 0.2f));
        int dmg = baseDmg + rng.Next(-variance, variance + 1);
        if (rng.NextDouble() < 0.05) dmg = Mathf.RoundToInt(dmg * 1.5f); // crit
        dmg = Mathf.Max(1, dmg);
        target.HP = Mathf.Max(0, target.HP - dmg);
        return dmg; // display the number
    }

    // helpers
    void LogRoundSummary()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Round end — Party: ");
        for (int i = 0; i < party.Count; i++)
        {
            var u = party[i];
            sb.Append($"{u.Name} {u.HP}/{u.MaxHP}");
            if (i < party.Count - 1) sb.Append(", ");
        }
        Log(sb.ToString());
    }

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
        if (logText != null) logText.text = msg; // overwrite
        // Debug.Log(msg);
    }
}

// Companion types
public enum CommandType { Fight, Run, Magic, Item }   // new

public struct QueuedAction
{
    public bool actorIsPlayer;
    public int actorIndex;
    public CommandType type;
    public int targetIndex;
}