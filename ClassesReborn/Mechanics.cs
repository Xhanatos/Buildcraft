using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;

namespace ClassesReborn;

internal static class CombatChecks {
    internal static bool IsRangedWeapon(Kingmaker.Items.ItemEntityWeapon weapon) =>
        weapon?.Blueprint?.IsRanged == true;

    internal static bool WasKilledBy(UnitEntityData target, UnitEntityData owner) {
        if (target == null || owner == null || target == owner || !target.IsEnemy(owner)) {
            return false;
        }

        return target.LastHandledDamage?.Initiator == owner;
    }

    internal static bool IsInvisibleTo(UnitEntityData unit, UnitEntityData observer) {
        if (unit == null || observer == null || !unit.IsEnemy(observer)) {
            return false;
        }

        var hasInvisibility = unit.Buffs
            .GetFactsContainingComponent<BuffInvisibility>()
            .Any();
        if (!hasInvisibility) {
            return false;
        }

        var concealment = unit.Get<UnitPartConcealment>();
        return concealment?.IsConcealedFor(observer) ?? true;
    }

    internal static bool IsFavoredEnemy(
        UnitEntityData hunter,
        UnitEntityData target) {
        if (hunter == null || target == null || hunter == target) {
            return false;
        }

        var favoredEnemies = hunter.Get<UnitPartFavoredEnemy>();
        return favoredEnemies?.Entries.Any(entry =>
            entry.CheckedFeatures?.Any(feature =>
                feature != null && target.Descriptor.HasFact(feature)) == true) == true;
    }

    internal static TimeSpan Rounds(int rounds) => TimeSpan.FromSeconds(rounds * 6.0);
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("96c18eaa-7155-4d03-9047-1b028e0c0ac9")]
public sealed class ShillelaghWeaponBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        var category = evt.Weapon?.Blueprint?.Category;
        if (category != WeaponCategory.Club &&
            category != WeaponCategory.Quarterstaff) {
            return;
        }

        evt.Enhancement.AddModifier(new Modifier(
            1,
            Fact,
            ModifierDescriptor.Enhancement));
        evt.EnhancementTotal += 1;
        evt.IncreaseWeaponSize(2);
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("676ee021-2b34-469f-b703-d87ba612d8fe")]
public sealed class WailingProjectilesTrigger : UnitFactComponentDelegate, IUnitFinallyDeadHandler {
    public BlueprintBuffReference m_D8Buff;
    public BlueprintBuffReference m_D12Buff;
    public BlueprintBuffReference m_2D8Buff;
    public BlueprintFeatureReference m_Level10Feature;
    public BlueprintFeatureReference m_Level15Feature;

    public void HandleUnitBecameFinallyDead(UnitEntityData unit) {
        if (!CombatChecks.WasKilledBy(unit, Owner)) {
            return;
        }

        var damage = unit.LastHandledDamage;
        var weapon = damage?.DamageBundle?.Weapon ?? damage?.AttackRoll?.Weapon;
        if (!CombatChecks.IsRangedWeapon(weapon)) {
            return;
        }

        var selectedBuff = m_D8Buff?.Get();
        var level15Feature = m_Level15Feature?.Get();
        var level10Feature = m_Level10Feature?.Get();
        if (level15Feature != null && Owner.Descriptor.HasFact(level15Feature)) {
            selectedBuff = m_2D8Buff?.Get();
        } else if (level10Feature != null && Owner.Descriptor.HasFact(level10Feature)) {
            selectedBuff = m_D12Buff?.Get();
        }

        RemoveBuff(m_D8Buff);
        RemoveBuff(m_D12Buff);
        RemoveBuff(m_2D8Buff);

        if (selectedBuff != null) {
            Owner.Buffs.AddBuff(selectedBuff, Owner, CombatChecks.Rounds(3));
        }
    }

    private void RemoveBuff(BlueprintBuffReference buffReference) {
        var blueprint = buffReference?.Get();
        if (blueprint == null) {
            return;
        }

        Owner.Buffs.GetBuff(blueprint)?.Remove();
    }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("fd77aa0e-7834-4f5f-b0e4-c6577966a3c6")]
public sealed class RangedSonicDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public DiceFormula Dice;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var weapon = evt.DamageBundle?.Weapon;
        if (!CombatChecks.IsRangedWeapon(weapon)) {
            return;
        }

        evt.Add(new EnergyDamage(Dice, DamageEnergyType.Sonic));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("51e2e7a8-79e7-4fc7-8f6c-3b8a079d5d36")]
public sealed class PerfectSelfForceDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon?.Blueprint?.IsUnarmed != true) {
            return;
        }

        evt.Add(new ForceDamage(
            new ModifiableDiceFormula(new DiceFormula(1, DiceType.D10)),
            0));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("1a505694-78f2-4caa-99c9-7af613fdbc1a")]
public sealed class StrengthOfStoneUnarmedDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon?.Blueprint?.IsUnarmed != true) {
            return;
        }

        evt.Add(new PhysicalDamage(
            new ModifiableDiceFormula(new DiceFormula(1, DiceType.D6)),
            0,
            PhysicalDamageForm.Bludgeoning));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("af6594e0-7303-44e4-8aa8-fe0f7071bc0e")]
public sealed class FlameDanceFireDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public BlueprintFeatureReference m_FlameDanceFeature;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon == null) {
            return;
        }

        var performer = Fact.MaybeContext?.MaybeCaster;
        var feature = m_FlameDanceFeature?.Get();
        var rank = performer != null && feature != null
            ? performer.GetFact(feature)?.GetRank() ?? 0
            : 0;
        var dice = rank switch {
            >= 3 => new DiceFormula(1, DiceType.D10),
            2 => new DiceFormula(1, DiceType.D8),
            1 => new DiceFormula(1, DiceType.D4),
            _ => default,
        };
        if (rank <= 0) {
            return;
        }

        evt.Add(new EnergyDamage(dice, DamageEnergyType.Fire));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b60eaeef-b499-4d91-b67e-1c4adff62a33")]
public sealed class ElementalManifestationDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage> {
    public DamageEnergyType EnergyType;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        if (evt.DamageBundle?.Weapon == null) {
            return;
        }

        evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), EnergyType));
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("fcf6b773-b1c6-41d7-a51a-8ca9a480ccee")]
public sealed class ApplyBuffWhileAnyFactActive : UnitFactComponentDelegate,
    IUnitGainFactHandler,
    IUnitLostFactHandler {
    public BlueprintBuffReference m_BonusBuff;
    public BlueprintUnitFactReference[] m_RequiredFacts =
        Array.Empty<BlueprintUnitFactReference>();

    public override void OnTurnOn() {
        base.OnTurnOn();
        RefreshBonus();
    }

    public override void OnTurnOff() {
        RemoveBonus();
        base.OnTurnOff();
    }

    public void HandleUnitGainFact(EntityFact fact) {
        if (IsRequiredFact(fact)) {
            ApplyBonus();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (IsRequiredFact(fact) && !HasRequiredFact(fact)) {
            RemoveBonus();
        }
    }

    private bool IsRequiredFact(EntityFact fact) =>
        fact?.Blueprint != null &&
        m_RequiredFacts.Any(reference => reference?.Get() == fact.Blueprint);

    private bool HasRequiredFact(EntityFact excluded = null) =>
        m_RequiredFacts.Any(reference => {
            var requiredFact = reference?.Get();
            var activeFact = requiredFact == null ? null : Owner.GetFact(requiredFact);
            return activeFact != null && activeFact != excluded;
        });

    private void RefreshBonus() {
        if (HasRequiredFact()) {
            ApplyBonus();
        } else {
            RemoveBonus();
        }
    }

    private void ApplyBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null && Owner.Buffs.GetBuff(buff) == null) {
            Owner.Buffs.AddBuff(buff, Owner, null);
        }
    }

    private void RemoveBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null) {
            Owner.Buffs.GetBuff(buff)?.Remove();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6bd6bdc0-6519-466e-aef9-5dce8994f794")]
public sealed class HagRivenClawCriticalRange : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public BlueprintItemWeaponReference[] m_Claws =
        Array.Empty<BlueprintItemWeaponReference>();

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) {
        var weapon = evt.Weapon?.Blueprint;
        if (weapon == null || !m_Claws.Any(reference => reference?.Get() == weapon)) {
            return;
        }

        const int requestedCriticalRange = 4;
        var missingRange = requestedCriticalRange - evt.CriticalRange;
        if (missingRange > 0) {
            evt.CriticalEdgeBonus += missingRange;
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("c828397c-21ca-43c5-a0f0-b212d12ddf4e")]
public sealed class HagboundClawMasteryCriticalRange : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.Blueprint?.Category == WeaponCategory.Claw) {
            evt.CriticalEdgeBonus += Fact.GetRank();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("b6d9313d-fad8-4486-ba4e-fb7cb58446a4")]
public sealed class BloodInTheEyesTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll> {
    public BlueprintBuffReference m_Debuff;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (evt.IsFake || !evt.IsHit || evt.Target == null || !evt.Target.IsEnemy(Owner) ||
            !CombatChecks.IsRangedWeapon(evt.Weapon)) {
            return;
        }

        var debuff = m_Debuff?.Get();
        if (debuff != null) {
            evt.Target.Buffs.AddBuff(debuff, Owner, CombatChecks.Rounds(2));
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("1e1532e6-d09b-4ba9-b7a8-40e0806a296c")]
public sealed class FeastOnTheirScreamsTrigger : UnitFactComponentDelegate, IUnitFinallyDeadHandler {
    public BlueprintBuffReference m_Buff;

    public void HandleUnitBecameFinallyDead(UnitEntityData unit) {
        if (!CombatChecks.WasKilledBy(unit, Owner)) {
            return;
        }

        var buff = m_Buff?.Get();
        if (buff != null) {
            Owner.Buffs.AddBuff(buff, Owner, CombatChecks.Rounds(5));
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("f516af5e-4b85-41a1-8fb8-ed9bdf1dc7f3")]
public sealed class BattlebornRageRestore : UnitFactComponentDelegate, IUnitFinallyDeadHandler {
    public BlueprintAbilityResourceReference[] m_RageResources =
        Array.Empty<BlueprintAbilityResourceReference>();

    public void HandleUnitBecameFinallyDead(UnitEntityData unit) {
        if (!CombatChecks.WasKilledBy(unit, Owner)) {
            return;
        }

        foreach (var resourceReference in m_RageResources) {
            var resource = resourceReference?.Get();
            if (resource == null || Owner.Resources.GetResource(resource) == null) {
                continue;
            }

            Owner.Resources.Restore(resource, 1);
            return;
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6a65596a-a19c-46e3-8442-672087da56c4")]
public sealed class GloriousChargeTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon> {
    public BlueprintBuffReference m_Buff;
    public int DurationRounds = 2;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt) {
        if (!evt.IsCharge || evt.Initiator != Owner || DurationRounds <= 0) {
            return;
        }

        var buff = m_Buff?.Get();
        if (buff == null) {
            return;
        }

        Owner.Buffs.GetBuff(buff)?.Remove();
        Owner.Buffs.AddBuff(buff, Owner, CombatChecks.Rounds(DurationRounds));
    }
}

[AllowedOn(typeof(BlueprintAbility))]
[TypeId("2b33ab64-f992-4aba-afad-0fb2ad57f4d8")]
public sealed class ConsumingRageRestriction : BlueprintComponent, IAbilityCasterRestriction {
    public int SpellLevel;
    public BlueprintCharacterClassReference m_BloodragerClass;
    public BlueprintAbilityResourceReference m_BloodrageResource;

    public bool IsCasterRestrictionPassed(UnitEntityData caster) =>
        CanConvert(
            caster,
            m_BloodragerClass,
            m_BloodrageResource,
            SpellLevel,
            out _,
            out _);

    public string GetAbilityCasterRestrictionUIText() =>
        $"Requires an available level {SpellLevel} Bloodrager spell slot and a non-full Bloodrage pool.";

    internal static bool CanConvert(
        UnitEntityData caster,
        BlueprintCharacterClassReference classReference,
        BlueprintAbilityResourceReference resourceReference,
        int spellLevel,
        out Spellbook spellbook,
        out BlueprintAbilityResource resource) {
        spellbook = null;
        resource = resourceReference?.Get();
        var characterClass = classReference?.Get();
        if (caster == null || characterClass == null || resource == null ||
            spellLevel < 1 || spellLevel > 4) {
            return false;
        }

        spellbook = caster.Descriptor.GetSpellbook(characterClass);
        var rage = caster.Resources.GetResource(resource);
        return spellbook?.Blueprint?.Spontaneous == true &&
               spellbook.GetSpontaneousSlots(spellLevel) > 0 &&
               rage != null &&
               rage.Amount < rage.GetMaxAmount(caster.Descriptor);
    }
}

[TypeId("27bbd21e-60c1-47c5-91e8-f61d3710e317")]
public sealed class ContextActionConsumingRage : ContextAction {
    public int SpellLevel;
    public BlueprintCharacterClassReference m_BloodragerClass;
    public BlueprintAbilityResourceReference m_BloodrageResource;

    public override string GetCaption() =>
        $"Sacrifice a level {SpellLevel} Bloodrager spell slot to restore Bloodrage";

    public override void RunAction() {
        var caster = Context?.MaybeCaster;
        if (!ConsumingRageRestriction.CanConvert(
                caster,
                m_BloodragerClass,
                m_BloodrageResource,
                SpellLevel,
                out var spellbook,
                out var resource)) {
            return;
        }

        spellbook.RestoreSpontaneousSlots(SpellLevel, -1);
        caster.Resources.Restore(resource, SpellLevel);
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("e226d3d4-fb85-4295-8b1c-98071b375c48")]
public sealed class ArcaneConcordanceMetamagic : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    public Metamagic GrantedMetamagic;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        if (evt.Spellbook?.Blueprint?.IsArcane != true) {
            return;
        }

        evt.AddBonusDC(1, ModifierDescriptor.Enhancement);
        evt.AddMetamagic(GrantedMetamagic);
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("edd244de-bbd9-4aa2-8c9e-d9884ba5fbd5")]
public sealed class BladeTutorsSpiritReduction : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget> {
    private static readonly HashSet<string> ScalingPenaltyBuffs =
        new(StringComparer.OrdinalIgnoreCase) {
            "PowerAttackBuff",
            "PiranhaStrikeBuff",
            "CombatExpertiseBuff",
        };

    private static readonly HashSet<string> FixedPenaltyBuffs =
        new(StringComparer.OrdinalIgnoreCase) {
            "ChargeBuff",
            "FightingDefensivelyBuff",
            "SpellCombatBuff",
        };

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (evt.Weapon?.Blueprint?.IsMelee != true) {
            return;
        }

        var baseAttackBonus = Owner.Stats.BaseAttackBonus.ModifiedValue;
        var scalingPenalty = 1 + baseAttackBonus / 4;
        var voluntaryPenalty = 0;
        foreach (var buff in Owner.Buffs) {
            var name = buff.Blueprint?.name;
            if (name == null) {
                continue;
            }

            if (ScalingPenaltyBuffs.Contains(name)) {
                voluntaryPenalty += scalingPenalty;
            } else if (FixedPenaltyBuffs.Contains(name)) {
                // These effects impose at least a -2 attack penalty. Keeping
                // the cap conservative prevents the spell from turning a
                // reduced penalty into a net bonus.
                voluntaryPenalty += 2;
            }
        }

        if (voluntaryPenalty <= 0) {
            return;
        }

        var casterLevel = Math.Max(
            1,
            Fact.MaybeContext?.Params.CasterLevel ?? 1);
        var reduction = Math.Min(
            voluntaryPenalty,
            1 + casterLevel / 5);
        evt.AddModifier(
            reduction,
            Fact,
            ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("14138a50-e36d-4ce0-b3d8-876cd8bc7ccc")]
public sealed class DeadlyJuggernautKillTrigger : UnitFactComponentDelegate,
    IUnitFinallyDeadHandler {
    public BlueprintBuffReference[] m_TierBuffs =
        Array.Empty<BlueprintBuffReference>();

    public void HandleUnitBecameFinallyDead(UnitEntityData unit) {
        if (!CombatChecks.WasKilledBy(unit, Owner)) {
            return;
        }

        var damage = unit.LastHandledDamage;
        var weapon = damage?.DamageBundle?.Weapon ?? damage?.AttackRoll?.Weapon;
        if (weapon?.Blueprint?.IsMelee != true) {
            return;
        }

        var minimumHitDice = Math.Max(1, Owner.Progression.CharacterLevel - 4);
        if (unit.Progression.CharacterLevel < minimumHitDice) {
            return;
        }

        var tiers = m_TierBuffs
            .Select(reference => reference?.Get())
            .Where(buff => buff != null)
            .ToArray();
        if (tiers.Length == 0) {
            return;
        }

        var currentTier = Array.FindIndex(
            tiers,
            tier => Owner.Buffs.GetBuff(tier) != null);
        if (currentTier >= tiers.Length - 1) {
            return;
        }

        if (currentTier >= 0) {
            Owner.Buffs.GetBuff(tiers[currentTier])?.Remove();
        }

        var duration = (Fact as Buff)?.TimeLeft ?? TimeSpan.Zero;
        if (duration > TimeSpan.Zero) {
            Owner.Buffs.AddBuff(tiers[currentTier + 1], Owner, duration);
        }
    }

    public override void OnTurnOff() {
        foreach (var reference in m_TierBuffs) {
            var tier = reference?.Get();
            if (tier != null) {
                Owner.Buffs.GetBuff(tier)?.Remove();
            }
        }
    }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("6dd9f43d-bd97-4128-9523-80a23a1a5518")]
public sealed class DeadlyJuggernautBonuses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
    IInitiatorRulebookHandler<RuleStatCheck>,
    IInitiatorRulebookHandler<RuleSkillCheck> {
    public int Bonus;

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        if (evt.Weapon?.Blueprint?.IsMelee == true) {
            evt.AddModifier(Bonus, Fact, ModifierDescriptor.Luck);
        }
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        if (evt.Weapon?.Blueprint?.IsMelee == true) {
            evt.AddDamageModifier(Bonus, Fact, ModifierDescriptor.Luck);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    public void OnEventAboutToTrigger(RuleStatCheck evt) {
        if (evt.StatType != StatType.Strength) {
            return;
        }

        // Ability checks use the attribute modifier rather than the score.
        // A temporary +2 Strength per tier therefore produces the intended
        // +1 luck bonus to the check without affecting persistent statistics.
        var modifier = Owner.Stats.Strength.AddModifier(
            Bonus * 2,
            Runtime,
            ModifierDescriptor.Luck);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleStatCheck evt) { }

    public void OnEventAboutToTrigger(RuleSkillCheck evt) {
        if (evt.StatType != StatType.SkillAthletics) {
            return;
        }

        var modifier = evt.Bonus.AddModifier(
            Bonus,
            Runtime,
            ModifierDescriptor.Luck);
        evt.AddTemporaryModifier(modifier);
    }

    public void OnEventDidTrigger(RuleSkillCheck evt) { }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("2a538baa-7aa5-47f8-8a15-4b6e9dc3e6b6")]
public sealed class BlisteringInvectiveDemoralizeHandler :
    UnitFactComponentDelegate,
    IInitiatorDemoralizeHandler {
    public BlueprintAbilityReference m_Ability;
    public BlueprintBuffReference m_CatchFireBuff;

    public void AfterIntimidateSuccess(
        Demoralize action,
        RuleSkillCheck intimidateCheck,
        Buff appliedBuff) {
        var context = action.Context;
        var caster = context?.MaybeCaster;
        var target = action.Target?.Unit;
        var catchFire = m_CatchFireBuff?.Get();
        if (!intimidateCheck.Success || appliedBuff == null ||
            context?.SourceAbility != m_Ability?.Get() ||
            caster == null || target == null || catchFire == null) {
            return;
        }

        var resistance = context.TriggerRule(
            new RuleSpellResistanceCheck(context, target));
        if (resistance.IsSpellResisted) {
            return;
        }

        context.TriggerRule(new RuleDealDamage(
            caster,
            target,
            new EnergyDamage(
                new DiceFormula(1, DiceType.D10),
                DamageEnergyType.Fire)));

        var save = context.TriggerRule(new RuleSavingThrow(
            target,
            SavingThrowType.Reflex,
            context.Params.DC));
        if (!save.IsPassed) {
            target.Buffs.AddBuff(
                catchFire,
                context,
                CombatChecks.Rounds(10));
        }
    }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("e499918b-a199-4815-a82d-26d6fa5b849c")]
public sealed class BlisteringInvectiveCatchFire : UnitFactComponentDelegate,
    IUnitNewCombatRoundHandler {
    public override void OnTurnOn() => DealFireDamage();

    public void HandleNewCombatRound(UnitEntityData unit) {
        if (unit != Owner) {
            return;
        }

        var context = Fact.MaybeContext;
        if (context == null) {
            return;
        }

        var save = context.TriggerRule(new RuleSavingThrow(
            Owner,
            SavingThrowType.Reflex,
            15));
        if (save.IsPassed) {
            (Fact as Buff)?.Remove();
            return;
        }

        DealFireDamage();
    }

    private void DealFireDamage() {
        var context = Fact.MaybeContext;
        if (context == null) {
            return;
        }

        context.TriggerRule(new RuleDealDamage(
            context.MaybeCaster ?? Owner,
            Owner,
            new EnergyDamage(
                new DiceFormula(1, DiceType.D6),
                DamageEnergyType.Fire)));
    }
}

[TypeId("4ad2a86f-a256-4756-95ae-a2140126b7e1")]
public sealed class ContextActionBurstOfRadiance : ContextAction {
    public BlueprintBuffReference m_BlindBuff;
    public BlueprintBuffReference m_DazzledBuff;

    public override string GetCaption() => "apply Burst of Radiance";

    public override void RunAction() {
        var caster = Context?.MaybeCaster;
        var target = Target.Unit;
        var blind = m_BlindBuff?.Get();
        var dazzled = m_DazzledBuff?.Get();
        if (caster == null || target == null || blind == null || dazzled == null) {
            return;
        }

        var duration = Context.TriggerRule(new RuleRollDice(
            caster,
            new DiceFormula(1, DiceType.D4))).Result;
        var save = Context.TriggerRule(new RuleSavingThrow(
            target,
            SavingThrowType.Reflex,
            Context.Params.DC));
        target.Buffs.AddBuff(
            save.IsPassed ? dazzled : blind,
            Context,
            CombatChecks.Rounds(duration));

        var alignment = target.Descriptor.Alignment.ValueVisible;
        if (alignment != Alignment.NeutralEvil &&
            alignment != Alignment.LawfulEvil &&
            alignment != Alignment.ChaoticEvil) {
            return;
        }

        var diceCount = Math.Max(
            1,
            Math.Min(5, Context.Params.CasterLevel));
        Context.TriggerRule(new RuleDealDamage(
            caster,
            target,
            new DirectDamage(
                new DiceFormula(diceCount, DiceType.D4),
                0)));
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("57f617fd-6cfd-49d6-811f-f776eddc23d7")]
public sealed class DangerSenseAgainstInvisibleEnemies : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (CombatChecks.IsInvisibleTo(evt.Initiator, Owner)) {
            evt.AddModifier(Fact.GetRank(), Fact, ModifierDescriptor.Dodge);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        if (evt.Type != SavingThrowType.Reflex) {
            return;
        }

        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (CombatChecks.IsInvisibleTo(source, Owner)) {
            var modifier = Owner.Stats.SaveReflex.AddModifier(
                Fact.GetRank(),
                Runtime,
                ModifierDescriptor.UntypedStackable);
            evt.AddTemporaryModifier(modifier);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("91e4849c-0320-48dc-991d-ce9775e980f3")]
public sealed class MasterHunterFavoredEnemyDefense : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>,
    IInitiatorRulebookHandler<RuleSavingThrow> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        if (CombatChecks.IsFavoredEnemy(Owner, evt.Initiator)) {
            evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }

    public void OnEventAboutToTrigger(RuleSavingThrow evt) {
        var source = evt.Reason?.Caster ?? evt.Reason?.SourceUnit;
        if (!CombatChecks.IsFavoredEnemy(Owner, source)) {
            return;
        }

        var modifier = evt.Type switch {
            SavingThrowType.Fortitude => Owner.Stats.SaveFortitude.AddModifier(
                2,
                Runtime,
                ModifierDescriptor.UntypedStackable),
            SavingThrowType.Reflex => Owner.Stats.SaveReflex.AddModifier(
                2,
                Runtime,
                ModifierDescriptor.UntypedStackable),
            SavingThrowType.Will => Owner.Stats.SaveWill.AddModifier(
                2,
                Runtime,
                ModifierDescriptor.UntypedStackable),
            _ => null,
        };
        if (modifier != null) {
            evt.AddTemporaryModifier(modifier);
        }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a8accc2c-624a-4709-9866-c2ccf732f62c")]
public sealed class TrueLuckReroll : UnitFactComponentDelegate<TrueLuckReroll.ComponentData>,
    IInitiatorRulebookHandler<RuleRollD20>,
    IUnitNewCombatRoundHandler {
    public sealed class ComponentData {
        public bool Used;
        public TimeSpan LastUse;
    }

    public void HandleNewCombatRound(UnitEntityData unit) {
        if (unit == Owner) {
            Data.Used = false;
        }
    }

    public void OnEventAboutToTrigger(RuleRollD20 evt) {
        if (evt.IsFake || !IsReady()) {
            return;
        }

        var check = Rulebook.CurrentContext?.PreviousEvent;
        if (check == null || check.Initiator != Owner) {
            return;
        }

        var originalRoll = evt.PreRollDice();
        var bonus = Math.Max(0, Owner.Stats.Charisma.Bonus / 2);
        var failed = check switch {
            RuleAttackRoll attack => !attack.IsSuccessRoll(originalRoll),
            RuleSavingThrow saving => !((ISuccessable)saving).IsSuccessRoll(originalRoll),
            RuleSkillCheck skill => !((ISuccessable)skill).IsSuccessRoll(originalRoll),
            RuleSpellResistanceCheck resistance =>
                resistance.SpellResistance > resistance.SpellPenetration + originalRoll,
            _ => false,
        };
        if (!failed) {
            return;
        }

        ApplyLuckBonus(check, bonus);
        evt.AddReroll(1, takeBest: true, Fact);
        Data.Used = true;
        Data.LastUse = CurrentGameTime();
    }

    public void OnEventDidTrigger(RuleRollD20 evt) { }

    private bool IsReady() {
        if (!Data.Used) {
            return true;
        }

        if (Owner.IsInCombat) {
            return false;
        }

        if (CurrentGameTime() - Data.LastUse >= CombatChecks.Rounds(1)) {
            Data.Used = false;
            return true;
        }

        return false;
    }

    private void ApplyLuckBonus(RulebookEvent check, int bonus) {
        if (bonus <= 0) {
            return;
        }

        switch (check) {
            case RuleAttackRoll attack: {
                var stat = Owner.Stats.AdditionalAttackBonus;
                var before = stat.ModifiedValue;
                var modifier = stat.AddModifier(bonus, Runtime, ModifierDescriptor.Luck);
                attack.AddTemporaryModifier(modifier);
                attack.AttackBonus += stat.ModifiedValue - before;
                break;
            }
            case RuleSavingThrow saving: {
                var stat = Owner.Stats.GetStat(saving.StatType);
                var modifier = stat.AddModifier(bonus, Runtime, ModifierDescriptor.Luck);
                saving.AddTemporaryModifier(modifier);
                break;
            }
            case RuleSkillCheck skill: {
                var modifier = skill.Bonus.AddModifier(
                    bonus,
                    Runtime,
                    ModifierDescriptor.Luck);
                skill.AddTemporaryModifier(modifier);
                break;
            }
            case RuleSpellResistanceCheck resistance: {
                var before = resistance.m_AdditionalSpellPenetration == null
                    ? 0
                    : (int)resistance.m_AdditionalSpellPenetration;
                resistance.AddSpellPenetration(bonus, ModifierDescriptor.Luck);
                var after = resistance.m_AdditionalSpellPenetration == null
                    ? 0
                    : (int)resistance.m_AdditionalSpellPenetration;
                resistance.SpellPenetration += after - before;
                break;
            }
        }
    }

    private static TimeSpan CurrentGameTime() =>
        Game.HasInstance && Game.Instance.TimeController != null
            ? Game.Instance.TimeController.GameTime
            : TimeSpan.Zero;
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("e5f8e4b4-cc11-405c-92b5-dee4450fcbb8")]
public sealed class BeastsOfLegendsShapeshiftBonus : UnitFactComponentDelegate,
    IPolymorphActivatedHandler,
    IPolymorphDeactivatedHandler {
    public BlueprintBuffReference m_Buff;

    public override void OnTurnOn() {
        base.OnTurnOn();
        if (IsPolymorphed()) {
            ApplyBuff();
        }
    }

    public override void OnTurnOff() {
        RemoveBuff();
        base.OnTurnOff();
    }

    public void OnPolymorphActivated(UnitEntityData unit, Polymorph polymorph) {
        if (unit == Owner) {
            ApplyBuff();
        }
    }

    public void OnPolymorphDeactivated(UnitEntityData unit, Polymorph polymorph) {
        if (unit == Owner && !IsPolymorphed(polymorph)) {
            RemoveBuff();
        }
    }

    private bool IsPolymorphed(Polymorph excluded = null) =>
        Owner?.Buffs?
            .GetFactsContainingComponent<Polymorph>()
            .Any(buff => buff.Blueprint
                .GetComponents<Polymorph>()
                .Any(component => component != excluded)) == true;

    private void ApplyBuff() {
        var buff = m_Buff?.Get();
        if (buff != null && Owner.Buffs.GetBuff(buff) == null) {
            Owner.Buffs.AddBuff(buff, Owner, null);
        }
    }

    private void RemoveBuff() {
        var buff = m_Buff?.Get();
        if (buff != null) {
            Owner.Buffs.GetBuff(buff)?.Remove();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("df7c9cdd-d40b-4376-84f2-442213a640fb")]
public sealed class MasterHunterAnimalFocusBonusController : UnitFactComponentDelegate,
    IUnitGainFactHandler,
    IUnitLostFactHandler {
    public BlueprintBuffReference m_BonusBuff;
    public BlueprintUnitFactReference[] m_AnimalFocusEffects =
        Array.Empty<BlueprintUnitFactReference>();

    public override void OnTurnOn() {
        base.OnTurnOn();
        RefreshBonus();
    }

    public override void OnTurnOff() {
        RemoveBonus();
        base.OnTurnOff();
    }

    public void HandleUnitGainFact(EntityFact fact) {
        if (IsAnimalFocus(fact)) {
            ApplyBonus();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (IsAnimalFocus(fact) && !HasAnimalFocus(fact)) {
            RemoveBonus();
        }
    }

    private bool IsAnimalFocus(EntityFact fact) =>
        fact?.Blueprint != null &&
        m_AnimalFocusEffects.Any(reference => reference?.Get() == fact.Blueprint);

    private bool HasAnimalFocus(EntityFact excluded = null) =>
        m_AnimalFocusEffects.Any(reference => {
            var animalFocus = reference?.Get();
            var activeFact = animalFocus == null ? null : Owner.GetFact(animalFocus);
            return activeFact != null && activeFact != excluded;
        });

    private void RefreshBonus() {
        if (HasAnimalFocus()) {
            ApplyBonus();
        } else {
            RemoveBonus();
        }
    }

    private void ApplyBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null && Owner.Buffs.GetBuff(buff) == null) {
            Owner.Buffs.AddBuff(buff, Owner, null);
        }
    }

    private void RemoveBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null) {
            Owner.Buffs.GetBuff(buff)?.Remove();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("078ec36b-7791-44ca-8844-1c917fc0bd75")]
public sealed class DivineHoundJudgmentBonusController : UnitFactComponentDelegate,
    IUnitGainFactHandler,
    IUnitLostFactHandler {
    public BlueprintBuffReference m_BonusBuff;
    public BlueprintBuffReference[] m_JudgmentBuffs =
        Array.Empty<BlueprintBuffReference>();

    public override void OnTurnOn() {
        base.OnTurnOn();
        RefreshBonus();
    }

    public override void OnTurnOff() {
        RemoveBonus();
        base.OnTurnOff();
    }

    public void HandleUnitGainFact(EntityFact fact) {
        if (IsJudgment(fact)) {
            ApplyBonus();
        }
    }

    public void HandleUnitLostFact(EntityFact fact) {
        if (IsJudgment(fact) && !HasJudgment(fact)) {
            RemoveBonus();
        }
    }

    private bool IsJudgment(EntityFact fact) =>
        fact?.Blueprint != null &&
        m_JudgmentBuffs.Any(reference => reference?.Get() == fact.Blueprint);

    private bool HasJudgment(EntityFact excluded = null) =>
        m_JudgmentBuffs.Any(reference => {
            var judgment = reference?.Get();
            var activeFact = judgment == null ? null : Owner.GetFact(judgment);
            return activeFact != null && activeFact != excluded;
        });

    private void RefreshBonus() {
        if (HasJudgment()) {
            ApplyBonus();
        } else {
            RemoveBonus();
        }
    }

    private void ApplyBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null && Owner.Buffs.GetBuff(buff) == null) {
            Owner.Buffs.AddBuff(buff, Owner, null);
        }
    }

    private void RemoveBonus() {
        var buff = m_BonusBuff?.Get();
        if (buff != null) {
            Owner.Buffs.GetBuff(buff)?.Remove();
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("505d2fc8-fa32-4dfe-a467-19ab9339740d")]
public sealed class TandemExecutionController : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RulePrepareDamage>,
    IUnitCombatHandler {
    public bool IsPet;
    public BlueprintBuffReference m_MyPetBuff;
    public BlueprintBuffReference m_HunterMark;
    public BlueprintBuffReference m_PetMark;

    public override void OnTurnOff() {
        ClearPairMarks();
        base.OnTurnOff();
    }

    public void HandleUnitJoinCombat(UnitEntityData unit) {
        if (unit == Owner) {
            ClearPairMarks();
        }
    }

    public void HandleUnitLeaveCombat(UnitEntityData unit) {
        if (unit == Owner) {
            ClearPairMarks();
        }
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt) {
        if (evt.IsFake || evt.Weapon == null || evt.Target == null ||
            !evt.Target.IsEnemy(Owner)) {
            return;
        }

        var hunter = GetHunter();
        var marker = (IsPet ? m_PetMark : m_HunterMark)?.Get();
        if (hunter != null && marker != null) {
            evt.Target.Buffs.AddBuff(marker, hunter, null);
        }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) {
        var target = evt.ParentRule?.Target;
        var attackRoll = evt.ParentRule?.AttackRoll;
        if (evt.DamageBundle?.Weapon == null || target == null ||
            !target.IsEnemy(Owner) ||
            attackRoll?.FortificationNegatesSneakAttack == true ||
            attackRoll?.ImmuneToSneakAttack == true) {
            return;
        }

        var hunter = GetHunter();
        if (hunter == null ||
            !HasMarkFromHunter(target, m_HunterMark, hunter) ||
            !HasMarkFromHunter(target, m_PetMark, hunter)) {
            return;
        }

        var baseDamage = evt.DamageBundle.FirstOrDefault();
        if (baseDamage == null) {
            return;
        }

        var damage = baseDamage
            .CreateTypeDescription()
            .GetDamageDescriptor(new DiceFormula(1, DiceType.D6), 0)
            .CreateDamage();
        damage.Precision = true;
        damage.Sneak = true;
        damage.SourceFact = Fact;
        evt.Add(damage);
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }

    private UnitEntityData GetHunter() {
        if (!IsPet) {
            return Owner;
        }

        var myPetBuff = m_MyPetBuff?.Get();
        return (myPetBuff == null
            ? null
            : Owner.Buffs.GetBuff(myPetBuff)?.MaybeContext?.MaybeCaster)
            ?? Fact.MaybeContext?.MaybeCaster;
    }

    private void ClearPairMarks() {
        var hunter = GetHunter();
        var uniqueBuffs = hunter?.Get<UnitPartUniqueBuffs>()?.Buffs;
        if (uniqueBuffs == null) {
            return;
        }

        var hunterMark = m_HunterMark?.Get();
        var petMark = m_PetMark?.Get();
        foreach (var buff in uniqueBuffs
            .Where(buff => buff.Blueprint == hunterMark || buff.Blueprint == petMark)
            .ToArray()) {
            buff.Remove();
        }
    }

    private static bool HasMarkFromHunter(
        UnitEntityData target,
        BlueprintBuffReference markerReference,
        UnitEntityData hunter) {
        var marker = markerReference?.Get();
        if (marker == null) {
            return false;
        }

        foreach (var buff in target.Buffs) {
            if (buff.Blueprint == marker &&
                buff.MaybeContext?.MaybeCaster == hunter) {
                return true;
            }
        }

        return false;
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("cc79081c-0db2-4507-9a87-a77a18e339c0")]
public sealed class PositiveConstitutionResourceBonus : UnitFactComponentDelegate,
    IResourceAmountBonusHandler {
    public BlueprintAbilityResourceReference m_Resource;

    public void CalculateMaxResourceAmount(
        BlueprintAbilityResource resource,
        ref int bonus) {
        if (resource == m_Resource?.Get()) {
            bonus += Math.Max(0, Owner.Stats.Constitution.Bonus);
        }
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("a1206730-2b03-4a5f-9a27-50ec172f18cb")]
public sealed class MasterSlayerStudiedTargetInsight : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus> {
    public BlueprintBuffReference m_StudiedTargetBuff;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt) {
        var target = evt.Target;
        var studiedTargetBuff = m_StudiedTargetBuff?.Get();
        if (target == null || studiedTargetBuff == null ||
            !IsStudiedByOwner(target, studiedTargetBuff)) {
            return;
        }

        var bonus = Math.Max(0, Owner.Stats.Intelligence.Bonus / 2);
        if (bonus > 0) {
            evt.AddModifier(bonus, Fact, ModifierDescriptor.Insight);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

    private bool IsStudiedByOwner(
        UnitEntityData target,
        BlueprintBuff studiedTargetBuff) {
        foreach (var buff in target.Buffs) {
            if (buff.Blueprint == studiedTargetBuff &&
                buff.MaybeContext?.MaybeCaster == Owner) {
                return true;
            }
        }

        return false;
    }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("d934a9a4-9646-4cc2-b114-c4fcb01fd698")]
public sealed class IAmYourShieldArmorClassBonus : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC> {
    public void OnEventAboutToTrigger(RuleCalculateAC evt) {
        var specialist = Fact.MaybeContext?.MaybeCaster;
        var shield = specialist?.Descriptor?.Body?.SecondaryHand?.MaybeShield
            ?? specialist?.Descriptor?.Body?.PrimaryHand?.MaybeShield;
        var shieldArmor = shield?.Blueprint?.ArmorComponent;
        if (shieldArmor?.ProficiencyGroup != ArmorProficiencyGroup.TowerShield) {
            return;
        }

        var shieldBonus = shieldArmor.ArmorBonus + Math.Max(0, shield.EnchantmentValue);
        if (shieldBonus > 0) {
            evt.AddModifier(shieldBonus, Fact, ModifierDescriptor.Shield);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
}
