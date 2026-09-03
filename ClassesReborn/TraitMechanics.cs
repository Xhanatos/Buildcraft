using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands.Base;
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

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("89752c8e-f3ff-4584-81d0-a3a16afc5c2b")]
public sealed class MathematicalProdigyDispelCheckBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDispelMagic> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleDispelMagic evt) {
        evt.Bonus += Bonus;
    }

    public void OnEventDidTrigger(RuleDispelMagic evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("42b39565-0854-4462-a901-8ba446f7a232")]
public sealed class ArmorMaximumDexterityTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateArmorMaxDexBonusLimit> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateArmorMaxDexBonusLimit evt) {
        if (evt.Armor != null && Bonus > 0) {
            evt.AddBonus(Bonus);
        }
    }

    public void OnEventDidTrigger(RuleCalculateArmorMaxDexBonusLimit evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a6cd92b4-ab1b-46bb-afe5-e5e668046b11")]
public sealed class TargetFactDamageTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_TargetFact;
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (evt.Target?.Descriptor?.HasFact(m_TargetFact?.Get()) == true &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("54ae049d-6d30-4c5e-9fa0-cb217256f8a3")]
public sealed class DivineCasterDamageTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (evt.DamageBundle.WeaponDamage == null ||
            evt.Target?.Descriptor?.Progression?.Classes?.Any(classData =>
                classData.Level > 0 &&
                classData.CharacterClass?.IsDivineCaster == true) != true) {
            return;
        }

        evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("24a4cfeb-2ec1-47ae-8f00-bc4c904572f9")]
public sealed class TargetFactAttackTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public BlueprintFeatureReference m_TargetFact;
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (evt.Target?.Descriptor?.HasFact(m_TargetFact?.Get()) == true) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("094cbe91-c1cd-42b5-97ef-309092a6d8e5")]
public sealed class SurpriseRoundTraitModifiers : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleInitiativeRoll>,
    ITargetRulebookHandler<RuleCalculateAC> {
    public int InitiativeBonus = 2;
    public int ArmorClassPenalty = -2;

    private bool IsSurprised => Owner.CombatState?.NotSurprised == false;

    public void OnEventAboutToTrigger(RuleInitiativeRoll evt) {
        if (IsSurprised) {
            evt.Modifier += InitiativeBonus;
        }
    }

    public void OnEventDidTrigger(RuleInitiativeRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (IsSurprised) {
            evt.AddModifier(ArmorClassPenalty, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("835815c4-2f41-40a6-876a-0c52059241f9")]
public sealed class DemoralizedTargetDamageTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var target = evt.Target;
        if (evt.DamageBundle.WeaponDamage != null && target != null &&
            (target.State.HasCondition(UnitCondition.Shaken) ||
             target.State.HasCondition(UnitCondition.Frightened) ||
             target.State.HasCondition(UnitCondition.Cowering))) {
            evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("022fc6d5-05ac-4920-93bc-db63f974c673")]
public sealed class FastTalkerMarker : UnitFactComponentDelegate { }

[HarmonyPatch(
    typeof(AbilityData),
    nameof(AbilityData.RuntimeActionType),
    MethodType.Getter)]
internal static class FastTalkerDemoralizeActionPatch {
    private static void Postfix(
        AbilityData __instance,
        ref UnitCommand.CommandType __result) {
        if (__result != UnitCommand.CommandType.Standard ||
            __instance?.Blueprint?.AssetGuid.ToString() != BlueprintIds.PersuasionUseAbility ||
            __instance.Caster?.Progression?.Features?
                .SelectFactComponents<FastTalkerMarker>().Any() != true) {
            return;
        }

        __result = UnitCommand.CommandType.Move;
    }
}
