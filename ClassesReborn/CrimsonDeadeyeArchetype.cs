using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class CrimsonDeadeyeArchetype {
    internal static void Configure() {
        var rangedTrainingIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponTrainingBows).Icon;
        var focusedAimIcon = BlueprintTool.Get<BlueprintAbility>(BlueprintIds.TrueStrikeAbility).Icon;
        var deadlyAimIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DeadlyAimFeature).Icon;
        var wailingIcon = BlueprintTool.Get<BlueprintAbility>(BlueprintIds.ShoutAbility).Icon;
        var feastIcon = BlueprintTool.Get<BlueprintAbility>(BlueprintIds.ShoutGreaterAbility).Icon;

        var proficiencies = ConfigureProficiencies();
        ConfigureRangedWeaponTraining(rangedTrainingIcon);
        ConfigureFocusedAim(focusedAimIcon);
        ConfigureWailingProjectiles(wailingIcon);
        ConfigureBloodInTheEyes(deadlyAimIcon);
        ConfigureFeastOnTheirScreams(feastIcon);

        BlueprintTool.Create<BlueprintArchetype>(
            "CrimsonDeadeyeArchetype",
            BlueprintIds.Archetype);
        var archetypeBlueprint = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.Archetype);
        archetypeBlueprint.LocalizedName = new LocalizedString {
            Key = "CrimsonDeadeye.Name",
        };
        archetypeBlueprint.LocalizedDescription = new LocalizedString {
            Key = "CrimsonDeadeye.Description",
        };

        var archetype = ArchetypeConfigurator.For(archetypeBlueprint)
            .SetClass(BlueprintIds.FighterClass)
            .SetIcon(deadlyAimIcon)
            .SetAddFeatures(LevelEntryBuilder.New()
                .AddEntry(1, BlueprintIds.CrimsonMarksmanProficiencies)
                .AddEntry(3, BlueprintIds.FocusedAimFeature)
                .AddEntry(5, BlueprintIds.RangedWeaponTraining)
                .AddEntry(6, BlueprintIds.WailingProjectilesFeature)
                .AddEntry(8, BlueprintIds.FocusedAimLevel8)
                .AddEntry(9,
                    BlueprintIds.RangedWeaponTraining,
                    BlueprintIds.BloodInTheEyesFeature)
                .AddEntry(10, BlueprintIds.WailingProjectilesLevel10)
                .AddEntry(12, BlueprintIds.FocusedAimLevel12)
                .AddEntry(13,
                    BlueprintIds.RangedWeaponTraining,
                    BlueprintIds.FeastOnTheirScreamsFeature)
                .AddEntry(15, BlueprintIds.WailingProjectilesLevel15)
                .AddEntry(17, BlueprintIds.RangedWeaponTraining)
                .AddEntry(18, BlueprintIds.FocusedAimLevel18))
            .SetRemoveFeatures(LevelEntryBuilder.New()
                .AddEntry(1, BlueprintIds.FighterProficiencies)
                .AddEntry(3, BlueprintIds.FighterArmorTraining)
                .AddEntry(5, BlueprintIds.FighterWeaponTrainingSelection)
                .AddEntry(6, BlueprintIds.FighterBonusFeatSelection)
                .AddEntry(7, BlueprintIds.FighterArmorTraining)
                .AddEntry(9,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(11, BlueprintIds.FighterArmorTraining)
                .AddEntry(13,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(15, BlueprintIds.FighterArmorTraining)
                .AddEntry(17,
                    BlueprintIds.FighterWeaponTrainingSelection,
                    BlueprintIds.FighterWeaponTrainingRankUpSelection)
                .AddEntry(19, BlueprintIds.FighterArmorMastery))
            .Configure();

        ValidateArchetype(archetype, proficiencies);
    }

    private static BlueprintFeature ConfigureProficiencies() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.LightArmorProficiency).Icon;

        return FeatureConfigurator.New(
                "CrimsonMarksmanProficiencies",
                BlueprintIds.CrimsonMarksmanProficiencies)
            .SetDisplayName("CrimsonDeadeye.Proficiencies.Name")
            .SetDescription("CrimsonDeadeye.Proficiencies.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddFacts(new() {
                BlueprintIds.SimpleWeaponProficiency,
                BlueprintIds.MartialWeaponProficiency,
                BlueprintIds.LightArmorProficiency,
            })
            .Configure();
    }

    private static void ValidateArchetype(
        BlueprintArchetype archetype,
        BlueprintFeature proficiencies) {
        var expectedProficiencies = new[] {
            BlueprintIds.SimpleWeaponProficiency,
            BlueprintIds.MartialWeaponProficiency,
            BlueprintIds.LightArmorProficiency,
        }
            .Select(BlueprintTool.Get<BlueprintFeature>)
            .ToArray();
        var addFacts = proficiencies.GetComponents<AddFacts>().ToArray();
        var grantedProficiencies = addFacts.Length == 1
            ? addFacts[0].m_Facts.Select(reference => reference?.Get()).ToArray()
            : Array.Empty<BlueprintUnitFact>();
        var armorTrainingLevels = new[] { 3, 7, 11, 15 };

        if (CountFeatureAtLevel(archetype.AddFeatures, proficiencies, 1) != 1 ||
            addFacts.Length != 1 ||
            grantedProficiencies.Length != expectedProficiencies.Length ||
            expectedProficiencies.Any(feature =>
                grantedProficiencies.Count(candidate => candidate == feature) != 1) ||
            CountFeature(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterProficiencies)) != 1 ||
            CountFeatureAtLevel(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterProficiencies),
                1) != 1 ||
            armorTrainingLevels.Any(level => CountFeatureAtLevel(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterArmorTraining),
                level) != 1) ||
            CountFeature(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterArmorTraining)) !=
                armorTrainingLevels.Length ||
            CountFeature(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterArmorMastery)) != 1 ||
            CountFeatureAtLevel(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterArmorMastery),
                19) != 1 ||
            CountFeature(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.FighterBonusFeatSelection)) != 1 ||
            CountFeatureAtLevel(
                archetype.RemoveFeatures,
                BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.FighterBonusFeatSelection),
                6) != 1) {
            throw new InvalidOperationException(
                "Crimson Marksman must retain only simple/martial weapon and light armor proficiencies, remove Armor Training and Armor Mastery, and lose only the level-6 Fighter Bonus Feat.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static void ConfigureRangedWeaponTraining(UnityEngine.Sprite icon) {
        var rangedTraining = FeatureConfigurator.New(
                "CrimsonDeadeyeRangedWeaponTraining",
                BlueprintIds.RangedWeaponTraining)
            .SetDisplayName("CrimsonDeadeye.RangedWeaponTraining.Name")
            .SetDescription("CrimsonDeadeye.RangedWeaponTraining.Description")
            .SetIcon(icon)
            .SetRanks(10)
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

        // Crimson Marksman gains this feature directly from its archetype
        // progression. Tagging it as global WeaponTraining makes every selection
        // filtered by that group offer it, including Disciple of the Pike's
        // spear-or-polearm-only selection.
        var pikeSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.DiscipleOfPikeWeaponTrainingSelection);
        var explicitlyListed =
            (pikeSelection.m_Features?.Any(reference => reference?.Get() == rangedTraining) ?? false) ||
            (pikeSelection.m_AllFeatures?.Any(reference => reference?.Get() == rangedTraining) ?? false);
        if ((rangedTraining.Groups?.Contains(FeatureGroup.WeaponTraining) ?? false) ||
            explicitlyListed ||
            rangedTraining.Ranks < 4 ||
            rangedTraining.GetComponent<WeaponTraining>() == null) {
            throw new InvalidOperationException(
                "Ranged Weapon Training must remain rankable but isolated from global weapon-training selections.");
        }
    }

    private static void ConfigureFocusedAim(UnityEngine.Sprite icon) {
        BuffConfigurator.New("CrimsonDeadeyeFocusedAimBuff", BlueprintIds.FocusedAimBuff)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.BuffDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new WeaponCriticalEdgeIncreaseStackable {
                IncludeCategories = Array.Empty<WeaponCategory>(),
                ExcludeCategories = Array.Empty<WeaponCategory>(),
                IncludeAttackTypes = new[] { AttackType.Ranged, AttackType.RangedTouch },
                ExcludeAttackTypes = Array.Empty<AttackType>(),
                Value = 2,
            })
            .Configure();

        BuffConfigurator.New("CrimsonDeadeyeFocusedAimAttackBuff", BlueprintIds.FocusedAimAttackBuff)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.AttackBuffDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new WeaponCriticalEdgeIncreaseStackable {
                IncludeCategories = Array.Empty<WeaponCategory>(),
                ExcludeCategories = Array.Empty<WeaponCategory>(),
                IncludeAttackTypes = new[] { AttackType.Ranged, AttackType.RangedTouch },
                ExcludeAttackTypes = Array.Empty<AttackType>(),
                Value = 2,
            })
            .AddComponent(new WeaponParametersAttackBonus {
                Ranged = true,
                AttackBonus = 2,
                Descriptor = ModifierDescriptor.UntypedStackable,
            })
            .Configure();

        FeatureConfigurator.New("CrimsonDeadeyeFocusedAimLevel8", BlueprintIds.FocusedAimLevel8)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Level8.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.Level8.Description")
            .SetIcon(icon)
            .AddIncreaseResourceAmount(BlueprintIds.FocusedAimResource, 1)
            .Configure();

        FeatureConfigurator.New("CrimsonDeadeyeFocusedAimLevel12", BlueprintIds.FocusedAimLevel12)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Level12.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.Level12.Description")
            .SetIcon(icon)
            .Configure();

        FeatureConfigurator.New("CrimsonDeadeyeFocusedAimLevel18", BlueprintIds.FocusedAimLevel18)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Level18.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.Level18.Description")
            .SetIcon(icon)
            .AddIncreaseResourceAmount(BlueprintIds.FocusedAimResource, 3)
            .Configure();

        AbilityResourceConfigurator.New("CrimsonDeadeyeFocusedAimResource", BlueprintIds.FocusedAimResource)
            .SetLocalizedName("CrimsonDeadeye.FocusedAim.ResourceName")
            .SetLocalizedDescription("CrimsonDeadeye.FocusedAim.ResourceDescription")
            .SetIcon(icon)
            .SetMax(1)
            .Configure();

        var focusedAimActions = ActionsBuilder.New()
            .Conditional(
                ConditionsBuilder.New().CasterHasFact(BlueprintIds.FocusedAimLevel12),
                ifTrue: ActionsBuilder.New().ApplyBuff(
                    BlueprintIds.FocusedAimAttackBuff,
                    ContextDuration.Fixed(6)),
                ifFalse: ActionsBuilder.New().Conditional(
                    ConditionsBuilder.New().CasterHasFact(BlueprintIds.FocusedAimLevel8),
                    ifTrue: ActionsBuilder.New().ApplyBuff(
                        BlueprintIds.FocusedAimBuff,
                        ContextDuration.Fixed(6)),
                    ifFalse: ActionsBuilder.New().ApplyBuff(
                        BlueprintIds.FocusedAimBuff,
                        ContextDuration.Fixed(3))));

        AbilityConfigurator.New("CrimsonDeadeyeFocusedAimAbility", BlueprintIds.FocusedAimAbility)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.Description")
            .SetIcon(icon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Swift)
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddAbilityEffectRunAction(focusedAimActions)
            .AddAbilityResourceLogic(
                amount: 1,
                isSpendResource: true,
                requiredResource: BlueprintIds.FocusedAimResource)
            .Configure();

        FeatureConfigurator.New("CrimsonDeadeyeFocusedAimFeature", BlueprintIds.FocusedAimFeature)
            .SetDisplayName("CrimsonDeadeye.FocusedAim.Name")
            .SetDescription("CrimsonDeadeye.FocusedAim.Description")
            .SetIcon(icon)
            .AddFacts(new() { BlueprintIds.FocusedAimAbility })
            .AddAbilityResources(
                amount: 0,
                resource: BlueprintIds.FocusedAimResource,
                restoreAmount: true,
                restoreOnLevelUp: true)
            .Configure();
    }

    private static void ConfigureWailingProjectiles(UnityEngine.Sprite icon) {
        ConfigureWailingBuff(
            "CrimsonDeadeyeWailingProjectilesD8Buff",
            BlueprintIds.WailingProjectilesD8Buff,
            "CrimsonDeadeye.WailingProjectiles.D8BuffDescription",
            new DiceFormula(1, DiceType.D8),
            icon);
        ConfigureWailingBuff(
            "CrimsonDeadeyeWailingProjectilesD12Buff",
            BlueprintIds.WailingProjectilesD12Buff,
            "CrimsonDeadeye.WailingProjectiles.D12BuffDescription",
            new DiceFormula(1, DiceType.D12),
            icon);
        ConfigureWailingBuff(
            "CrimsonDeadeyeWailingProjectiles2D8Buff",
            BlueprintIds.WailingProjectiles2D8Buff,
            "CrimsonDeadeye.WailingProjectiles.2D8BuffDescription",
            new DiceFormula(2, DiceType.D8),
            icon);

        FeatureConfigurator.New(
                "CrimsonDeadeyeWailingProjectilesLevel10",
                BlueprintIds.WailingProjectilesLevel10)
            .SetDisplayName("CrimsonDeadeye.WailingProjectiles.Level10.Name")
            .SetDescription("CrimsonDeadeye.WailingProjectiles.Level10.Description")
            .SetIcon(icon)
            .Configure();

        FeatureConfigurator.New(
                "CrimsonDeadeyeWailingProjectilesLevel15",
                BlueprintIds.WailingProjectilesLevel15)
            .SetDisplayName("CrimsonDeadeye.WailingProjectiles.Level15.Name")
            .SetDescription("CrimsonDeadeye.WailingProjectiles.Level15.Description")
            .SetIcon(icon)
            .Configure();

        FeatureConfigurator.New(
                "CrimsonDeadeyeWailingProjectilesFeature",
                BlueprintIds.WailingProjectilesFeature)
            .SetDisplayName("CrimsonDeadeye.WailingProjectiles.Name")
            .SetDescription("CrimsonDeadeye.WailingProjectiles.Description")
            .SetIcon(icon)
            .AddComponent(new WailingProjectilesTrigger {
                m_D8Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.WailingProjectilesD8Buff),
                m_D12Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.WailingProjectilesD12Buff),
                m_2D8Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.WailingProjectiles2D8Buff),
                m_Level10Feature = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.WailingProjectilesLevel10),
                m_Level15Feature = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.WailingProjectilesLevel15),
            })
            .Configure();
    }

    private static void ConfigureWailingBuff(
        string name,
        string guid,
        string description,
        DiceFormula dice,
        UnityEngine.Sprite icon) {
        BuffConfigurator.New(name, guid)
            .SetDisplayName("CrimsonDeadeye.WailingProjectiles.Name")
            .SetDescription(description)
            .SetIcon(icon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new RangedSonicDamage { Dice = dice })
            .Configure();
    }

    private static void ConfigureBloodInTheEyes(UnityEngine.Sprite icon) {
        BuffConfigurator.New("CrimsonDeadeyeBloodInTheEyesDebuff", BlueprintIds.BloodInTheEyesDebuff)
            .SetDisplayName("CrimsonDeadeye.BloodInTheEyes.Name")
            .SetDescription("CrimsonDeadeye.BloodInTheEyes.DebuffDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Replace)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.AC,
                value: -2)
            .Configure();

        FeatureConfigurator.New("CrimsonDeadeyeBloodInTheEyesFeature", BlueprintIds.BloodInTheEyesFeature)
            .SetDisplayName("CrimsonDeadeye.BloodInTheEyes.Name")
            .SetDescription("CrimsonDeadeye.BloodInTheEyes.Description")
            .SetIcon(icon)
            .AddComponent(new BloodInTheEyesTrigger {
                m_Debuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.BloodInTheEyesDebuff),
            })
            .Configure();
    }

    private static void ConfigureFeastOnTheirScreams(UnityEngine.Sprite icon) {
        BuffConfigurator.New(
                "CrimsonDeadeyeFeastOnTheirScreamsBuff",
                BlueprintIds.FeastOnTheirScreamsBuff)
            .SetDisplayName("CrimsonDeadeye.FeastOnTheirScreams.Name")
            .SetDescription("CrimsonDeadeye.FeastOnTheirScreams.BuffDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Stack)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SaveFortitude,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SaveReflex,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SaveWill,
                value: 1)
            .Configure();

        FeatureConfigurator.New(
                "CrimsonDeadeyeFeastOnTheirScreamsFeature",
                BlueprintIds.FeastOnTheirScreamsFeature)
            .SetDisplayName("CrimsonDeadeye.FeastOnTheirScreams.Name")
            .SetDescription("CrimsonDeadeye.FeastOnTheirScreams.Description")
            .SetIcon(icon)
            .AddComponent(new FeastOnTheirScreamsTrigger {
                m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.FeastOnTheirScreamsBuff),
            })
            .Configure();
    }
}
