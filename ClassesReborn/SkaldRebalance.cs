using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class SkaldRebalance {
    private static readonly int[] SkaldTalentLevels = { 2, 5, 8, 11, 14, 17 };

    internal static void Configure() {
        ConfigureBonusFeat();
        ConfigureRepeatableCombatTrick();
        ConfigureTalentProgression();
        ConfigureHuntCallerRagePowers();
        ValidateDanceOfAHundredCutsSupport();
    }

    private static void ConfigureBonusFeat() {
        var bonusFeat = FeatureSelectionConfigurator.For(
                BlueprintIds.SkaldFeatSelection)
            .SetDescription("ClassesReborn.SkaldBonusFeat.Description")
            .Configure();
        var weaponFocus = BlueprintTool.Get<BlueprintParametrizedFeature>(
            BlueprintIds.WeaponFocus);
        if (!bonusFeat.m_AllFeatures.Any(reference =>
                reference?.Get() == weaponFocus)) {
            bonusFeat.m_AllFeatures = bonusFeat.m_AllFeatures
                .Append(BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.WeaponFocus))
                .ToArray();
        }

        if (bonusFeat.m_AllFeatures.Count(reference =>
                reference?.Get() == weaponFocus) != 1) {
            throw new InvalidOperationException(
                "The level-1 Skald Bonus Feat selection must contain Weapon Focus exactly once.");
        }
    }

    private static void ConfigureRepeatableCombatTrick() {
        var original = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CombatTrick);
        var repeatable = FeatureSelectionConfigurator.New(
                "ClassesRebornRepeatableSkaldCombatTrick",
                BlueprintIds.RepeatableSkaldCombatTrick)
            .CopyFrom(BlueprintIds.CombatTrick)
            .SetRanks(20)
            .Configure();
        var skaldTalents = FeatureSelectionConfigurator.For(
                BlueprintIds.SkaldTalentSelection)
            .SetDescription("ClassesReborn.SkaldTalent.Description")
            .Configure();

        skaldTalents.m_AllFeatures = ReplaceFeatureReference(
            skaldTalents.m_AllFeatures,
            original,
            repeatable);
        skaldTalents.m_Features = ReplaceFeatureReference(
            skaldTalents.m_Features,
            original,
            repeatable);

        if (skaldTalents.m_AllFeatures.Count(reference =>
                reference?.Get() == repeatable) != 1 ||
            skaldTalents.m_AllFeatures.Any(reference =>
                reference?.Get() == original) ||
            repeatable.Ranks < 20) {
            throw new InvalidOperationException(
                "Skald Talent must contain exactly one repeatable Combat Trick selection.");
        }
    }

    private static void ConfigureTalentProgression() {
        var skaldClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.SkaldClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.SkaldProgression);
        var skaldTalent = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SkaldTalentSelection);
        var archetypesTradingTalents = skaldClass.Archetypes
            .Where(archetype =>
                CountFeature(archetype.RemoveFeatures, skaldTalent) > 0)
            .ToArray();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, skaldTalent);
        foreach (var level in SkaldTalentLevels) {
            AddFeature(levelEntries, level, skaldTalent);
        }
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        foreach (var archetype in archetypesTradingTalents) {
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            RemoveFeature(removals, skaldTalent);
            foreach (var level in SkaldTalentLevels) {
                AddFeature(removals, level, skaldTalent);
            }
            archetype.RemoveFeatures = removals
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        if (SkaldTalentLevels.Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    skaldTalent,
                    level) != 1) ||
            CountFeature(progression.LevelEntries, skaldTalent) !=
                SkaldTalentLevels.Length ||
            archetypesTradingTalents.Any(archetype =>
                SkaldTalentLevels.Any(level =>
                    CountFeatureAtLevel(
                        archetype.RemoveFeatures,
                        skaldTalent,
                        level) != 1) ||
                CountFeature(archetype.RemoveFeatures, skaldTalent) !=
                    SkaldTalentLevels.Length)) {
            throw new InvalidOperationException(
                "Skald Talents must follow the 2/5/8/11/14/17 schedule while preserving archetype tradeoffs.");
        }
    }

    private static void ConfigureHuntCallerRagePowers() {
        var huntCaller = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.HuntCallerArchetype);
        var ragePower = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SkaldRagePowerSelection);
        var removals = huntCaller.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(removals, ragePower);
        huntCaller.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(huntCaller.RemoveFeatures, ragePower) != 0) {
            throw new InvalidOperationException(
                "Huntcaller must retain every base Skald Rage Power selection.");
        }
    }

    private static void ValidateDanceOfAHundredCutsSupport() {
        var danceBuff = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.DanceOfAHundredCutsBuff);
        var extraEffects = danceBuff.GetComponents<BuffExtraEffects>().ToArray();
        var danceAbility = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.DanceOfAHundredCutsAbility);
        var rankConfigs = danceAbility.GetComponents<ContextRankConfig>().ToArray();
        var skaldClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.SkaldClass);

        if (extraEffects.Length != 1 ||
            BlueprintIds.SkaldRagingSongBuffs.Any(buffId =>
                extraEffects[0].m_CheckedBuffList.Count(reference =>
                    reference?.Get()?.AssetGuid.ToString() == buffId) != 1) ||
            rankConfigs.Length != 1 ||
            rankConfigs[0].m_Class.Count(reference =>
                reference?.Get() == skaldClass) != 1) {
            throw new InvalidOperationException(
                "Dance of a Hundred Cuts must recognize every Skald Raging Song and count Skald levels for duration.");
        }
    }

    private static BlueprintFeatureReference[] ReplaceFeatureReference(
        BlueprintFeatureReference[] references,
        BlueprintFeature original,
        BlueprintFeature replacement) =>
        references?.Select(reference => reference?.Get() == original
                ? BlueprintTool.GetRef<BlueprintFeatureReference>(
                    replacement.AssetGuid.ToString())
                : reference)
            .ToArray()
        ?? Array.Empty<BlueprintFeatureReference>();

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0) ?? 0;

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeatureBase feature) {
        var entry = entries.FirstOrDefault(candidate => candidate.Level == level);
        if (entry == null) {
            entry = new LevelEntry { Level = level, m_Features = new() };
            entries.Add(entry);
        }

        entry.m_Features ??= new();
        if (!entry.m_Features.Any(reference => reference?.Get() == feature)) {
            entry.m_Features.Add(BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                feature.AssetGuid.ToString()));
        }
    }

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }
}
