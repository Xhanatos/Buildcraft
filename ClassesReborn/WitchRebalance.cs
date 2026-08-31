using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class WitchRebalance {
    private static readonly int[] ClawMasteryLevels = { 3, 7, 11 };

    internal static void Configure() {
        var witchProgression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.WitchProgression);
        var hagbound = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.HagboundWitchArchetype);
        var hexSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.WitchHexSelection);
        var hagsClaws = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.HagboundWitchHagsClawsFeature);

        var removals = hagbound.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(removals, hexSelection);
        hagbound.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var clawMastery = FeatureConfigurator.New(
                "ClassesRebornHagboundClawMasteryFeature",
                FutureContentIds.Get("Witch.Hagbound.ClawMastery"))
            .SetDisplayName("ClassesReborn.Hagbound.ClawMastery.Name")
            .SetDescription("ClassesReborn.Hagbound.ClawMastery.Description")
            .SetIcon(hagsClaws.Icon)
            .SetIsClassFeature(true)
            .SetRanks(ClawMasteryLevels.Length)
            .AddComponent(new WeaponCategoryAttackBonus {
                Category = WeaponCategory.Claw,
                AttackBonus = 1,
                Descriptor = ModifierDescriptor.UntypedStackable,
            })
            .AddComponent(new HagboundClawMasteryCriticalRange())
            .Configure();

        var additions = hagbound.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(additions, clawMastery);
        foreach (var level in ClawMasteryLevels) {
            AddFeature(additions, level, clawMastery);
        }
        hagbound.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        ConfigureKeenEyedAdventurer(hexSelection);
        ConfigureLeyLineGuardian(witchProgression, hexSelection);
        var witchcraft = ConfigureWitchcraft(witchProgression, hexSelection);
        Validate(
            witchProgression,
            hagbound,
            hexSelection,
            clawMastery,
            witchcraft);
    }

    private static BlueprintFeature ConfigureWitchcraft(
        BlueprintProgression witchProgression,
        BlueprintFeatureSelection hexSelection) {
        var hexAbilities = hexSelection.m_AllFeatures
            .Select(reference => reference?.Get() as BlueprintFeature)
            .Where(feature => feature != null)
            .SelectMany(feature => feature.GetComponents<AddFacts>())
            .SelectMany(component => component.Facts)
            .OfType<BlueprintAbility>()
            .SelectMany(ExpandVariants)
            .Distinct()
            .ToArray();
        var witchcraft = FeatureConfigurator.New(
                "ClassesRebornWitchcraftFeature",
                FutureContentIds.Get("Witch.Witchcraft"))
            .SetDisplayName("ClassesReborn.Witchcraft.Name")
            .SetDescription("ClassesReborn.Witchcraft.Description")
            .SetIcon(hexSelection.Icon)
            .SetIsClassFeature(true)
            .SetRanks(1)
            .AddComponent(new WitchcraftMastery())
            .Configure();

        var levels = witchProgression.LevelEntries.ToList();
        RemoveFeature(levels, witchcraft);
        AddFeature(levels, 20, witchcraft);
        witchProgression.LevelEntries = levels
            .OrderBy(entry => entry.Level)
            .ToArray();
        WitchcraftRuntime.Configure(witchcraft, hexAbilities);
        return witchcraft;
    }

    private static void ConfigureLeyLineGuardian(
        BlueprintProgression witchProgression,
        BlueprintFeatureSelection hexSelection) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.LeyLineGuardianWitchArchetype);
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        if (CountFeatureAtLevel(removals, hexSelection, 8) != 1) {
            throw new InvalidOperationException(
                "Ley Line Guardian's native level-8 Hex removal was not found.");
        }

        RemoveFeatureAtLevel(removals, hexSelection, 8);
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeatureAtLevel(archetype.RemoveFeatures, hexSelection, 8) != 0 ||
            CountFeatureAtLevel(witchProgression.LevelEntries, hexSelection, 8) != 1) {
            throw new InvalidOperationException(
                "Ley Line Guardian must retain the standard level-8 Witch Hex selection.");
        }
    }

    private static void ConfigureKeenEyedAdventurer(
        BlueprintFeatureSelection hexSelection) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.RayMasterWitchArchetype);
        var cantripMastery = FeatureConfigurator.For(
                BlueprintIds.CantripMasteryFeature)
            .SetDescription(
                "ClassesReborn.KeenEyedAdventurer.CantripMastery.Description")
            .Configure();

        var additions = archetype.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        if (CountFeatureAtLevel(additions, cantripMastery, 20) != 1 ||
            CountFeatureAtLevel(removals, hexSelection, 20) != 1) {
            throw new InvalidOperationException(
                "Keen-Eyed Adventurer's native level-20 Cantrip Mastery exchange was not found.");
        }

        RemoveFeatureAtLevel(additions, cantripMastery, 20);
        AddFeature(additions, 10, cantripMastery);
        RemoveFeatureAtLevel(removals, hexSelection, 20);
        AddFeature(removals, 10, hexSelection);
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeatureAtLevel(archetype.AddFeatures, cantripMastery, 10) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, cantripMastery, 20) != 0 ||
            CountFeature(archetype.AddFeatures, cantripMastery) != 1 ||
            CountFeatureAtLevel(archetype.RemoveFeatures, hexSelection, 10) != 1 ||
            CountFeatureAtLevel(archetype.RemoveFeatures, hexSelection, 20) != 0) {
            throw new InvalidOperationException(
                "Keen-Eyed Adventurer must exchange its level-10 Hex for Cantrip Mastery.");
        }
    }

    private static void Validate(
        BlueprintProgression witchProgression,
        BlueprintArchetype hagbound,
        BlueprintFeatureSelection hexSelection,
        BlueprintFeature clawMastery,
        BlueprintFeature witchcraft) {
        var attackBonuses = clawMastery
            .GetComponents<WeaponCategoryAttackBonus>()
            .ToArray();
        var criticalBonuses = clawMastery
            .GetComponents<HagboundClawMasteryCriticalRange>()
            .ToArray();

        if (CountFeatureAtLevel(witchProgression.LevelEntries, hexSelection, 1) != 1 ||
            CountFeature(hagbound.RemoveFeatures, hexSelection) != 0) {
            throw new InvalidOperationException(
                "Hagbound must retain the standard level-1 Witch Hex selection.");
        }
        if (clawMastery.Ranks != ClawMasteryLevels.Length ||
            attackBonuses.Length != 1 ||
            attackBonuses[0].Category != WeaponCategory.Claw ||
            attackBonuses[0].AttackBonus != 1 ||
            attackBonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            criticalBonuses.Length != 1 ||
            ClawMasteryLevels.Any(level =>
                CountFeatureAtLevel(hagbound.AddFeatures, clawMastery, level) != 1) ||
            CountFeature(hagbound.AddFeatures, clawMastery) !=
                ClawMasteryLevels.Length) {
            throw new InvalidOperationException(
                "Hagbound Claw Mastery must have three ranks at levels 3, 7, and 11.");
        }
        if (CountFeatureAtLevel(witchProgression.LevelEntries, witchcraft, 20) != 1 ||
            CountFeature(witchProgression.LevelEntries, witchcraft) != 1 ||
            witchcraft.GetComponents<WitchcraftMastery>().Count() != 1) {
            throw new InvalidOperationException(
                "Witchcraft must be granted exactly once at Witch level 20.");
        }
    }

    private static IEnumerable<BlueprintAbility> ExpandVariants(
        BlueprintAbility ability) {
        yield return ability;
        var variants = ability.GetComponent<AbilityVariants>();
        if (variants == null) {
            yield break;
        }
        foreach (var variant in variants.Variants) {
            if (variant != null) {
                yield return variant;
            }
        }
    }

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeature feature) {
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
        if (entries == null) {
            return;
        }

        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }

    private static void RemoveFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature,
        int level) {
        foreach (var entry in entries?.Where(candidate => candidate.Level == level)
                     ?? Enumerable.Empty<LevelEntry>()) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
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
}
