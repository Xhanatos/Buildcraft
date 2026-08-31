using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using static Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupHelper;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b61a9368-ac3f-47c2-97ca-57c5c2038d05")]
public sealed class TraitAttackOfOpportunityBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public bool UnarmedOnly;
    public WeaponFighterGroup[] FighterGroups = Array.Empty<WeaponFighterGroup>();

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (evt.RuleAttackWithWeapon?.IsAttackOfOpportunity != true ||
            evt.Weapon?.Blueprint is not BlueprintItemWeapon weapon) {
            return;
        }

        var eligible = UnarmedOnly
            ? weapon.IsUnarmed
            : FighterGroups.Any(group => weapon.FighterGroup.Contains(group));
        if (eligible) {
            evt.AddModifier(1, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("fe0086b8-ded3-400e-9143-daef408afcff")]
public sealed class DirtyFighterTraitDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (evt.ParentRule?.AttackRoll?.TargetIsFlanked == true &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("e87655d8-05bb-43e5-a083-5bb8658a0dd1")]
public sealed class KillerTraitDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage> {
    public void OnEventAboutToTrigger(RuleDealDamage evt) {
        var attack = evt.AttackRoll;
        if (attack?.IsCriticalConfirmed == true &&
            attack.WeaponStats != null &&
            attack.Weapon != null) {
            evt.AddModifier(
                attack.WeaponStats.CriticalMultiplier,
                Fact,
                ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleDealDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("d2f96051-fbd1-4a5d-9e43-d97b8a571426")]
public sealed class SharpNailsCriticalMultiplier : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.Blueprint?.Category == WeaponCategory.Claw) {
            evt.AdditionalCriticalMultiplier.Add(ModifierDescriptor.Trait, 1);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("06d757a1-8542-4c4c-b3a6-c0e646318672")]
public sealed class ShieldFighterTraitComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        var equipment = Owner.Body.CurrentHandsEquipmentSet;
        var primary = equipment.PrimaryHand.MaybeWeapon;
        var secondary = equipment.SecondaryHand.MaybeWeapon;
        if (primary == null || secondary?.IsShield != true ||
            secondary.Blueprint.IsLight ||
            (evt.Weapon != primary && evt.Weapon != secondary) ||
            (evt.Reason?.Rule is RuleAttackWithWeapon attack &&
             !attack.IsFullAttack) ||
            Owner.Descriptor.State.AdditionalFeatures.ShieldMaster) {
            return;
        }

        var training = Owner.Get<UnitPartWeaponTraining>();
        if (Owner.State.Features.EffortlessDualWielding &&
            training?.IsSuitableWeapon(secondary) == true) {
            return;
        }

        // A light off-hand weapon reduces both two-weapon attack penalties by 2.
        evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (evt.ParentRule?.AttackRoll?.Weapon?.IsShield == true &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("4bdeaefe-03f0-4a71-a235-106ad20518ec")]
public sealed class HistoryOfHeresySaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (Owner.Progression.Classes.Any(classData =>
                classData.Level > 0 &&
                classData.CharacterClass?.IsDivineCaster == true) ||
            evt.Reason?.Ability?.SpellDescriptor.HasFlag(SpellDescriptor.Divine) != true) {
            return;
        }

        var modifier = evt.Type switch {
            SavingThrowType.Fortitude => Owner.Stats.SaveFortitude.AddModifier(
                1,
                Runtime,
                ModifierDescriptor.Trait),
            SavingThrowType.Reflex => Owner.Stats.SaveReflex.AddModifier(
                1,
                Runtime,
                ModifierDescriptor.Trait),
            SavingThrowType.Will => Owner.Stats.SaveWill.AddModifier(
                1,
                Runtime,
                ModifierDescriptor.Trait),
            _ => null,
        };
        if (modifier != null) {
            evt.AddTemporaryModifier(modifier);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b96694ab-9fe4-4952-a5bb-977a3e6c19cf")]
public sealed class SacredConduitDcBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    private const SpellDescriptor ChannelDescriptors =
        SpellDescriptor.ChannelPositiveHeal |
        SpellDescriptor.ChannelNegativeHeal |
        SpellDescriptor.ChannelPositiveHarm |
        SpellDescriptor.ChannelNegativeHarm;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spell != null &&
            (evt.Spell.SpellDescriptor & ChannelDescriptors) != 0) {
            evt.AddBonusDC(1, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}
