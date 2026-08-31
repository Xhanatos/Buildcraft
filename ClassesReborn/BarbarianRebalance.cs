using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class BarbarianRebalance {
    private static readonly int[] DamageReductionLevels = { 1, 4, 7, 10, 13, 16, 19 };
    private static readonly int[] InvulnerableRagerDamageReductionLevels =
        { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };

    internal static void Configure() {
        ConfigureBattleborn();

        FeatureConfigurator.For(BlueprintIds.BarbarianDamageReduction)
            .SetDescription("ClassesReborn.BarbarianDamageReduction.Description")
            .SetRanks(DamageReductionLevels.Length)
            .Configure();

        var barbarianClass = BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.BarbarianClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(BlueprintIds.BarbarianProgression);
        var damageReduction = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.BarbarianDamageReduction);
        var battleborn = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.BattlebornFeature);

        RemoveFeature(progression.LevelEntries, damageReduction);
        RemoveFeature(progression.LevelEntries, battleborn);

        // Archetypes can remove or relocate the base feature. Clear those entries before
        // applying the class-wide progression; targeted exceptions are configured afterward.
        foreach (var archetype in barbarianClass.Archetypes) {
            RemoveFeature(archetype.AddFeatures, damageReduction);
            RemoveFeature(archetype.RemoveFeatures, damageReduction);
            RemoveFeature(archetype.AddFeatures, battleborn);
            RemoveFeature(archetype.RemoveFeatures, battleborn);
        }

        ConfigureArmoredHulkUncannyDodge(progression);
        ConfigureArmoredHulkImprovedArmoredSwiftness();
        ConfigureFleshEaterUncannyDodge(progression);
        ConfigureInvulnerableRager(damageReduction);
        ConfigureMadDogDamageReduction(damageReduction);
        ConfigurePackRagerDamageReduction(damageReduction);
        ConfigureBeastkinBerserker();

        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        foreach (var level in DamageReductionLevels) {
            var levelEntry = levelEntries.FirstOrDefault(entry => entry.Level == level);
            if (levelEntry == null) {
                levelEntry = new LevelEntry { Level = level, m_Features = new() };
                levelEntries.Add(levelEntry);
            }

            levelEntry.m_Features ??= new();
            levelEntry.m_Features.Add(
                BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                    BlueprintIds.BarbarianDamageReduction));
        }

        var battlebornEntry = levelEntries.FirstOrDefault(entry => entry.Level == 8);
        if (battlebornEntry == null) {
            battlebornEntry = new LevelEntry { Level = 8, m_Features = new() };
            levelEntries.Add(battlebornEntry);
        }

        battlebornEntry.m_Features ??= new();
        battlebornEntry.m_Features.Add(
            BlueprintTool.GetRef<BlueprintFeatureBaseReference>(BlueprintIds.BattlebornFeature));

        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var configuredRanks = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == damageReduction) ?? 0);
        if (configuredRanks != DamageReductionLevels.Length) {
            throw new InvalidOperationException(
                $"Expected {DamageReductionLevels.Length} Barbarian damage reduction ranks, " +
                $"but configured {configuredRanks}.");
        }

        var configuredBattlebornEntries = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == battleborn) ?? 0);
        if (configuredBattlebornEntries != 1) {
            throw new InvalidOperationException(
                $"Expected one Battleborn progression entry, but configured " +
                $"{configuredBattlebornEntries}.");
        }
    }

    private static void ConfigureBattleborn() {
        var rageIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.BarbarianRage).Icon;

        FeatureConfigurator.New("ClassesRebornBattleborn", BlueprintIds.BattlebornFeature)
            .SetDisplayName("ClassesReborn.Battleborn.Name")
            .SetDescription("ClassesReborn.Battleborn.Description")
            .SetIcon(rageIcon)
            .AddComponent(new BattlebornRageRestore {
                m_RageResources = new[] {
                    BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        BlueprintIds.FocusedRageResource),
                    BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        BlueprintIds.BarbarianRageResource),
                },
            })
            .Configure();
    }

    internal static void ConfigureDangerSenseChanges() {
        var configuredEntries = new List<(string Id, string Description)>();
        if (Main.Settings.Barbarian) {
            configuredEntries.Add((
                BlueprintIds.BarbarianDangerSense,
                "ClassesReborn.DangerSense.Description"));
        }
        if (Main.Settings.Rogue) {
            configuredEntries.Add((
                BlueprintIds.RogueDangerSense,
                "ClassesReborn.DangerSense.Rogue.Description"));
        }
        if (Main.Settings.Bard) {
            configuredEntries.Add((
                BlueprintIds.ArchaeologistDangerSense,
                "ClassesReborn.DangerSense.Archaeologist.Description"));
        }

        var dangerSenseFeatures = configuredEntries
            .Select(entry => FeatureConfigurator.For(entry.Id)
            .SetDescription(entry.Description)
            .AddComponent(new DangerSenseAgainstInvisibleEnemies())
            .Configure())
            .ToArray();

        if (dangerSenseFeatures.Length != configuredEntries.Count ||
            dangerSenseFeatures.Any(feature =>
                feature.GetComponents<DangerSenseAgainstInvisibleEnemies>()
                    .Count() != 1)) {
            throw new InvalidOperationException(
                "Enabled Barbarian, Rogue, and Archaeologist Danger Sense changes must each be configured exactly once.");
        }
    }

    private static void ConfigureArmoredHulkUncannyDodge(BlueprintProgression progression) {
        var armoredHulk = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ArmoredHulkArchetype);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var improvedUncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUncannyDodge);

        // Barbarian level 2 grants the checker, which in turn grants the visible Uncanny
        // Dodge fact. Armored Hulk must stop excluding that checker, not its display wrapper.
        RemoveFeature(armoredHulk.RemoveFeatures, uncannyDodgeChecker);

        if (CountFeatureAtLevel(progression.LevelEntries, uncannyDodgeChecker, 2) != 1) {
            throw new InvalidOperationException(
                "The Barbarian progression does not grant Uncanny Dodge Checker at level 2.");
        }
        if (CountFeature(armoredHulk.RemoveFeatures, uncannyDodgeChecker) != 0) {
            throw new InvalidOperationException(
                "Armored Hulk still removes normal Uncanny Dodge after configuration.");
        }

        if (CountFeature(armoredHulk.RemoveFeatures, improvedUncannyDodge) == 0) {
            throw new InvalidOperationException(
                "Armored Hulk must continue to remove Improved Uncanny Dodge.");
        }
    }

    private static void ConfigureArmoredHulkImprovedArmoredSwiftness() {
        var armoredSwiftness = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ArmoredHulkArmoredSwiftness);

        var improvedArmoredSwiftness = FeatureConfigurator.For(
                BlueprintIds.ArmoredHulkImprovedArmoredSwiftness)
            .SetDescription(
                "ClassesReborn.ArmoredHulkImprovedArmoredSwiftness.Description")
            .RemoveComponents(component =>
                component is ArmorSpeedPenaltyRemoval ||
                component is SpeedBonusInArmorCategory ||
                component is RemoveFeatureOnApply)
            .AddComponent(new SpeedBonusInArmorCategory {
                Category = new[] {
                    ArmorProficiencyGroup.Medium,
                    ArmorProficiencyGroup.Heavy,
                },
                Bonus = 10,
                Descriptor = ModifierDescriptor.UntypedStackable,
            })
            .AddComponent(new RemoveFeatureOnApply {
                m_Feature = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.ArmoredHulkArmoredSwiftness),
            })
            .Configure();

        var speedBonuses = improvedArmoredSwiftness
            .GetComponents<SpeedBonusInArmorCategory>()
            .ToArray();
        var removedBaseFeatures = improvedArmoredSwiftness
            .GetComponents<RemoveFeatureOnApply>()
            .Where(component => component.m_Feature?.Get() == armoredSwiftness)
            .ToArray();

        if (speedBonuses.Length != 1 ||
            speedBonuses[0].Bonus != 10 ||
            speedBonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            speedBonuses[0].Category?.Length != 2 ||
            !speedBonuses[0].Category.Contains(ArmorProficiencyGroup.Medium) ||
            !speedBonuses[0].Category.Contains(ArmorProficiencyGroup.Heavy) ||
            improvedArmoredSwiftness.GetComponents<ArmorSpeedPenaltyRemoval>().Any() ||
            removedBaseFeatures.Length != 1) {
            throw new InvalidOperationException(
                "Improved Armored Swiftness must replace the capped level-2 feature " +
                "with one uncapped +10-foot medium/heavy armor speed bonus.");
        }
    }

    private static void ConfigureFleshEaterUncannyDodge(BlueprintProgression progression) {
        var fleshEater = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.FleshEaterArchetype);
        var uncannyDodgeChecker = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var improvedUncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUncannyDodge);

        // Restore the actual level-2 checker as well as the direct level-5 Improved
        // Uncanny Dodge grant. The base progression supplies both, so no duplicates are added.
        RemoveFeature(fleshEater.RemoveFeatures, uncannyDodgeChecker);
        RemoveFeature(fleshEater.RemoveFeatures, improvedUncannyDodge);

        if (CountFeatureAtLevel(progression.LevelEntries, uncannyDodgeChecker, 2) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, improvedUncannyDodge, 5) != 1) {
            throw new InvalidOperationException(
                "The Barbarian progression is missing an Uncanny Dodge base grant.");
        }
        if (CountFeature(fleshEater.RemoveFeatures, uncannyDodgeChecker) != 0 ||
            CountFeature(fleshEater.RemoveFeatures, improvedUncannyDodge) != 0) {
            throw new InvalidOperationException(
                "Flesheater still removes Uncanny Dodge or Improved Uncanny Dodge.");
        }
    }

    private static void ConfigureInvulnerableRager(BlueprintFeature baseDamageReduction) {
        var invulnerableDamageReductionIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.InvulnerableRagerDamageReduction).Icon;

        FeatureConfigurator.For(BlueprintIds.InvulnerableRagerDamageReduction)
            .SetDescription("ClassesReborn.InvulnerableRagerDamageReduction.Description")
            .SetRanks(InvulnerableRagerDamageReductionLevels.Length)
            .Configure();

        FeatureConfigurator.For(BlueprintIds.InvulnerableRagerExtremeEndurance)
            .SetDescription("ClassesReborn.ExtremeEndurance.Description")
            .AddComponent(new ResistEnergyContext {
                Value = ContextValues.Rank(),
                Type = DamageEnergyType.Electricity,
            })
            .Configure();

        BuffConfigurator.New(
                "ClassesRebornJustAScratchBuff",
                BlueprintIds.JustAScratchBuff)
            .SetDisplayName("ClassesReborn.JustAScratch.Name")
            .SetDescription("ClassesReborn.JustAScratch.BuffDescription")
            .SetIcon(invulnerableDamageReductionIcon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new AddDamageResistancePhysical {
                Value = ContextValues.Constant(2),
                m_IsStackable = true,
            })
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornJustAScratch",
                BlueprintIds.JustAScratchFeature)
            .SetDisplayName("ClassesReborn.JustAScratch.Name")
            .SetDescription("ClassesReborn.JustAScratch.Description")
            .SetIcon(invulnerableDamageReductionIcon)
            .AddComponent(new BuffOnHealthTickingTrigger {
                m_TriggeredBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.JustAScratchBuff),
                HealthPercent = 0.5f,
            })
            .Configure();

        var invulnerableRager = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.InvulnerableRagerArchetype);
        var invulnerableDamageReduction = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.InvulnerableRagerDamageReduction);
        var justAScratch = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.JustAScratchFeature);

        RemoveFeature(invulnerableRager.AddFeatures, invulnerableDamageReduction);
        RemoveFeature(invulnerableRager.AddFeatures, justAScratch);
        RemoveFeature(invulnerableRager.RemoveFeatures, justAScratch);

        var addFeatures = invulnerableRager.AddFeatures?.ToList() ?? new List<LevelEntry>();
        foreach (var level in InvulnerableRagerDamageReductionLevels) {
            var levelEntry = GetOrCreateLevelEntry(addFeatures, level);
            levelEntry.m_Features.Add(
                BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                    BlueprintIds.InvulnerableRagerDamageReduction));
        }

        var levelTenEntry = GetOrCreateLevelEntry(addFeatures, 10);
        levelTenEntry.m_Features.Add(
            BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                BlueprintIds.JustAScratchFeature));
        invulnerableRager.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        // The archetype uses its own ten-rank DR feature instead of the standard
        // Barbarian DR progression configured for other archetypes.
        RemoveFeature(invulnerableRager.RemoveFeatures, baseDamageReduction);
        var removeFeatures = invulnerableRager.RemoveFeatures?.ToList() ??
            new List<LevelEntry>();
        foreach (var level in DamageReductionLevels) {
            var levelEntry = GetOrCreateLevelEntry(removeFeatures, level);
            levelEntry.m_Features.Add(
                BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                    BlueprintIds.BarbarianDamageReduction));
        }
        invulnerableRager.RemoveFeatures = removeFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var configuredDamageReductionRanks = CountFeature(
            invulnerableRager.AddFeatures,
            invulnerableDamageReduction);
        var configuredJustAScratchEntries = CountFeature(
            invulnerableRager.AddFeatures,
            justAScratch);
        var configuredBaseDamageReductionRemovals = CountFeature(
            invulnerableRager.RemoveFeatures,
            baseDamageReduction);
        var justAScratchAtLevelTen = invulnerableRager.AddFeatures
            .Where(entry => entry.Level == 10)
            .Sum(entry =>
                entry.m_Features?.Count(reference => reference?.Get() == justAScratch) ?? 0);

        if (configuredDamageReductionRanks !=
            InvulnerableRagerDamageReductionLevels.Length) {
            throw new InvalidOperationException(
                "Invulnerable Rager damage reduction must have ten configured ranks.");
        }
        if (configuredJustAScratchEntries != 1 || justAScratchAtLevelTen != 1) {
            throw new InvalidOperationException(
                "Just a Scratch must be granted exactly once at Invulnerable Rager level 10.");
        }
        if (configuredBaseDamageReductionRemovals != DamageReductionLevels.Length) {
            throw new InvalidOperationException(
                "Invulnerable Rager must replace every standard Barbarian DR rank.");
        }
    }

    private static void ConfigureMadDogDamageReduction(BlueprintFeature baseDamageReduction) {
        FeatureConfigurator.For(BlueprintIds.MadDogDamageReductionMaster)
            .SetDescription("ClassesReborn.MadDogDamageReduction.Description")
            .SetRanks(DamageReductionLevels.Length)
            .Configure();
        FeatureConfigurator.For(BlueprintIds.MadDogDamageReductionPet)
            .SetDescription("ClassesReborn.MadDogDamageReduction.Description")
            .SetRanks(DamageReductionLevels.Length)
            .Configure();

        var madDog = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.MadDogArchetype);
        var sharedDamageReduction = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MadDogDamageReductionMaster);

        RemoveFeature(madDog.AddFeatures, sharedDamageReduction);
        RemoveFeature(madDog.RemoveFeatures, sharedDamageReduction);
        RemoveFeature(madDog.AddFeatures, baseDamageReduction);

        var addFeatures = madDog.AddFeatures?.ToList() ?? new List<LevelEntry>();
        foreach (var level in DamageReductionLevels) {
            var levelEntry = GetOrCreateLevelEntry(addFeatures, level);
            levelEntry.m_Features.Add(
                BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                    BlueprintIds.MadDogDamageReductionMaster));
        }
        madDog.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        // Mad Dog's version also grants each rank to its animal companion. Replace the
        // ordinary class feature at the same levels to avoid doubling the master's DR.
        RemoveFeature(madDog.RemoveFeatures, baseDamageReduction);
        var removeFeatures = madDog.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        foreach (var level in DamageReductionLevels) {
            var levelEntry = GetOrCreateLevelEntry(removeFeatures, level);
            levelEntry.m_Features.Add(
                BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                    BlueprintIds.BarbarianDamageReduction));
        }
        madDog.RemoveFeatures = removeFeatures.OrderBy(entry => entry.Level).ToArray();

        var configuredSharedRanks = CountFeature(madDog.AddFeatures, sharedDamageReduction);
        var configuredBaseRemovals = CountFeature(madDog.RemoveFeatures, baseDamageReduction);
        if (configuredSharedRanks != DamageReductionLevels.Length ||
            configuredBaseRemovals != DamageReductionLevels.Length) {
            throw new InvalidOperationException(
                "Mad Dog damage reduction must follow the seven-rank Barbarian schedule.");
        }
    }

    private static void ConfigurePackRagerDamageReduction(BlueprintFeature baseDamageReduction) {
        var packRager = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.PackRagerArchetype);

        // Pack Rager has no replacement DR feature; clearing its old exclusions makes it
        // inherit the class progression at 1/4/7/10/13/16/19 exactly like a Barbarian.
        RemoveFeature(packRager.AddFeatures, baseDamageReduction);
        RemoveFeature(packRager.RemoveFeatures, baseDamageReduction);

        if (CountFeature(packRager.AddFeatures, baseDamageReduction) != 0 ||
            CountFeature(packRager.RemoveFeatures, baseDamageReduction) != 0) {
            throw new InvalidOperationException(
                "Pack Rager still overrides the standard Barbarian DR schedule.");
        }
    }

    private static void ConfigureBeastkinBerserker() {
        var fastMovementIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.BarbarianFastMovement).Icon;

        BuffConfigurator.New(
                "ClassesRebornFeralSwiftnessBuff",
                BlueprintIds.FeralSwiftnessBuff)
            .SetDisplayName("ClassesReborn.FeralSwiftness.Name")
            .SetDescription("ClassesReborn.FeralSwiftness.Description")
            .SetIcon(fastMovementIcon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new AddStatBonus {
                Stat = StatType.Speed,
                Value = 10,
                Descriptor = ModifierDescriptor.UntypedStackable,
            })
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornFeralSwiftness",
                BlueprintIds.FeralSwiftnessFeature)
            .SetDisplayName("ClassesReborn.FeralSwiftness.Name")
            .SetDescription("ClassesReborn.FeralSwiftness.Description")
            .SetIcon(fastMovementIcon)
            .AddComponent(new BuffExtraEffects {
                m_CheckedBuffList = new[] {
                    BlueprintTool.GetRef<BlueprintBuffReference>(
                        BlueprintIds.FeralTransformationIBuff),
                    BlueprintTool.GetRef<BlueprintBuffReference>(
                        BlueprintIds.FeralTransformationIIBuff),
                    BlueprintTool.GetRef<BlueprintBuffReference>(
                        BlueprintIds.FeralTransformationIIIBuff),
                },
                m_ExtraEffectBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.FeralSwiftnessBuff),
            })
            .Configure();

        var beastkinBerserker = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.BeastkinBerserkerArchetype);
        var feralSwiftness = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FeralSwiftnessFeature);

        RemoveFeature(beastkinBerserker.AddFeatures, feralSwiftness);
        RemoveFeature(beastkinBerserker.RemoveFeatures, feralSwiftness);

        var addFeatures = beastkinBerserker.AddFeatures?.ToList() ?? new List<LevelEntry>();
        var levelOneEntry = addFeatures.FirstOrDefault(entry => entry.Level == 1);
        if (levelOneEntry == null) {
            levelOneEntry = new LevelEntry { Level = 1, m_Features = new() };
            addFeatures.Add(levelOneEntry);
        }

        levelOneEntry.m_Features ??= new();
        levelOneEntry.m_Features.Add(
            BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                BlueprintIds.FeralSwiftnessFeature));
        beastkinBerserker.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        var configuredEntries = beastkinBerserker.AddFeatures.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feralSwiftness) ?? 0);
        var configuredAtLevelOne = beastkinBerserker.AddFeatures
            .Where(entry => entry.Level == 1)
            .Sum(entry =>
                entry.m_Features?.Count(reference => reference?.Get() == feralSwiftness) ?? 0);
        if (configuredEntries != 1 || configuredAtLevelOne != 1) {
            throw new InvalidOperationException(
                "Feral Swiftness must be granted exactly once at Beastkin Berserker level 1.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> levelEntries,
        BlueprintFeature feature) {
        return levelEntries?.Sum(levelEntry =>
            levelEntry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
    }

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> levelEntries,
        BlueprintFeature feature,
        int level) {
        return levelEntries?.Where(levelEntry => levelEntry.Level == level).Sum(levelEntry =>
            levelEntry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
    }

    private static LevelEntry GetOrCreateLevelEntry(
        List<LevelEntry> levelEntries,
        int level) {
        var levelEntry = levelEntries.FirstOrDefault(entry => entry.Level == level);
        if (levelEntry == null) {
            levelEntry = new LevelEntry { Level = level, m_Features = new() };
            levelEntries.Add(levelEntry);
        }

        levelEntry.m_Features ??= new();
        return levelEntry;
    }

    private static void RemoveFeature(
        IEnumerable<LevelEntry> levelEntries,
        BlueprintFeature feature) {
        if (levelEntries == null) {
            return;
        }

        foreach (var levelEntry in levelEntries) {
            levelEntry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }
}
