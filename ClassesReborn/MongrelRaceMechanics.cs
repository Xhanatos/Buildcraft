using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("baf907f6-5d70-4240-92ee-cd568d5ecdaa")]
public sealed class EvilOutsiderDamageBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_OutsiderType;
    public BlueprintFeatureReference m_EvilSubtype;
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (Bonus <= 0 || evt.DamageBundle?.WeaponDamage == null ||
            !DefenderOfTheTrueWorldRuntime.IsEvilOutsider(
                evt.Target,
                m_OutsiderType,
                m_EvilSubtype)) {
            return;
        }

        evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("96825409-02a3-438c-b0c3-51f2a425cdd4")]
public sealed class AdaptiveLineageWeaponTraining : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateCMB> {
    public int Bonus = 1;

    public override void OnTurnOn() {
        if (Param?.WeaponCategory is WeaponCategory category) {
            Owner.Proficiencies.Add(category);
        }
    }

    public override void OnTurnOff() {
        if (Param?.WeaponCategory is not WeaponCategory category ||
            HasAnotherProficiencySource(category)) {
            return;
        }
        Owner.Proficiencies.Remove(category);
    }

    public void OnEventAboutToTrigger(RuleCalculateCMB evt) {
        if (Param?.WeaponCategory is not WeaponCategory category) {
            return;
        }

        var equipment = Owner.Body.CurrentHandsEquipmentSet;
        var usesSelectedWeapon =
            equipment?.PrimaryHand?.MaybeWeapon?.Blueprint?.Category == category ||
            equipment?.SecondaryHand?.MaybeWeapon?.Blueprint?.Category == category;
        if (usesSelectedWeapon) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Racial);
        }
    }

    public void OnEventDidTrigger(RuleCalculateCMB evt) { }

    private bool HasAnotherProficiencySource(WeaponCategory category) {
        var otherAdaptiveSource = Owner.Progression.Features
            .SelectFactComponents<AdaptiveLineageWeaponTraining>()
            .Any(component => component.Fact != Fact &&
                component.Param?.WeaponCategory == category);
        if (otherAdaptiveSource) {
            return true;
        }

        return Owner.Progression.Features
            .SelectFactComponents<AddProficiencies>()
            .Any(component => component.Fact != Fact &&
                component.WeaponProficiencies?.Contains(category) == true);
    }
}
