using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0975ac8a-3d07-455e-a1dd-eed860406b62")]
public sealed class RicochetComponent :
    UnitFactComponentDelegate<RicochetComponent.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    ITickEachRound {
    private static readonly WeaponCategory[] ThrownWeaponCategories = {
        WeaponCategory.ThrowingAxe,
        WeaponCategory.Dart,
        WeaponCategory.Javelin,
        WeaponCategory.Shuriken,
    };

    public sealed class ComponentData {
        public bool UsedThisRound;
        public bool ResolvingSecondaryAttack;
    }

    public void OnNewRound() {
        Data.UsedThisRound = false;
        Data.ResolvingSecondaryAttack = false;
    }

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt) {
        if (Data.UsedThisRound || Data.ResolvingSecondaryAttack ||
            evt?.Initiator != Owner || evt.AttackRoll?.IsHit != true ||
            !IsThrownWeapon(evt.Weapon) || evt.Target == null || !Game.HasInstance) {
            return;
        }

        var secondaryTarget = Game.Instance.State.AwakeUnits
            .Where(unit => unit != null && unit != Owner && unit != evt.Target &&
                           unit.IsEnemy(Owner) && !unit.State.IsDead &&
                           !unit.State.HasCondition(UnitCondition.Unconscious) &&
                           unit.DistanceTo(evt.Target) <= 15.Feet().Meters)
            .OrderBy(unit => unit.DistanceTo(evt.Target))
            .FirstOrDefault();
        if (secondaryTarget == null) {
            return;
        }

        Data.UsedThisRound = true;
        Data.ResolvingSecondaryAttack = true;
        try {
            Rulebook.Trigger(new RuleAttackWithWeapon(
                Owner,
                secondaryTarget,
                evt.Weapon,
                0));
        } finally {
            Data.ResolvingSecondaryAttack = false;
        }
    }

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (Data.ResolvingSecondaryAttack && IsThrownWeapon(evt.Weapon)) {
            evt.AddModifier(-2, Fact, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    private static bool IsThrownWeapon(ItemEntityWeapon weapon) =>
        weapon?.Blueprint != null &&
        ThrownWeaponCategories.Contains(weapon.Blueprint.Category);
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8ccac46c-478c-4759-88f0-87749f1a15c2")]
public sealed class BashingBulwarkComponent :
    UnitFactComponentDelegate<BashingBulwarkComponent.ComponentData>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    ITickEachRound {
    public sealed class ComponentData {
        public bool TriggeredThisRound;
    }

    public BlueprintBuffReference m_AcBuff;

    public override void OnTurnOff() {
        RemoveAcBuff();
        Data.TriggeredThisRound = false;
        base.OnTurnOff();
    }

    public void OnNewRound() {
        RemoveAcBuff();
        Data.TriggeredThisRound = false;
    }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.IsShield == true) {
            evt.IncreaseWeaponSize(1);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (Data.TriggeredThisRound || evt?.IsFake == true || evt?.IsHit != true ||
            evt.Weapon?.IsShield != true) {
            return;
        }

        var buff = m_AcBuff?.Get();
        if (buff == null) {
            return;
        }

        Data.TriggeredThisRound = true;
        Owner.Buffs.GetBuff(buff)?.Remove();
        Owner.Buffs.AddBuff(buff, Owner, null);
    }

    private void RemoveAcBuff() {
        if (m_AcBuff?.Get() is BlueprintBuff buff) {
            Owner.Buffs.GetBuff(buff)?.Remove();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f54e8a67-916e-433f-b62d-3ca9604dd3b0")]
public sealed class ShieldedCastingComponent : UnitFactComponentDelegate { }

[HarmonyPatch(
    typeof(UnitPartMagus),
    nameof(UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand))]
internal static class ShieldedCastingFreeHandPatch {
    [HarmonyPostfix]
    private static void Postfix(
        UnitDescriptor unit,
        ref bool __result) {
        if (__result || unit?.Body == null ||
            !unit.Progression.Features
                .SelectFactComponents<ShieldedCastingComponent>()
                .Any()) {
            return;
        }

        var equipment = unit.Body.CurrentHandsEquipmentSet;
        var primaryWeapon = equipment?.PrimaryHand?.MaybeWeapon;
        if (primaryWeapon != null &&
            equipment.SecondaryHand.MaybeShield != null &&
            UnitPartMagus.IsOneHandedWeapon(primaryWeapon)) {
            __result = true;
        }
    }
}
