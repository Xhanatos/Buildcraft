using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace ClassesReborn;

internal static class DefenderOfTheTrueWorldRuntime {
    internal static bool IsEvilOutsider(
        UnitEntityData unit,
        BlueprintFeatureReference outsiderType,
        BlueprintFeatureReference evilSubtype) =>
        unit?.Descriptor?.HasFact(outsiderType?.Get()) == true &&
        unit.Descriptor.HasFact(evilSubtype?.Get());
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("701a53b9-daad-4c2f-ad02-6767e7a4db56")]
public sealed class EvilOutsiderWeaponBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_OutsiderType;
    public BlueprintFeatureReference m_EvilSubtype;
    public BlueprintCharacterClassReference m_DruidClass;
    public int FixedBonus;
    public bool ScaleAsFeyStalker;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        var bonus = Bonus();
        if (bonus > 0 && IsEligible(evt.Target)) {
            evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var bonus = Bonus();
        if (bonus > 0 && IsEligible(evt.Target) &&
            evt.DamageBundle?.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(bonus, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }

    private bool IsEligible(UnitEntityData target) =>
        DefenderOfTheTrueWorldRuntime.IsEvilOutsider(
            target,
            m_OutsiderType,
            m_EvilSubtype);

    private int Bonus() {
        if (!ScaleAsFeyStalker) {
            return Math.Max(0, FixedBonus);
        }

        var factRank = Math.Min(4, Math.Max(0, Fact.GetRank()));
        var source = Fact.MaybeContext?.MaybeCaster ?? Owner;
        var druid = m_DruidClass?.Get();
        var level = druid == null || source == null
            ? 0
            : source.Progression.GetClassLevel(druid);
        var levelBonus = level switch {
            >= 18 => 4,
            >= 13 => 3,
            >= 8 => 2,
            >= 3 => 1,
            _ => 0,
        };
        return Math.Max(factRank, levelBonus);
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a5c0e6a6-e09c-40b0-98fc-bb9f1be27267")]
public sealed class EvilOutsiderFeybaneBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookHandler<RuleSpellResistanceCheck> {
    public BlueprintFeatureReference m_OutsiderType;
    public BlueprintFeatureReference m_EvilSubtype;
    public int SaveBonus = 4;
    public int SpellPenetrationBonus = 2;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (DefenderOfTheTrueWorldRuntime.IsEvilOutsider(
                source,
                m_OutsiderType,
                m_EvilSubtype)) {
            RacialTraitRuleHelpers.AddSaveModifier(
                evt,
                this,
                SaveBonus,
                ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }

    public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt) {
        if (DefenderOfTheTrueWorldRuntime.IsEvilOutsider(
                evt.Target,
                m_OutsiderType,
                m_EvilSubtype)) {
            evt.AddSpellPenetration(
                SpellPenetrationBonus,
                ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }
}
