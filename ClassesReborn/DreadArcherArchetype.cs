using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace ClassesReborn;

internal static class DreadArcherArchetype {
    private static readonly int[] BraveryLevels = { 2, 6, 10, 14, 18 };
    private static readonly int[] WeaponTrainingLevels = { 5, 9, 13, 17 };
    private static readonly int[] ReplacedBonusFeatLevels = { 1, 4, 8, 12, 16 };

    internal static void Configure() {
        var braveryIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterBravery).Icon;
        var deadlyAimIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DeadlyAimFeature).Icon;
        var trainingIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponTrainingBows).Icon;
        var carnageIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DreadfulCarnage).Icon;
        var demoralizeActions = CreateDemoralizeActions();

        var proficiencies = ConfigureProficiencies();
        var reputation = ConfigureMercilessReputation(braveryIcon);
        var rangedTraining = ConfigureRangedWeaponTraining(trainingIcon);
        var painfulShots = ConfigurePainfulShots(deadlyAimIcon, demoralizeActions);
        var merciless = ConfigureMerciless(carnageIcon);
        var rangeUpgrade = ConfigureDreadfulCarnageRangeUpgrade(carnageIcon, demoralizeActions);

        BlueprintTool.Create<BlueprintArchetype>(
            "DreadArcherArchetype",
            BlueprintIds.DreadArcherArchetype);
        var archetypeBlueprint = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.DreadArcherArchetype);
        archetypeBlueprint.LocalizedName = new LocalizedString {
            Key = "ClassesReborn.DreadArcher.Name",
        };
        archetypeBlueprint.LocalizedDescription = new LocalizedString {
            Key = "ClassesReborn.DreadArcher.Description",
        };

        var archetype = ArchetypeConfigurator.For(archetypeBlueprint)
            .SetClass(BlueprintIds.FighterClass)
            .SetIcon(deadlyAimIcon)
            .SetAddFeatures(LevelEntryBuilder.New()
                .AddEntry(1,
                    BlueprintIds.DreadArcherProficiencies,
                    BlueprintIds.DeadlyAimFeature)
                .AddEntry(2, BlueprintIds.MercilessReputation)
                .AddEntry(4, BlueprintIds.PainfulShots)
                .AddEntry(5, BlueprintIds.DreadArcherRangedWeaponTraining)
                .AddEntry(6, BlueprintIds.MercilessReputation)
                .AddEntry(8, BlueprintIds.Merciless)
                .AddEntry(9, BlueprintIds.DreadArcherRangedWeaponTraining)
                .AddEntry(10, BlueprintIds.MercilessReputation)
                .AddEntry(12, BlueprintIds.DreadfulCarnage)
                .AddEntry(13, BlueprintIds.DreadArcherRangedWeaponTraining)
                .AddEntry(14, BlueprintIds.MercilessReputation)
                .AddEntry(16, BlueprintIds.Merciless)
                .AddEntry(17, BlueprintIds.DreadArcherRangedWeaponTraining)
                .AddEntry(18,
                    BlueprintIds.MercilessReputation,
                    BlueprintIds.DreadfulCarnageRangeUpgrade))
            .SetRemoveFeatures(LevelEntryBuilder.New()
                .AddEntry(1,
                    BlueprintIds.FighterProficiencies,
                    BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(2, BlueprintIds.FighterBravery)
                .AddEntry(4, BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(5, BlueprintIds.FighterWeaponTrainingSelection)
                .AddEntry(6, BlueprintIds.FighterBravery)
                .AddEntry(8, BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(9,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(10, BlueprintIds.FighterBravery)
                .AddEntry(12, BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(13,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(14, BlueprintIds.FighterBravery)
                .AddEntry(16, BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(17,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(18, BlueprintIds.FighterBravery))
            .Configure();

        ValidateArchetype(
            archetype,
            proficiencies,
            reputation,
            rangedTraining,
            painfulShots,
            merciless,
            rangeUpgrade);
    }

    private static BlueprintFeature ConfigureProficiencies() =>
        FeatureConfigurator.New(
                "DreadArcherProficiencies",
                BlueprintIds.DreadArcherProficiencies)
            .SetDisplayName("ClassesReborn.DreadArcher.Proficiencies.Name")
            .SetDescription("ClassesReborn.DreadArcher.Proficiencies.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.MediumArmorProficiency).Icon)
            .SetIsClassFeature(true)
            .AddFacts(new() {
                BlueprintIds.SimpleWeaponProficiency,
                BlueprintIds.MartialWeaponProficiency,
                BlueprintIds.LightArmorProficiency,
                BlueprintIds.MediumArmorProficiency,
            })
            .Configure();

    private static BlueprintFeature ConfigureMercilessReputation(
        UnityEngine.Sprite icon) =>
        FeatureConfigurator.New(
                "DreadArcherMercilessReputation",
                BlueprintIds.MercilessReputation)
            .SetDisplayName("ClassesReborn.DreadArcher.MercilessReputation.Name")
            .SetDescription("ClassesReborn.DreadArcher.MercilessReputation.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetRanks(5)
            .AddComponent<MercilessReputationComponent>()
            .Configure();

    private static BlueprintFeature ConfigureRangedWeaponTraining(
        UnityEngine.Sprite icon) =>
        FeatureConfigurator.New(
                "DreadArcherRangedWeaponTraining",
                BlueprintIds.DreadArcherRangedWeaponTraining)
            .SetDisplayName("ClassesReborn.DreadArcher.RangedWeaponTraining.Name")
            .SetDescription("ClassesReborn.DreadArcher.RangedWeaponTraining.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetRanks(4)
            .AddComponent(new WeaponParametersAttackBonus {
                Ranged = true,
                AttackBonus = 1,
                Descriptor = ModifierDescriptor.WeaponTraining,
            })
            .AddComponent(new WeaponParametersDamageBonus {
                Ranged = true,
                DamageBonus = 1,
            })
            .AddComponent(new WeaponTraining())
            .Configure();

    private static BlueprintFeature ConfigurePainfulShots(
        UnityEngine.Sprite icon,
        ActionList demoralizeActions) =>
        FeatureConfigurator.New(
                "DreadArcherPainfulShots",
                BlueprintIds.PainfulShots)
            .SetDisplayName("ClassesReborn.DreadArcher.PainfulShots.Name")
            .SetDescription("ClassesReborn.DreadArcher.PainfulShots.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddComponent(new PainfulShotsComponent {
                m_DeadlyAimBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.DeadlyAimBuff),
                m_DeadlyAimEffectBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.DeadlyAimBuffEffect),
                DemoralizeActions = demoralizeActions,
            })
            .Configure();

    private static BlueprintFeature ConfigureMerciless(UnityEngine.Sprite icon) =>
        FeatureConfigurator.New(
                "DreadArcherMerciless",
                BlueprintIds.Merciless)
            .SetDisplayName("ClassesReborn.DreadArcher.Merciless.Name")
            .SetDescription("ClassesReborn.DreadArcher.Merciless.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetRanks(2)
            .AddComponent<MercilessDamageComponent>()
            .Configure();

    private static BlueprintFeature ConfigureDreadfulCarnageRangeUpgrade(
        UnityEngine.Sprite icon,
        ActionList demoralizeActions) =>
        FeatureConfigurator.New(
                "DreadArcherDreadfulCarnageRangeUpgrade",
                BlueprintIds.DreadfulCarnageRangeUpgrade)
            .SetDisplayName("ClassesReborn.DreadArcher.DreadfulCarnageRange.Name")
            .SetDescription("ClassesReborn.DreadArcher.DreadfulCarnageRange.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddComponent(new DreadfulCarnageRangeExtension {
                DemoralizeActions = demoralizeActions,
                NativeRadius = new(30),
                ExtendedRadius = new(50),
            })
            .Configure();

    private static ActionList CreateDemoralizeActions() {
        var persuasion = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.PersuasionUseAbility);
        var template = persuasion.GetComponent<AbilityEffectRunAction>()?
            .Actions.Actions
            .OfType<Demoralize>()
            .SingleOrDefault() ?? throw new InvalidOperationException(
                "The native Persuasion ability must contain one Demoralize action.");

        return ActionsBuilder.New().Add<Demoralize>(action => {
            action.m_Buff = template.m_Buff;
            action.m_GreaterBuff = template.m_GreaterBuff;
            action.DazzlingDisplay = template.DazzlingDisplay;
            action.m_SwordlordProwessFeature = template.m_SwordlordProwessFeature;
            action.m_ShatterConfidenceFeature = template.m_ShatterConfidenceFeature;
            action.m_ShatterConfidenceBuff = template.m_ShatterConfidenceBuff;
            action.Bonus = template.Bonus;
            action.TricksterRank3Actions = template.TricksterRank3Actions;
        }).Build();
    }

    private static void ValidateArchetype(
        BlueprintArchetype archetype,
        BlueprintFeature proficiencies,
        BlueprintFeature reputation,
        BlueprintFeature rangedTraining,
        BlueprintFeature painfulShots,
        BlueprintFeature merciless,
        BlueprintFeature rangeUpgrade) {
        var expectedProficiencies = new[] {
            BlueprintIds.SimpleWeaponProficiency,
            BlueprintIds.MartialWeaponProficiency,
            BlueprintIds.LightArmorProficiency,
            BlueprintIds.MediumArmorProficiency,
        }.Select(BlueprintTool.Get<BlueprintFeature>).ToArray();
        var facts = proficiencies.GetComponent<AddFacts>()?.m_Facts
            ?.Select(reference => reference?.Get()).ToArray() ??
            Array.Empty<BlueprintUnitFact>();

        if (facts.Length != expectedProficiencies.Length ||
            expectedProficiencies.Any(expected => facts.Count(fact => fact == expected) != 1) ||
            BraveryLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, reputation, level) != 1 ||
                CountFeatureAtLevel(archetype.RemoveFeatures,
                    BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterBravery), level) != 1) ||
            WeaponTrainingLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, rangedTraining, level) != 1) ||
            ReplacedBonusFeatLevels.Any(level => CountFeatureAtLevel(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.FighterBonusFeatSelection),
                level) != 1) ||
            CountFeatureAtLevel(archetype.AddFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DeadlyAimFeature), 1) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, painfulShots, 4) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, merciless, 8) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, merciless, 16) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DreadfulCarnage), 12) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, rangeUpgrade, 18) != 1 ||
            reputation.Ranks != 5 || rangedTraining.Ranks != 4 || merciless.Ranks != 2 ||
            rangedTraining.GetComponent<WeaponTraining>() == null ||
            (rangedTraining.Groups?.Contains(FeatureGroup.WeaponTraining) ?? false)) {
            throw new InvalidOperationException(
                "Dread Archer progression, proficiencies, feature ranks, or weapon-training compatibility is invalid.");
        }
    }

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
}
