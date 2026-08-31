using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0af0267c-7d2d-4420-ab68-47b39fa2141d")]
public sealed class BackgroundEnemyBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_EnemyType;
    public int Bonus = 1;
    public bool ApplyToAttack = true;

    private bool IsEligible(Kingmaker.EntitySystem.Entities.UnitEntityData target) =>
        target?.Descriptor?.HasFact(m_EnemyType?.Get()) == true;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (ApplyToAttack && IsEligible(evt.Target)) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (IsEligible(evt.Target) && evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a5390935-1a6f-4413-b388-70a4af36356b")]
public sealed class RedeemedCultistDemonSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintFeatureReference m_DemonType;
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (source?.Descriptor?.HasFact(m_DemonType?.Get()) != true) {
            return;
        }
        RacialTraitRuleHelpers.AddSaveModifier(
            evt,
            this,
            Bonus,
            ModifierDescriptor.Trait);
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("957848a5-6ff3-483d-a88a-fe1ec1e6457d")]
public sealed class WorldwoundCartographerInitiative : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleInitiativeRoll> {
    public int Bonus = 4;

    public void OnEventAboutToTrigger(RuleInitiativeRoll evt) {
        if (Owner.CombatState?.NotSurprised == false) {
            evt.Modifier += Bonus;
        }
    }

    public void OnEventDidTrigger(RuleInitiativeRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("51f78593-302e-4215-af65-65c024483691")]
public sealed class PersonalCarryingCapacityBonus : UnitFactComponentDelegate {
    public int Bonus = 50;

    public override void OnTurnOn() {
        Owner.Ensure<UnitPartAdditionalEncumbrance>().AdditionalEncumbrance += Bonus;
    }

    public override void OnTurnOff() {
        var part = Owner.Get<UnitPartAdditionalEncumbrance>();
        if (part != null) {
            part.AdditionalEncumbrance = Math.Max(
                0,
                part.AdditionalEncumbrance - Bonus);
        }
    }
}
