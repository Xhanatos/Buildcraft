using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("7e467e4b-f649-432e-9798-109eaf12af56")]
public sealed class MercilessReputationComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSkillCheck> {
    public void OnEventAboutToTrigger(RuleSkillCheck evt) {
        if (!BruisingIntellectContext.IsDemoralizing ||
            evt.StatType != StatType.SkillPersuasion) {
            return;
        }

        var modifier = evt.Bonus.AddModifier(
            2 * Math.Max(1, Fact.GetRank()),
            Runtime,
            ModifierDescriptor.UntypedStackable);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleSkillCheck evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ab642231-34f8-4f4d-a46b-6fad0fbfee1f")]
public sealed class PainfulShotsComponent :
    UnitFactComponentDelegate<PainfulShotsComponent.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IUnitNewCombatRoundHandler {
    public sealed class ComponentData {
        public bool Used;
    }

    public BlueprintBuffReference m_DeadlyAimBuff;
    public BlueprintBuffReference m_DeadlyAimEffectBuff;
    public ActionList DemoralizeActions;

    public void HandleNewCombatRound(UnitEntityData unit) {
        if (unit == Owner) {
            Data.Used = false;
        }
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (Data.Used || evt.IsFake || !evt.IsHit || evt.Target == null ||
            !evt.Target.IsEnemy(Owner) || !CombatChecks.IsRangedWeapon(evt.Weapon) ||
            !IsDeadlyAimActive()) {
            return;
        }

        Data.Used = true;
        Fact.RunActionInContext(DemoralizeActions, evt.Target);
    }

    private bool IsDeadlyAimActive() {
        var deadlyAim = m_DeadlyAimBuff?.Get();
        var deadlyAimEffect = m_DeadlyAimEffectBuff?.Get();
        return (deadlyAim != null && Owner.Descriptor.HasFact(deadlyAim)) ||
               (deadlyAimEffect != null && Owner.Descriptor.HasFact(deadlyAimEffect));
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("146d8de8-097c-41b8-8158-8286d610fbb3")]
public sealed class MercilessDamageComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var attack = evt.ParentRule?.AttackRoll;
        var target = evt.ParentRule?.Target;
        var weaponDamage = evt.DamageBundle?.WeaponDamage as PhysicalDamage;
        if (attack?.IsHit != true || target == null || !target.IsEnemy(Owner) ||
            weaponDamage == null || !IsAffectedByFear(target)) {
            return;
        }

        var dice = Math.Max(1, Math.Min(2, Fact.GetRank()));
        evt.Add(new PhysicalDamage(
            new ModifiableDiceFormula(new DiceFormula(dice, DiceType.D6)),
            0,
            weaponDamage.Form) {
            CriticalModifier = 1,
        });
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }

    private static bool IsAffectedByFear(UnitEntityData target) {
        if (target.State.HasCondition(UnitCondition.Shaken) ||
            target.State.HasCondition(UnitCondition.Frightened) ||
            target.State.HasCondition(UnitCondition.Cowering)) {
            return true;
        }

        foreach (var buff in target.Buffs) {
            if (buff?.Blueprint != null &&
                (buff.Blueprint.SpellDescriptor & SpellDescriptor.Fear) != 0) {
                return true;
            }
        }

        return false;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b09ede3c-b7da-4249-bce3-e44a04197fce")]
public sealed class DreadfulCarnageRangeExtension : UnitFactComponentDelegate,
    IUnitFinallyDeadHandler {
    public ActionList DemoralizeActions;
    public Feet NativeRadius = new(30);
    public Feet ExtendedRadius = new(50);

    public void HandleUnitBecameFinallyDead(UnitEntityData unit) {
        if (!CombatChecks.WasKilledBy(unit, Owner) || !Game.HasInstance) {
            return;
        }

        var nativeRadius = NativeRadius.Meters;
        var extendedRadius = ExtendedRadius.Meters;
        foreach (var target in Game.Instance.State.AwakeUnits.Where(target =>
                     target != null && target != Owner && target.IsEnemy(Owner) &&
                     !target.State.IsDead &&
                     !target.State.HasCondition(UnitCondition.Unconscious) &&
                     target.DistanceTo(Owner) > nativeRadius &&
                     target.DistanceTo(Owner) <= extendedRadius)) {
            Fact.RunActionInContext(DemoralizeActions, target);
        }
    }
}
