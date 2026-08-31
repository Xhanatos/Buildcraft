using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class SlayerRebalance {
    private static readonly int[] SneakAttackLevels = { 3, 6, 9, 12, 15, 18 };
    private static readonly int[] SlayerTalentLevels =
        { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

    internal static void Configure() {
        ConfigureArcaneEnforcer();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.SlayerProgression);
        var imitator = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ImitatorArchetype);
        var sneakAttack = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.SneakAttackFeature);
        var studiedTargetBuff = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.SlayerStudyTargetBuff);
        var masterSlayer = FeatureConfigurator.For(
                BlueprintIds.MasterSlayerFeature)
            .SetDescription("ClassesReborn.MasterSlayer.Description")
            .AddComponent(new MasterSlayerStudiedTargetInsight {
                m_StudiedTargetBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.SlayerStudyTargetBuff),
            })
            .Configure();
        var talentSelections = BlueprintIds.SlayerTalentSelections
            .Select(BlueprintTool.Get<BlueprintFeatureSelection>)
            .ToArray();

        ValidateBaseProgression(progression, sneakAttack, talentSelections);
        ValidateMasterSlayer(progression, masterSlayer, studiedTargetBuff);
        ConfigureTalentOptions(talentSelections);
        RequestedTalentRebalance.ConfigureSlayerTalents(talentSelections);

        var removals = imitator.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(removals, sneakAttack);
        foreach (var talentSelection in talentSelections) {
            RemoveFeature(removals, talentSelection);
        }
        imitator.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(imitator.RemoveFeatures, sneakAttack) != 0 ||
            talentSelections.Any(selection =>
                CountFeature(imitator.RemoveFeatures, selection) != 0)) {
            throw new InvalidOperationException(
                "Imitator must retain every base Slayer Sneak Attack rank and Slayer Talent selection.");
        }
    }

    private static void ConfigureArcaneEnforcer() {
        const string reservoirDescription =
            "ClassesReborn.ArcaneEnforcer.ArcaneReservoir.Description";
        const string exploitsDescription =
            "ClassesReborn.ArcaneEnforcer.ArcaneExploits.Description";

        var reservoirFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ArcaneEnforcerArcaneReservoirFeature);
        var nativeResources = reservoirFeature
            .GetComponents<AddAbilityResources>()
            .Select(component => component.m_Resource?.Get())
            .Where(resource => resource != null)
            .Distinct()
            .ToList();
        if (nativeResources.Count == 0) {
            throw new InvalidOperationException(
                "Arcane Enforcer Arcane Reservoir no longer grants an ability resource.");
        }

        var affectedResources = nativeResources.ToList();
        var sharedReservoir = BlueprintTool.Get<BlueprintAbilityResource>(
            BlueprintIds.ArcanistReservoirResource);
        if (!affectedResources.Contains(sharedReservoir)) {
            affectedResources.Add(sharedReservoir);
        }

        var configuredFeature = FeatureConfigurator.For(
                BlueprintIds.ArcaneEnforcerArcaneReservoirFeature)
            .SetDescription(reservoirDescription)
            .AddComponent(new ArcaneEnforcerIntelligenceExploitScaling {
                m_Resources = affectedResources
                    .Select(resource => BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        resource.AssetGuid.ToString()))
                    .ToArray(),
            })
            .Configure();

        FeatureConfigurator.For(BlueprintIds.ArcanistExploitsFeature)
            .SetDescription(exploitsDescription)
            .Configure();
        FeatureSelectionConfigurator.For(BlueprintIds.ArcanistExploitSelection)
            .SetDescription(exploitsDescription)
            .Configure();

        var scalingComponents = configuredFeature
            .GetComponents<ArcaneEnforcerIntelligenceExploitScaling>()
            .ToArray();
        if (scalingComponents.Length != 1 ||
            affectedResources.Any(resource =>
                scalingComponents[0].m_Resources.Count(reference =>
                    reference?.Get() == resource) != 1)) {
            throw new InvalidOperationException(
                "Arcane Enforcer must use Intelligence for its reservoir and Arcane Exploit scaling.");
        }
    }

    private static void ValidateMasterSlayer(
        BlueprintProgression progression,
        BlueprintFeature masterSlayer,
        BlueprintBuff studiedTargetBuff) {
        var components = masterSlayer
            .GetComponents<MasterSlayerStudiedTargetInsight>()
            .ToArray();
        if (CountFeatureAtLevel(
                progression.LevelEntries,
                masterSlayer,
                20) != 1 ||
            CountFeature(progression.LevelEntries, masterSlayer) != 1 ||
            components.Length != 1 ||
            components[0].m_StudiedTargetBuff?.Get() != studiedTargetBuff) {
            throw new InvalidOperationException(
                "Master Slayer must grant its Intelligence-based insight attack bonus against the Slayer's own studied targets.");
        }
    }

    private static void ConfigureTalentOptions(
        IReadOnlyList<BlueprintFeatureSelection> talentSelections) {
        var evasion = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerEvasion);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var uncannyDodge = FeatureConfigurator.New(
                "ClassesRebornSlayerTalentUncannyDodge",
                BlueprintIds.SlayerTalentUncannyDodge)
            .SetDisplayName("ClassesReborn.SlayerTalentUncannyDodge.Name")
            .SetDescription("ClassesReborn.SlayerTalentUncannyDodge.Description")
            .SetIcon(uncannyDodgeChecker.Icon)
            .SetRanks(1)
            .SetHideInCharacterSheetAndLevelUp(false)
            .AddFacts(new() { BlueprintIds.UncannyDodgeChecker })
            .Configure();
        var originalCombatTrick = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CombatTrick);
        var repeatableCombatTrick = FeatureSelectionConfigurator.New(
                "ClassesRebornRepeatableSlayerCombatTrick",
                BlueprintIds.RepeatableSlayerCombatTrick)
            .CopyFrom(BlueprintIds.CombatTrick)
            .SetRanks(20)
            .Configure();

        foreach (var selection in talentSelections) {
            selection.m_AllFeatures = ConfigureSelectionFeatures(
                selection.m_AllFeatures,
                originalCombatTrick,
                repeatableCombatTrick,
                evasion,
                uncannyDodge);
            selection.m_Features = ConfigureSelectionFeatures(
                selection.m_Features,
                originalCombatTrick,
                repeatableCombatTrick,
                evasion,
                uncannyDodge);
        }

        var uncannyDodgeFacts = uncannyDodge
            .GetComponents<AddFacts>()
            .SelectMany(component =>
                component.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
            .ToArray();
        if (talentSelections.Any(selection =>
                CountSelectionFeature(selection, evasion) != 1 ||
                CountSelectionFeature(selection, uncannyDodge) != 1 ||
                CountSelectionFeature(selection, repeatableCombatTrick) != 1 ||
                CountSelectionFeature(selection, originalCombatTrick) != 0 ||
                CountSelectionFeature(selection, uncannyDodgeChecker) != 0) ||
            repeatableCombatTrick.Ranks < 20 ||
            uncannyDodge.HideInCharacterSheetAndLevelUp ||
            uncannyDodge.Ranks != 1 ||
            uncannyDodgeFacts.Length != 1 ||
            uncannyDodgeFacts.Count(reference =>
                reference?.Get() == uncannyDodgeChecker) != 1) {
            throw new InvalidOperationException(
                "Every Slayer Talent tier must offer Evasion, a visible multiclass-safe Uncanny Dodge, and repeatable Combat Trick exactly once.");
        }
    }

    private static void ValidateBaseProgression(
        BlueprintProgression progression,
        BlueprintFeature sneakAttack,
        IReadOnlyList<BlueprintFeatureSelection> talentSelections) {
        if (SneakAttackLevels.Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    sneakAttack,
                    level) != 1) ||
            CountFeature(progression.LevelEntries, sneakAttack) !=
                SneakAttackLevels.Length ||
            SlayerTalentLevels.Any(level =>
                talentSelections.Sum(selection =>
                    CountFeatureAtLevel(
                        progression.LevelEntries,
                        selection,
                        level)) != 1) ||
            talentSelections.Sum(selection =>
                CountFeature(progression.LevelEntries, selection)) !=
                SlayerTalentLevels.Length) {
            throw new InvalidOperationException(
                "The native Slayer Sneak Attack or Slayer Talent progression no longer matches the expected schedule.");
        }
    }

    private static BlueprintFeatureReference[] ConfigureSelectionFeatures(
        BlueprintFeatureReference[] references,
        BlueprintFeature originalCombatTrick,
        BlueprintFeature repeatableCombatTrick,
        params BlueprintFeature[] addedFeatures) {
        var result = references?
            .Select(reference => reference?.Get() == originalCombatTrick
                ? BlueprintTool.GetRef<BlueprintFeatureReference>(
                    repeatableCombatTrick.AssetGuid.ToString())
                : reference)
            .Where(reference =>
                reference?.Get() != repeatableCombatTrick &&
                !addedFeatures.Contains(reference?.Get()))
            .ToList()
            ?? new List<BlueprintFeatureReference>();
        result.Add(BlueprintTool.GetRef<BlueprintFeatureReference>(
            repeatableCombatTrick.AssetGuid.ToString()));
        foreach (var feature in addedFeatures) {
            result.Add(BlueprintTool.GetRef<BlueprintFeatureReference>(
                feature.AssetGuid.ToString()));
        }
        return result.ToArray();
    }

    private static int CountSelectionFeature(
        BlueprintFeatureSelection selection,
        BlueprintFeature feature) =>
        selection.m_AllFeatures?.Count(reference =>
            reference?.Get() == feature) ?? 0;

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

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }
}
