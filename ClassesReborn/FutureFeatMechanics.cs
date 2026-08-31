using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("07266758-8c92-4e66-b505-46c53de183ee")]
public sealed class RacialHeritageMarker : UnitFactComponentDelegate {
    public BlueprintRaceReference m_Race;
}

[AllowedOn(typeof(BlueprintFeatureBase))]
[TypeId("187e614b-bfc7-46a0-88fe-61f016966805")]
public sealed class CharacterRacePrerequisite : Prerequisite {
    public BlueprintRaceReference m_Race;
    public string RaceName;

    public override bool CheckInternal(
        FeatureSelectionState selectionState,
        UnitDescriptor unit,
        LevelUpState state) {
        var race = state?.SelectedRace ?? unit?.Progression?.Race;
        var requiredRace = m_Race?.Get();
        return race != null && race == requiredRace ||
            requiredRace != null && unit?.Progression?.Features
                .SelectFactComponents<RacialHeritageMarker>()
                .Any(component => component.m_Race?.Get() == requiredRace) == true;
    }

    public override string GetUITextInternal(UnitDescriptor unit) =>
        $"Race: {RaceName}";
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("eaed042c-9a4c-409d-b63e-b04d585a49ae")]
public sealed class FeralCombatTrainingComponent : UnitFactComponentDelegate { }

internal static class FeralCombatTrainingHelpers {
    internal static bool HasTraining(UnitDescriptor owner, WeaponCategory category) =>
        owner?.Progression?.Features
            .SelectFactComponents<FeralCombatTrainingComponent>()
            .Any(component => component.Param?.WeaponCategory == category) == true;

    internal static bool WeaponQualifies(
        UnitDescriptor owner,
        BlueprintItemWeapon weapon) =>
        owner != null && weapon != null && HasTraining(owner, weapon.Category);
}

[HarmonyPatch(
    typeof(AbstractWeaponTrigger),
    "IsSuitable",
    new[] { typeof(RuleAttackWithWeapon) })]
internal static class FeralAbstractWeaponTriggerPatch {
    private static void Postfix(
        AbstractWeaponTrigger __instance,
        RuleAttackWithWeapon evt,
        ref bool __result) {
        if (__result || __instance.CheckWeaponCategory != true ||
            __instance.Category != WeaponCategory.UnarmedStrike) {
            return;
        }
        __result = FeralCombatTrainingHelpers.WeaponQualifies(
            evt?.Initiator?.Descriptor,
            evt?.Weapon?.Blueprint);
    }
}

[HarmonyPatch(
    typeof(AbilityCasterMainWeaponCheck),
    nameof(AbilityCasterMainWeaponCheck.IsCasterRestrictionPassed))]
internal static class FeralAbilityCasterWeaponPatch {
    private static void Postfix(
        AbilityCasterMainWeaponCheck __instance,
        UnitEntityData caster,
        ref bool __result) {
        if (__result || __instance.Category?.Contains(WeaponCategory.UnarmedStrike) != true) {
            return;
        }
        __result = FeralCombatTrainingHelpers.WeaponQualifies(
            caster?.Descriptor,
            caster?.Body?.PrimaryHand?.MaybeWeapon?.Blueprint);
    }
}

[HarmonyPatch(typeof(AdditionalStatBonusOnAttackDamage), "CheckConditions")]
internal static class FeralAdditionalStatBonusPatch {
    private static void Postfix(
        AdditionalStatBonusOnAttackDamage __instance,
        RuleCalculateWeaponStats evt,
        ref bool __result) {
        if (__result || !__instance.CheckCategory ||
            __instance.Category != WeaponCategory.UnarmedStrike) {
            return;
        }
        __result = FeralCombatTrainingHelpers.WeaponQualifies(
            evt?.Initiator?.Descriptor,
            evt?.Weapon?.Blueprint);
    }
}

internal static class FeralAdditionalDiceHelper {
    internal static void TryQualify(
        AdditionalDiceOnAttack component,
        UnitDescriptor owner,
        BlueprintItemWeapon weapon,
        ref bool result) {
        if (!result && component.CheckWeaponCategory &&
            component.Category == WeaponCategory.UnarmedStrike) {
            result = FeralCombatTrainingHelpers.WeaponQualifies(owner, weapon);
        }
    }
}

[HarmonyPatch(
    typeof(AdditionalDiceOnAttack),
    "CheckCondition",
    new[] { typeof(RuleAttackRoll), typeof(UnitEntityData) })]
internal static class FeralAdditionalDiceAttackRollPatch {
    private static void Postfix(
        AdditionalDiceOnAttack __instance,
        RuleAttackRoll __0,
        UnitEntityData __1,
        ref bool __result) =>
        FeralAdditionalDiceHelper.TryQualify(
            __instance,
            __1?.Descriptor,
            __0?.Weapon?.Blueprint,
            ref __result);
}

[HarmonyPatch(
    typeof(AdditionalDiceOnAttack),
    "CheckCondition",
    new[] { typeof(RuleAttackWithWeapon), typeof(UnitEntityData) })]
internal static class FeralAdditionalDiceWeaponPatch {
    private static void Postfix(
        AdditionalDiceOnAttack __instance,
        RuleAttackWithWeapon __0,
        UnitEntityData __1,
        ref bool __result) =>
        FeralAdditionalDiceHelper.TryQualify(
            __instance,
            __1?.Descriptor,
            __0?.Weapon?.Blueprint,
            ref __result);
}

[HarmonyPatch(
    typeof(AdditionalDiceOnAttack),
    "CheckCondition",
    new[] { typeof(RulePrepareDamage), typeof(UnitEntityData) })]
internal static class FeralAdditionalDiceDamagePatch {
    private static void Postfix(
        AdditionalDiceOnAttack __instance,
        RulePrepareDamage __0,
        UnitEntityData __1,
        ref bool __result) =>
        FeralAdditionalDiceHelper.TryQualify(
            __instance,
            __1?.Descriptor,
            __0?.ParentRule?.AttackRoll?.Weapon?.Blueprint,
            ref __result);
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0205c101-5e3b-4f38-b407-f49bd12c6a07")]
public sealed class CutFromTheAirComponent : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleAttackRoll> {
    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (evt?.Weapon?.Blueprint?.IsRanged != true ||
            evt.RuleAttackWithWeapon == null ||
            evt.IsTargetFlatFooted ||
            evt.AutoMiss ||
            Owner.CombatState?.CanAttackOfOpportunity != true ||
            Owner.CombatState.AttackOfOpportunityCount <= 0) {
            return;
        }

        ItemEntityWeapon defendingWeapon = Owner.GetThreatHandMelee()?.Weapon;
        if (defendingWeapon?.Blueprint?.IsMelee != true) {
            return;
        }

        Owner.CombatState.AttackOfOpportunityCount--;
        var roll = Rulebook.Trigger(new RuleRollD20(Owner)).Result;
        var attackBonus = Rulebook.Trigger(
            new RuleCalculateAttackBonusWithoutTarget(
                Owner,
                defendingWeapon,
                0)).Result;
        if (roll + attackBonus >= evt.Roll + evt.AttackBonus) {
            evt.AutoMiss = true;
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}
