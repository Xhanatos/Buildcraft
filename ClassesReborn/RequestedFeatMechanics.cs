using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintFeature))]
[TypeId("8ced94c3-a733-4c38-b538-d3d186391f33")]
public sealed class NaturalAttackCountPrerequisite : Prerequisite {
    public bool AnyNaturalAttack = true;
    public WeaponCategory Category;
    public int Minimum = 1;

    public override bool CheckInternal(
        FeatureSelectionState selectionState,
        UnitDescriptor unit,
        LevelUpState state) =>
        GetWeapons(unit).Count(weapon =>
            weapon?.Blueprint?.IsNatural == true &&
            (AnyNaturalAttack || weapon.Blueprint.Category == Category)) >= Minimum;

    public override string GetUITextInternal(UnitDescriptor unit) =>
        AnyNaturalAttack
            ? "Natural attack"
            : Minimum <= 1
                ? Category.ToString()
                : $"{Minimum} {Category.ToString().ToLowerInvariant()} attacks";

    internal static IEnumerable<ItemEntityWeapon> GetWeapons(UnitDescriptor unit) {
        if (unit?.Body == null) {
            yield break;
        }
        var equipment = unit.Body.CurrentHandsEquipmentSet;
        if (equipment?.PrimaryHand?.MaybeWeapon != null) {
            yield return equipment.PrimaryHand.MaybeWeapon;
        }
        if (equipment?.SecondaryHand?.MaybeWeapon != null) {
            yield return equipment.SecondaryHand.MaybeWeapon;
        }
        foreach (var slot in unit.Body.AdditionalLimbs) {
            if (slot?.MaybeWeapon != null) {
                yield return slot.MaybeWeapon;
            }
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f80305a0-46db-4c4a-8bc4-6a70d79ed598")]
public sealed class ImprovedNaturalAttackComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.Blueprint?.IsNatural == true &&
            Param?.WeaponCategory == evt.Weapon.Blueprint.Category) {
            evt.IncreaseWeaponSize(1);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("57f12ed0-aaed-4749-a521-4aeb8868c888")]
public sealed class ClawPounceComponent : UnitFactComponentDelegate { }

[HarmonyPatch(typeof(UnitAttack), "InitAttacks")]
internal static class ClawPounceAttackPatch {
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions) {
        var implicitFlag = AccessTools.Method(
            typeof(CountableFlag),
            "op_Implicit",
            new[] { typeof(CountableFlag) });
        foreach (var instruction in instructions) {
            yield return instruction;
            if (instruction.Calls(implicitFlag)) {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(
                    typeof(ClawPounceAttackPatch),
                    nameof(IncludeClawPounce));
            }
        }
    }

    private static bool IncludeClawPounce(bool original, UnitAttack attack) =>
        original || Applies(attack);

    private static void Postfix(UnitAttack __instance) {
        var descriptor = __instance?.Executor?.Descriptor;
        if (__instance?.IsCharge != true || descriptor == null ||
            descriptor.State.Features.Pounce || !Applies(__instance)) {
            return;
        }
        __instance.m_AllAttacks?.RemoveAll(attack =>
            attack?.Weapon?.Blueprint?.Category != WeaponCategory.Claw);
    }

    private static bool Applies(UnitAttack attack) {
        var descriptor = attack?.Executor?.Descriptor;
        if (attack?.IsCharge != true || descriptor == null ||
            descriptor.Progression.Features
                .SelectFactComponents<ClawPounceComponent>().Any() != true) {
            return false;
        }
        var equipment = descriptor.Body.CurrentHandsEquipmentSet;
        var manufacturedWeapon = new[] {
                equipment?.PrimaryHand?.MaybeWeapon,
                equipment?.SecondaryHand?.MaybeWeapon,
            }
            .Any(weapon => weapon?.Blueprint != null &&
                           !weapon.Blueprint.IsNatural &&
                           !weapon.Blueprint.IsUnarmed);
        return !manufacturedWeapon &&
               NaturalAttackCountPrerequisite.GetWeapons(descriptor)
                   .Count(weapon => weapon?.Blueprint?.Category == WeaponCategory.Claw) >= 2;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ee20c04b-3a9e-4d73-b6c6-c6f0efce74be")]
public sealed class JabbingStyleComponent :
    UnitFactComponentDelegate<JabbingStyleComponent.ComponentData>,
    IInitiatorRulebookHandler<RulePrepareDamage>,
    IUnitNewCombatRoundHandler {
    public sealed class ComponentData {
        public EntityRef<UnitEntityData> Target;
        public int Hits;
    }

    public void HandleNewCombatRound(UnitEntityData unit) {
        if (unit == Owner) {
            Data.Target = new EntityRef<UnitEntityData>();
            Data.Hits = 0;
        }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var attack = evt.ParentRule?.AttackRoll;
        var target = evt.ParentRule?.Target;
        if (attack?.IsHit != true ||
            attack.Weapon?.Blueprint?.IsUnarmed != true || target == null) {
            return;
        }
        if (Data.Target != target) {
            Data.Target = target;
            Data.Hits = 0;
        }
        Data.Hits++;
        if (Data.Hits < 2) {
            return;
        }
        var master = Owner.Progression.Features
            .SelectFactComponents<JabbingMasterComponent>().Any();
        var dice = master ? 2 : 1;
        evt.Add(new PhysicalDamage(
            new ModifiableDiceFormula(new DiceFormula(dice, DiceType.D6)),
            0,
            PhysicalDamageForm.Bludgeoning) {
            Precision = true,
        });
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("32fb9ab7-915b-491b-8289-27add2bfdb35")]
public sealed class JabbingMasterComponent : UnitFactComponentDelegate { }

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("42c66898-e565-472c-9a77-74e49a9bb0c6")]
public sealed class VolleyFireComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget> {
    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (evt.Weapon?.Blueprint?.IsRanged != true || !Game.HasInstance) {
            return;
        }
        var soloTactics = (bool)Owner.State.Features.SoloTactics;
        var bonus = Game.Instance.State.AwakeUnits
            .Where(unit => unit != null && unit != Owner &&
                           !unit.IsEnemy(Owner) && !unit.State.IsDead &&
                           !unit.State.HasCondition(UnitCondition.Unconscious) &&
                           unit.DistanceTo(Owner) <= 30.Feet().Meters &&
                           HasRangedWeapon(unit) &&
                           (soloTactics || unit.Descriptor.HasFact(Fact.Blueprint)))
            .Take(4)
            .Count();
        if (bonus > 0) {
            evt.AddModifier(bonus, Fact, ModifierDescriptor.Competence);
        }
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    private static bool HasRangedWeapon(UnitEntityData unit) {
        var equipment = unit?.Body?.CurrentHandsEquipmentSet;
        return equipment?.PrimaryHand?.MaybeWeapon?.Blueprint?.IsRanged == true ||
               equipment?.SecondaryHand?.MaybeWeapon?.Blueprint?.IsRanged == true;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8f26daca-f8cb-4b9a-ad0c-86836164d108")]
public sealed class FocusedShotComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();

    internal bool IsEligible(ItemEntityWeapon weapon) =>
        weapon?.Blueprint?.IsRanged == true &&
        Categories.Contains(weapon.Blueprint.Category);

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (IsEligible(evt.Weapon)) {
            evt.OverrideDamageBonusStat(StatType.Intelligence);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[HarmonyPatch(typeof(RuleCalculateWeaponStats),
    nameof(RuleCalculateWeaponStats.OverrideDamageBonusStat))]
internal static class FocusedShotDamageStatPatch {
    private static void Prefix(
        RuleCalculateWeaponStats __instance,
        ref StatType __0) {
        if (__instance?.Initiator?.Descriptor?.Progression?.Features
                .SelectFactComponents<FocusedShotComponent>()
                .Any(component => component.IsEligible(__instance.Weapon)) == true) {
            __0 = StatType.Intelligence;
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("4cd8aaad-612a-4b7c-95c8-85f109975017")]
public sealed class TacticalReflexesComponent : UnitFactComponentDelegate { }

[HarmonyPatch(typeof(UnitCombatState),
    nameof(UnitCombatState.AttackOfOpportunityPerRound), MethodType.Getter)]
internal static class TacticalReflexesAttackCountPatch {
    private static void Postfix(UnitCombatState __instance, ref int __result) {
        var descriptor = __instance?.Unit?.Descriptor;
        if (descriptor?.Progression?.Features
                .SelectFactComponents<TacticalReflexesComponent>().Any() != true) {
            return;
        }
        __result = Math.Max(1, 1 + descriptor.Stats.Intelligence.Bonus);
    }
}
