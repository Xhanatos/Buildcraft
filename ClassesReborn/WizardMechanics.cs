using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a14a119c-63ca-4924-b8e2-150457697c3c")]
public sealed class KnowledgeIsPowerComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateCMB>,
    ITargetRulebookHandler<RuleCalculateCMD> {
    public void OnEventAboutToTrigger(RuleCalculateCMB evt) {
        evt.AddModifier(
            Owner.Stats.Intelligence?.Bonus ?? 0,
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateCMB evt) { }

    public void OnEventAboutToTrigger(RuleCalculateCMD evt) {
        evt.AddModifier(
            Owner.Stats.Intelligence?.Bonus ?? 0,
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateCMD evt) { }
}

[AllowedOn(typeof(BlueprintFeature), false)]
[TypeId("41052d35-6c91-4f22-ab8b-c7edc27951d8")]
public sealed class OppositionResearchComponent : UnitFactComponentDelegate {
    public SpellSchool School;

    public override void OnActivate() {
        foreach (var spellbook in Owner.Spellbooks) {
            if (spellbook.OppositionSchools.Contains(School) &&
                !spellbook.ExOppositionSchools.Contains(School)) {
                spellbook.ExOppositionSchools.Add(School);
            }
            spellbook.OppositionSchools.RemoveAll(school => school == School);
        }
    }

    public override void OnDeactivate() {
        foreach (var spellbook in Owner.Spellbooks) {
            if (spellbook.ExOppositionSchools.Contains(School) &&
                !spellbook.OppositionSchools.Contains(School)) {
                spellbook.OppositionSchools.Add(School);
            }
            spellbook.ExOppositionSchools.RemoveAll(school => school == School);
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact), false)]
[TypeId("af987aed-82cf-41dc-89c7-279b053e12c3")]
public sealed class SharedSpellListAffinityComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public BlueprintSpellListReference[] m_SpellLists =
        Array.Empty<BlueprintSpellListReference>();

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spellbook == null || evt.Spell == null ||
            m_SpellLists.Any(reference =>
                reference?.Get() is not BlueprintSpellList list ||
                !list.Contains(evt.Spell))) {
            return;
        }

        evt.AddBonusCasterLevel(1, ModifierDescriptor.UntypedStackable);
        evt.AddBonusDC(1, ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact), false)]
[TypeId("8c452db1-c20d-412b-b24e-727337e23bb0")]
public sealed class CreativeDestructionComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage> {
    public BlueprintBuffReference m_TemporaryHitPointsBuff;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt) {
        var context = evt.Reason?.Context;
        var dice = evt.DamageBundle?
            .Sum(damage => Math.Max(0, damage.Dice.ModifiedValue.Rolls)) ?? 0;
        if (evt.Result <= 0 || dice <= 0 || context?.MaybeCaster != Owner ||
            context.SourceItem != null || context.SpellSchool != SpellSchool.Evocation ||
            context.SourceAbility?.Type != AbilityType.Spell ||
            m_TemporaryHitPointsBuff?.Get() is not BlueprintBuff buff) {
            return;
        }

        Owner.Buffs.GetBuff(buff)?.Remove();
        Owner.Buffs.AddBuff(
            buff,
            Owner,
            TimeSpan.FromHours(1),
            new AbilityParams { CasterLevel = dice });
    }
}

[AllowedOn(typeof(BlueprintUnitFact), false)]
[TypeId("bdd5129d-4039-47fe-9406-255489d7acb4")]
public sealed class SupremeIntellectSpellCheckBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSpellResistanceCheck>,
    IInitiatorRulebookHandler<RuleDispelMagic> {
    public BlueprintCharacterClassReference m_WizardClass;
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt) {
        if (IsWizardSpell(evt.Context)) {
            evt.AddSpellPenetration(Bonus, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }

    public void OnEventAboutToTrigger(RuleDispelMagic evt) {
        if (IsWizardSpell(evt.Reason?.Context)) {
            evt.Bonus += Bonus;
        }
    }

    public void OnEventDidTrigger(RuleDispelMagic evt) { }

    private bool IsWizardSpell(MechanicsContext context) =>
        context?.SourceAbilityContext?.Ability?.Spellbook?.Blueprint?.CharacterClass ==
        m_WizardClass?.Get();
}

[HarmonyPatch(
    typeof(ModifiableValue),
    "ApplyModifiersFiltered",
    new[] { typeof(int), typeof(Func<ModifiableValue.Modifier, bool>) })]
internal static class IdealizeEnhancementBonusPatch {
    private static void Postfix(
        ModifiableValue __instance,
        Func<ModifiableValue.Modifier, bool> filter,
        ref int __result) {
        var owner = __instance?.Owner;
        if (owner?.Stats == null || !IsAbilityScore(__instance, owner)) {
            return;
        }

        var idealize = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(
            FutureContentIds.Get("Wizard.ArcaneDiscovery.Idealize"));
        var wizard = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(
            BlueprintIds.WizardClass);
        if (idealize == null || wizard == null) {
            return;
        }

        var normalEnhancementMaximum = 0;
        var idealizedEnhancementMaximum = 0;
        foreach (var modifier in __instance.Modifiers) {
            if (modifier.ModDescriptor != ModifierDescriptor.Enhancement ||
                modifier.ModValue <= 0 ||
                (filter != null && !filter(modifier))) {
                continue;
            }

            normalEnhancementMaximum = Math.Max(
                normalEnhancementMaximum,
                modifier.ModValue);

            if (modifier.Source is not Buff buff) {
                continue;
            }

            var context = buff.MaybeContext;
            var caster = context?.MaybeCaster;
            if (context == null || caster == null || context.SourceItem != null ||
                context.SpellSchool != SpellSchool.Transmutation ||
                !caster.HasFact(idealize)) {
                continue;
            }

            var idealizeBonus =
                caster.Progression.GetClassLevel(wizard) >= 20 ? 4 : 2;
            idealizedEnhancementMaximum = Math.Max(
                idealizedEnhancementMaximum,
                modifier.ModValue + idealizeBonus);
        }

        __result += Math.Max(
            0,
            idealizedEnhancementMaximum - normalEnhancementMaximum);
    }

    private static bool IsAbilityScore(
        ModifiableValue value,
        Kingmaker.UnitLogic.UnitDescriptor owner) =>
        value == owner.Stats.Strength ||
        value == owner.Stats.Dexterity ||
        value == owner.Stats.Constitution ||
        value == owner.Stats.Intelligence ||
        value == owner.Stats.Wisdom ||
        value == owner.Stats.Charisma;
}
