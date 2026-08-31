using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.Utility;

namespace ClassesReborn;

internal static class WitchcraftRuntime {
    private static BlueprintFeatureReference s_Witchcraft;
    private static HashSet<BlueprintGuid> s_WitchHexes = new();

    internal static void Configure(
        BlueprintFeature witchcraft,
        IEnumerable<BlueprintAbility> witchHexes) {
        s_Witchcraft = witchcraft.ToReference<BlueprintFeatureReference>();
        s_WitchHexes = witchHexes
            .Where(ability => ability != null)
            .Select(ability => Canonical(ability).AssetGuid)
            .ToHashSet();
    }

    internal static BlueprintAbility Canonical(BlueprintAbility ability) {
        while (ability?.Parent != null) {
            ability = ability.Parent;
        }
        return ability;
    }

    internal static bool HasWitchcraft(UnitEntityData unit) =>
        unit != null && s_Witchcraft?.Get() is { } feature &&
        unit.HasFact(feature);

    internal static bool IsHex(BlueprintAbility ability) {
        var canonical = Canonical(ability);
        return ability != null &&
            (ability.SpellDescriptor.HasFlag(SpellDescriptor.Hex) ||
             (canonical != null && s_WitchHexes.Contains(canonical.AssetGuid)));
    }

    internal static bool IsHexOrCurse(BlueprintAbility ability) =>
        IsHex(ability) ||
        ability?.SpellDescriptor.HasFlag(SpellDescriptor.Curse) == true;

    internal static bool IsOncePerDayHex(BlueprintAbility ability) {
        if (!IsHex(ability)) {
            return false;
        }

        for (var current = ability; current != null; current = current.Parent) {
            if (current.GetComponents<AbilityTargetHasFact>().Any(component =>
                    component.Inverted &&
                    component.m_CheckedFacts?.Any(reference =>
                        reference != null && !reference.IsEmpty()) == true)) {
                return true;
            }
        }
        return false;
    }
}

internal sealed class UnitPartWitchcraft : OldStyleUnitPart {
    public List<RetryEntry> Entries = new();

    public sealed class RetryEntry {
        public BlueprintAbilityReference m_Ability;
        public EntityRef<UnitEntityData> Unit;
        public bool InProgress;
    }

    internal bool CanRetry(BlueprintAbility ability, UnitEntityData target) =>
        Find(ability, target) is { InProgress: false };

    internal void RecordSuccessfulSave(
        BlueprintAbility ability,
        UnitEntityData target) {
        var canonical = WitchcraftRuntime.Canonical(ability);
        if (canonical == null || target == null) {
            return;
        }

        var existing = Find(canonical, target);
        if (existing?.InProgress == true) {
            return;
        }
        if (existing != null) {
            existing.InProgress = false;
            return;
        }

        Entries.Add(new RetryEntry {
            m_Ability = canonical.ToReference<BlueprintAbilityReference>(),
            Unit = target,
        });
    }

    internal void BeginRetry(BlueprintAbility ability, UnitEntityData target) {
        var entry = Find(ability, target);
        if (entry != null && !entry.InProgress) {
            entry.InProgress = true;
        }
    }

    internal void EndRetry(BlueprintAbility ability, UnitEntityData target) {
        var entry = Find(ability, target);
        if (entry?.InProgress == true) {
            Entries.Remove(entry);
        }
    }

    internal void Clear() => Entries.Clear();

    private RetryEntry Find(BlueprintAbility ability, UnitEntityData target) {
        var canonical = WitchcraftRuntime.Canonical(ability);
        return canonical == null || target == null
            ? null
            : Entries.FirstOrDefault(entry =>
                entry.m_Ability?.Get() == canonical && entry.Unit == target);
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9f6be9b6-c836-4d42-80c8-fd1ad7f3b38f")]
public sealed class WitchcraftMastery : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
    IInitiatorRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookHandler<RuleCastSpell>,
    IRestFinishedHandler {
    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (WitchcraftRuntime.IsHexOrCurse(
                evt?.Spell ?? evt?.Blueprint as BlueprintAbility)) {
            evt.AddBonusDC(2, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (!WitchcraftRuntime.IsHexOrCurse(evt?.Reason?.Ability?.Blueprint)) {
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
                2,
                Runtime,
                ModifierDescriptor.UntypedStackable));
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }

    public void OnEventAboutToTrigger(RuleCastSpell evt) {
        Owner.Get<UnitPartWitchcraft>()?.BeginRetry(
            evt?.Spell?.Blueprint,
            evt?.SpellTarget?.Unit);
    }

    public void OnEventDidTrigger(RuleCastSpell evt) {
        Owner.Get<UnitPartWitchcraft>()?.EndRetry(
            evt?.Spell?.Blueprint,
            evt?.SpellTarget?.Unit);
    }

    public void HandleRestFinished(RestStatus status) =>
        Owner.Get<UnitPartWitchcraft>()?.Clear();

    public override void OnTurnOff() =>
        Owner.Get<UnitPartWitchcraft>()?.Clear();
}

[HarmonyPatch(typeof(RuleSavingThrow), "OnTrigger")]
internal static class WitchcraftSuccessfulSavePatch {
    private static void Postfix(RuleSavingThrow __instance) {
        var ability = __instance?.Reason?.Ability?.Blueprint;
        var caster = __instance?.Reason?.Caster ?? __instance?.Reason?.SourceUnit;
        var target = __instance?.Initiator;
        if (__instance?.Success != true ||
            ability == null || target == null ||
            !WitchcraftRuntime.HasWitchcraft(caster) ||
            !WitchcraftRuntime.IsOncePerDayHex(ability)) {
            return;
        }

        caster.Ensure<UnitPartWitchcraft>()
            .RecordSuccessfulSave(ability, target);
    }
}

[HarmonyPatch(
    typeof(AbilityTargetHasFact),
    nameof(AbilityTargetHasFact.IsTargetRestrictionPassed))]
internal static class WitchcraftHexTargetRestrictionPatch {
    private static void Postfix(
        AbilityTargetHasFact __instance,
        UnitEntityData caster,
        TargetWrapper target,
        ref bool __result) {
        if (__result || __instance?.Inverted != true ||
            caster == null || target?.Unit == null ||
            __instance.OwnerBlueprint is not BlueprintAbility ability ||
            !WitchcraftRuntime.HasWitchcraft(caster) ||
            !WitchcraftRuntime.IsOncePerDayHex(ability)) {
            return;
        }

        if (caster.Get<UnitPartWitchcraft>()?.CanRetry(
                ability,
                target.Unit) == true) {
            __result = true;
        }
    }
}
