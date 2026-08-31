using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace ClassesReborn;

internal static class RequestedTalentRebalance {
    private const string HeavyArmorProficiency = "1b0f68188dcc435429fb87a022239681";
    private const string NativeEmboldeningStrike = "ba6bea8c93c64764ab7e5bbdb88fb9a6";
    private const string SlayerClass = "c75e0971973957d4dbad24bc7957e4fb";

    private static string Id(string name) => FutureContentIds.Get($"RequestedTalent.{name}");

    internal static void ConfigureRogueTalents() {
        var sneakIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.SneakAttackFeature).Icon;
        var armorPenaltyBuffs = Enumerable.Range(1, 10)
            .Select(rank => BuffConfigurator.New(
                    $"ClassesRebornArmorPiercerPenalty{rank}",
                    Id($"ArmorPiercer.Penalty.{rank}"))
                .SetDisplayName("ClassesReborn.RogueTalent.ArmorPiercer.Name")
                .SetDescription("ClassesReborn.RogueTalent.ArmorPiercer.Description")
                .SetIcon(sneakIcon)
                .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.NaturalArmor,
                    stat: StatType.AC,
                    value: -rank)
                .Configure())
            .ToArray();

        var hamstringBuff = BuffConfigurator.New(
                "ClassesRebornHamstringStrikeModeBuff",
                Id("HamstringStrike.ModeBuff"))
            .SetDisplayName("ClassesReborn.RogueTalent.HamstringStrike.Name")
            .SetDescription("ClassesReborn.RogueTalent.HamstringStrike.Description")
            .SetIcon(sneakIcon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new HamstringStrikeComponent {
                m_RogueClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.RogueClass),
                m_EffectBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    Id("HamstringStrike.EffectBuff")),
                m_OtherMode = BlueprintTool.GetRef<BlueprintBuffReference>(
                    Id("ArmorPiercer.ModeBuff")),
            })
            .Configure();
        var hamstringEffect = BuffConfigurator.New(
                "ClassesRebornHamstringStrikeEffectBuff",
                Id("HamstringStrike.EffectBuff"))
            .SetDisplayName("ClassesReborn.RogueTalent.HamstringStrike.Name")
            .SetDescription("ClassesReborn.RogueTalent.HamstringStrike.EffectDescription")
            .SetIcon(sneakIcon)
            .AddCondition(UnitCondition.Prone)
            .AddCondition(UnitCondition.CantMove)
            .Configure();
        var hamstringAbility = ActivatableAbilityConfigurator.New(
                "ClassesRebornHamstringStrikeModeAbility",
                Id("HamstringStrike.ModeAbility"))
            .SetDisplayName("ClassesReborn.RogueTalent.HamstringStrike.Name")
            .SetDescription("ClassesReborn.RogueTalent.HamstringStrike.Description")
            .SetIcon(sneakIcon)
            .SetBuff(hamstringBuff)
            .SetActivationType(AbilityActivationType.Immediately)
            .SetDeactivateImmediately(false)
            .SetDoNotTurnOffOnRest(true)
            .Configure();
        var hamstring = FeatureConfigurator.New(
                "ClassesRebornHamstringStrikeTalent",
                Id("HamstringStrike.Feature"))
            .SetDisplayName("ClassesReborn.RogueTalent.HamstringStrike.Name")
            .SetDescription("ClassesReborn.RogueTalent.HamstringStrike.Description")
            .SetIcon(sneakIcon)
            .SetIsClassFeature(true)
            .AddPrerequisiteClassLevel(BlueprintIds.RogueClass, 10)
            .AddFacts(new() { hamstringAbility.AssetGuid.ToString() })
            .Configure();

        var armorModeBuff = BuffConfigurator.New(
                "ClassesRebornArmorPiercerModeBuff",
                Id("ArmorPiercer.ModeBuff"))
            .SetDisplayName("ClassesReborn.RogueTalent.ArmorPiercer.Name")
            .SetDescription("ClassesReborn.RogueTalent.ArmorPiercer.Description")
            .SetIcon(sneakIcon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new ArmorPiercerComponent {
                m_PenaltyBuffs = armorPenaltyBuffs
                    .Select(buff => buff.ToReference<BlueprintBuffReference>())
                    .ToArray(),
                m_OtherMode = hamstringBuff.ToReference<BlueprintBuffReference>(),
            })
            .Configure();
        var armorAbility = ActivatableAbilityConfigurator.New(
                "ClassesRebornArmorPiercerModeAbility",
                Id("ArmorPiercer.ModeAbility"))
            .SetDisplayName("ClassesReborn.RogueTalent.ArmorPiercer.Name")
            .SetDescription("ClassesReborn.RogueTalent.ArmorPiercer.Description")
            .SetIcon(sneakIcon)
            .SetBuff(armorModeBuff)
            .SetActivationType(AbilityActivationType.Immediately)
            .SetDeactivateImmediately(false)
            .SetDoNotTurnOffOnRest(true)
            .Configure();
        var armorPiercer = FeatureConfigurator.New(
                "ClassesRebornArmorPiercerTalent",
                Id("ArmorPiercer.Feature"))
            .SetDisplayName("ClassesReborn.RogueTalent.ArmorPiercer.Name")
            .SetDescription("ClassesReborn.RogueTalent.ArmorPiercer.Description")
            .SetIcon(sneakIcon)
            .SetIsClassFeature(true)
            .AddPrerequisiteClassLevel(BlueprintIds.RogueClass, 10)
            .AddFacts(new() { armorAbility.AssetGuid.ToString() })
            .Configure();

        _ = hamstringEffect;
        var emboldening = FeatureConfigurator.For(NativeEmboldeningStrike)
            .SetHideInUI(false)
            .SetHideInCharacterSheetAndLevelUp(false)
            .Configure();
        foreach (var selectionId in new[] {
                     BlueprintIds.RogueTalentSelection,
                     BlueprintIds.SylvanTricksterTalentSelection,
                 }) {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
            AddToSelection(selection, hamstring, armorPiercer, emboldening);
        }
    }

    internal static void ConfigureSlayerTalents(
        IReadOnlyList<BlueprintFeatureSelection> talentSelections) {
        if (talentSelections == null || talentSelections.Count < 3) {
            throw new InvalidOperationException(
                "Slayer talent expansion requires all three native talent tiers.");
        }
        var armorIcon = BlueprintTool.Get<BlueprintFeature>(HeavyArmorProficiency).Icon;
        var reapingIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFocus).Icon;
        var marauder = FeatureConfigurator.New(
                "ClassesRebornArmoredMarauderTalent",
                Id("ArmoredMarauder.Feature"))
            .SetDisplayName("ClassesReborn.SlayerTalent.ArmoredMarauder.Name")
            .SetDescription("ClassesReborn.SlayerTalent.ArmoredMarauder.Description")
            .SetIcon(armorIcon)
            .SetIsClassFeature(true)
            .AddFacts(new() { HeavyArmorProficiency })
            .AddComponent(new SlayerHeavyArmorScaling {
                m_SlayerClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    SlayerClass),
                ReduceArmorCheckPenalty = true,
            })
            .Configure();
        var swiftness = FeatureConfigurator.New(
                "ClassesRebornArmoredSwiftnessTalent",
                Id("ArmoredSwiftness.Feature"))
            .SetDisplayName("ClassesReborn.SlayerTalent.ArmoredSwiftness.Name")
            .SetDescription("ClassesReborn.SlayerTalent.ArmoredSwiftness.Description")
            .SetIcon(armorIcon)
            .SetIsClassFeature(true)
            .AddPrerequisiteFeature(marauder)
            .AddComponent(new HeavyArmorSpeedPenaltyRemoval())
            .AddComponent(new SlayerHeavyArmorScaling {
                m_SlayerClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    SlayerClass),
                IncreaseMaximumDexterity = true,
            })
            .Configure();
        var reaping = FeatureConfigurator.New(
                "ClassesRebornReapingStalkerTalent",
                Id("ReapingStalker.Feature"))
            .SetDisplayName("ClassesReborn.SlayerTalent.ReapingStalker.Name")
            .SetDescription("ClassesReborn.SlayerTalent.ReapingStalker.Description")
            .SetIcon(reapingIcon)
            .SetIsClassFeature(true)
            .AddPrerequisiteClassLevel(SlayerClass, 10)
            .AddComponent(new ReapingStalkerComponent())
            .Configure();
        foreach (var selection in talentSelections) {
            AddToSelection(selection, marauder, swiftness);
        }
        AddToSelection(talentSelections[2], reaping);

        if (talentSelections.Any(selection =>
                selection.m_AllFeatures.Count(reference =>
                    reference?.Get() == marauder) != 1 ||
                selection.m_AllFeatures.Count(reference =>
                    reference?.Get() == swiftness) != 1) ||
            talentSelections.Take(2).Any(selection =>
                selection.m_AllFeatures.Any(reference =>
                    reference?.Get() == reaping)) ||
            talentSelections[2].m_AllFeatures.Count(reference =>
                reference?.Get() == reaping) != 1) {
            throw new InvalidOperationException(
                "Armored Marauder and Armored Swiftness must be normal Slayer talents while Reaping Stalker remains advanced.");
        }
    }

    private static void AddToSelection(
        BlueprintFeatureSelection selection,
        params BlueprintFeature[] features) {
        selection.m_AllFeatures = Append(selection.m_AllFeatures, features);
        if (selection.m_Features?.Length > 0) {
            selection.m_Features = Append(selection.m_Features, features);
        }
    }

    private static BlueprintFeatureReference[] Append(
        BlueprintFeatureReference[] existing,
        IEnumerable<BlueprintFeature> features) {
        var result = existing?.ToList() ?? new List<BlueprintFeatureReference>();
        foreach (var feature in features) {
            if (!result.Any(reference => reference?.Get() == feature)) {
                result.Add(feature.ToReference<BlueprintFeatureReference>());
            }
        }
        return result.ToArray();
    }
}

internal static class SneakSacrificeHelpers {
    internal static bool TryGetSneakDice(
        RulePrepareDamage evt,
        UnitDescriptor owner,
        bool requireMelee,
        out int dice) {
        dice = 0;
        var attack = evt.ParentRule?.AttackRoll;
        var weapon = attack?.Weapon;
        if (attack?.IsHit != true || attack.IsSneakAttack != true ||
            (requireMelee && weapon?.Blueprint?.IsMelee != true)) {
            return false;
        }
        dice = evt.DamageBundle
            .Where(damage => damage.Sneak)
            .Sum(damage => Math.Max(0, damage.Dice.ModifiedValue.Rolls));
        return dice > 0;
    }

    internal static void RemoveSneakDamage(RulePrepareDamage evt) =>
        evt.ParentRule.Remove(damage => damage.Sneak);
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("17ec1d7e-03d2-4cc5-bb2c-0ae16e4b4c63")]
public sealed class HamstringStrikeComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public BlueprintCharacterClassReference m_RogueClass;
    public BlueprintBuffReference m_EffectBuff;
    public BlueprintBuffReference m_OtherMode;

    public override void OnTurnOn() {
        var other = m_OtherMode?.Get();
        if (other != null) {
            Owner.Buffs.GetBuff(other)?.Remove();
        }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (!SneakSacrificeHelpers.TryGetSneakDice(
                evt, Owner, requireMelee: false, out _)) {
            return;
        }
        SneakSacrificeHelpers.RemoveSneakDamage(evt);
        var target = evt.ParentRule?.Target;
        var effect = m_EffectBuff?.Get();
        var rogueClass = m_RogueClass?.Get();
        if (target == null || effect == null || rogueClass == null) {
            return;
        }
        var dc = 10 + Owner.Progression.GetClassLevel(rogueClass) / 2 +
                 Math.Max(0, Owner.Stats.Dexterity.Bonus);
        var save = Rulebook.Trigger(new RuleSavingThrow(
            target,
            SavingThrowType.Fortitude,
            dc));
        if (!save.IsPassed) {
            target.Buffs.AddBuff(effect, Owner, CombatChecks.Rounds(1));
        }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("3a530187-f879-4996-85a8-4176b908c694")]
public sealed class ArmorPiercerComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public BlueprintBuffReference[] m_PenaltyBuffs =
        Array.Empty<BlueprintBuffReference>();
    public BlueprintBuffReference m_OtherMode;

    public override void OnTurnOn() {
        var other = m_OtherMode?.Get();
        if (other != null) {
            Owner.Buffs.GetBuff(other)?.Remove();
        }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (!SneakSacrificeHelpers.TryGetSneakDice(
                evt, Owner, requireMelee: false, out var dice)) {
            return;
        }
        SneakSacrificeHelpers.RemoveSneakDamage(evt);
        var target = evt.ParentRule?.Target;
        if (target == null || m_PenaltyBuffs.Length == 0) {
            return;
        }
        foreach (var reference in m_PenaltyBuffs) {
            var old = reference?.Get();
            if (old != null) {
                target.Buffs.GetBuff(old)?.Remove();
            }
        }
        var penalty = m_PenaltyBuffs[Math.Min(dice, m_PenaltyBuffs.Length) - 1]?.Get();
        if (penalty != null) {
            target.Buffs.AddBuff(penalty, Owner, CombatChecks.Rounds(1));
        }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("2e2274ce-2ae1-4d18-b86d-9939b249677b")]
public sealed class SlayerHeavyArmorScaling : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateArmorCheckPenalty>,
    IInitiatorRulebookHandler<RuleCalculateArmorMaxDexBonusLimit> {
    public BlueprintCharacterClassReference m_SlayerClass;
    public bool ReduceArmorCheckPenalty;
    public bool IncreaseMaximumDexterity;

    private int Bonus => m_SlayerClass?.Get() is BlueprintCharacterClass slayer
        ? Owner.Progression.GetClassLevel(slayer) / 6
        : 0;

    private static bool IsHeavy(ItemEntityArmor armor) =>
        armor?.Blueprint?.ProficiencyGroup == ArmorProficiencyGroup.Heavy;

    public void OnEventAboutToTrigger(RuleCalculateArmorCheckPenalty evt) {
        if (ReduceArmorCheckPenalty && IsHeavy(evt.Armor) && Bonus > 0) {
            evt.AddBonus(Bonus);
        }
    }

    public void OnEventDidTrigger(RuleCalculateArmorCheckPenalty evt) { }

    public void OnEventAboutToTrigger(RuleCalculateArmorMaxDexBonusLimit evt) {
        if (IncreaseMaximumDexterity && IsHeavy(evt.Armor) && Bonus > 0) {
            evt.AddBonus(Bonus);
        }
    }

    public void OnEventDidTrigger(RuleCalculateArmorMaxDexBonusLimit evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0f510523-1ba2-4d24-8451-90ef651228e1")]
public sealed class HeavyArmorSpeedPenaltyRemoval :
    UnitFactComponentDelegate<HeavyArmorSpeedPenaltyRemoval.ComponentData>,
    IUnitEquipmentHandler {
    public sealed class ComponentData {
        public bool Applied;
    }

    public override void OnTurnOn() => Refresh();

    public override void OnTurnOff() {
        if (Data.Applied) {
            Owner.State.Features.ImmuneToArmorSpeedPenalty.Release();
            Data.Applied = false;
        }
        Owner.Body.Armor.Armor?.RecalculateStats();
    }

    public void HandleEquipmentSlotUpdated(
        Kingmaker.Items.Slots.ItemSlot slot,
        Kingmaker.Items.ItemEntity previousItem) => Refresh();

    private void Refresh() {
        var armor = Owner.Body.Armor.Armor;
        var shouldApply = armor?.Blueprint?.ProficiencyGroup ==
                          ArmorProficiencyGroup.Heavy;
        if (shouldApply == Data.Applied) {
            return;
        }
        if (shouldApply) {
            Owner.State.Features.ImmuneToArmorSpeedPenalty.Retain();
        } else {
            Owner.State.Features.ImmuneToArmorSpeedPenalty.Release();
        }
        Data.Applied = shouldApply;
        armor?.RecalculateStats();
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("5b4cce3b-3059-4494-bbd9-b1d466ce0bbc")]
public sealed class ReapingStalkerComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    internal static bool IsReapingWeapon(ItemEntityWeapon weapon) =>
        weapon?.Blueprint?.Category is WeaponCategory.Sickle or WeaponCategory.Scythe;

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (IsReapingWeapon(evt.Weapon)) {
            evt.IncreaseWeaponSize(1);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[HarmonyPatch(typeof(RuleCalculateWeaponStats), nameof(RuleCalculateWeaponStats.OnTrigger))]
internal static class ReapingStalkerCriticalRangePatch {
    private static void Postfix(RuleCalculateWeaponStats __instance) {
        if (__instance.CriticalEdgeBonus != 0 ||
            !ReapingStalkerComponent.IsReapingWeapon(__instance.Weapon) ||
            __instance.Initiator?.Descriptor?.Progression?.Features
                .SelectFactComponents<ReapingStalkerComponent>().Any() != true) {
            return;
        }
        __instance.CriticalEdgeBonus = 1;
    }
}
