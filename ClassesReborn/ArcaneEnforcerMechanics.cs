using HarmonyLib;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintFeature))]
[TypeId("f4b57b2b-1e75-4d0a-8b42-0b7f99c5ac67")]
public sealed class ArcaneEnforcerIntelligenceExploitScaling :
    UnitFactComponentDelegate,
    IResourceAmountBonusHandler,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public BlueprintAbilityResourceReference[] m_Resources =
        Array.Empty<BlueprintAbilityResourceReference>();

    public void CalculateMaxResourceAmount(
        BlueprintAbilityResource resource,
        ref int bonus) {
        if (resource == null ||
            m_Resources?.Any(reference => reference?.Get() == resource) != true) {
            return;
        }

        bonus += Owner.Stats.Intelligence.BonusWithoutTemp -
            Owner.Stats.Charisma.BonusWithoutTemp;
    }

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (ArcaneEnforcerExploitScalingPatch.IsArcaneExploit(evt?.Spell) ||
            ArcaneEnforcerExploitScalingPatch.IsArcaneExploit(evt?.Blueprint)) {
            evt.ReplaceStat = StatType.Intelligence;
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[HarmonyPatch(typeof(ContextRankConfig), "GetBaseValue")]
internal static class ArcaneEnforcerExploitScalingPatch {
    private static BlueprintFeature s_ArcaneEnforcerReservoir;

    internal static bool IsArcaneExploit(SimpleBlueprint blueprint) =>
        blueprint?.name?.StartsWith(
            "ArcanistExploit",
            StringComparison.Ordinal) == true;

    private static bool Prefix(
        ContextRankConfig __instance,
        MechanicsContext context,
        ref int __result) {
        if (__instance?.m_BaseValueType != ContextRankBaseValueType.StatBonus ||
            __instance.m_Stat != StatType.Charisma ||
            (!IsArcaneExploit(__instance.OwnerBlueprint) &&
             !HasArcaneExploitContext(context))) {
            return true;
        }

        var unit = context?.MaybeCaster ?? context?.MaybeOwner;
        s_ArcaneEnforcerReservoir ??= BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ArcaneEnforcerArcaneReservoirFeature);
        if (unit?.Descriptor?.HasFact(s_ArcaneEnforcerReservoir) != true) {
            return true;
        }

        __result = unit.Stats.Intelligence.Bonus;
        return false;
    }

    private static bool HasArcaneExploitContext(MechanicsContext context) {
        for (var current = context; current != null; current = current.ParentContext) {
            if (IsArcaneExploit(current.AssociatedBlueprint)) {
                return true;
            }
        }
        return false;
    }
}
