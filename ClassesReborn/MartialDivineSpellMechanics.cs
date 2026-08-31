using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintAbility))]
[TypeId("b4a52f36-f6de-4879-a201-a10ce31858b9")]
public sealed class WeaponSpellTargetRestriction : BlueprintComponent,
    IAbilityTargetRestriction {
    private static readonly LocalizedString RestrictionText = new() {
        Key = "ClassesReborn.WeaponSpell.TargetRestriction",
    };

    public bool SecondaryHand;
    public bool MeleeOnly;
    public bool AllowUnarmed;
    public bool UnarmedRequiresWarpriest;
    public BlueprintCharacterClassReference m_WarpriestClass;
    public BlueprintItemEnchantmentReference m_ExcludedEnchantment;

    public string GetAbilityTargetRestrictionUIText(
        UnitEntityData caster,
        TargetWrapper target) => RestrictionText;

    public bool IsTargetRestrictionPassed(
        UnitEntityData caster,
        TargetWrapper target) {
        var unit = target?.Unit;
        var weapon = GetWeapon(unit, SecondaryHand);
        if (weapon?.Blueprint == null || weapon.IsShield ||
            (MeleeOnly && !weapon.Blueprint.IsMelee) ||
            (weapon.Blueprint.IsNatural && !weapon.Blueprint.IsUnarmed) ||
            (weapon.Blueprint.IsUnarmed && !AllowUnarmed)) {
            return false;
        }

        if (weapon.Blueprint.IsUnarmed && UnarmedRequiresWarpriest) {
            var warpriest = m_WarpriestClass?.Get();
            if (warpriest == null || caster?.Progression.GetClassLevel(warpriest) < 1) {
                return false;
            }
        }

        var excluded = m_ExcludedEnchantment?.Get();
        return excluded == null || !weapon.HasEnchantment(excluded);
    }

    internal static ItemEntityWeapon GetWeapon(
        UnitEntityData unit,
        bool secondaryHand) {
        var equipment = unit?.Body?.CurrentHandsEquipmentSet;
        return secondaryHand
            ? equipment?.SecondaryHand?.MaybeWeapon
            : equipment?.PrimaryHand?.MaybeWeapon;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("61e31e0b-96cd-420f-b9ad-338551530029")]
public sealed class WeaponOfAweBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public bool SecondaryHand;
    public BlueprintBuffReference m_ShakenBuff;

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon == WeaponSpellTargetRestriction.GetWeapon(
                Owner,
                SecondaryHand)) {
            evt.AddDamageModifier(2, Fact, ModifierDescriptor.Sacred);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        var shaken = m_ShakenBuff?.Get();
        if (evt.IsFake || !evt.IsCriticalConfirmed || evt.Target == null ||
            !evt.Target.IsEnemy(Owner) || shaken == null ||
            evt.Weapon != WeaponSpellTargetRestriction.GetWeapon(
                Owner,
                SecondaryHand)) {
            return;
        }

        var context = Fact.MaybeContext;
        if (context != null) {
            evt.Target.Buffs.AddBuff(
                shaken,
                context,
                CombatChecks.Rounds(1));
        } else {
            evt.Target.Buffs.AddBuff(
                shaken,
                Owner,
                CombatChecks.Rounds(1));
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9d1cebe8-9845-43b7-827c-b58d1e429d24")]
public sealed class SanctifyArmorBonuses : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>,
    ITargetRulebookHandler<RuleCalculateDamage> {
    public BlueprintBuffReference m_JudgmentWatcher;
    public BlueprintBuffReference[] m_SmiteBuffs =
        Array.Empty<BlueprintBuffReference>();

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (Owner.Body.Armor.Armor != null) {
            return;
        }

        var casterLevel = Fact.MaybeContext?.Params.CasterLevel ?? 0;
        var bonus = Math.Max(1, Math.Min(5, casterLevel / 4));
        evt.AddModifier(bonus, Fact, ModifierDescriptor.Enhancement);
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (!HasActiveJudgmentOrSmite()) {
            return;
        }

        foreach (var damage in evt.DamageBundle.OfType<PhysicalDamage>()) {
            if (!damage.Alignments.Contains(DamageAlignment.Evil)) {
                damage.SetReductionBecauseResistance(5, Fact);
            }
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }

    private bool HasActiveJudgmentOrSmite() {
        var judgment = m_JudgmentWatcher?.Get();
        if (judgment != null && Owner.HasFact(judgment)) {
            return true;
        }

        foreach (var buff in Owner.Buffs) {
            if (IsSmiteBuff(buff)) {
                return true;
            }
        }

        return Owner.Get<UnitPartUniqueBuffs>()?.Buffs.Any(IsSmiteBuff) == true;
    }

    private bool IsSmiteBuff(Kingmaker.UnitLogic.Buffs.Buff buff) =>
        buff?.Blueprint != null && m_SmiteBuffs.Any(reference =>
            reference?.Get() == buff.Blueprint);
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("99ea1567-c0f4-4dbc-85fc-224920823ec4")]
public sealed class ForcefulStrikeHandler :
    UnitFactComponentDelegate<ForcefulStrikeHandler.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public sealed class ComponentData {
        public bool Consumed;
    }

    public BlueprintAbilityReference m_Ability;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (Data.Consumed) {
            return;
        }
        Data.Consumed = true;

        var context = Fact.MaybeContext;
        var target = evt.Target;
        if (evt.IsFake || !evt.IsHit || target == null || context == null) {
            return;
        }

        var save = context.TriggerRule(new RuleSavingThrow(
            target,
            SavingThrowType.Fortitude,
            context.Params.DC));
        var diceCount = Math.Max(1, Math.Min(10, context.Params.CasterLevel));
        var forceDamage = new ForceDamage(
            new ModifiableDiceFormula(new DiceFormula(diceCount, DiceType.D4)),
            0) {
            SourceFact = Fact,
        };
        var damageRule = new RuleDealDamage(Owner, target, forceDamage) {
            AttackRoll = evt,
            Half = save.IsPassed,
            SourceAbility = m_Ability?.Get(),
        };
        context.TriggerRule(damageRule);

        if (save.IsPassed) {
            return;
        }

        var bullRush = new RuleCombatManeuver(
            Owner,
            target,
            CombatManeuver.BullRush,
            evt.AttackBonusRule) {
            ReplaceAttackBonus = context.Params.CasterLevel,
        };
        context.TriggerRule(bullRush);
    }
}

[TypeId("8ec10375-7f21-47dc-b16e-8e32cf65131d")]
public sealed class ContextActionApplyWrathfulWeaponBuff : ContextAction {
    public BlueprintBuffReference m_Buff;
    public BlueprintCharacterClassReference m_WarpriestClass;

    public override string GetCaption() => "apply Wrathful Weapon";

    public override void RunAction() {
        var caster = Context?.MaybeCaster;
        var target = Target.Unit;
        var buff = m_Buff?.Get();
        if (caster == null || target == null || buff == null) {
            return;
        }

        var multiplier = 1;
        var warpriest = m_WarpriestClass?.Get();
        if (target == caster && warpriest != null &&
            caster.Progression.GetClassLevel(warpriest) > 0) {
            multiplier *= 2;
        }
        if (Context.Params.Metamagic.HasFlag(Metamagic.Extend)) {
            multiplier *= 2;
        }

        var casterLevel = Math.Max(1, Context.Params.CasterLevel);
        target.Buffs.AddBuff(
            buff,
            Context,
            TimeSpan.FromMinutes(casterLevel * multiplier));
    }
}
