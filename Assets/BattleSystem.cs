using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace FF.Battle
{
    public class BattleSystem : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text partyText;
        public TMP_Text enemyText;
        public TMP_Text logText;
        public TMP_Dropdown targetDropdown;
        public Button fightButton;
        public Button runButton;

        List<UnitStats> party = new();
        List<UnitStats> enemies = new();
        List<QueuedAction> partyChoices = new();
        System.Random rng = new System.Random();
        int currentActor = 0;
        bool roundResolving = false;

        void Awake()
        {
            // temporary test data so the scene runs standalone
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
            RefreshTargets();
            Log("A battle begins!");
            BeginRoundSelection();
        }

        void BeginRoundSelection()
        {
            partyChoices.Clear();
            currentActor = 0;
            roundResolving = false;
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
            fightButton.interactable = interactable;
            runButton.interactable = interactable;
            targetDropdown.interactable = interactable;
        }

        void RefreshHUD()
        {
            partyText.text = "";
            foreach (var u in party)
                partyText.text += $"{u.Name}  HP {u.HP}/{u.MaxHP}\n";

            enemyText.text = "";
            foreach (var u in enemies)
                enemyText.text += $"{u.Name}  HP {u.HP}/{u.MaxHP}\n";
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

        void OnChoose(CommandType choice)
        {
            if (roundResolving || currentActor >= party.Count) return;

            var qa = new QueuedAction
            {
                actorIsPlayer = true,
                actorIndex = currentActor,
                type = choice,
                targetIndex = Mathf.Clamp(targetDropdown.value, 0, enemies.Count - 1)
            };
            partyChoices.Add(qa);
            Log($"{party[currentActor].Name} chooses {choice}.");

            currentActor++;
            SkipDeadActorsForSelection();
            if (currentActor >= party.Count)
            {
                SetChoiceUIInteractable(false);
                StartCoroutine(ResolveRound());
            }
        }

        System.Collections.IEnumerator ResolveRound()
        {
            roundResolving = true;

            // player actions
            foreach (var act in partyChoices)
            {
                if (AllEnemiesDead() || AllPartyDead()) break;
                yield return ResolveAction(act);
                RefreshHUD();
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
            if (qa.type == CommandType.Run && qa.actorIsPlayer)
            {
                if (rng.NextDouble() < 0.60)
                {
                    Log("Party ran away!");
                    yield break;
                }
                else
                {
                    Log("Couldn't run!");
                    yield break;
                }
            }

            if (qa.type == CommandType.Fight)
            {
                if (qa.actorIsPlayer)
                {
                    if (!TargetAlive(enemies, qa.targetIndex))
                    {
                        Log("Ineffective.");
                        yield break;
                    }
                    DealDamage(party[qa.actorIndex], enemies[qa.targetIndex]);
                }
                else
                {
                    if (!TargetAlive(party, qa.targetIndex)) yield break;
                    DealDamage(enemies[qa.actorIndex], party[qa.targetIndex]);
                }
            }
        }

        void DealDamage(UnitStats attacker, UnitStats target)
        {
            int baseDmg = Mathf.Max(1, attacker.Attack - target.Defense);
            int variance = Mathf.Max(1, Mathf.RoundToInt(baseDmg * 0.2f));
            int dmg = baseDmg + rng.Next(-variance, variance + 1);
            if (rng.NextDouble() < 0.05) dmg = Mathf.RoundToInt(dmg * 1.5f);
            dmg = Mathf.Max(1, dmg);
            target.HP = Mathf.Max(0, target.HP - dmg);
            Log($"{attacker.Name} hits {target.Name} for {dmg}.");
        }

        bool TargetAlive(List<UnitStats> list, int idx)
        {
            return idx >= 0 && idx < list.Count && !list[idx].IsDead;
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
            }
            else if (AllPartyDead())
            {
                Log("Party wiped...");
                SetChoiceUIInteractable(false);
            }
        }

        void Log(string msg)
        {
            if (logText != null) logText.text = msg;  // overwrite instead of append
            Debug.Log(msg);
        }
    }
}
