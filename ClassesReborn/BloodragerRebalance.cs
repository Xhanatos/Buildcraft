using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;

namespace ClassesReborn;

internal static class BloodragerRebalance {
    private sealed class Variant {
        internal Variant(
            int spellLevel,
            string blueprintName,
            string blueprintId,
            string nameKey,
            string descriptionKey) {
            SpellLevel = spellLevel;
            BlueprintName = blueprintName;
            BlueprintId = blueprintId;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
        }

        internal int SpellLevel { get; }
        internal string BlueprintName { get; }
        internal string BlueprintId { get; }
        internal string NameKey { get; }
        internal string DescriptionKey { get; }
    }

    private static readonly Variant[] Variants = {
        new(
            1,
            "ClassesRebornConsumingRageLevel1Ability",
            BlueprintIds.ConsumingRageLevel1Ability,
            "ClassesReborn.ConsumingRage.Level1.Name",
            "ClassesReborn.ConsumingRage.Level1.Description"),
        new(
            2,
            "ClassesRebornConsumingRageLevel2Ability",
            BlueprintIds.ConsumingRageLevel2Ability,
            "ClassesReborn.ConsumingRage.Level2.Name",
            "ClassesReborn.ConsumingRage.Level2.Description"),
        new(
            3,
            "ClassesRebornConsumingRageLevel3Ability",
            BlueprintIds.ConsumingRageLevel3Ability,
            "ClassesReborn.ConsumingRage.Level3.Name",
            "ClassesReborn.ConsumingRage.Level3.Description"),
        new(
            4,
            "ClassesRebornConsumingRageLevel4Ability",
            BlueprintIds.ConsumingRageLevel4Ability,
            "ClassesReborn.ConsumingRage.Level4.Name",
            "ClassesReborn.ConsumingRage.Level4.Description"),
    };

    internal static void Configure() {
        var bloodragerClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.BloodragerClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.BloodragerProgression);
        var rageFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.BloodragerRageFeature);
        var classReference = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
            BlueprintIds.BloodragerClass);
        var resourceReference = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
            BlueprintIds.BloodragerRageResource);

        ConfigureSteelbloodDamageReduction(progression);
        ConfigureSpellEaterUncannyDodge(progression);
        ConfigureHagRivenUncannyDodge(progression);
        ConfigureHagRivenClaws();

        foreach (var variant in Variants) {
            var actions = ActionsBuilder.New().Add<ContextActionConsumingRage>(action => {
                action.SpellLevel = variant.SpellLevel;
                action.m_BloodragerClass = classReference;
                action.m_BloodrageResource = resourceReference;
            });

            AbilityConfigurator.New(variant.BlueprintName, variant.BlueprintId)
                .SetDisplayName(variant.NameKey)
                .SetDescription(variant.DescriptionKey)
                .SetIcon(rageFeature.Icon)
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetActionType(UnitCommand.CommandType.Swift)
                .SetParent(BlueprintIds.ConsumingRageAbility)
                .AllowTargeting(point: false, enemies: false, friends: false, self: true)
                .AddComponent(new ConsumingRageRestriction {
                    SpellLevel = variant.SpellLevel,
                    m_BloodragerClass = classReference,
                    m_BloodrageResource = resourceReference,
                })
                .AddAbilityEffectRunAction(actions)
                .Configure();
        }

        var parentAbility = AbilityConfigurator.New(
                "ClassesRebornConsumingRageAbility",
                BlueprintIds.ConsumingRageAbility)
            .SetDisplayName("ClassesReborn.ConsumingRage.Name")
            .SetDescription("ClassesReborn.ConsumingRage.Description")
            .SetIcon(rageFeature.Icon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Swift)
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddAbilityVariants(new() {
                BlueprintIds.ConsumingRageLevel1Ability,
                BlueprintIds.ConsumingRageLevel2Ability,
                BlueprintIds.ConsumingRageLevel3Ability,
                BlueprintIds.ConsumingRageLevel4Ability,
            })
            .Configure();

        var feature = FeatureConfigurator.New(
                "ClassesRebornConsumingRageFeature",
                BlueprintIds.ConsumingRageFeature)
            .SetDisplayName("ClassesReborn.ConsumingRage.Name")
            .SetDescription("ClassesReborn.ConsumingRage.Description")
            .SetIcon(rageFeature.Icon)
            .SetIsClassFeature(true)
            .AddFacts(new() { BlueprintIds.ConsumingRageAbility })
            .Configure();

        var entries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(entries, 8, feature);
        progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();

        Validate(bloodragerClass, progression, feature, parentAbility);
    }

    private static void ConfigureSteelbloodDamageReduction(BlueprintProgression progression) {
        var steelblood = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.SteelbloodArchetype);
        var damageReduction = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.BloodragerDamageReduction);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var improvedUncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUncannyDodge);
        var damageReductionLevels = new[] { 7, 10, 13, 16, 19 };

        // Restore the five standard Bloodrager DR ranks without changing any of
        // Steelblood's other feature exchanges.
        RemoveFeature(steelblood.RemoveFeatures, damageReduction);

        if (damageReductionLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, damageReduction, level) != 1)) {
            throw new InvalidOperationException(
                "The Bloodrager progression is missing a standard Damage Reduction rank.");
        }
        if (CountFeature(steelblood.RemoveFeatures, damageReduction) != 0) {
            throw new InvalidOperationException(
                "Steelblood still removes standard Bloodrager Damage Reduction.");
        }
        if (CountFeature(steelblood.RemoveFeatures, uncannyDodgeChecker) == 0 ||
            CountFeature(steelblood.RemoveFeatures, improvedUncannyDodge) == 0) {
            throw new InvalidOperationException(
                "Steelblood's Uncanny Dodge tradeoffs must remain unchanged.");
        }
    }

    private static void ConfigureSpellEaterUncannyDodge(BlueprintProgression progression) {
        var spellEater = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.SpellEaterArchetype);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var improvedUncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUncannyDodge);

        // Bloodrager level 2 grants this hidden checker, which supplies the visible
        // Uncanny Dodge fact. Spell Eater keeps that base feature but continues to
        // trade away the direct Improved Uncanny Dodge grant at level 5.
        RemoveFeature(spellEater.RemoveFeatures, uncannyDodgeChecker);

        if (CountFeatureAtLevel(progression.LevelEntries, uncannyDodgeChecker, 2) != 1) {
            throw new InvalidOperationException(
                "The Bloodrager progression does not grant Uncanny Dodge Checker at level 2.");
        }
        if (CountFeature(spellEater.RemoveFeatures, uncannyDodgeChecker) != 0) {
            throw new InvalidOperationException(
                "Spell Eater still removes normal Uncanny Dodge after configuration.");
        }
        if (CountFeature(spellEater.RemoveFeatures, improvedUncannyDodge) == 0) {
            throw new InvalidOperationException(
                "Spell Eater must continue to remove Improved Uncanny Dodge.");
        }
    }

    private static void ConfigureHagRivenUncannyDodge(BlueprintProgression progression) {
        var hagRiven = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.HagRivenArchetype);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var improvedUncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUncannyDodge);

        // Bloodrager level 2 grants the hidden checker, which supplies the visible
        // Uncanny Dodge fact. Restore only that checker; Hag-Riven still trades away
        // the direct Improved Uncanny Dodge grant at level 5.
        RemoveFeature(hagRiven.RemoveFeatures, uncannyDodgeChecker);

        if (CountFeatureAtLevel(progression.LevelEntries, uncannyDodgeChecker, 2) != 1) {
            throw new InvalidOperationException(
                "The Bloodrager progression does not grant Uncanny Dodge Checker at level 2.");
        }
        if (CountFeature(hagRiven.RemoveFeatures, uncannyDodgeChecker) != 0) {
            throw new InvalidOperationException(
                "Hag-Riven still removes normal Uncanny Dodge after configuration.");
        }
        if (CountFeature(hagRiven.RemoveFeatures, improvedUncannyDodge) == 0) {
            throw new InvalidOperationException(
                "Hag-Riven must continue to remove Improved Uncanny Dodge.");
        }
    }

    private static void ConfigureHagRivenClaws() {
        var clawTierFeatureIds = new[] {
            BlueprintIds.HagRivenClawsFeatureLevel1,
            BlueprintIds.HagRivenClawsFeatureLevel2,
            BlueprintIds.HagRivenClawsFeatureLevel5,
            BlueprintIds.HagRivenClawsFeatureLevel9,
            BlueprintIds.HagRivenClawsFeatureLevel13,
            BlueprintIds.HagRivenClawsFeatureLevel17,
        };
        var clawUpgradeWrapperIds = new[] {
            BlueprintIds.HagRivenClawsFeatureAddLevel,
            BlueprintIds.HagRivenClawsFeatureAddLevel1,
            BlueprintIds.HagRivenClawsFeatureAddLevel2,
            BlueprintIds.HagRivenClawsFeatureAddLevel3,
            BlueprintIds.HagRivenClawsFeatureAddLevel4,
            BlueprintIds.HagRivenClawsFeatureAddLevel5,
        };
        foreach (var featureId in clawTierFeatureIds.Concat(clawUpgradeWrapperIds)) {
            FeatureConfigurator.For(featureId)
                .SetDescription("ClassesReborn.HagRivenClaws.Description")
                .Configure();
        }

        var level17Feature = FeatureConfigurator.For(
                BlueprintIds.HagRivenClawsFeatureLevel17)
            .AddComponent(new HagRivenClawCriticalRange {
                m_Claws = new[] {
                    BlueprintTool.GetRef<BlueprintItemWeaponReference>(
                        BlueprintIds.HagRivenClaw1D6),
                    BlueprintTool.GetRef<BlueprintItemWeaponReference>(
                        BlueprintIds.HagRivenClaw1D8),
                },
            })
            .Configure();

        var criticalComponents = level17Feature
            .GetComponents<HagRivenClawCriticalRange>()
            .ToArray();
        var configuredClaws = criticalComponents.SingleOrDefault()?.m_Claws
            .Select(reference => reference?.Get())
            .Where(weapon => weapon != null)
            .ToArray() ?? Array.Empty<BlueprintItemWeapon>();
        // Owlcat grants the tier features through HagRivenClawsFeatureAddLevel
        // wrappers rather than placing the tier blueprint directly in the
        // archetype's AddFeatures table. Validate the component on the actual
        // level-17 tier without assuming that internal delivery path.
        if (criticalComponents.Length != 1 ||
            configuredClaws.Length != 2 ||
            configuredClaws.Distinct().Count() != 2) {
            throw new InvalidOperationException(
                "Hag-Riven level-17 claw critical-range configuration is invalid.");
        }
    }

    private static void Validate(
        BlueprintCharacterClass bloodragerClass,
        BlueprintProgression progression,
        BlueprintFeature feature,
        BlueprintAbility parentAbility) {
        var variants = parentAbility.GetComponent<AbilityVariants>()?.Variants
            .ToArray() ?? Array.Empty<BlueprintAbility>();
        var expected = Variants
            .Select(variant => BlueprintTool.Get<BlueprintAbility>(variant.BlueprintId))
            .ToArray();
        var featureCount = progression.LevelEntries
            .Where(entry => entry.Level == 8)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0);

        if (bloodragerClass.Progression != progression ||
            featureCount != 1 ||
            variants.Length != expected.Length ||
            expected.Any(ability => !variants.Contains(ability)) ||
            expected.Any(ability => ability.Parent != parentAbility) ||
            expected.Any(ability => ability.GetComponents<ConsumingRageRestriction>().Count() != 1)) {
            throw new InvalidOperationException(
                "Consuming Rage progression, variants, or spell-slot restrictions are invalid.");
        }
    }

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeature feature) {
        var entry = entries.FirstOrDefault(candidate => candidate.Level == level);
        if (entry == null) {
            entry = new LevelEntry { Level = level, m_Features = new() };
            entries.Add(entry);
        }

        entry.m_Features ??= new();
        if (!entry.m_Features.Any(reference => reference?.Get() == feature)) {
            entry.m_Features.Add(BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                feature.AssetGuid.ToString()));
        }
    }

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) {
        if (entries == null) {
            return;
        }

        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?.Where(entry => entry.Level == level).Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
}
