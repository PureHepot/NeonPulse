using System.Collections.Generic;
using UnityEngine;

public class RewardDirector
{
    private readonly List<RewardEntryConfig> fallbackEntries = new();

    public RewardRollResult CurrentResult { get; private set; }
    public bool IsComplete { get; private set; }

    public void OpenLoopReward(
        BattleThemeConfig theme,
        CombatLoopRuntimeSaveData loop,
        ScoreConfig scoreConfig,
        int themeIndex,
        int loopIndex,
        InRunRuntimeSaveData runtime)
    {
        if (loop == null || runtime == null)
        {
            CurrentResult = null;
            IsComplete = true;
            return;
        }

        loop.grade = ScoreResolver.ResolveGrade(loop, scoreConfig, theme, themeIndex, loopIndex);
        loop.loopCurrencyGain = Mathf.Max(loop.loopCurrencyGain, loop.loopScoreRaw);
        runtime.runCurrency += loop.loopCurrencyGain;
        runtime.runScoreTotal += loop.loopScoreRaw;

        var pool = theme != null ? theme.loopRewardPool : null;
        var rule = ResolveRule(pool, loop.grade);

        CurrentResult = new RewardRollResult
        {
            grade = loop.grade,
            picksAllowed = Mathf.Max(1, rule.picksAllowed)
        };

        BuildChoices(pool, Mathf.Max(1, rule.offerCount), CurrentResult.choices);
        IsComplete = CurrentResult.choices.Count == 0;
    }

    public void OpenBossReward(BattleThemeConfig theme, InRunRuntimeSaveData runtime)
    {
        if (runtime == null)
        {
            CurrentResult = null;
            IsComplete = true;
            return;
        }

        var pool = theme != null ? theme.bossRewardPool : null;
        var rule = ResolveRule(pool, CombatGrade.SSS);

        CurrentResult = new RewardRollResult
        {
            grade = CombatGrade.SSS,
            picksAllowed = Mathf.Max(1, rule.picksAllowed)
        };

        BuildChoices(pool, Mathf.Max(1, rule.offerCount), CurrentResult.choices);
        IsComplete = CurrentResult.choices.Count == 0;
    }

    public void Tick(InRunRuntimeSaveData runtime)
    {
        if (IsComplete || CurrentResult == null || runtime == null)
            return;

        for (int i = 0; i < CurrentResult.choices.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                ClaimChoice(i, runtime);
                return;
            }
        }
    }

    public void ClaimChoice(int index, InRunRuntimeSaveData runtime)
    {
        if (IsComplete || CurrentResult == null || runtime == null)
            return;

        if (index < 0 || index >= CurrentResult.choices.Count)
            return;

        var choice = CurrentResult.choices[index];
        if (choice == null || choice.selected)
            return;

        if (!CanClaim(choice, runtime))
            return;

        choice.selected = true;
        CurrentResult.picksMade++;

        string source = CurrentResult.grade == CombatGrade.SSS ? "BossReward" : "LoopReward";
        ApplyChoice(choice, runtime, source);

        if (CurrentResult.picksMade >= CurrentResult.picksAllowed)
            IsComplete = true;
    }

    public void Reset()
    {
        CurrentResult = null;
        IsComplete = false;
    }

    private GradeRewardRule ResolveRule(RewardPoolConfig pool, CombatGrade grade)
    {
        if (pool != null && pool.gradeRules != null)
        {
            foreach (var rule in pool.gradeRules)
            {
                if (rule != null && rule.grade == grade)
                    return rule;
            }
        }

        return grade switch
        {
            CombatGrade.F => new GradeRewardRule { grade = grade, offerCount = 1, picksAllowed = 1 },
            CombatGrade.D => new GradeRewardRule { grade = grade, offerCount = 1, picksAllowed = 1 },
            CombatGrade.C => new GradeRewardRule { grade = grade, offerCount = 2, picksAllowed = 1 },
            CombatGrade.B => new GradeRewardRule { grade = grade, offerCount = 2, picksAllowed = 1 },
            CombatGrade.A => new GradeRewardRule { grade = grade, offerCount = 3, picksAllowed = 1 },
            CombatGrade.S => new GradeRewardRule { grade = grade, offerCount = 3, picksAllowed = 1 },
            CombatGrade.SS => new GradeRewardRule { grade = grade, offerCount = 3, picksAllowed = 2 },
            _ => new GradeRewardRule { grade = grade, offerCount = 4, picksAllowed = 2 }
        };
    }

    private void BuildChoices(RewardPoolConfig pool, int offerCount, List<RewardChoice> target)
    {
        target.Clear();
        fallbackEntries.Clear();

        if (pool != null && pool.entries != null)
        {
            foreach (var entry in pool.entries)
            {
                if (entry != null)
                    fallbackEntries.Add(entry);
            }
        }

        if (fallbackEntries.Count == 0)
        {
            target.Add(new RewardChoice
            {
                rewardId = "reward_currency_small",
                itemId = "reward_currency_small",
                displayName = "Credit Cache",
                description = "Gain 25 run currency.",
                itemType = InRunItemType.Currency,
                rarity = RewardRarity.Common,
                currencyBonus = 25
            });
            target.Add(new RewardChoice
            {
                rewardId = "reward_damage_chip",
                itemId = "reward_damage_chip",
                displayName = "Damage Chip",
                description = "Placeholder reward for future module integration.",
                itemType = InRunItemType.Module,
                rarity = RewardRarity.Uncommon,
                currencyBonus = 10
            });
            target.Add(new RewardChoice
            {
                rewardId = "reward_mobility_chip",
                itemId = "reward_mobility_chip",
                displayName = "Mobility Chip",
                description = "Placeholder reward for future movement integration.",
                itemType = InRunItemType.Plugin,
                rarity = RewardRarity.Uncommon,
                currencyBonus = 10
            });
            target.Add(new RewardChoice
            {
                rewardId = "reward_core_fragment",
                itemId = "reward_core_fragment",
                displayName = "Core Fragment",
                description = "Placeholder reward with higher long-term value.",
                itemType = InRunItemType.Core,
                rarity = RewardRarity.Rare,
                currencyBonus = 15
            });
        }
        else
        {
            int count = Mathf.Min(offerCount, fallbackEntries.Count);
            var usedIndices = new HashSet<int>();
            while (target.Count < count)
            {
                int index = Random.Range(0, fallbackEntries.Count);
                if (!usedIndices.Add(index))
                    continue;

                var entry = fallbackEntries[index];
                target.Add(new RewardChoice
                {
                    rewardId = entry.rewardId,
                    itemId = string.IsNullOrWhiteSpace(entry.itemId) ? entry.rewardId : entry.itemId,
                    displayName = entry.displayName,
                    description = entry.description,
                    itemType = entry.itemType,
                    rarity = entry.rarity,
                    currencyBonus = entry.currencyBonus,
                    warehouseSlotsDelta = entry.warehouseSlotsDelta
                });
            }
        }

        while (target.Count > offerCount)
            target.RemoveAt(target.Count - 1);
    }

    private static bool CanClaim(RewardChoice choice, InRunRuntimeSaveData runtime)
    {
        if (choice == null || runtime == null)
            return false;

        if (choice.itemType == InRunItemType.Currency || choice.warehouseSlotsDelta > 0)
            return true;

        return WarehouseRuntimeState.HasSpace(runtime);
    }

    private static void ApplyChoice(RewardChoice choice, InRunRuntimeSaveData runtime, string source)
    {
        runtime.pendingRewards.Add(new RunRewardSaveData
        {
            rewardId = choice.rewardId,
            itemId = choice.itemId,
            displayName = choice.displayName,
            description = choice.description,
            source = source,
            currencyBonus = choice.currencyBonus,
            itemType = choice.itemType,
            warehouseSlotsDelta = choice.warehouseSlotsDelta
        });

        runtime.runCurrency += choice.currencyBonus;
        if (choice.warehouseSlotsDelta != 0)
            WarehouseRuntimeState.ApplyCapacityDelta(runtime, choice.warehouseSlotsDelta);

        if (choice.itemType != InRunItemType.Currency)
        {
            WarehouseRuntimeState.TryAddItem(
                runtime,
                choice.rewardId,
                choice.itemType,
                choice.itemId,
                choice.displayName,
                choice.description,
                source);
        }
    }
}
