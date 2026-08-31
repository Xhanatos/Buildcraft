using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Conditions.Builder.NewEx;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.Serialization;
using static Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupHelper;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6e7197f0-9e9f-4a0e-8823-52cf74f6ef5f")]
public sealed class WeaponCategoryAttackStatReplacement : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget> {
    public StatType ReplacementStat;
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();

    public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) {
        var category = evt.Weapon?.Blueprint?.Category;
        if (category == null || !Categories.Contains(category.Value)) {
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
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("bbf42252-226d-4848-92e3-61d24f42eacb")]
public sealed class DeityFavoredWeaponAttackStatReplacement : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget> {
    public StatType ReplacementStat;
    public BlueprintUnitFactReference[] m_Deities =
        Array.Empty<BlueprintUnitFactReference>();
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();

    public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) {
        var category = evt.Weapon?.Blueprint?.Category;
        if (category == null || m_Deities.Length != Categories.Length) {
            return;
        }

        var matches = Enumerable.Range(0, Categories.Length).Any(index =>
            Categories[index] == category.Value &&
            m_Deities[index]?.Get() is BlueprintUnitFact deity &&
            Owner.HasFact(deity));
        if (!matches) {
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
}

[TypeId("1fd63eec-f836-4abb-8adc-dfd0fbe355d4")]
[AllowedOn(typeof(BlueprintFeature))]
public sealed class ConstrainFeatureRank : UnitFactComponentDelegate<CompanionBoonData>,
    IUnitCompleteLevelUpHandler {
    [FormerlySerializedAs("RankFeature")]
    public BlueprintFeatureReference TargetFeature;

    public override void OnActivate() => Apply();

    public override void OnDeactivate() {
        while (Data.AppliedRank > 0) {
            Owner.RemoveFact(TargetFeature);
            Data.AppliedRank--;
        }
    }

    public void HandleUnitCompleteLevelup(UnitEntityData unit) {
        if (unit == Owner) {
            Apply();
        }
    }

    private void Apply() {
        var baseRank = Owner.GetFact(Fact.Blueprint)?.GetRank() ?? 0;
        var targetRank = Owner.GetFact(TargetFeature)?.GetRank() ?? 0;
        while (baseRank > targetRank) {
            var added = Owner.AddFact(TargetFeature);
            Data.AppliedRank++;
            targetRank = added?.GetRank() ?? targetRank;
            if (added == null) {
                break;
            }
        }

        while (baseRank < targetRank && Owner.GetFact(TargetFeature) is Feature feature) {
            feature.RemoveRank();
            Data.AppliedRank--;
            targetRank = feature.GetRank();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[AllowMultipleComponents]
[TypeId("8168878e-9a40-4166-ba2b-ed681eeafa5e")]
public sealed class FeatureForPrerequisite : UnitFactComponentDelegate {
    public BlueprintUnitFactReference FakeFact;
}

[HarmonyPatch(typeof(Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite),
    nameof(Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite.Check))]
internal static class FeatureForPrerequisitePatch {
    private static void Postfix(
        Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState,
        UnitDescriptor unit,
        Kingmaker.UnitLogic.Class.LevelUp.LevelUpState state,
        Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite __instance,
        ref bool __result) {
        if (__result || unit == null) {
            return;
        }

        var fakes = unit.Progression.Features
            .SelectFactComponents<FeatureForPrerequisite>()
            .ToArray();
        if (fakes.Length == 0) {
            return;
        }

        if (__instance is Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature feature) {
            __result = fakes.Any(fake => fake.FakeFact.Is(feature.m_Feature));
        } else if (__instance is Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList list) {
            var count = list.Features.Count(required =>
                (selectionState == null || !selectionState.IsSelectedInChildren(required)) &&
                (unit.HasFact(required) || fakes.Any(fake => fake.FakeFact.Is(required))));
            __result = count >= list.Amount;
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("d0593fd3-3ab1-4266-abab-3a5329eeb639")]
public sealed class DirtyFightingBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateCMB> {
    public BlueprintAbilityReference[] m_Maneuvers =
        Array.Empty<BlueprintAbilityReference>();
    public BlueprintUnitFactReference[] m_ImprovedFeats =
        Array.Empty<BlueprintUnitFactReference>();

    public void OnEventAboutToTrigger(RuleCalculateCMB evt) {
        var source = Context?.SourceAbility;
        if (source == null || m_Maneuvers.Length != m_ImprovedFeats.Length) {
            return;
        }

        var index = Array.FindIndex(m_Maneuvers,
            reference => reference?.Get() == source);
        if (index < 0) {
            return;
        }

        var flanked = evt.Target?.CombatState?.IsFlanked == true;
        var hasImprovedFeat = m_ImprovedFeats[index]?.Get() is BlueprintUnitFact feat &&
            Owner.HasFact(feat);
        var modifier = flanked
            ? hasImprovedFeat ? 2 : -2
            : hasImprovedFeat ? 0 : -4;
        if (modifier != 0) {
            evt.AddModifier(modifier, Fact, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleCalculateCMB evt) { }
}

public interface IInitiatorDemoralizeHandler : IUnitSubscriber {
    void AfterIntimidateSuccess(
        Demoralize action,
        RuleSkillCheck intimidateCheck,
        Buff appliedBuff);
}

public interface IAbilityGetCommandTypeHandler : IUnitSubscriber {
    void HandleGetCommandType(
        AbilityData ability,
        ref UnitCommand.CommandType commandType);
}

[HarmonyPatch(typeof(AbilityData), nameof(AbilityData.ActionType), MethodType.Getter)]
internal static class AbilityCommandTypePatch {
    private static void Postfix(
        AbilityData __instance,
        ref UnitCommand.CommandType __result) {
        var result = __result;
        EventBus.RaiseEvent<IAbilityGetCommandTypeHandler>(
            __instance.Caster,
            handler => handler.HandleGetCommandType(__instance, ref result));
        __result = result;
    }
}

[HarmonyPatch(typeof(Demoralize))]
internal static class DemoralizeEventPatch {
    private static readonly MethodInfo RuleStatCheckSuccess =
        AccessTools.PropertyGetter(typeof(RuleStatCheck), nameof(RuleStatCheck.Success));
    private static readonly MethodInfo BuffStoreFact =
        AccessTools.Method(typeof(Buff), nameof(Buff.StoreFact));

    private static void NotifyIntimidateSuccess(
        Demoralize action,
        RuleSkillCheck intimidateCheck,
        Buff appliedBuff) {
        EventBus.RaiseEvent<IInitiatorDemoralizeHandler>(
            action.Context?.MaybeCaster,
            handler => handler.AfterIntimidateSuccess(
                action,
                intimidateCheck,
                appliedBuff));
    }

    [HarmonyPatch(nameof(Demoralize.RunAction)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) {
        try {
            var code = instructions.ToList();
            var newJumpTarget = generator.DefineLabel();
            var insertionIndex = 0;
            var leaveLabels = new List<Label>();
            var index = code.Count - 1;
            for (; index >= 0; index--) {
                if (code[index].opcode == OpCodes.Leave_S) {
                    insertionIndex = index;
                    leaveLabels = code[index].labels;
                    break;
                }
            }
            if (insertionIndex == 0) {
                throw new InvalidOperationException(
                    "Missing Demoralize transpiler insertion point.");
            }

            CodeInstruction loadCheck = null;
            CodeInstruction loadBuff = null;
            for (index--; index >= 0; index--) {
                if (code[index].Calls(RuleStatCheckSuccess)) {
                    loadCheck = code[index - 1].Clone();
                    break;
                }
                if (code[index].Calls(BuffStoreFact)) {
                    loadBuff = code[index - 2].Clone();
                }
                if (code[index].operand is Label jump && leaveLabels.Contains(jump)) {
                    code[index].operand = newJumpTarget;
                }
            }

            if (loadCheck == null || loadBuff == null) {
                throw new InvalidOperationException(
                    "Missing Demoralize transpiler local-variable anchors.");
            }

            code.InsertRange(insertionIndex, new[] {
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(newJumpTarget),
                loadCheck,
                loadBuff,
                CodeInstruction.Call(
                    typeof(DemoralizeEventPatch),
                    nameof(NotifyIntimidateSuccess)),
            });
            return code;
        } catch (Exception exception) {
            Main.Log.Error("Hurtful Demoralize transpiler failed; Hurtful will not trigger.");
            Main.Log.LogException(exception);
            return instructions;
        }
    }
}

[AllowedOn(typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff))]
[TypeId("34ec7317-3214-455d-af2a-170e97099765")]
public sealed class HurtfulTrigger : UnitFactComponentDelegate,
    IInitiatorDemoralizeHandler {
    private readonly Kingmaker.ElementsSystem.ConditionsChecker Conditions;

    public HurtfulTrigger() : this(
        ConditionsBuilder.New()
            .TargetInMeleeRange()
            .HasActionsAvailable(requireSwift: true)) { }

    private HurtfulTrigger(ConditionsBuilder conditions) {
        Conditions = conditions.Build();
    }

    public void AfterIntimidateSuccess(
        Demoralize action,
        RuleSkillCheck intimidateCheck,
        Buff appliedBuff) {
        if (!intimidateCheck.Success || appliedBuff == null || !Conditions.Check()) {
            return;
        }

        var caster = Context?.MaybeCaster;
        var target = action.Target?.Unit;
        var threatHand = caster?.GetThreatHandMelee();
        if (caster == null || target == null || threatHand?.Weapon == null) {
            return;
        }

        caster.SpendAction(UnitCommand.CommandType.Swift, false, 0);
        var attack = Context.TriggerRule(new RuleAttackWithWeapon(
            caster,
            target,
            threatHand.Weapon,
            0));
        if (!attack.AttackRoll.IsHit) {
            appliedBuff.Remove();
        }
    }
}

internal sealed class UnitPartSplitHex : OldStyleUnitPart {
    public CountableFlag Enabled = new();
    public SplitHexData Data = new();

    internal bool ValidTarget(BlueprintAbility ability, UnitEntityData target) =>
        !Enabled || !Data.HasStoredHex ||
        Data.StoredHex != ability || Data.Unit != target;

    public sealed class SplitHexData {
        private BlueprintAbilityReference m_StoredHex;
        public EntityRef<UnitEntityData> Unit;
        public bool HasStoredHex => !m_StoredHex?.IsEmpty() ?? false;
        public BlueprintAbility StoredHex => m_StoredHex?.Get();

        internal void Store(BlueprintAbility ability, UnitEntityData unit) {
            m_StoredHex = ability.ToReference<BlueprintAbilityReference>();
            Unit = unit;
        }

        internal void Clear() {
            m_StoredHex = null;
            Unit = new EntityRef<UnitEntityData>();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("5cd7105f-8de6-488e-9d98-481c61da74f0")]
public sealed class SplitHexToggle : UnitFactComponentDelegate {
    public override void OnTurnOn() => Owner.Ensure<UnitPartSplitHex>().Enabled.Retain();

    public override void OnTurnOff() {
        Owner.Get<UnitPartSplitHex>()?.Enabled.Release();
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("7ff72aeb-8ad2-4e1f-bf20-bc51d40a12d6")]
public sealed class SplitHexTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>,
    IAbilityGetCommandTypeHandler,
    ITickEachRound {
    public BlueprintAbilityReference[] m_ExcludedHexes =
        Array.Empty<BlueprintAbilityReference>();

    public void OnEventAboutToTrigger(RuleCastSpell evt) {
        var part = Owner.Ensure<UnitPartSplitHex>();
        if (part.Enabled && part.Data.HasStoredHex &&
            !evt.IsDuplicateSpellApplied &&
            part.Data.StoredHex == evt.Spell?.Blueprint) {
            evt.IsDuplicateSpellApplied = true;
            part.Data.Clear();
        }
    }

    public void OnEventDidTrigger(RuleCastSpell evt) {
        var ability = evt.Spell?.Blueprint;
        var target = evt.SpellTarget?.Unit;
        if (!evt.Success || evt.IsDuplicateSpellApplied || ability == null ||
            target == null || evt.Spell.IsAOE ||
            !ability.SpellDescriptor.HasFlag(
                Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Hex) ||
            m_ExcludedHexes.Any(reference => reference?.Get() == ability)) {
            return;
        }

        var part = Owner.Ensure<UnitPartSplitHex>();
        if (part.Data.HasStoredHex) {
            if (part.Data.StoredHex == ability) {
                part.Data.Clear();
            }
        } else {
            part.Data.Store(ability, target);
        }
    }

    public void HandleGetCommandType(
        AbilityData ability,
        ref UnitCommand.CommandType commandType) {
        var part = Owner.Ensure<UnitPartSplitHex>();
        if (part.Enabled && part.Data.HasStoredHex &&
            part.Data.StoredHex == ability.Blueprint) {
            commandType = UnitCommand.CommandType.Free;
        }
    }

    public void OnNewRound() => Owner.Ensure<UnitPartSplitHex>().Data.Clear();
}

[AllowMultipleComponents]
[TypeId("2870f857-7b75-4581-9eb1-0bddecd7396f")]
public sealed class AbilityTargetNoSplitHexRepeat : BlueprintComponent,
    IAbilityTargetRestriction {
    private static readonly LocalizedString RestrictionText = new() {
        Key = "ClassesReborn.SplitHex.TargetRestriction",
    };

    public string GetAbilityTargetRestrictionUIText(
        UnitEntityData caster,
        TargetWrapper target) => RestrictionText;

    public bool IsTargetRestrictionPassed(
        UnitEntityData caster,
        TargetWrapper target) {
        var ability = OwnerBlueprint as BlueprintAbility;
        return ability == null || target?.Unit == null ||
            caster.Get<UnitPartSplitHex>()?.ValidTarget(ability, target.Unit) != false;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("71ee7eae-ef5d-4793-8458-bf049292f548")]
public sealed class ShieldBraceAttackPenalty : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget> {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) {
        var weapon = evt.Weapon;
        var shield = Owner.Body.CurrentHandsEquipmentSet.SecondaryHand.MaybeShield;
        if (!ShieldBracePatch.IsEligible(weapon, shield)) {
            return;
        }

        var penalty = Rulebook.Trigger(new RuleCalculateArmorCheckPenalty(
            Owner,
            shield.ArmorComponent)).Result;
        if (penalty < 0) {
            evt.AddModifier(penalty, Fact, ModifierDescriptor.Shield);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
}

[HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.CanTakeOneHand))]
internal static class ShieldBracePatch {
    private static BlueprintFeature ShieldBrace =>
        ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintIds.ShieldBraceFeat);

    private static void Postfix(
        ItemEntityWeapon __instance,
        UnitEntityData unit,
        ref bool __result) {
        if (__result || unit == null || ShieldBrace == null ||
            !unit.HasFact(ShieldBrace)) {
            return;
        }

        __result = IsEligible(
            __instance,
            unit.Body.CurrentHandsEquipmentSet.SecondaryHand.MaybeShield);
    }

    internal static bool IsEligible(
        ItemEntityWeapon weapon,
        ItemEntityShield shield) {
        if (weapon?.Blueprint?.IsTwoHanded != true || shield == null ||
            shield.ArmorComponent?.Blueprint?.ProficiencyGroup ==
                ArmorProficiencyGroup.Buckler) {
            return false;
        }

        var groups = weapon.Blueprint.FighterGroup;
        return groups.Contains(WeaponFighterGroup.Spears) ||
               groups.Contains(WeaponFighterGroup.Polearms);
    }
}
