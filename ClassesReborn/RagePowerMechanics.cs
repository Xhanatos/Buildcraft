using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace ClassesReborn;

internal static class RagePowerRuntime {
    internal static bool IsRaging(
        UnitEntityData unit,
        IEnumerable<BlueprintBuffReference> rageBuffs) =>
        unit != null && rageBuffs.Any(reference =>
            reference?.Get() is BlueprintBuff buff && unit.HasFact(buff));

    internal static bool IsRageFact(
        EntityFact fact,
        IEnumerable<BlueprintBuffReference> rageBuffs) =>
        fact?.Blueprint != null && rageBuffs.Any(reference =>
            reference?.Get() == fact.Blueprint);

    internal static int EffectiveLevel(UnitEntityData owner, EntityFact sourceFact = null) {
        var source = owner;
        var ownLevel = RageClassLevel(owner);
        var caster = sourceFact?.MaybeContext?.MaybeCaster;
        var casterLevel = RageClassLevel(caster);
        if (casterLevel > ownLevel) {
            source = caster;
            ownLevel = casterLevel;
        }
        return Math.Max(1, ownLevel > 0
            ? ownLevel
            : source?.Descriptor?.Progression?.CharacterLevel ?? 1);
    }

    internal static bool IsEnemyMagic(RuleSavingThrow savingThrow, UnitEntityData owner) {
        var ability = savingThrow?.Reason?.Ability;
        var source = savingThrow?.Reason?.Caster ?? savingThrow?.Reason?.SourceUnit;
        if (ability == null || source == null || !source.IsEnemy(owner)) {
            return false;
        }

        return ability.Blueprint.Type == AbilityType.Spell ||
               ability.Blueprint.Type == AbilityType.SpellLike ||
               ability.Blueprint.Type == AbilityType.Supernatural;
    }

    internal static bool HasSpellsOrSpellLikeAbilities(UnitEntityData unit) {
        if (unit == null) {
            return false;
        }

        var classSpellcasting = unit.Descriptor.Progression.Classes.Any(classData =>
            classData.Level > 0 && classData.CharacterClass?.Spellbook != null);
        if (classSpellcasting) {
            return true;
        }

        return unit.Descriptor.Facts.List.Any(fact =>
            fact?.Blueprint is BlueprintAbility ability &&
            (ability.Type == AbilityType.Spell ||
             ability.Type == AbilityType.SpellLike));
    }

    internal static UnitEntityData RagingMaster(EntityFact petFact, UnitEntityData petUnit) {
        var master = petFact?.MaybeContext?.MaybeCaster;
        if (petUnit == null || master == null ||
            petUnit.DistanceTo(master) > 10.Feet().Meters) {
            return null;
        }
        return master;
    }

    private static int RageClassLevel(UnitEntityData unit) {
        if (unit == null) {
            return 0;
        }

        var result = 0;
        foreach (var classId in new[] {
                     BlueprintIds.BarbarianClass,
                     BlueprintIds.SkaldClass,
                     BlueprintIds.BloodragerClass,
                     BlueprintIds.ShifterClass,
                 }) {
            var characterClass = BlueprintTool.Get<BlueprintCharacterClass>(classId);
            result = Math.Max(result, unit.Progression.GetClassLevel(characterClass));
        }
        return result;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("ef3810a0-7240-4f11-987a-4e20e6cb9ec2")]
public sealed class SuperstitionEnemyMagicSaveBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (!RagePowerRuntime.IsRaging(Owner, m_RageBuffs) ||
            !RagePowerRuntime.IsEnemyMagic(evt, Owner)) {
            return;
        }

        var bonus = 2 + RagePowerRuntime.EffectiveLevel(Owner, Fact) / 4;
        var stat = evt.Type switch {
            SavingThrowType.Fortitude => Owner.Stats.SaveFortitude,
            SavingThrowType.Reflex => Owner.Stats.SaveReflex,
            SavingThrowType.Will => Owner.Stats.SaveWill,
            _ => null,
        };
        if (stat != null) {
            evt.AddTemporaryModifier(stat.AddModifier(
                bonus,
                Runtime,
                ModifierDescriptor.Morale));
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("8104e5f2-b7c9-4d4b-833a-775c2754b8c6")]
public sealed class WitchHunterRageDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateDamage> {
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        if (!RagePowerRuntime.IsRaging(Owner, m_RageBuffs) ||
            !RagePowerRuntime.HasSpellsOrSpellLikeAbilities(evt.Target) ||
            evt.DamageBundle?.WeaponDamage == null) {
            return;
        }

        var level = RagePowerRuntime.EffectiveLevel(Owner, Fact);
        evt.DamageBundle.WeaponDamage.AddModifier(1 + level / 4, Fact);
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("65a22731-2278-434f-ab50-1bbc3a069746")]
public sealed class EaterOfMagicReroll :
    UnitFactComponentDelegate<EaterOfMagicReroll.ComponentData>,
    IInitiatorRulebookHandler<RuleRollD20>,
    IInitiatorRulebookHandler<RuleSavingThrow>,
    IUnitGainFactHandler {
    public sealed class ComponentData {
        public bool UsedThisRage;
        public bool TriggeredThisSave;
    }

    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();
    public BlueprintBuffReference m_TemporaryHitPointsBuff;

    public void HandleUnitGainFact(EntityFact fact) {
        if (RagePowerRuntime.IsRageFact(fact, m_RageBuffs)) {
            Data.UsedThisRage = false;
            Data.TriggeredThisSave = false;
        }
    }

    public void OnEventAboutToTrigger(RuleRollD20 evt) {
        if (evt.IsFake || Data.UsedThisRage ||
            !RagePowerRuntime.IsRaging(Owner, m_RageBuffs) ||
            Rulebook.CurrentContext?.PreviousEvent is not RuleSavingThrow savingThrow ||
            savingThrow.Initiator != Owner ||
            !RagePowerRuntime.IsEnemyMagic(savingThrow, Owner)) {
            return;
        }

        var originalRoll = evt.PreRollDice();
        if (((ISuccessable)savingThrow).IsSuccessRoll(originalRoll)) {
            return;
        }

        evt.AddReroll(1, takeBest: true, Fact);
        Data.UsedThisRage = true;
        Data.TriggeredThisSave = true;
    }

    public void OnEventDidTrigger(RuleRollD20 evt) { }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        Data.TriggeredThisSave = false;
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) {
        if (!Data.TriggeredThisSave) {
            return;
        }

        Data.TriggeredThisSave = false;
        if (!evt.IsPassed || !RagePowerRuntime.IsEnemyMagic(evt, Owner)) {
            return;
        }

        var buff = m_TemporaryHitPointsBuff?.Get();
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (buff == null || source == null) {
            return;
        }

        Owner.Buffs.GetBuff(buff)?.Remove();
        var duration = TimeSpan.FromMinutes(1);
        if (evt.Reason.Context != null) {
            Owner.Buffs.AddBuff(buff, evt.Reason.Context, duration);
            return;
        }

        Owner.Buffs.AddBuff(
            buff,
            source,
            duration,
            new AbilityParams {
                CasterLevel = Math.Max(
                    1,
                    source.Descriptor.Progression.CharacterLevel),
            });
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("0a8e0657-671e-4d6e-884c-cf8a906c16ba")]
public sealed class StrengthSurgeResourceController : UnitFactComponentDelegate,
    IUnitGainFactHandler,
    IUnitLostFactHandler {
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();
    public BlueprintAbilityResourceReference m_Resource;
    public BlueprintBuffReference m_SurgeBuff;

    public override void OnTurnOn() {
        base.OnTurnOn();
        SetUses();
    }

    public void HandleUnitGainFact(EntityFact fact) {
        if (RagePowerRuntime.IsRageFact(fact, m_RageBuffs)) {
            SetUses();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (!RagePowerRuntime.IsRageFact(fact, m_RageBuffs) ||
            RagePowerRuntime.IsRaging(Owner, m_RageBuffs)) {
            return;
        }

        var surgeBuff = m_SurgeBuff?.Get();
        if (surgeBuff != null) {
            Owner.Buffs.GetBuff(surgeBuff)?.Remove();
        }
    }

    private void SetUses() {
        var resource = m_Resource?.Get();
        var available = resource == null ? null : Owner.Resources.GetResource(resource);
        if (resource == null || available == null) {
            return;
        }

        var level = RagePowerRuntime.EffectiveLevel(Owner, Fact);
        var uses = level >= 16 ? 3 : level >= 12 ? 2 : 1;
        available.Amount = uses;
    }
}

[AllowedOn(typeof(BlueprintAbility))]
[TypeId("5740bb3f-e89a-4ba1-a122-6fb3674c0580")]
public sealed class StrengthSurgeRestriction : BlueprintComponent, IAbilityCasterRestriction {
    private static readonly Kingmaker.Localization.LocalizedString RestrictionText = new() {
        Key = "ClassesReborn.RagePower.StrengthSurge.Restriction",
    };

    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();
    public BlueprintBuffReference m_SurgeBuff;

    public bool IsCasterRestrictionPassed(UnitEntityData caster) =>
        RagePowerRuntime.IsRaging(caster, m_RageBuffs) &&
        (m_SurgeBuff?.Get() is not BlueprintBuff buff || !caster.HasFact(buff));

    public string GetAbilityCasterRestrictionUIText() => RestrictionText;
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("43e038fb-6e05-43f7-bf02-4fd03f361ce1")]
public sealed class StrengthSurgeNextManeuver : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateCMB>,
    ITargetRulebookHandler<RuleCalculateCMD> {
    public void OnEventAboutToTrigger(RuleCalculateCMB evt) {
        evt.AddModifier(
            RagePowerRuntime.EffectiveLevel(Owner, Fact),
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateCMB evt) =>
        (Fact as Buff)?.Remove();

    public void OnEventAboutToTrigger(RuleCalculateCMD evt) {
        evt.AddModifier(
            RagePowerRuntime.EffectiveLevel(Owner, Fact),
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateCMD evt) =>
        (Fact as Buff)?.Remove();
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("3f251494-b1de-4275-b911-748a7996e4d4")]
public sealed class ElementalRageDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public DamageEnergyType EnergyType;
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon != null &&
            RagePowerRuntime.IsRaging(Owner, m_RageBuffs)) {
            evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), EnergyType));
        }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("5b6d30b7-e212-4200-ae93-2bed7fe3df5f")]
public sealed class GreaterElementalRageCriticalDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public DamageEnergyType EnergyType;
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var attack = evt.ParentRule?.AttackRoll;
        if (evt.DamageBundle?.Weapon == null || attack?.IsCriticalConfirmed != true ||
            !RagePowerRuntime.IsRaging(Owner, m_RageBuffs)) {
            return;
        }

        var dice = Math.Max(1, attack.WeaponStats?.CriticalMultiplier - 1 ?? 1);
        evt.Add(new EnergyDamage(new DiceFormula(dice, DiceType.D6), EnergyType));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("a65bbf15-229b-4c2d-9ac6-4b290ca45f72")]
public sealed class GhostRagerTouchDefense : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        var attackType = (Rulebook.CurrentContext?.PreviousEvent as RuleAttackRoll)?.AttackType;
        if (attackType != AttackType.Touch && attackType != AttackType.RangedTouch) {
            return;
        }

        evt.AddModifier(
            2 + RagePowerRuntime.EffectiveLevel(Owner, Fact) / 4,
            Fact,
            ModifierDescriptor.Morale);
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("947d558a-492a-4fed-9d26-ea4a4d01c767")]
public sealed class FerociousMountPetRageBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    private UnitEntityData RagingMaster() {
        var master = RagePowerRuntime.RagingMaster(Fact, Owner);
        return master != null && RagePowerRuntime.IsRaging(master, RagePowerRebalanceRageBuffs.All)
            ? master
            : null;
    }

    private int Bonus(UnitEntityData master) {
        var level = RagePowerRuntime.EffectiveLevel(master);
        return level >= 20 ? 4 : level >= 11 ? 3 : 2;
    }

    public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) {
        var master = RagingMaster();
        if (master != null && evt.Weapon?.Blueprint?.IsMelee == true) {
            evt.AddModifier(Bonus(master), Fact, ModifierDescriptor.Morale);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        var master = RagingMaster();
        if (master != null && evt.Weapon?.Blueprint?.IsMelee == true) {
            evt.AddDamageModifier(Bonus(master), Fact, ModifierDescriptor.Morale);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var master = RagingMaster();
        if (master == null || evt.Type != SavingThrowType.Will) {
            return;
        }
        evt.AddTemporaryModifier(Owner.Stats.SaveWill.AddModifier(
            Bonus(master), Runtime, ModifierDescriptor.Morale));
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6e2a4f6b-d90a-494f-bb17-6a47421a6be9")]
public sealed class GreaterFerociousMountRagePowerSharing :
    UnitFactComponentDelegate<GreaterFerociousMountRagePowerSharing.ComponentData>,
    IUnitGainFactHandler,
    IUnitLostFactHandler,
    IOwnerGainLevelHandler {
    public BlueprintBuffReference[] m_RageBuffs = Array.Empty<BlueprintBuffReference>();
    public BlueprintFeatureReference[] m_RagePowerFeatures =
        Array.Empty<BlueprintFeatureReference>();

    public override void OnActivate() {
        base.OnActivate();
        Sync();
    }

    public override void OnDeactivate() {
        Clear();
        base.OnDeactivate();
    }

    public void HandleUnitGainFact(EntityFact fact) {
        if (RagePowerRuntime.IsRageFact(fact, m_RageBuffs) ||
            IsRagePower(fact?.Blueprint as BlueprintUnitFact)) {
            Sync();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (RagePowerRuntime.IsRageFact(fact, m_RageBuffs) ||
            IsRagePower(fact?.Blueprint as BlueprintUnitFact)) {
            Sync();
        }
    }

    public void HandleUnitGainLevel() => Sync();

    private void Sync() {
        Clear();

        Buff rage = null;
        foreach (var buff in Owner.Buffs) {
            if (RagePowerRuntime.IsRageFact(buff, m_RageBuffs)) {
                rage = buff;
                break;
            }
        }
        var pet = Owner.GetPet(PetType.AnimalCompanion);
        if (rage?.Blueprint is not BlueprintBuff rageBlueprint || pet == null) {
            return;
        }

        Data.Pet = pet;
        Data.AppliedFeatures ??= new List<EntityFact>();
        foreach (var reference in m_RagePowerFeatures) {
            if (reference?.Get() is not BlueprintFeature feature ||
                !Owner.HasFact(feature) ||
                pet.Descriptor.Progression.Features.HasFact(feature)) {
                continue;
            }

            var applied = pet.Descriptor.Progression.Features.AddFact(feature, Context);
            if (applied != null) {
                Data.AppliedFeatures.Add(applied);
            }
        }

        if (pet.Buffs.GetBuff(rageBlueprint) == null) {
            Data.AppliedRage = pet.Buffs.AddBuff(
                rageBlueprint,
                rage.MaybeContext,
                duration: null);
        }
    }

    private void Clear() {
        Data.AppliedRage?.Remove();
        Data.AppliedRage = null;

        if (Data.Pet != null && Data.AppliedFeatures != null) {
            foreach (var feature in Data.AppliedFeatures.Where(feature => feature != null)) {
                Data.Pet.Descriptor.Progression.Features.RemoveFact(feature);
            }
        }
        Data.AppliedFeatures?.Clear();
        Data.Pet = null;
    }

    private bool IsRagePower(BlueprintUnitFact fact) =>
        fact != null && m_RagePowerFeatures.Any(reference => reference?.Get() == fact);

    public sealed class ComponentData {
        [JsonProperty]
        public UnitEntityData Pet;

        [JsonProperty]
        public Buff AppliedRage;

        [JsonProperty]
        public List<EntityFact> AppliedFeatures = new();
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("767e7c27-a933-4528-9a46-8133fe9aa72b")]
public sealed class GreaterFerociousMountPetPowerSharing : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookHandler<RuleCalculateDamage>,
    IInitiatorRulebookHandler<RulePrepareDamage>,
    ITargetRulebookHandler<RuleCalculateAC> {
    public BlueprintUnitFactReference m_Superstition;
    public BlueprintUnitFactReference m_WitchHunter;
    public BlueprintUnitFactReference m_GhostRager;
    public BlueprintUnitFactReference[] m_ElementalRageFeatures =
        Array.Empty<BlueprintUnitFactReference>();
    public BlueprintUnitFactReference[] m_GreaterElementalRageFeatures =
        Array.Empty<BlueprintUnitFactReference>();

    private UnitEntityData Master() {
        var master = RagePowerRuntime.RagingMaster(Fact, Owner);
        return master != null && RagePowerRuntime.IsRaging(master, RagePowerRebalanceRageBuffs.All)
            ? master
            : null;
    }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var master = Master();
        if (master == null || !Has(master, m_Superstition) ||
            !RagePowerRuntime.IsEnemyMagic(evt, Owner)) {
            return;
        }
        var bonus = 2 + RagePowerRuntime.EffectiveLevel(master) / 4;
        var stat = evt.Type switch {
            SavingThrowType.Fortitude => Owner.Stats.SaveFortitude,
            SavingThrowType.Reflex => Owner.Stats.SaveReflex,
            SavingThrowType.Will => Owner.Stats.SaveWill,
            _ => null,
        };
        if (stat != null) {
            evt.AddTemporaryModifier(stat.AddModifier(
                bonus, Runtime, ModifierDescriptor.Morale));
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var master = Master();
        if (master == null || !Has(master, m_WitchHunter) ||
            !RagePowerRuntime.HasSpellsOrSpellLikeAbilities(evt.Target) ||
            evt.DamageBundle?.WeaponDamage == null) {
            return;
        }
        var level = RagePowerRuntime.EffectiveLevel(master);
        evt.DamageBundle.WeaponDamage.AddModifier(
            1 + level / 4,
            Fact);
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var master = Master();
        if (master == null || evt.DamageBundle?.Weapon == null) {
            return;
        }

        var energies = new[] {
            DamageEnergyType.Acid,
            DamageEnergyType.Cold,
            DamageEnergyType.Electricity,
            DamageEnergyType.Fire,
        };
        for (var index = 0; index < energies.Length; index++) {
            if (!Has(master, m_ElementalRageFeatures.ElementAtOrDefault(index))) {
                continue;
            }
            evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), energies[index]));
            if (evt.ParentRule?.AttackRoll?.IsCriticalConfirmed == true &&
                Has(master, m_GreaterElementalRageFeatures.ElementAtOrDefault(index))) {
                var multiplier = evt.ParentRule.AttackRoll.WeaponStats?.CriticalMultiplier ?? 2;
                evt.Add(new EnergyDamage(
                    new DiceFormula(Math.Max(1, multiplier - 1), DiceType.D6),
                    energies[index]));
            }
        }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }

    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        var master = Master();
        if (master != null && Has(master, m_GhostRager) &&
            (Rulebook.CurrentContext?.PreviousEvent as RuleAttackRoll)?.AttackType is
                AttackType.Touch or AttackType.RangedTouch) {
            evt.AddModifier(
                2 + RagePowerRuntime.EffectiveLevel(master) / 4,
                Fact,
                ModifierDescriptor.Morale);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }

    private static bool Has(UnitEntityData unit, BlueprintUnitFactReference reference) =>
        reference?.Get() is BlueprintUnitFact fact && unit.HasFact(fact);
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("08e77b1f-f35f-4898-9c29-98be0c82b2dd")]
public sealed class SpiritSteedDamageReduction : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateDamage> {
    public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
        var master = RagePowerRuntime.RagingMaster(Fact, Owner);
        if (master == null || !RagePowerRuntime.IsRaging(master, RagePowerRebalanceRageBuffs.All)) {
            return;
        }

        var reduction = Math.Max(1, RagePowerRuntime.EffectiveLevel(master) / 2);
        foreach (var damage in evt.DamageBundle.OfType<PhysicalDamage>()
                     .Where(damage => damage.Enchantment <= 0)) {
            damage.AddModifier(-reduction, Fact);
        }
    }

    public void OnEventDidTrigger(RuleCalculateDamage evt) { }
}

internal static class RagePowerRebalanceRageBuffs {
    internal static readonly BlueprintBuffReference[] All = {
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.StandardRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.FocusedRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.BloodragerStandardRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.BloodragerGreaterRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.BloodragerMightyRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.FleshEaterUnboundRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.ArmyStandardRageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.ReformedFiendBloodrageBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.RageshaperDevastatingFormBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.RageshaperGreaterDevastatingFormBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.RageshaperMightyDevastatingFormBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.InspiredRageEffectBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.InspiredRageBeforeMasterSkaldBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.InspiredRageMythicBuff),
        BlueprintTool.GetRef<BlueprintBuffReference>(BlueprintIds.InspiredRageNoCasterBuff),
    };
}
