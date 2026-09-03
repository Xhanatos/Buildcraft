using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("119ed06a-6717-482d-af19-56c1c4963551")]
public sealed class FatesFavoredMarker : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public override void OnTurnOn() {
        Owner.Ensure<UnitPartFatesFavored>().Enabled.Retain();
        RefreshLuckBonuses();
    }

    public override void OnTurnOff() {
        Owner.Get<UnitPartFatesFavored>()?.Enabled.Release();
        RefreshLuckBonuses();
    }

    private void RefreshLuckBonuses() {
        var stats = Owner?.Stats?.AllStats;
        if (stats == null) {
            return;
        }

        foreach (var stat in stats) {
            if (stat?.Modifiers?.Any(modifier =>
                    modifier.ModDescriptor == ModifierDescriptor.Luck) == true) {
                stat.UpdateValue();
            }
        }
    }

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        var modifiers = evt?.StatModifiersAtTheMoment?.ToList();
        if (modifiers == null) {
            return;
        }

        var bestLuckIndex = -1;
        for (var index = 0; index < modifiers.Count; index++) {
            var modifier = modifiers[index];
            if (modifier?.ModDescriptor == ModifierDescriptor.Luck &&
                modifier.ModValue > 0 &&
                (bestLuckIndex < 0 ||
                 modifier.ModValue > modifiers[bestLuckIndex].ModValue)) {
                bestLuckIndex = index;
            }
        }

        if (bestLuckIndex < 0) {
            return;
        }

        var luck = modifiers[bestLuckIndex];
        modifiers[bestLuckIndex] = new ModifiableValue.Modifier {
            AppliedTo = luck.AppliedTo,
            ModDescriptor = luck.ModDescriptor,
            StackMode = luck.StackMode,
            ModValue = luck.ModValue + 1,
            Source = luck.Source,
            SourceComponent = luck.SourceComponent,
            ItemSource = luck.ItemSource,
        };
        evt.StatModifiersAtTheMoment = modifiers;
    }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) {
        var damage = evt?.DamageDescription?.FirstOrDefault();
        if (damage == null) {
            return;
        }

        var luck = damage.Modifiers
            .Where(modifier =>
                modifier.Descriptor == ModifierDescriptor.Luck &&
                modifier.Value > 0)
            .OrderByDescending(modifier => modifier.Value)
            .FirstOrDefault();
        if (luck.Value > 0) {
            damage.AddModifier(new Modifier(
                luck.Value + 1,
                luck.Fact,
                luck.Descriptor));
        }
    }
}

internal sealed class UnitPartFatesFavored : OldStyleUnitPart {
    public CountableFlag Enabled = new();
}

[HarmonyPatch(
    typeof(ModifiableValue),
    "ApplyModifiersFiltered",
    new[] { typeof(int), typeof(Func<ModifiableValue.Modifier, bool>) })]
internal static class FatesFavoredLuckBonusPatch {
    private static void Postfix(
        ModifiableValue __instance,
        Func<ModifiableValue.Modifier, bool> filter,
        ref int __result) {
        // CharacterStats.PostLoad can recalculate values before unit facts have
        // been activated. The runtime part is absent in that state, so this
        // global postfix remains inert; activating the marker refreshes any
        // already-loaded Luck modifiers afterward.
        var unit = __instance?.Owner?.Unit;
        var modifiers = __instance?.Modifiers;
        var part = unit?.Get<UnitPartFatesFavored>();
        if (part == null || !part.Enabled || modifiers == null) {
            return;
        }

        if (modifiers.Any(modifier =>
                modifier.ModDescriptor == ModifierDescriptor.Luck &&
                modifier.ModValue > 0 &&
                (filter == null || filter(modifier)))) {
            __result++;
        }
    }
}

internal static class BruisingIntellectContext {
    [ThreadStatic]
    private static int Depth;

    internal static bool IsDemoralizing => Depth > 0;
    internal static void Enter() => Depth++;
    internal static void Exit() => Depth = Math.Max(0, Depth - 1);
}

[HarmonyPatch(typeof(Demoralize), nameof(Demoralize.RunAction))]
internal static class BruisingIntellectDemoralizePatch {
    private static void Prefix() => BruisingIntellectContext.Enter();
    private static Exception Finalizer(Exception __exception) {
        BruisingIntellectContext.Exit();
        return __exception;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("3ae710d1-09d1-4aee-bf9f-0b3ae00b50aa")]
public sealed class BruisingIntellectComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSkillCheck> {
    public void OnEventAboutToTrigger(RuleSkillCheck evt) {
        if (!BruisingIntellectContext.IsDemoralizing ||
            evt.StatType != StatType.SkillPersuasion) {
            return;
        }

        var intelligence = Owner.Stats.Intelligence?.Bonus ?? 0;
        var charisma = Owner.Stats.Charisma?.Bonus ?? 0;
        var difference = intelligence - charisma;
        if (difference == 0) {
            return;
        }

        var modifier = evt.Bonus.AddModifier(
            difference,
            Runtime,
            ModifierDescriptor.Trait);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleSkillCheck evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("d15e42b5-e3f4-4bd6-9652-1f0be1a78f70")]
public sealed class GiftedAdeptCasterLevel : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (Param?.Blueprint is BlueprintAbility selectedSpell &&
            evt.Spell == selectedSpell) {
            evt.AddBonusCasterLevel(1, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintParametrizedFeature), false)]
[TypeId("7bc098d1-c15b-47be-a1e1-52a48fbba70c")]
public sealed class GiftedAdeptSpellPrerequisite : BlueprintComponent,
    IParamPrerequisite {
    public bool CanBeSelected(
        BlueprintParametrizedFeature parametrizedFeature,
        UnitDescriptor unit,
        FeatureParam param) {
        if (unit == null ||
            param?.Blueprint is not BlueprintAbility selectedSpell) {
            return false;
        }

        foreach (var spellbook in unit.Spellbooks) {
            var spellLevel = spellbook?.Blueprint?.SpellList?.GetLevel(selectedSpell) ?? -1;
            if (spellLevel >= 1 && spellLevel <= spellbook.MaxSpellLevel) {
                return true;
            }
        }

        if (unit.Progression?.Classes == null) {
            return false;
        }

        foreach (var classData in unit.Progression.Classes) {
            var spellbook = ResolveSpellbook(classData);
            var spellLevel = spellbook?.SpellList?.GetLevel(selectedSpell) ?? -1;
            if (spellLevel >= 1 &&
                HasSpellLevel(spellbook, classData.Level, spellLevel)) {
                return true;
            }
        }

        return false;
    }

    private static BlueprintSpellbook ResolveSpellbook(ClassData classData) {
        if (classData?.Spellbook != null) {
            return classData.Spellbook;
        }

        var replacement = classData?.Archetypes?
            .Select(archetype => archetype?.ReplaceSpellbook)
            .FirstOrDefault(spellbook => spellbook != null);
        if (replacement != null) {
            return replacement;
        }

        if (classData?.Archetypes?.Any(archetype =>
                archetype?.RemoveSpellbook == true) == true) {
            return null;
        }

        return classData?.CharacterClass?.Spellbook;
    }

    private static bool HasSpellLevel(
        BlueprintSpellbook spellbook,
        int classLevel,
        int spellLevel) =>
        spellbook != null && classLevel > 0 && (
            HasSpellLevel(spellbook.SpellsPerDay, classLevel, spellLevel) ||
            HasSpellLevel(spellbook.SpellSlots, classLevel, spellLevel) ||
            HasSpellLevel(spellbook.SpellsKnown, classLevel, spellLevel));

    private static bool HasSpellLevel(
        BlueprintSpellsTable table,
        int classLevel,
        int spellLevel) =>
        table != null && table.GetCount(classLevel, spellLevel) > 0;
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("7d47a18d-a2bb-4d59-83a9-72438eec962f")]
public sealed class VulpineAmbusherAttackBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (evt.Target?.CombatState?.NotSurprised == false) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}
