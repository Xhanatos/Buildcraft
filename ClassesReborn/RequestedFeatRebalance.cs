using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static partial class FeatRebalance {
    private const string NativeMultiattack = "8ac319e47057e2741b42229210eb43ed";
    private const string NativePounce = "1a8149c09e0bdfc48a305ee6ac3729a8";
    private const string NativeTandemTrip = "d26eb8ab2aabd0e45a4d7eec0340bbce";
    private const string NativeVolleyFire = "c4b555225f565bb40a855c1bfeeff07e";
    private const string NativeFocusedShot = "f979ed68d1e74d21962edc66f0a1d169";
    private const string ImprovedTrip = "0f15c6f70d8fb2b49aa6cc24239cc5fa";
    private const string CavalierTacticianAbility = "3ff8ef7ba7b5be0429cf32cd4ddf637c";
    private const string CavalierTacticianAbilitySwift = "78b8d3fd0999f964f82d1c5ec30900e8";

    private static readonly WeaponCategory[] FocusedShotCategories = {
        WeaponCategory.Shortbow,
        WeaponCategory.Longbow,
        WeaponCategory.LightCrossbow,
        WeaponCategory.HeavyCrossbow,
        WeaponCategory.HandCrossbow,
        WeaponCategory.LightRepeatingCrossbow,
        WeaponCategory.HeavyRepeatingCrossbow,
    };

    private static string RequestedFeatId(string name) =>
        FutureContentIds.Get($"RequestedFeat.{name}");

    private static void ConfigureRequestedFeats() {
        if (Main.Settings.Multiattack) {
            ConfigureMultiattack();
        }
        if (Main.Settings.ImprovedNaturalAttack) {
            ConfigureImprovedNaturalAttack();
        }
        if (Main.Settings.ClawPounce) {
            ConfigureClawPounce();
        }
        if (Main.Settings.CloseQuartersThrower) {
            ConfigureCloseQuartersThrower();
        }
        if (Main.Settings.JabbingStyle || Main.Settings.JabbingMaster) {
            ConfigureJabbingChain();
        }
        if (Main.Settings.TandemTrip) {
            ConfigureTandemTrip();
        }
        if (Main.Settings.VolleyFire) {
            ConfigureVolleyFire();
        }
        if (Main.Settings.FocusedShot) {
            ConfigureFocusedShot();
        }
        if (Main.Settings.TacticalReflexes) {
            ConfigureTacticalReflexes();
        }
    }

    private static void ConfigureMultiattack() {
        var icon = BlueprintTool.Get<BlueprintFeature>(NativeMultiattack).Icon;
        var feat = FeatureConfigurator.New(
                "ClassesRebornMultiattackFeat",
                RequestedFeatId("Multiattack"))
            .SetDisplayName("ClassesReborn.Multiattack.Name")
            .SetDescription("ClassesReborn.Multiattack.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new NaturalAttackCountPrerequisite { Minimum = 1 })
            .AddComponent(new AddMechanicsFeature {
                m_Feature = AddMechanicsFeature.MechanicsFeatureType
                    .ReduceSecondaryNaturalAttackPenalty,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureImprovedNaturalAttack() {
        var icon = BlueprintTool.Get<BlueprintFeature>(NativeMultiattack).Icon;
        var feat = ParametrizedFeatureConfigurator.New(
                "ClassesRebornImprovedNaturalAttackFeat",
                RequestedFeatId("ImprovedNaturalAttack"))
            .SetDisplayName("ClassesReborn.ImprovedNaturalAttack.Name")
            .SetDescription("ClassesReborn.ImprovedNaturalAttack.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(20)
            .SetReapplyOnLevelUp(true)
            .SetParameterType(FeatureParameterType.WeaponCategory)
            .SetWeaponSubCategory(WeaponSubCategory.Natural)
            .SetRequireProficiency(true)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 4)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Damage | FeatureTag.Melee)
            .AddComponent(new NaturalAttackCountPrerequisite { Minimum = 1 })
            .AddComponent(new ImprovedNaturalAttackComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureClawPounce() {
        var icon = BlueprintTool.Get<BlueprintFeature>(NativePounce).Icon;
        var feat = FeatureConfigurator.New(
                "ClassesRebornClawPounceFeat",
                RequestedFeatId("ClawPounce"))
            .SetDisplayName("ClassesReborn.ClawPounce.Name")
            .SetDescription("ClassesReborn.ClawPounce.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 10)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new NaturalAttackCountPrerequisite {
                AnyNaturalAttack = false,
                Category = WeaponCategory.Claw,
                Minimum = 2,
            })
            .AddComponent(new ClawPounceComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureCloseQuartersThrower() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFocus).Icon;
        var feat = ParametrizedFeatureConfigurator.New(
                "ClassesRebornCloseQuartersThrowerFeat",
                RequestedFeatId("CloseQuartersThrower"))
            .SetDisplayName("ClassesReborn.CloseQuartersThrower.Name")
            .SetDescription("ClassesReborn.CloseQuartersThrower.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(20)
            .SetReapplyOnLevelUp(true)
            .SetParameterType(FeatureParameterType.WeaponCategory)
            .SetWeaponSubCategory(WeaponSubCategory.Thrown)
            .SetRequireProficiency(true)
            .SetPrerequisite(BlueprintIds.WeaponFocus)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
            .AddComponent(new PointBlankMasterParametrized())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureJabbingChain() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUnarmedStrike).Icon;
        var style = FeatureConfigurator.New(
                "ClassesRebornJabbingStyleFeat",
                RequestedFeatId("JabbingStyle"))
            .SetDisplayName("ClassesReborn.JabbingStyle.Name")
            .SetDescription("ClassesReborn.JabbingStyle.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteFeature(BlueprintIds.ImprovedUnarmedStrike)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 6)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Damage | FeatureTag.Melee)
            .AddComponent(new JabbingStyleComponent())
            .Configure();
        var master = FeatureConfigurator.New(
                "ClassesRebornJabbingMasterFeat",
                RequestedFeatId("JabbingMaster"))
            .SetDisplayName("ClassesReborn.JabbingMaster.Name")
            .SetDescription("ClassesReborn.JabbingMaster.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteFeature(style)
            .AddPrerequisiteFeature(BlueprintIds.PowerAttack)
            .AddPrerequisiteFeature(BlueprintIds.ImprovedUnarmedStrike)
            .AddPrerequisiteFeature(FeatureRefs.Dodge.ToString())
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 12)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Damage | FeatureTag.Melee)
            .AddComponent(new JabbingMasterComponent())
            .Configure();
        if (Main.Settings.JabbingStyle) {
            AddAsFeat(style, combatFeat: true);
        }
        if (Main.Settings.JabbingMaster) {
            AddAsFeat(master, combatFeat: true);
        }
    }

    private static void ConfigureTandemTrip() {
        var feat = FeatureConfigurator.For(NativeTandemTrip)
            .SetHideInUI(false)
            .SetHideInCharacterSheetAndLevelUp(false)
            .SetGroups(
                FeatureGroup.Feat,
                FeatureGroup.CombatFeat,
                FeatureGroup.TeamworkFeat)
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureVolleyFire() {
        var icon = BlueprintTool.Get<BlueprintFeature>(NativeVolleyFire).Icon;
        var tacticianAbilities = new[] {
            BlueprintTool.Get<BlueprintAbility>(CavalierTacticianAbility),
            BlueprintTool.Get<BlueprintAbility>(CavalierTacticianAbilitySwift),
        };
        var originalComponents = tacticianAbilities.ToDictionary(
            ability => ability,
            ability => ability.ComponentsArray ?? Array.Empty<BlueprintComponent>());
        var applyEffectType = typeof(AbilityApplyFact).BaseType;
        BlueprintFeature feat;
        try {
            foreach (var ability in tacticianAbilities) {
                ability.ComponentsArray = originalComponents[ability]
                    .Where(component =>
                        component is AbilityApplyFact ||
                        applyEffectType?.IsInstanceOfType(component) != true)
                    .ToArray();
            }

            feat = FeatureConfigurator.New(
                    "ClassesRebornVolleyFireFeat",
                    RequestedFeatId("VolleyFire"))
                .SetDisplayName("ClassesReborn.VolleyFire.Name")
                .SetDescription("ClassesReborn.VolleyFire.Description")
                .SetIcon(icon)
                .SetGroups(
                    FeatureGroup.Feat,
                    FeatureGroup.CombatFeat,
                    FeatureGroup.TeamworkFeat)
                .AddAsTeamworkFeat(
                    cavalierBuffGuid: RequestedFeatId("VolleyFire.CavalierBuff"),
                    vanguardBuffGuid: RequestedFeatId("VolleyFire.VanguardBuff"),
                    vanguardAbilityGuid: RequestedFeatId("VolleyFire.VanguardAbility"),
                    packRagerBuffGuid: RequestedFeatId("VolleyFire.PackRagerBuff"),
                    packRagerAreaGuid: RequestedFeatId("VolleyFire.PackRagerArea"),
                    packRagerAreaBuffGuid: RequestedFeatId("VolleyFire.PackRagerAreaBuff"),
                    packRagerToggleBuffGuid: RequestedFeatId("VolleyFire.PackRagerToggleBuff"),
                    packRagerToggleGuid: RequestedFeatId("VolleyFire.PackRagerToggle"))
                .AddPrerequisiteFeature("0da0c194d6e1d43419eb8d990b28e0ab")
                .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
                .AddComponent(new VolleyFireComponent())
                .Configure();
        }
        finally {
            foreach (var ability in tacticianAbilities) {
                ability.ComponentsArray = originalComponents[ability];
            }
        }
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureFocusedShot() {
        var icon = BlueprintTool.Get<BlueprintFeature>(NativeFocusedShot).Icon;
        var feat = FeatureConfigurator.New(
                "ClassesRebornFocusedShotFeat",
                RequestedFeatId("FocusedShot"))
            .SetDisplayName("ClassesReborn.FocusedShot.Name")
            .SetDescription("ClassesReborn.FocusedShot.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteStatValue(StatType.Intelligence, 13)
            .AddFeatureTagsComponent(FeatureTag.Damage | FeatureTag.Ranged)
            .AddComponent(new FocusedShotComponent {
                Categories = FocusedShotCategories,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureTacticalReflexes() {
        var combatReflexes = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.CombatReflexes);
        var feat = FeatureConfigurator.New(
                "ClassesRebornTacticalReflexesFeat",
                RequestedFeatId("TacticalReflexes"))
            .SetDisplayName("ClassesReborn.TacticalReflexes.Name")
            .SetDescription("ClassesReborn.TacticalReflexes.Description")
            .SetIcon(combatReflexes.Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteStatValue(StatType.Intelligence, 13)
            .AddPrerequisiteNoFeature(BlueprintIds.CombatReflexes)
            .AddFeatureTagsComponent(FeatureTag.Attack)
            .AddFacts(new() { BlueprintIds.CombatReflexes })
            .AddComponent(new TacticalReflexesComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }
}
