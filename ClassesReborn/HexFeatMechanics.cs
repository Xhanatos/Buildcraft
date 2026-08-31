using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("4203c451-f91b-40b7-b955-dba1313316d9")]
public sealed class CursingGazeActionType : UnitFactComponentDelegate,
    IAbilityGetCommandTypeHandler {
    public BlueprintAbilityReference[] m_Abilities =
        Array.Empty<BlueprintAbilityReference>();

    public void HandleGetCommandType(
        AbilityData ability,
        ref UnitCommand.CommandType commandType) {
        if (ability?.Blueprint != null &&
            m_Abilities.Any(reference => reference?.Get() == ability.Blueprint)) {
            commandType = UnitCommand.CommandType.Swift;
        }
    }
}

internal sealed class UnitPartHexStrike : OldStyleUnitPart {
    private BlueprintAbilityReference[] m_Abilities =
        Array.Empty<BlueprintAbilityReference>();
    public EntityRef<UnitEntityData> Unit;

    internal bool Ready => m_Abilities.Any(reference =>
        reference != null && !reference.IsEmpty());

    internal void Store(
        IEnumerable<BlueprintAbilityReference> abilities,
        UnitEntityData unit) {
        m_Abilities = abilities
            .Where(reference => reference != null && !reference.IsEmpty())
            .ToArray();
        Unit = unit;
    }

    internal bool Matches(BlueprintAbility ability) =>
        Ready && m_Abilities.Any(reference => reference.Get() == ability);

    internal bool ValidTarget(BlueprintAbility ability, UnitEntityData target) =>
        !Matches(ability) || Unit == target;

    internal void Clear() {
        m_Abilities = Array.Empty<BlueprintAbilityReference>();
        Unit = new EntityRef<UnitEntityData>();
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9fbb4bc2-75c2-47f0-ab1a-d4cbbf490720")]
public sealed class HexStrikeToggle : UnitFactComponentDelegate {
    public override void OnTurnOff() =>
        Owner.Get<UnitPartHexStrike>()?.Clear();
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b0af22b2-0495-4b26-8e80-ae9e2db2bf3b")]
public sealed class HexStrikeTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RuleCastSpell>,
    IAbilityGetCommandTypeHandler,
    ITickEachRound {
    public BlueprintAbilityReference[] m_Hexes =
        Array.Empty<BlueprintAbilityReference>();
    public BlueprintBuffReference m_ToggleBuff;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        var weapon = evt.Weapon?.Blueprint;
        var target = evt.Target;
        var toggle = m_ToggleBuff?.Get();
        if (!evt.IsHit || evt.IsFake || target == null || toggle == null ||
            !Owner.HasFact(toggle) ||
            weapon == null || (!weapon.IsUnarmed && !weapon.IsNatural)) {
            return;
        }

        if (m_Hexes.Any(reference => reference?.Get() != null)) {
            Owner.Ensure<UnitPartHexStrike>().Store(m_Hexes, target);
        }
    }

    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt) {
        var ability = evt.Spell?.Blueprint;
        if (ability == null) {
            return;
        }

        var part = Owner.Get<UnitPartHexStrike>();
        if (part?.Matches(ability) == true) {
            part.Clear();
        }
    }

    public void HandleGetCommandType(
        AbilityData ability,
        ref UnitCommand.CommandType commandType) {
        if (ability?.Blueprint != null &&
            Owner.Get<UnitPartHexStrike>()?.Matches(ability.Blueprint) == true) {
            commandType = UnitCommand.CommandType.Swift;
        }
    }

    public void OnNewRound() => Owner.Get<UnitPartHexStrike>()?.Clear();
}

[AllowMultipleComponents]
[TypeId("7b3c82ed-7981-4306-bb22-c58d1149b7d8")]
public sealed class AbilityTargetHexStrike : BlueprintComponent,
    IAbilityTargetRestriction {
    private static readonly LocalizedString RestrictionText = new() {
        Key = "ClassesReborn.HexStrike.TargetRestriction",
    };

    public string GetAbilityTargetRestrictionUIText(
        UnitEntityData caster,
        TargetWrapper target) => RestrictionText;

    public bool IsTargetRestrictionPassed(
        UnitEntityData caster,
        TargetWrapper target) {
        var ability = OwnerBlueprint as BlueprintAbility;
        return ability == null || target?.Unit == null ||
            caster.Get<UnitPartHexStrike>()?.ValidTarget(ability, target.Unit) != false;
    }
}
