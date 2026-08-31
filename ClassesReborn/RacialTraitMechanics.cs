using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("77eb1491-b85d-4be7-8fb8-9bf1c164473c")]
public sealed class TuskedNaturalAttackSizeIncrease : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        var category = evt.Weapon?.Blueprint?.Category;
        if (category == WeaponCategory.Bite || category == WeaponCategory.Gore) {
            evt.IncreaseWeaponSize(1);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("2cf26bf8-142e-468e-bcdc-20d4c31cacaf")]
public sealed class VileChemistBombDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon?.Blueprint?.Category != WeaponCategory.Bomb) {
            return;
        }

        evt.Add(new EnergyDamage(
            new DiceFormula(1, DiceType.D2),
            Kingmaker.Enums.Damage.DamageEnergyType.Acid));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintFeatureBase))]
[TypeId("1037367f-e690-4a10-bf3f-b382efb38dfe")]
public sealed class RaceTraitPrerequisite : Prerequisite {
    public BlueprintRaceReference m_Race;
    public BlueprintFeatureSelectionReference m_AdoptedSelection;
    public string RaceName;

    public override bool CheckInternal(
        FeatureSelectionState selectionState,
        UnitDescriptor unit,
        LevelUpState state) {
        var requiredRace = m_Race?.Get();
        var selectedRace = state?.SelectedRace ?? unit?.Progression?.Race;
        var adoptedSelection = m_AdoptedSelection?.Get();
        if (adoptedSelection != null && selectionState?.Selection == adoptedSelection) {
            return requiredRace != null && selectedRace != null && selectedRace != requiredRace;
        }
        if (requiredRace != null && selectedRace == requiredRace) {
            return true;
        }
        return requiredRace != null && unit?.Progression?.Features
            .SelectFactComponents<RacialHeritageMarker>()
            .Any(component => component.m_Race?.Get() == requiredRace) == true;
    }

    public override string GetUITextInternal(UnitDescriptor unit) =>
        $"Race: {RaceName}";
}

internal static class RacialTraitRuleHelpers {
    internal static void AddSaveModifier(
        RuleSavingThrow evt,
        UnitFactComponentDelegate component,
        int value,
        ModifierDescriptor descriptor = ModifierDescriptor.Trait) {
        var stat = evt.Type switch {
            SavingThrowType.Fortitude => component.Owner.Stats.SaveFortitude,
            SavingThrowType.Reflex => component.Owner.Stats.SaveReflex,
            SavingThrowType.Will => component.Owner.Stats.SaveWill,
            _ => null,
        };
        if (stat == null) {
            return;
        }

        var modifier = stat.AddModifier(value, component.Runtime, descriptor);
        evt.AddTemporaryModifier(modifier);
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("75aaebdd-4ad4-456d-845d-2491b6c18c0f")]
public sealed class ColorThiefStealthBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSkillCheck> {
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleSkillCheck evt) {
        if (evt.StatType != StatType.SkillStealth) {
            return;
        }

        var armor = Owner.Body.Armor.Armor;
        if (armor != null &&
            armor.Blueprint.ProficiencyGroup !=
                Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.Light) {
            return;
        }

        var modifier = evt.Bonus.AddModifier(
            Bonus,
            Runtime,
            ModifierDescriptor.Trait);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleSkillCheck evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f69a15de-aeb9-477a-85ad-432bea65d0b7")]
public sealed class DemoralizeTraitBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSkillCheck> {
    public int Bonus = 2;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Trait;

    public void OnEventAboutToTrigger(RuleSkillCheck evt) {
        if (!BruisingIntellectContext.IsDemoralizing ||
            evt.StatType != StatType.SkillPersuasion) {
            return;
        }

        var modifier = evt.Bonus.AddModifier(
            Bonus,
            Runtime,
            BonusDescriptor);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleSkillCheck evt) { }
}

[TypeId("6299882f-0e96-420e-8211-90046df9bffc")]
public sealed class ContextActionFoulBelch : ContextAction {
    public BlueprintBuffReference m_SickenedBuff;

    public override string GetCaption() => "apply Foul Belch";

    public override void RunAction() {
        var caster = Context?.MaybeCaster;
        var target = Target.Unit;
        var sickened = m_SickenedBuff?.Get();
        if (caster == null || target == null || sickened == null) {
            return;
        }

        var dc = 10 + caster.Progression.CharacterLevel / 2 +
                 caster.Stats.Constitution.Bonus;
        var save = Context.TriggerRule(new RuleSavingThrow(
            target,
            SavingThrowType.Fortitude,
            dc));
        if (save.IsPassed) {
            return;
        }

        var duration = Context.TriggerRule(new RuleRollDice(
            caster,
            new DiceFormula(1, DiceType.D6))).Result;
        target.Buffs.AddBuff(
            sickened,
            Context,
            CombatChecks.Rounds(Math.Max(1, duration)));
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("01b34e2c-5929-4b97-9181-5eca3ec7965d")]
public sealed class GoblinFoolhardinessAttackBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        var weapon = evt.Weapon?.Blueprint;
        if (evt.Target == null || weapon?.IsMelee != true ||
            weapon.AttackRange.Meters > 5.Feet().Meters ||
            evt.Target.Descriptor.State.Size <= Owner.Descriptor.State.Size ||
            HasAdjacentAlly()) {
            return;
        }

        evt.AddModifier(Bonus, Fact, ModifierDescriptor.Trait);
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    private bool HasAdjacentAlly() {
        if (!Game.HasInstance) {
            return false;
        }

        var ownerCorpulence = Owner.View?.Corpulence ?? 0f;
        return Game.Instance.State.AwakeUnits.Any(unit =>
            unit != null && unit != Owner && !unit.IsEnemy(Owner) &&
            unit.DistanceTo(Owner) <= ownerCorpulence +
                (unit.View?.Corpulence ?? 0f) + 5.Feet().Meters);
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("75740dd3-a4c0-422b-a9d0-f7aca6c7d50f")]
public sealed class BouncyTripDefense : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateCMD> {
    public int Bonus = 2;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Racial;

    public void OnEventAboutToTrigger(RuleCalculateCMD evt) {
        if (evt.Type == CombatManeuver.Trip) {
            evt.AddModifier(Bonus, Fact, BonusDescriptor);
        }
    }

    public void OnEventDidTrigger(RuleCalculateCMD evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8f6db482-231a-4ad7-961c-dd2a0237090a")]
public sealed class UnderfootMenaceArmorClass : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        var attack = Rulebook.CurrentContext?.PreviousEvent as RuleAttackRoll;
        if (attack?.RuleAttackWithWeapon?.IsAttackOfOpportunity == true &&
            evt.Initiator != null &&
            evt.Initiator.Descriptor.State.Size > Owner.Descriptor.State.Size) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Dodge);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("40a6e989-9f13-4c02-97ad-1aa4fb8ac9a6")]
public sealed class RacialSpellPenetrationBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSpellResistanceCheck> {
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt) =>
        evt.AddSpellPenetration(Bonus, ModifierDescriptor.Racial);

    public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("409f9b8b-0e8d-4d75-839f-9725af5e2554")]
public sealed class DiseaseSaveReroll : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleRollD20> {
    public void OnEventAboutToTrigger(RuleRollD20 evt) {
        if (evt.IsFake ||
            Rulebook.CurrentContext?.PreviousEvent is not RuleSavingThrow savingThrow ||
            (savingThrow.Reason?.Ability?.SpellDescriptor & SpellDescriptor.Disease) == 0) {
            return;
        }
        evt.AddReroll(1, takeBest: true, Fact);
    }

    public void OnEventDidTrigger(RuleRollD20 evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("c52159cf-7cfa-4248-8223-5ffdcec6a004")]
public sealed class RacialWeaponDamageBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public WeaponCategory[] Categories = Array.Empty<WeaponCategory>();
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var weapon = evt.ParentRule?.AttackRoll?.Weapon?.Blueprint;
        if (weapon != null && Categories.Contains(weapon.Category) &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(Bonus, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6400c6a0-7dc5-461f-a8cd-c3d45a7ed3af")]
public sealed class RacialCasterLevelBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public SpellSchool School = SpellSchool.None;
    public SpellDescriptor Descriptor = SpellDescriptor.None;
    public string[] NameFragments = Array.Empty<string>();
    public int Bonus = 1;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Trait;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spell == null) {
            return;
        }

        var schoolMatches = School != SpellSchool.None && evt.Spell.School == School;
        var descriptorMatches = Descriptor != SpellDescriptor.None &&
            (evt.Spell.SpellDescriptor & Descriptor) != 0;
        var nameMatches = NameFragments.Any(fragment =>
            !string.IsNullOrWhiteSpace(fragment) &&
            evt.Spell.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        if (schoolMatches || descriptorMatches || nameMatches) {
            evt.AddBonusCasterLevel(Bonus, BonusDescriptor);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0f6e1434-4f5e-4c09-86ea-7c3b1a149e31")]
public sealed class RacialSpellDcBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public SpellSchool School = SpellSchool.None;
    public SpellDescriptor Descriptor = SpellDescriptor.None;
    public int Bonus = 1;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Trait;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spell == null) {
            return;
        }

        var schoolMatches = School != SpellSchool.None && evt.Spell.School == School;
        var descriptorMatches = Descriptor != SpellDescriptor.None &&
            (evt.Spell.SpellDescriptor & Descriptor) != 0;
        if (schoolMatches || descriptorMatches) {
            evt.AddBonusDC(Bonus, BonusDescriptor);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8f695ccb-3d4b-4987-9208-3038046683d7")]
public sealed class RacialCasterLevelPenalty : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public int Penalty = 2;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spell != null) {
            evt.AddBonusCasterLevel(-Math.Abs(Penalty), ModifierDescriptor.Penalty);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9fb0cd14-c806-4639-90f9-ecaf38f958cf")]
public sealed class SuppressHeritageSkillBonuses : UnitFactComponentDelegate,
    IUnitGainFactHandler,
    IUnitLostFactHandler {
    private static readonly StatType[] SkillStats = {
        StatType.SkillAthletics,
        StatType.SkillMobility,
        StatType.SkillThievery,
        StatType.SkillStealth,
        StatType.SkillKnowledgeArcana,
        StatType.SkillKnowledgeWorld,
        StatType.SkillLoreNature,
        StatType.SkillLoreReligion,
        StatType.SkillPerception,
        StatType.SkillPersuasion,
        StatType.SkillUseMagicDevice,
        StatType.CheckBluff,
        StatType.CheckDiplomacy,
        StatType.CheckIntimidate,
    };

    public BlueprintFeatureReference[] m_Heritages =
        Array.Empty<BlueprintFeatureReference>();

    public override void OnTurnOn() => Reapply();

    public override void OnTurnOff() => Clear();

    public void HandleUnitGainFact(EntityFact fact) {
        if (IsHeritage(fact?.Blueprint)) {
            Reapply();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (IsHeritage(fact?.Blueprint)) {
            Reapply();
        }
    }

    private bool IsHeritage(BlueprintFact fact) =>
        fact != null && m_Heritages.Any(reference => reference?.Get() == fact);

    private void Clear() {
        foreach (var statType in SkillStats) {
            Owner.Stats.GetStat(statType)?.RemoveModifiersFrom(Runtime);
        }
    }

    private void Reapply() {
        Clear();
        foreach (var reference in m_Heritages) {
            var heritage = reference?.Get();
            if (heritage == null || !Owner.HasFact(heritage)) {
                continue;
            }

            foreach (var bonus in heritage.GetComponents<AddStatBonus>()) {
                if (bonus.Value <= 0 || bonus.Descriptor != ModifierDescriptor.Racial ||
                    !SkillStats.Contains(bonus.Stat)) {
                    continue;
                }
                Owner.Stats.GetStat(bonus.Stat)?.AddModifier(
                    -bonus.Value,
                    Runtime,
                    ModifierDescriptor.Racial);
            }
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("9b265af2-d95e-4354-a4ee-2afcb83a4b47")]
public sealed class SourceCreatureSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintFeatureReference m_SourceType;
    public int Bonus = 2;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Trait;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        var sourceType = m_SourceType?.Get();
        if (source != null && sourceType != null && source.Descriptor.HasFact(sourceType)) {
            RacialTraitRuleHelpers.AddSaveModifier(evt, this, Bonus, BonusDescriptor);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("e8868b33-d7de-4dc0-8117-d147743d06de")]
public sealed class RelentlessManeuverBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCombatManeuver> {
    public int Bonus = 2;
    public ModifierDescriptor BonusDescriptor = ModifierDescriptor.Trait;

    public void OnEventAboutToTrigger(RuleCombatManeuver evt) {
        if (evt.Type == CombatManeuver.BullRush || evt.Type == CombatManeuver.Overrun) {
            evt.AddModifier(Bonus, Fact, BonusDescriptor);
        }
    }

    public void OnEventDidTrigger(RuleCombatManeuver evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("1b306ddf-03b7-4acb-a008-edfa889ad355")]
public sealed class EternalHopeReroll : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleRollD20> {
    public BlueprintAbilityResourceReference m_Resource;

    public void OnEventAboutToTrigger(RuleRollD20 evt) {
        var resource = m_Resource?.Get();
        if (evt.IsFake || resource == null || evt.PreRollDice() != 1 ||
            !Owner.Resources.HasEnoughResource(resource, 1)) {
            return;
        }

        Owner.Resources.Spend(resource, 1);
        evt.AddReroll(1, takeBest: true, Fact);
    }

    public void OnEventDidTrigger(RuleRollD20 evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("eb641613-328f-4875-9bc5-fb8ab393a3ab")]
public sealed class LowBlowConfirmationBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public int Bonus = 1;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (evt.Target != null && evt.Target.Descriptor.State.Size > Owner.Descriptor.State.Size) {
            evt.CriticalConfirmationBonus += Bonus;
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f1cf2b50-c5b5-4862-8e7b-f285463b24b9")]
public sealed class CelestialCrusaderBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    ITargetRulebookHandler<RuleCalculateAC> {
    public BlueprintFeatureReference m_OutsiderType;
    public BlueprintFeatureReference m_EvilSubtype;
    public int Bonus = 1;

    private bool IsEvilOutsider(UnitEntityData unit) =>
        unit != null &&
        unit.Descriptor.HasFact(m_OutsiderType?.Get()) &&
        unit.Descriptor.HasFact(m_EvilSubtype?.Get());

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (IsEvilOutsider(evt.Target)) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Insight);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (IsEvilOutsider(evt.Initiator)) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Insight);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("305a3872-eb44-41d4-b4e4-078e9e10fe49")]
public sealed class FiendishSprinterChargeSpeed :
    UnitFactComponentDelegate<FiendishSprinterChargeSpeed.ComponentData>,
    IUnitCommandStartHandler,
    IUnitCommandEndHandler,
    IUnitCommandActHandler {
    public sealed class ComponentData {
        public bool Applied;
    }

    public int Bonus = 10;

    public void HandleUnitCommandDidStart(UnitCommand command) {
        if (command?.Executor != Owner || command is not UnitUseAbility useAbility ||
            useAbility.Ability?.Blueprint?.GetComponent<AbilityCustomCharge>() == null) {
            return;
        }

        Owner.Stats.Speed.RemoveModifiersFrom(Runtime);
        Owner.Stats.Speed.AddModifier(Bonus, Runtime, ModifierDescriptor.Racial);
        Data.Applied = true;
    }

    public void HandleUnitCommandDidAct(UnitCommand command) => Clear(command);

    public void HandleUnitCommandDidEnd(UnitCommand command) => Clear(command);

    public override void OnTurnOff() {
        Owner.Stats.Speed.RemoveModifiersFrom(Runtime);
        Data.Applied = false;
    }

    private void Clear(UnitCommand command) {
        if (!Data.Applied || command?.Executor != Owner) {
            return;
        }
        Owner.Stats.Speed.RemoveModifiersFrom(Runtime);
        Data.Applied = false;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ed5463b4-c360-4f8b-bf98-9e49b0251666")]
public sealed class StoneInTheBloodHealing :
    UnitFactComponentDelegate<StoneInTheBloodHealing.ComponentData>,
    ITargetRulebookHandler<RuleDealDamage>,
    IRestFinishedHandler {
    public sealed class ComponentData {
        public int HealedToday;
    }

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt) {
        if (evt.Target != Owner || evt.Result <= 0 ||
            !evt.DamageBundle.OfType<EnergyDamage>()
                .Any(damage => damage.EnergyType == Kingmaker.Enums.Damage.DamageEnergyType.Acid &&
                    !damage.Immune)) {
            return;
        }

        Rulebook.Trigger(new RuleHealDamage(Owner, Owner, 2));
    }

    public void HandleRestFinished(RestStatus status) => Data.HealedToday = 0;
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8ed84e9c-8695-4e27-abd3-d3c77ae52d93")]
public sealed class InsularSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintRaceReference m_ElfRace;
    public BlueprintRaceReference[] m_HumanoidRaces = Array.Empty<BlueprintRaceReference>();

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var descriptor = evt.Reason?.Ability?.SpellDescriptor ?? SpellDescriptor.None;
        if (evt.Type != SavingThrowType.Will ||
            (descriptor & (SpellDescriptor.Charm | SpellDescriptor.Compulsion | SpellDescriptor.Fear)) == 0) {
            return;
        }

        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        var sourceRace = source?.Descriptor?.Progression?.Race;
        if (sourceRace == null || sourceRace == m_ElfRace?.Get() ||
            !m_HumanoidRaces.Any(reference => reference?.Get() == sourceRace)) {
            return;
        }

        RacialTraitRuleHelpers.AddSaveModifier(evt, this, 2);
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b9400d4c-03d3-4e60-b3fe-5892dad71eb4")]
public sealed class TunnelFighterBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleInitiativeRoll>,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    private static bool IsUnderground =>
        AreaService.Instance?.CurrentAreaSetting == AreaSetting.Underground;

    public void OnEventAboutToTrigger(RuleInitiativeRoll evt) {
        if (IsUnderground) {
            evt.Modifier += 2;
        }
    }

    public void OnEventDidTrigger(RuleInitiativeRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (IsUnderground && evt.ParentRule?.AttackRoll?.IsCriticalConfirmed == true &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("62fbed3c-1300-4d0f-b438-e72e226f225a")]
public sealed class WarsmithDamageBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_ConstructType;
    public BlueprintFeatureReference m_ElementalSubtype;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var target = evt.Target;
        if (target == null || evt.DamageBundle.WeaponDamage == null) {
            return;
        }

        var blueprintName = target.Blueprint?.name ?? string.Empty;
        var eligible = target.Descriptor.HasFact(m_ConstructType?.Get()) ||
            target.Descriptor.HasFact(m_ElementalSubtype?.Get()) ||
            blueprintName.IndexOf("Gargoyle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            blueprintName.IndexOf("Golem", StringComparison.OrdinalIgnoreCase) >= 0 ||
            blueprintName.IndexOf("Stone", StringComparison.OrdinalIgnoreCase) >= 0 ||
            blueprintName.IndexOf("Crystal", StringComparison.OrdinalIgnoreCase) >= 0;
        if (eligible) {
            evt.DamageBundle.WeaponDamage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("bd829313-4e1f-4a7d-83e8-54532d2f52c5")]
public sealed class AdrenalineRushTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintAbilityResourceReference m_Resource;
    public BlueprintBuffReference m_Buff;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) { }

    public void OnEventDidTrigger(RuleSavingThrow evt) {
        var resource = m_Resource?.Get();
        var buff = m_Buff?.Get();
        if (evt.IsPassed || resource == null || buff == null ||
            (evt.Reason?.Ability?.SpellDescriptor & SpellDescriptor.Emotion) == 0 ||
            !Owner.Resources.HasEnoughResource(resource, 1)) {
            return;
        }

        Owner.Resources.Spend(resource, 1);
        Owner.Buffs.AddBuff(buff, Owner, CombatChecks.Rounds(10));
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("888681df-fe8e-43cc-beb2-ce7183a5fb13")]
public sealed class AnimalFriendSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintFeatureReference m_AnimalType;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (evt.Type != SavingThrowType.Will || !Game.HasInstance) {
            return;
        }

        var animalType = m_AnimalType?.Get();
        var hasNearbyAnimal = animalType != null && Game.Instance.State.AwakeUnits.Any(unit =>
            unit != null && unit != Owner && !unit.IsEnemy(Owner) &&
            unit.Descriptor.HasFact(animalType) && unit.DistanceTo(Owner) <= 30.Feet().Meters);
        if (hasNearbyAnimal) {
            RacialTraitRuleHelpers.AddSaveModifier(evt, this, 1);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("796272ba-17cb-4bae-97b3-300a4a7d758d")]
public sealed class ExperimentalRebelSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintRaceReference m_ElfRace;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (source?.Descriptor?.Progression?.Race == m_ElfRace?.Get()) {
            RacialTraitRuleHelpers.AddSaveModifier(evt, this, 2);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("2ccee777-736a-4c78-b45d-6983678cbc76")]
public sealed class CruelRagerTrigger : UnitFactComponentDelegate<CruelRagerTrigger.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IUnitGainFactHandler {
    public sealed class ComponentData {
        public bool UsedThisRage;
    }

    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();
    public BlueprintAbilityResourceReference[] m_RageResources =
        Array.Empty<BlueprintAbilityResourceReference>();

    public void HandleUnitGainFact(EntityFact fact) {
        if (fact?.Blueprint != null &&
            m_RageBuffs.Any(reference => reference?.Get() == fact.Blueprint)) {
            Data.UsedThisRage = false;
        }
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (Data.UsedThisRage || !evt.IsCriticalConfirmed ||
            !m_RageBuffs.Any(reference => reference?.Get() is BlueprintBuff buff && Owner.HasFact(buff))) {
            return;
        }

        foreach (var reference in m_RageResources) {
            var resource = reference?.Get();
            if (resource == null || Owner.Resources.GetResource(resource) == null) {
                continue;
            }

            Owner.Resources.Restore(resource, 1);
            Data.UsedThisRage = true;
            return;
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("c8cca3f6-b1d0-415f-8f5a-db0e6ac67a70")]
public sealed class FinishTheFightTracker : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public BlueprintBuffReference m_Marker;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt) {
        var marker = m_Marker?.Get();
        if (marker == null || evt.Result <= 0 || evt.Target == null || evt.Target == Owner) {
            return;
        }

        var existingMarkers = new List<Kingmaker.UnitLogic.Buffs.Buff>();
        foreach (var buff in evt.Target.Buffs) {
            if (buff.Blueprint == marker && buff.MaybeContext?.MaybeCaster == Owner) {
                existingMarkers.Add(buff);
            }
        }
        foreach (var existing in existingMarkers) {
            existing.Remove();
        }
        evt.Target.Buffs.AddBuff(marker, Owner, TimeSpan.FromHours(24));
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        var marker = m_Marker?.Get();
        if (marker == null || evt.Target == null) {
            return;
        }
        foreach (var buff in evt.Target.Buffs) {
            if (buff.Blueprint == marker && buff.MaybeContext?.MaybeCaster == Owner) {
                evt.AddModifier(1, Fact, ModifierDescriptor.Trait);
                return;
            }
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("5146cc04-080e-4856-9862-d478ea8901be")]
public sealed class BruteThreatDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage> {
    public void OnEventAboutToTrigger(RuleDealDamage evt) {
        var attack = evt.AttackRoll;
        if (attack?.IsCriticalRoll == true && attack.WeaponStats != null) {
            evt.AddModifier(attack.WeaponStats.CriticalMultiplier, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleDealDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b74d9ca2-e77a-4479-902e-5136df73b452")]
public sealed class MartyrsBloodAttackBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (Owner.HPLeft * 2 > Owner.MaxHP || evt.Target == null) {
            return;
        }

        var alignment = evt.Target.Descriptor.Alignment.ValueVisible;
        if (alignment == Alignment.LawfulEvil || alignment == Alignment.NeutralEvil ||
            alignment == Alignment.ChaoticEvil) {
            evt.AddModifier(1, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("de76bae8-c325-4a83-a24b-bd4bdf6d71c2")]
public sealed class BowCriticalConfirmationBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        var category = evt.Weapon?.Blueprint?.Category;
        if (category == WeaponCategory.Longbow || category == WeaponCategory.Shortbow) {
            evt.CriticalConfirmationBonus += 2;
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("66c388e1-0f38-42e2-9745-1a594b244e7d")]
public sealed class HardToPinDownArmorClass : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        var attack = Rulebook.CurrentContext?.PreviousEvent as RuleAttackRoll;
        if (attack?.RuleAttackWithWeapon?.IsAttackOfOpportunity != true || evt.Initiator == null) {
            return;
        }

        if (evt.Initiator.CombatState?.IsFlanked == true ||
            CombatChecks.IsInvisibleTo(Owner, evt.Initiator)) {
            evt.AddModifier(2, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ed9164d1-53a2-47d6-ab2f-fbbe375eab02")]
public sealed class ShadowStabberDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var attack = evt.ParentRule?.AttackRoll;
        if (attack?.Weapon?.Blueprint?.IsMelee == true && attack.Target != null &&
            CombatChecks.IsInvisibleTo(Owner, attack.Target) &&
            evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(2, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("e1e2f36e-e4d3-49f8-b54a-acac9a6fefa3")]
public sealed class EverWaryArmorClass : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (!evt.IsTargetFlatFooted || Owner.CombatState?.HadTurnInTMBCombat == true) {
            return;
        }

        var bonus = Math.Max(0, Owner.Stats.Dexterity.Bonus / 2);
        if (bonus > 0) {
            evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("3052419f-3acc-4f2d-bcd2-26c3020333ee")]
public sealed class StoicDignitySaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public bool IsSelf;

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if ((evt.Reason?.Ability?.SpellDescriptor & SpellDescriptor.MindAffecting) == 0) {
            return;
        }

        var source = IsSelf ? Owner : Fact.MaybeContext?.MaybeCaster;
        if (source == null || source.State.HasCondition(UnitCondition.Unconscious) ||
            source.State.IsDead) {
            return;
        }

        RacialTraitRuleHelpers.AddSaveModifier(
            evt,
            this,
            1,
            IsSelf ? ModifierDescriptor.Trait : ModifierDescriptor.Morale);
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("aa150e38-27d2-488a-998f-fc77a1edc5c1")]
public sealed class UndeadSlayerBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintFeatureReference m_UndeadType;

    private bool IsUndead(UnitEntityData target) =>
        target != null && target.Descriptor.HasFact(m_UndeadType?.Get());

    public void OnEventAboutToTrigger(RuleAttackRoll evt) {
        if (IsUndead(evt.Target)) {
            evt.AddModifier(1, Fact, ModifierDescriptor.Trait);
        }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (IsUndead(evt.Target) && evt.DamageBundle.WeaponDamage != null) {
            evt.DamageBundle.WeaponDamage.AddModifier(1, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}
