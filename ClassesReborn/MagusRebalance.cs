using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;

namespace ClassesReborn;

internal static class MagusRebalance {
    private static readonly int[] BonusFeatLevels = { 2, 6, 10, 14, 18 };

    internal static void Configure() {
        var magusClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.MagusClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.MagusProgression);
        var bonusFeat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.MagusBonusFeatSelection);
        var swordSaintCannyDefense = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.SwordSaintCannyDefense);

        var cannyDefense = FeatureConfigurator.New(
                "ClassesRebornMagusCannyDefenseFeature",
                BlueprintIds.MagusCannyDefenseFeature)
            .SetDisplayName("ClassesReborn.MagusCannyDefense.Name")
            .SetDescription("ClassesReborn.MagusCannyDefense.Description")
            .SetIcon(swordSaintCannyDefense.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new CannyDefensePermanent {
                m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.MagusClass),
                RequiresKensai = false,
                m_ChosenWeaponBlueprint = null,
            })
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Intelligence,
            })
            .Configure();

        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, cannyDefense);
        RemoveFeature(levelEntries, bonusFeat);
        AddFeature(levelEntries, 1, cannyDefense);
        foreach (var level in BonusFeatLevels) {
            AddFeature(levelEntries, level, bonusFeat);
        }
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var archetypes = BlueprintIds.MagusArchetypes
            .Select(BlueprintTool.Get<BlueprintArchetype>)
            .ToArray();
        foreach (var archetype in archetypes) {
            var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
            RemoveFeature(removals, bonusFeat);
            RemoveFeature(removals, cannyDefense);
            if (archetype.AssetGuid.ToString() == BlueprintIds.SwordSaintArchetype) {
                AddFeature(removals, 1, cannyDefense);
            }
            archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
        }

        Validate(
            magusClass,
            progression,
            cannyDefense,
            bonusFeat,
            swordSaintCannyDefense,
            archetypes);
    }

    private static void Validate(
        BlueprintCharacterClass magusClass,
        BlueprintProgression progression,
        BlueprintFeature cannyDefense,
        BlueprintFeatureSelection bonusFeat,
        BlueprintFeature swordSaintCannyDefense,
        IReadOnlyCollection<BlueprintArchetype> archetypes) {
        var cannyComponents = cannyDefense
            .GetComponents<CannyDefensePermanent>()
            .ToArray();
        var recalculations = cannyDefense
            .GetComponents<RecalculateOnStatChange>()
            .ToArray();
        var swordSaint = archetypes.Single(archetype =>
            archetype.AssetGuid.ToString() == BlueprintIds.SwordSaintArchetype);
        var otherArchetypes = archetypes.Where(archetype => archetype != swordSaint);

        if (CountFeatureAtLevel(progression.LevelEntries, cannyDefense, 1) != 1 ||
            CountFeature(progression.LevelEntries, cannyDefense) != 1 ||
            cannyComponents.Length != 1 ||
            cannyComponents[0].CharacterClass != magusClass ||
            cannyComponents[0].RequiresKensai ||
            cannyComponents[0].ChosenWeaponBlueprint != null ||
            recalculations.Length != 1 ||
            recalculations[0].Stat != StatType.Intelligence ||
            CountFeature(progression.LevelEntries, bonusFeat) != BonusFeatLevels.Length ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, bonusFeat, level) != 1) ||
            progression.LevelEntries.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                (entry.m_Features?.Any(reference => reference?.Get() == bonusFeat) ?? false)) ||
            archetypes.Any(archetype => CountFeature(archetype.RemoveFeatures, bonusFeat) != 0) ||
            CountFeatureAtLevel(swordSaint.RemoveFeatures, cannyDefense, 1) != 1 ||
            CountFeature(swordSaint.RemoveFeatures, cannyDefense) != 1 ||
            CountFeatureAtLevel(swordSaint.AddFeatures, swordSaintCannyDefense, 1) != 1 ||
            otherArchetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, cannyDefense) != 0)) {
            throw new InvalidOperationException(
                "Every Magus must gain unrestricted Canny Defense at level 1 and Magus Bonus Feats at levels 2, 6, 10, 14, and 18, while Sword Saint keeps only its original chosen-weapon version.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) =>
        entries.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0);

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature,
        int level) =>
        entries
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0);

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
        List<LevelEntry> entries,
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
        entries.RemoveAll(entry => entry.m_Features == null || entry.m_Features.Count == 0);
    }
}

[HarmonyPatch(typeof(CannyDefensePermanent), "CheckWeapon")]
internal static class MagusCannyDefenseWeaponPatch {
    [HarmonyPrefix]
    private static bool IgnoreWeaponForClassesRebornMagusCannyDefense(
        CannyDefensePermanent __instance,
        ref bool __result) {
        if (!Main.Settings.Magus ||
            __instance.Fact?.Blueprint?.AssetGuid.ToString() !=
            BlueprintIds.MagusCannyDefenseFeature) {
            return true;
        }

        __result = true;
        return false;
    }
}
