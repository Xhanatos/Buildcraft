using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

namespace ClassesReborn;

internal static class ShifterRebalance {
    private static readonly int[] BonusCombatTalentLevels = { 8, 16 };

    internal static void Configure() {
        var shifterClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.ShifterClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ShifterProgression);
        var finalAspect = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ShifterFinalAspectFeature);
        var fighterBonusFeats = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FighterBonusFeatSelection);
        var bonusCombatTalent = FeatureSelectionConfigurator.New(
                "ClassesRebornShifterBonusCombatTalentSelection",
                BlueprintIds.ShifterBonusCombatTalentSelection)
            .CopyFrom(BlueprintIds.FighterBonusFeatSelection)
            .SetDisplayName("ClassesReborn.ShifterBonusCombatTalent.Name")
            .SetDescription("ClassesReborn.ShifterBonusCombatTalent.Description")
            .SetIsClassFeature(true)
            .Configure();
        var primalShifting = FeatureConfigurator.New(
                "ClassesRebornPrimalShiftingFeature",
                FutureContentIds.Get("Shifter.PrimalShifting"))
            .SetDisplayName("ClassesReborn.PrimalShifting.Name")
            .SetDescription("ClassesReborn.PrimalShifting.Description")
            .SetIcon(finalAspect.Icon)
            .SetIsClassFeature(true)
            .SetRanks(1)
            .AddComponent(new PrimalShifting())
            .Configure();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, bonusCombatTalent);
        foreach (var level in BonusCombatTalentLevels) {
            AddFeature(levelEntries, level, bonusCombatTalent);
        }
        RemoveFeature(levelEntries, primalShifting);
        AddFeature(levelEntries, 20, primalShifting);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        foreach (var archetype in shifterClass.Archetypes) {
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            RemoveFeature(removals, primalShifting);
            foreach (var entry in removals.Where(entry =>
                         entry.m_Features?.Any(reference =>
                             reference?.Get() == finalAspect) == true)) {
                AddFeature(removals, entry.Level, primalShifting);
            }
            archetype.RemoveFeatures = removals
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        var expectedChoices = fighterBonusFeats.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        var actualChoices = bonusCombatTalent.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        if (!expectedChoices.SetEquals(actualChoices) ||
            bonusCombatTalent.m_AllFeatures.Length != expectedChoices.Count ||
            BonusCombatTalentLevels.Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    bonusCombatTalent,
                    level) != 1) ||
            CountFeature(progression.LevelEntries, bonusCombatTalent) !=
                BonusCombatTalentLevels.Length ||
            shifterClass.Archetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, bonusCombatTalent) != 0) ||
            CountFeatureAtLevel(progression.LevelEntries, finalAspect, 20) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, primalShifting, 20) != 1 ||
            CountFeature(progression.LevelEntries, primalShifting) != 1 ||
            primalShifting.GetComponents<PrimalShifting>().Count() != 1 ||
            shifterClass.Archetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, primalShifting) !=
                CountFeature(archetype.RemoveFeatures, finalAspect))) {
            throw new InvalidOperationException(
                "Shifter combat talents or Primal Shifting inheritance are invalid.");
        }
    }

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
