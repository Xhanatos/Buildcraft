using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f9cf357a-455d-4416-98f7-d15343ca0f81")]
public sealed class PrimalShifting : UnitFactComponentDelegate,
    IAbilityGetCommandTypeHandler,
    IPolymorphActivatedHandler,
    IPolymorphDeactivatedHandler,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    private const int SpeedBonus = 10;
    private const int SavingThrowBonus = 2;
    private const string MajorFormPrefix = "ShifterWildShape";
    private static readonly string[] MajorFormAbilityMarkers = {
        "Abillity",
        "Ability",
    };

    public override void OnTurnOn() {
        base.OnTurnOn();
        RefreshSpeedBonus();
    }

    public override void OnTurnOff() {
        Owner.Stats.Speed.RemoveModifiersFrom(Runtime);
        base.OnTurnOff();
    }

    public void HandleGetCommandType(
        AbilityData ability,
        ref UnitCommand.CommandType commandType) {
        if (IsMajorFormAbility(ability?.Blueprint)) {
            commandType = UnitCommand.CommandType.Swift;
        }
    }

    public void OnPolymorphActivated(UnitEntityData unit, Polymorph polymorph) {
        if (unit == Owner && IsMajorForm(polymorph)) {
            RefreshSpeedBonus();
        }
    }

    public void OnPolymorphDeactivated(UnitEntityData unit, Polymorph polymorph) {
        if (unit == Owner && IsMajorForm(polymorph)) {
            RefreshSpeedBonus(polymorph);
        }
    }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (!HasMajorForm()) {
            return;
        }

        var stat = evt.Type switch {
            SavingThrowType.Fortitude => Owner.Stats.SaveFortitude,
            SavingThrowType.Reflex => Owner.Stats.SaveReflex,
            SavingThrowType.Will => Owner.Stats.SaveWill,
            _ => null,
        };
        if (stat != null) {
            evt.AddTemporaryModifier(stat.AddModifier(
                SavingThrowBonus,
                Runtime,
                ModifierDescriptor.UntypedStackable));
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }

    private void RefreshSpeedBonus(Polymorph excluded = null) {
        Owner.Stats.Speed.RemoveModifiersFrom(Runtime);
        if (HasMajorForm(excluded)) {
            Owner.Stats.Speed.AddModifier(
                SpeedBonus,
                Runtime,
                ModifierDescriptor.UntypedStackable);
        }
    }

    private bool HasMajorForm(Polymorph excluded = null) =>
        Owner?.Buffs?
            .GetFactsContainingComponent<Polymorph>()
            .Any(buff => buff.Blueprint
                .GetComponents<Polymorph>()
                .Any(component => component != excluded) &&
                IsMajorFormBuff(buff.Blueprint)) == true;

    private static bool IsMajorForm(Polymorph polymorph) =>
        polymorph?.OwnerBlueprint is BlueprintBuff buff &&
        IsMajorFormBuff(buff);

    private static bool IsMajorFormAbility(BlueprintAbility ability) {
        var name = ability?.name;
        if (name?.StartsWith(MajorFormPrefix, StringComparison.Ordinal) != true ||
            name.Contains("AwesomeBlow")) {
            return false;
        }

        foreach (var marker in MajorFormAbilityMarkers) {
            var index = name.LastIndexOf(marker, StringComparison.Ordinal);
            if (index < 0) {
                continue;
            }
            var suffix = name.Substring(index + marker.Length);
            return suffix.Length == 0 || suffix.All(char.IsDigit);
        }
        return false;
    }

    private static bool IsMajorFormBuff(BlueprintBuff buff) =>
        buff?.name?.StartsWith(
            MajorFormPrefix,
            StringComparison.Ordinal) == true;
}
