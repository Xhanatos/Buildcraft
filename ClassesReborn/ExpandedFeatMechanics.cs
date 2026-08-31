using HarmonyLib;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintAbility))]
[TypeId("9a4197b8-dd44-4c1f-8a78-018a2e95058e")]
public sealed class QuickStudyComponent : AbilityApplyEffect,
    IAbilityRestriction,
    IAbilityRequiredParameters {
    public bool AnySpellLevel;
    [HideIf(nameof(AnySpellLevel))]
    public int SpellLevel;
    public BlueprintCharacterClassReference[] CharacterClass =
        Array.Empty<BlueprintCharacterClassReference>();
    public BlueprintArchetypeReference[] Archetypes =
        Array.Empty<BlueprintArchetypeReference>();

    public AbilityParameter RequiredParameters => AbilityParameter.SpellSlot;

    public override void Apply(
        AbilityExecutionContext context,
        TargetWrapper target) {
        var slot = context?.Ability?.ParamSpellSlot;
        var converted = context?.Ability?.m_ConvertedFrom;
        var spellbook = slot?.Spell?.Spellbook;
        if (slot?.Spell == null || converted == null || spellbook == null ||
            !ValidSpellbooks(context.MaybeCaster?.Descriptor).Contains(spellbook) ||
            (!AnySpellLevel && slot.SpellLevel > SpellLevel)) {
            Main.Log.Error("Quick Study received an invalid spell slot.");
            return;
        }

        spellbook.ForgetMemorized(slot);
        spellbook.Memorize(converted);
        var replacementSlot = GetUnavailableSpellSlot(converted);
        if (replacementSlot != null) {
            replacementSlot.Available = true;
        }

        using (context.GetDataScope(target)) {
            new ContextActionProvokeAttackOfOpportunity {
                ApplyToCaster = true,
            }.RunAction();
        }
    }

    public bool IsAbilityRestrictionPassed(AbilityData ability) {
        var slot = ability?.ParamSpellSlot;
        var spell = slot?.Spell;
        var spellbook = spell?.Spellbook;
        return spell != null && spellbook != null &&
               ValidSpellbooks(ability.Caster).Contains(spellbook) &&
               (AnySpellLevel || slot.SpellLevel <= SpellLevel) &&
               (spellbook.Blueprint.IsArcanist || slot.Available);
    }

    public string GetAbilityRestrictionUIText() => string.Empty;

    private bool AddAsVariant(
        SpellSlot slot,
        Spellbook spellbook,
        UnitDescriptor unit) =>
        spellbook != null && slot != null &&
        ValidSpellbooks(unit).Contains(spellbook) &&
        (AnySpellLevel || slot.SpellLevel <= SpellLevel) &&
        (spellbook.Blueprint.IsArcanist || slot.Available);

    private static bool SpellQualifies(
        Spellbook book,
        int level,
        SpellSlot slot,
        AbilityData spell) {
        var opposedSlot = book.OppositionSchools.Contains(slot.Spell.Blueprint.School) ||
            slot.Spell.Blueprint.SpellDescriptor.HasFlag(book.OppositionDescriptors);
        var opposedSpell = book.OppositionSchools.Contains(spell.Blueprint.School) ||
            spell.Blueprint.SpellDescriptor.HasFlag(book.OppositionDescriptors);
        return (!book.Blueprint.IsArcanist ||
                !book.m_MemorizedSpells[level]
                    .Any(existing => existing.Spell == spell && existing.Available)) &&
               (!opposedSpell || opposedSlot == opposedSpell);
    }

    private IEnumerable<Spellbook> ValidSpellbooks(UnitDescriptor unit) {
        if (unit == null) {
            return Enumerable.Empty<Spellbook>();
        }

        var result = new HashSet<BlueprintCharacterClass>();
        foreach (var classData in unit.Progression.Classes) {
            if (!CharacterClass.HasReference(classData.CharacterClass)) {
                continue;
            }

            if (Archetypes.Length == 0 || Archetypes.Any(reference => {
                    var archetype = reference.Get();
                    return !classData.CharacterClass.Archetypes.Contains(archetype) ||
                           classData.Archetypes.Contains(archetype);
                })) {
                result.Add(classData.CharacterClass);
            }
        }
        return result.Select(unit.DemandSpellbook).Where(book => book != null);
    }

    private static SpellSlot GetUnavailableSpellSlot(AbilityData ability) =>
        ability?.Spellbook?.GetMemorizedSpellSlots(ability.SpellLevel)
            .FirstOrDefault(slot => !slot.Available && slot.Spell == ability);

    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.GetConversions))]
    private static class GetConversionsPatch {
        private static void Postfix(
            AbilityData __instance,
            ref IEnumerable<AbilityData> __result) {
            var result = __result.ToList();
            if (__instance.SpellSlot == null || __instance.Spellbook == null) {
                __result = result;
                return;
            }

            foreach (var ability in __instance.Caster.Abilities) {
                var quickStudy = ability.Blueprint.GetComponent<QuickStudyComponent>();
                if (quickStudy?.AddAsVariant(
                        __instance.SpellSlot,
                        __instance.Spellbook,
                        __instance.Caster) != true) {
                    continue;
                }

                var spells = __instance.Spellbook
                    .GetKnownSpells(__instance.SpellSlot.SpellLevel)
                    .Concat(__instance.Spellbook
                        .GetCustomSpells(__instance.SpellSlot.SpellLevel));
                if (!__instance.Spellbook.Blueprint.IsArcanist) {
                    spells = spells.Where(spell =>
                        !spell.Equals(__instance.SpellSlot.Spell));
                }

                foreach (var spell in spells.Where(spell => SpellQualifies(
                             __instance.Spellbook,
                             __instance.SpellLevel,
                             __instance.SpellSlot,
                             spell))) {
                    AbilityData.AddAbilityUnique(ref result, new AbilityData(ability) {
                        m_ConvertedFrom = spell,
                        SaveSpellbookSlot = true,
                        ParamSpellSlot = __instance.SpellSlot,
                    });
                }
            }
            __result = result;
        }
    }

    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.IsAvailableInSpellbook),
        MethodType.Getter)]
    private static class IsAvailableInSpellbookPatch {
        private static void Postfix(AbilityData __instance, ref bool __result) {
            if (__instance.Blueprint.GetComponent<QuickStudyComponent>() != null) {
                __result = true;
            }
        }
    }
}

public class UnitConditionExceptionsFromBuff : UnitConditionExceptions {
    public BlueprintBuffReference[] Exceptions = Array.Empty<BlueprintBuffReference>();

    public override string GetCaption() => "rage spellcasting exception";

    internal bool IsException(Buff source) =>
        source != null && Exceptions.HasReference(source.Blueprint);
}

[AllowedOn(typeof(BlueprintUnitFact), false)]
[AllowMultipleComponents]
[TypeId("cf0aa3ed-00c8-49fd-aa89-395e18cbb20d")]
public sealed class AddConditionExceptions : UnitFactComponentDelegate,
    IUnitCombatHandler {
    public UnitCondition Condition;
    public UnitConditionExceptions Exception;

    public override void OnTurnOn() {
        var index = (int)Condition;
        var exceptions = Owner.State.m_ConditionsExceptions;
        exceptions[index] ??= new List<UnitConditionExceptions>();
        exceptions[index].Add(Exception);
    }

    public override void OnTurnOff() =>
        Owner.State.m_ConditionsExceptions?[(int)Condition]?.Remove(Exception);

    public void HandleUnitJoinCombat(UnitEntityData unit) {
        if (unit != Owner) {
            return;
        }
        OnTurnOff();
        OnTurnOn();
    }

    public void HandleUnitLeaveCombat(UnitEntityData unit) { }
}

[HarmonyPatch(typeof(UnitState), nameof(UnitState.AddCondition))]
internal static class UnitConditionExceptionPatch {
    private static void Prefix(
        UnitCondition condition,
        ref Buff source,
        UnitState __instance) {
        var exceptions = __instance.m_ConditionsExceptions[(int)condition];
        if (exceptions == null) {
            return;
        }

        var conditionSource = source;
        if (exceptions.OfType<UnitConditionExceptionsFromBuff>()
            .Any(exception => exception.IsException(conditionSource))) {
            __instance.m_Conditions[(int)condition]--;
            source = null;
        }
    }
}

internal static class RimeMetamagicExtension {
    internal static readonly Metamagic Rime = (Metamagic)(1 << 16);
    private static readonly string[] SpellLists = {
        BlueprintIds.AlchemistSpellList,
        BlueprintIds.WizardSpellList,
        BlueprintIds.BloodragerSpellList,
        BlueprintIds.MagusSpellList,
        BlueprintIds.WitchSpellList,
        BlueprintIds.DruidSpellList,
        BlueprintIds.BardSpellList,
        BlueprintIds.PaladinSpellList,
        BlueprintIds.ClericSpellList,
        BlueprintIds.InquisitorSpellList,
        BlueprintIds.RangerSpellList,
    };

    internal static void EnableOnColdSpells() {
        foreach (var listId in SpellLists) {
            var spellList = ResourcesLibrary.TryGetBlueprint<BlueprintSpellList>(listId);
            if (spellList == null) {
                continue;
            }

            foreach (var spell in spellList.SpellsByLevel.SelectMany(level => level.Spells)) {
                EnableIfCold(spell);
                foreach (var variant in spell.GetComponent<AbilityVariants>()?.Variants ??
                         Enumerable.Empty<BlueprintAbility>()) {
                    EnableIfCold(variant);
                }
            }
        }
    }

    private static void EnableIfCold(BlueprintAbility spell) {
        if (spell != null && spell.SpellDescriptor.HasFlag(SpellDescriptor.Cold)) {
            spell.AvailableMetamagic |= Rime;
        }
    }

    internal static Sprite Icon =>
        ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintIds.RimeSpellFeat)?.Icon;
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("bb85aa0d-5761-47c9-b79b-adc2e84fdf1c")]
public sealed class RimeSpellTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage> {
    public BlueprintBuffReference m_EntangledBuff;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt) {
        var context = evt.Reason?.Context;
        if (context == null ||
            !context.HasMetamagic(RimeMetamagicExtension.Rime) ||
            !context.SpellDescriptor.HasFlag(SpellDescriptor.Cold) ||
            !evt.DamageBundle.OfType<EnergyDamage>()
                .Any(damage => damage.EnergyType == DamageEnergyType.Cold &&
                               !damage.Immune) ||
            m_EntangledBuff?.Get() is not BlueprintBuff buff ||
            evt.Target == null) {
            return;
        }

        var rounds = Math.Max(1, context.Params?.SpellLevel ?? context.SpellLevel);
        var applied = evt.Target.Buffs.AddBuff(
            buff,
            context.MaybeCaster,
            CombatChecks.Rounds(rounds));
        if (applied != null) {
            applied.IsFromSpell = true;
        }
    }
}

[TypeId("05acd9e0-2b07-44a8-ad5a-a810815a6ce1")]
public sealed class PrerequisiteFirstCharacterLevel : Prerequisite {
    public override bool CheckInternal(
        FeatureSelectionState selectionState,
        UnitDescriptor unit,
        LevelUpState state) =>
        unit?.Progression?.CharacterLevel <= 1;

    public override string GetUITextInternal(UnitDescriptor unit) =>
        "Can only be selected at 1st character level";
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("7880d3b9-000d-4b8e-be22-9fc6fb925c7e")]
public sealed class FeyFoundlingHealing : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleHealDamage> {
    public void OnEventAboutToTrigger(RuleHealDamage evt) {
        var diceCount = Math.Max(0, evt.HealFormula.BaseFormula.Rolls);
        if (diceCount > 0) {
            evt.AddModifierBonus(2 * diceCount, Fact);
        }
    }

    public void OnEventDidTrigger(RuleHealDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("930f5255-3413-4d14-9909-b04452876649")]
public sealed class FeyFoundlingColdIronVulnerability : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateDamage> {
    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        foreach (var damage in evt.DamageBundle.OfType<PhysicalDamage>()
                     .Where(damage => damage.MaterialsMask
                         .HasFlag(PhysicalDamageMaterial.ColdIron))) {
            damage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("4c61279f-d8a4-468e-84b9-5f5d41acebe1")]
public sealed class ViciousStompComponent : UnitFactComponentDelegate,
    IUnitConditionsChanged,
    IGlobalSubscriber {
    public void HandleUnitConditionsChanged(
        UnitEntityData unit,
        UnitCondition condition) {
        if (condition != UnitCondition.Prone || unit == null || unit == Owner ||
            !unit.IsEnemy(Owner) || !unit.State.HasCondition(UnitCondition.Prone) ||
            Owner.CombatState == null || Owner.CombatState.AttackOfOpportunityCount <= 0 ||
            unit.DistanceTo(Owner) >
            unit.View.Corpulence + Owner.View.Corpulence + 5.Feet().Meters) {
            return;
        }

        var weapon = Owner.Body.EmptyHandWeapon;
        if (weapon == null) {
            return;
        }

        Owner.CombatState.AttackOfOpportunityCount--;
        Rulebook.Trigger(new RuleAttackWithWeapon(Owner, unit, weapon, 0) {
            IsAttackOfOpportunity = true,
        });
    }
}

[HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.DefaultCost))]
internal static class RimeDefaultCostPatch {
    private static bool Prefix(Metamagic metamagic, ref int __result) {
        if (metamagic != RimeMetamagicExtension.Rime) {
            return true;
        }
        __result = 1;
        return false;
    }
}

[HarmonyPatch(typeof(MetamagicHelper), nameof(MetamagicHelper.SpellIcon))]
internal static class RimeSpellIconPatch {
    private static bool Prefix(Metamagic metamagic, ref Sprite __result) {
        if (metamagic != RimeMetamagicExtension.Rime ||
            RimeMetamagicExtension.Icon == null) {
            return true;
        }
        __result = RimeMetamagicExtension.Icon;
        return false;
    }
}

[HarmonyPatch(typeof(RuleCollectMetamagic), nameof(RuleCollectMetamagic.AddMetamagic))]
internal static class RimeCollectMetamagicPatch {
    private static void Postfix(
        RuleCollectMetamagic __instance,
        Feature metamagicFeature) {
        if (!__instance.KnownMetamagics.Contains(metamagicFeature) ||
            metamagicFeature.GetComponent<AddMetamagicFeat>() is not { } component ||
            component.Metamagic != RimeMetamagicExtension.Rime ||
            __instance.m_SpellLevel < 0 || __instance.m_SpellLevel >= 10 ||
            __instance.m_SpellLevel + 1 > 10 || __instance.Spell == null ||
            __instance.SpellMetamagics.Contains(metamagicFeature) ||
            (__instance.Spell.AvailableMetamagic & component.Metamagic) !=
            component.Metamagic) {
            return;
        }

        __instance.SpellMetamagics.Add(metamagicFeature);
    }
}

internal static class RimeMetamagicUiPatches {
    private static bool Installed;

    internal static void Install() {
        if (Installed) {
            return;
        }

        var utilityTexts = typeof(Kingmaker.UI.Common.UIUtilityTexts);
        Main.HarmonyInstance.Patch(
            AccessTools.Method(
                utilityTexts,
                nameof(Kingmaker.UI.Common.UIUtilityTexts.GetMetamagicName)),
            postfix: new HarmonyMethod(AccessTools.Method(
                typeof(RimeMetamagicUiPatches),
                nameof(GetMetamagicNamePostfix))));
        Main.HarmonyInstance.Patch(
            AccessTools.Method(
                utilityTexts,
                nameof(Kingmaker.UI.Common.UIUtilityTexts.GetMetamagicList)),
            postfix: new HarmonyMethod(AccessTools.Method(
                typeof(RimeMetamagicUiPatches),
                nameof(GetMetamagicListPostfix))));
        Installed = true;
    }

    private static void GetMetamagicNamePostfix(
        Metamagic metamagic,
        ref string __result) {
        if (metamagic == RimeMetamagicExtension.Rime) {
            __result = "Rime";
        }
    }

    private static void GetMetamagicListPostfix(
        Metamagic mask,
        ref string __result) {
        if (!mask.HasMetamagic(RimeMetamagicExtension.Rime)) {
            return;
        }
        var builder = new StringBuilder(__result);
        if (builder.Length > 0) {
            builder.Append(", ");
        }
        builder.Append("Rime");
        __result = builder.ToString();
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("41f94677-01c0-43f0-a32d-40a6ad0205f6")]
public sealed class WeaponCategoryAttackAndDamageStatReplacement :
    UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public StatType ReplacementStat;
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();
    public bool RequireFreeOffHand;

    private bool IsEligible(ItemEntityWeapon weapon) {
        if (weapon?.Blueprint == null ||
            !Categories.Contains(weapon.Blueprint.Category)) {
            return false;
        }

        if (!RequireFreeOffHand) {
            return true;
        }

        return !weapon.HoldInTwoHands &&
               Owner.Body.CurrentHandsEquipmentSet.SecondaryHand.MaybeWeapon == null &&
               Owner.Body.CurrentHandsEquipmentSet.SecondaryHand.MaybeShield == null;
    }

    public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) {
        if (!IsEligible(evt.Weapon)) {
            return;
        }

        var current = Owner.Stats.GetStat(evt.AttackBonusStat)
            as ModifiableValueAttributeStat;
        var replacement = Owner.Stats.GetStat(ReplacementStat)
            as ModifiableValueAttributeStat;
        if (current != null && replacement != null &&
            replacement.Bonus >= current.Bonus) {
            evt.AttackBonusStat = ReplacementStat;
        }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (!IsEligible(evt.Weapon)) {
            return;
        }

        if (evt.DamageBonusStat == null) {
            return;
        }
        var current = Owner.Stats.GetStat(evt.DamageBonusStat.Value)
            as ModifiableValueAttributeStat;
        var replacement = Owner.Stats.GetStat(ReplacementStat)
            as ModifiableValueAttributeStat;
        if (current != null && replacement != null &&
            replacement.Bonus >= current.Bonus) {
            evt.OverrideDamageBonusStat(ReplacementStat);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("5a5f3a92-e9e8-4cc7-9b72-778857c6273e")]
public sealed class CrushingThrowComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();
    public BlueprintBuffReference m_PowerAttackBuff;
    public BlueprintUnitFactReference m_MythicPowerAttack;

    private bool IsEligible(ItemEntityWeapon weapon) =>
        weapon?.Blueprint?.IsRanged == true &&
        Categories.Contains(weapon.Blueprint.Category) &&
        m_PowerAttackBuff?.Get() is BlueprintBuff powerAttack &&
        Owner.Buffs.GetBuff(powerAttack) != null;

    private int Scaling =>
        1 + Owner.Stats.BaseAttackBonus.ModifiedValue / 4;

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (IsEligible(evt.Weapon)) {
            evt.AddModifier(
                -Scaling,
                Fact,
                ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (!IsEligible(evt.Weapon)) {
            return;
        }

        var multiplier =
            m_MythicPowerAttack?.Get() is BlueprintUnitFact mythicPowerAttack &&
            Owner.HasFact(mythicPowerAttack)
                ? 3
                : 2;
        var damageBonus = multiplier * Scaling;
        if (evt.IsSecondary) {
            damageBonus /= 2;
        }
        evt.AddDamageModifier(
            damageBonus,
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6f59169e-55a8-458b-b9b1-ab02e5d98912")]
public sealed class BalancedGripComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public BlueprintUnitFactReference m_MythicWeaponFinesse;

    private bool IsSelectedWeapon(ItemEntityWeapon weapon) =>
        weapon?.Blueprint?.IsMelee == true &&
        !weapon.HoldInTwoHands &&
        Param?.WeaponCategory == weapon.Blueprint.Category;

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (!IsSelectedWeapon(evt.Weapon)) {
            return;
        }

        var current = Owner.Stats.GetStat(evt.AttackBonusStat)
            as ModifiableValueAttributeStat;
        var dexterity = Owner.Stats.GetStat(StatType.Dexterity)
            as ModifiableValueAttributeStat;
        if (current != null && dexterity != null &&
            dexterity.Bonus >= current.Bonus) {
            evt.AttackBonusStat = StatType.Dexterity;
        }

        ApplyTwoWeaponPenaltyReduction(evt);
    }

    private void ApplyTwoWeaponPenaltyReduction(
        RuleCalculateAttackBonusWithoutTarget evt) {
        var equipment = Owner.Body.CurrentHandsEquipmentSet;
        var primary = equipment.PrimaryHand.MaybeWeapon;
        var secondary = equipment.SecondaryHand.MaybeWeapon;
        if (primary == null || secondary == null ||
            equipment.PrimaryHand.MaybeShield != null ||
            equipment.SecondaryHand.MaybeShield != null ||
            !IsSelectedWeapon(secondary) ||
            (evt.Weapon != primary && evt.Weapon != secondary) ||
            (evt.Reason?.Rule is RuleAttackWithWeapon attack &&
             !attack.IsFullAttack)) {
            return;
        }

        var training = Owner.Get<UnitPartWeaponTraining>();
        if (Owner.State.Features.EffortlessDualWielding &&
            training?.IsSuitableWeapon(secondary) == true) {
            return;
        }

        evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (!IsSelectedWeapon(evt.Weapon) || evt.DamageBonusStat == null ||
            m_MythicWeaponFinesse?.Get() is not BlueprintUnitFact mythicFinesse ||
            !Owner.HasFact(mythicFinesse)) {
            return;
        }

        var current = Owner.Stats.GetStat(evt.DamageBonusStat.Value)
            as ModifiableValueAttributeStat;
        var dexterity = Owner.Stats.GetStat(StatType.Dexterity)
            as ModifiableValueAttributeStat;
        if (current != null && dexterity != null &&
            dexterity.Bonus >= current.Bonus) {
            evt.OverrideDamageBonusStat(StatType.Dexterity);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("00b6a998-5ac0-4827-8c05-de32ab49e648")]
public sealed class TwoWeaponDefenseComponent : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    private static bool IsEligibleWeapon(ItemEntityWeapon weapon) =>
        weapon?.Blueprint != null &&
        !weapon.Blueprint.IsNatural &&
        !weapon.Blueprint.IsUnarmed;

    private bool IsDualWielding() {
        var equipment = Owner.Body.CurrentHandsEquipmentSet;
        var primary = equipment.PrimaryHand.MaybeWeapon;
        var secondary = equipment.SecondaryHand.MaybeWeapon;
        if (equipment.PrimaryHand.MaybeShield != null ||
            equipment.SecondaryHand.MaybeShield != null) {
            return false;
        }

        return IsEligibleWeapon(primary) &&
               (IsEligibleWeapon(secondary) || primary.Blueprint.Double);
    }

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (!IsDualWielding()) {
            return;
        }

        var fightingDefensively = false;
        foreach (var buff in Owner.Buffs) {
            if (buff.Blueprint?.name == "FightingDefensivelyBuff") {
                fightingDefensively = true;
                break;
            }
        }
        evt.AddModifier(
            fightingDefensively ? 2 : 1,
            Fact,
            ModifierDescriptor.Dodge);
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("819dbfcd-a1fa-4337-8682-6c5e5ba0e864")]
public sealed class GreaterUnarmedStrikeComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.Blueprint is not BlueprintItemWeapon weapon ||
            (!weapon.IsUnarmed &&
             !FeralCombatTrainingHelpers.HasTraining(Owner, weapon.Category))) {
            return;
        }

        var characterLevel = Owner.Progression.CharacterLevel;
        var targetDice = characterLevel >= 10
            ? new DiceFormula(1, DiceType.D10)
            : new DiceFormula(1, DiceType.D8);
        var monkLevel = Owner.Progression.Classes
            .FirstOrDefault(classData =>
                classData.CharacterClass?.AssetGuid.ToString() ==
                    BlueprintIds.MonkClass)
            ?.Level ?? 0;
        if (monkLevel > 0) {
            var monkDice = GetMonkDice(monkLevel + 4);
            if (DiceStrength(monkDice) > DiceStrength(targetDice)) {
                targetDice = monkDice;
            }
        }

        if (DiceStrength(targetDice) >
            DiceStrength(evt.WeaponDamageDice.ModifiedValue)) {
            evt.WeaponDamageDice.Modify(targetDice, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    private static DiceFormula GetMonkDice(int effectiveLevel) =>
        effectiveLevel switch {
            >= 20 => new DiceFormula(2, DiceType.D10),
            >= 16 => new DiceFormula(2, DiceType.D8),
            >= 12 => new DiceFormula(2, DiceType.D6),
            >= 8 => new DiceFormula(1, DiceType.D10),
            >= 4 => new DiceFormula(1, DiceType.D8),
            _ => new DiceFormula(1, DiceType.D6),
        };

    private static int DiceStrength(DiceFormula dice) =>
        dice.MinValue(0, false) + dice.MaxValue(0, false);
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ef525421-ef8c-4b96-a2df-a1db266bdc67")]
public sealed class MadMagicGreaterBloodrageDC : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public BlueprintUnitFactReference m_GreaterBloodrage;
    public BlueprintBuffReference m_GreaterBloodrageBuff;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spellbook == null || m_GreaterBloodrage?.Get() is not BlueprintUnitFact greater ||
            !Owner.HasFact(greater) || m_GreaterBloodrageBuff?.Get() is not BlueprintBuff buff ||
            Owner.Buffs.GetBuff(buff) == null) {
            return;
        }

        evt.AddBonusDC(1, ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b815d960-433b-4ad2-a56d-44ef54d36fdc")]
public sealed class CrusadersFlurryComponent : UnitFactComponentDelegate { }

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f2a79634-e1f0-41cd-bd04-160e5d1ba210")]
public sealed class BladedBrushComponent : UnitFactComponentDelegate { }

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("dbbe61f1-1209-4cf5-bb8b-bd79f9a8d4bd")]
public sealed class AsceticStyleComponent : UnitFactComponentDelegate { }

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("05624ad8-dcb0-46f8-8bb4-bb2cd7b2ccee")]
public sealed class AsceticFormComponent : UnitFactComponentDelegate { }

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9a66a230-8a3f-42ad-a502-650a033e7441")]
public sealed class AsceticStrikeComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (Param?.WeaponCategory != evt.Weapon?.Blueprint?.Category) {
            return;
        }

        var effectiveLevel = Math.Max(1, Owner.Progression.CharacterLevel - 4);
        var dice = effectiveLevel switch {
            >= 20 => new DiceFormula(2, DiceType.D10),
            >= 16 => new DiceFormula(2, DiceType.D8),
            >= 12 => new DiceFormula(2, DiceType.D6),
            >= 8 => new DiceFormula(1, DiceType.D10),
            >= 4 => new DiceFormula(1, DiceType.D8),
            _ => new DiceFormula(1, DiceType.D6),
        };
        if (dice.MinValue(0, false) + dice.MaxValue(0, false) >
            evt.WeaponDamageDice.ModifiedValue.MinValue(0, false) +
            evt.WeaponDamageDice.ModifiedValue.MaxValue(0, false)) {
            evt.WeaponDamageDice.Modify(dice, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

internal static class ExpandedWeaponEligibility {
    internal static bool HasParametrizedComponent<T>(
        UnitDescriptor owner,
        WeaponCategory category)
        where T : UnitFactComponentDelegate =>
        owner?.Progression?.Features
            .SelectFactComponents<T>()
            .Any(component => component.Param?.WeaponCategory == category) == true;

    internal static bool HasCrusadersFlurry(
        UnitDescriptor owner,
        BlueprintItemWeapon weapon) {
        if (owner == null || weapon == null ||
            weapon.m_Type?.Get()?.m_AttackType != AttackType.Melee ||
            !owner.Progression.Features
                .SelectFactComponents<CrusadersFlurryComponent>().Any()) {
            return false;
        }

        var focus = ResourcesLibrary.TryGetBlueprint<BlueprintParametrizedFeature>(
            BlueprintIds.WeaponFocus);
        if (focus == null ||
            owner.GetFeature(focus, (FeatureParam)weapon.Category) == null) {
            return false;
        }

        var sourceController = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(
            BlueprintIds.WarpriestDeitySacredWeaponFeature);
        if (sourceController == null) {
            return false;
        }

        return sourceController.GetComponents<AddFeatureIfHasFact>()
            .Where(source => source.m_CheckedFact?.Get() is BlueprintUnitFact deity &&
                             owner.HasFact(deity))
            .SelectMany(source =>
                (source.m_Feature?.Get() as BlueprintFeature)?
                    .GetComponents<SacredWeaponFavoriteDamageOverride>() ??
                Enumerable.Empty<SacredWeaponFavoriteDamageOverride>())
            .Any(component => component.Category == weapon.Category);
    }
}

[HarmonyPatch(typeof(MonkNoArmorAndMonkWeaponFeatureUnlock),
    nameof(MonkNoArmorAndMonkWeaponFeatureUnlock.CheckEligibility))]
internal static class ExpandedMonkWeaponEligibilityPatch {
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions) {
        var isMonk = AccessTools.PropertyGetter(
            typeof(BlueprintItemWeapon),
            nameof(BlueprintItemWeapon.IsMonk));
        foreach (var instruction in instructions) {
            yield return instruction;
            if (instruction.Calls(isMonk)) {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(
                    typeof(ExpandedMonkWeaponEligibilityPatch),
                    nameof(IsExpandedMonkWeapon));
            }
        }
    }

    private static bool IsExpandedMonkWeapon(
        bool original,
        MonkNoArmorAndMonkWeaponFeatureUnlock component) {
        if (original || component?.Owner?.Body?.PrimaryHand?.MaybeWeapon?.Blueprint is not
            BlueprintItemWeapon weapon) {
            return original;
        }

        return ExpandedWeaponEligibility.HasCrusadersFlurry(component.Owner, weapon) ||
               FeralCombatTrainingHelpers.HasTraining(component.Owner, weapon.Category) ||
               ExpandedWeaponEligibility.HasParametrizedComponent<AsceticFormComponent>(
                   component.Owner,
                   weapon.Category);
    }
}

[HarmonyPatch(typeof(UnitPartMagus), nameof(UnitPartMagus.IsOneHandedWeapon))]
internal static class BladedBrushMagusPatch {
    private static void Postfix(ItemEntityWeapon weapon, ref bool __result) {
        if (__result || weapon?.Owner == null ||
            weapon.Blueprint.Category != WeaponCategory.Glaive) {
            return;
        }

        __result = weapon.Owner.Progression.Features
            .SelectFactComponents<BladedBrushComponent>().Any();
    }
}
