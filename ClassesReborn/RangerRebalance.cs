using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class RangerRebalance {
    private static readonly int[] FavoriteTerrainLevels = { 3, 6, 9, 13, 16, 19 };

    private static readonly (int Level, string SelectionId)[] GeneralSchedule = {
        (2, BlueprintIds.RangerStyleSelection2),
        (4, BlueprintIds.RangerStyleSelection2),
        (6, BlueprintIds.RangerStyleSelection6),
        (8, BlueprintIds.RangerStyleSelection6),
        (10, BlueprintIds.RangerStyleSelection10),
        (12, BlueprintIds.RangerStyleSelection10),
        (14, BlueprintIds.RangerStyleSelection10),
        (16, BlueprintIds.RangerStyleSelection10),
        (18, BlueprintIds.RangerStyleSelection10),
    };

    private static readonly (int Level, string SelectionId)[] ArcherySchedule = {
        (2, BlueprintIds.RangerStyleArcherySelection2),
        (4, BlueprintIds.RangerStyleArcherySelection2),
        (6, BlueprintIds.RangerStyleArcherySelection6),
        (8, BlueprintIds.RangerStyleArcherySelection6),
        (10, BlueprintIds.RangerStyleArcherySelection10),
        (12, BlueprintIds.RangerStyleArcherySelection10),
        (14, BlueprintIds.RangerStyleArcherySelection10),
        (16, BlueprintIds.RangerStyleArcherySelection10),
        (18, BlueprintIds.RangerStyleArcherySelection10),
    };

    internal static void Configure() {
        var rangerClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.RangerClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.RangerProgression);
        ConfigureMasterHunter();
        ConfigureFavoriteTerrain(rangerClass, progression);
        var generalSelections = GeneralSchedule
            .Select(entry => entry.SelectionId)
            .Distinct()
            .ToDictionary(
                id => id,
                BlueprintTool.Get<BlueprintFeatureSelection>);

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        foreach (var entry in GeneralSchedule) {
            AddFeature(
                levelEntries,
                entry.Level,
                generalSelections[entry.SelectionId]);
        }
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        var archerySelections = ArcherySchedule
            .Select(entry => entry.SelectionId)
            .Distinct()
            .ToDictionary(
                id => id,
                BlueprintTool.Get<BlueprintFeatureSelection>);
        var fixedArcheryArchetypes = new[] {
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.NomadArchetype),
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.StormwalkerArchetype),
        };
        foreach (var archetype in fixedArcheryArchetypes) {
            ConfigureFixedArcheryArchetype(
                archetype,
                generalSelections,
                archerySelections);
        }

        ConfigureNomadExceptionalBreed();
        ConfigureStormwalkerRetainedFeatures(progression);
        ConfigureStormwalkerWindTreaderAndResistance();
        ConfigureFlamewardenEvasion(progression);
        ConfigureEspionageExpert();

        ValidateSchedule(
            progression.LevelEntries,
            GeneralSchedule,
            generalSelections,
            "Ranger");
        foreach (var archetype in fixedArcheryArchetypes) {
            ValidateSchedule(
                archetype.RemoveFeatures,
                GeneralSchedule,
                generalSelections,
                $"{archetype.name} general-style replacement");
            ValidateSchedule(
                archetype.AddFeatures,
                ArcherySchedule,
                archerySelections,
                $"{archetype.name} fixed Archery style");
        }
    }

    private static void ConfigureFavoriteTerrain(
        BlueprintCharacterClass rangerClass,
        BlueprintProgression progression) {
        var initialSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.RangerFavoriteTerrainSelection);
        var rankUpSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.RangerFavoriteTerrainRankUpSelection);
        var replacingArchetypes = rangerClass.Archetypes
            .Where(archetype =>
                CountFeature(archetype.RemoveFeatures, initialSelection) != 0 ||
                CountFeature(archetype.RemoveFeatures, rankUpSelection) != 0)
            .ToArray();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, initialSelection);
        RemoveFeature(levelEntries, rankUpSelection);
        AddFeature(levelEntries, FavoriteTerrainLevels[0], initialSelection);
        foreach (var level in FavoriteTerrainLevels.Skip(1)) {
            AddFeature(levelEntries, level, rankUpSelection);
        }
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        foreach (var archetype in replacingArchetypes) {
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            RemoveFeature(removals, initialSelection);
            RemoveFeature(removals, rankUpSelection);
            AddFeature(removals, FavoriteTerrainLevels[0], initialSelection);
            foreach (var level in FavoriteTerrainLevels.Skip(1)) {
                AddFeature(removals, level, rankUpSelection);
            }
            archetype.RemoveFeatures = removals
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        var retainedArchetypes = rangerClass.Archetypes
            .Except(replacingArchetypes)
            .ToArray();
        if (CountFeature(progression.LevelEntries, initialSelection) != 1 ||
            CountFeatureAtLevel(
                progression.LevelEntries,
                initialSelection,
                FavoriteTerrainLevels[0]) != 1 ||
            CountFeature(progression.LevelEntries, rankUpSelection) != 5 ||
            FavoriteTerrainLevels.Skip(1).Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    rankUpSelection,
                    level) != 1) ||
            replacingArchetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, initialSelection) != 1 ||
                CountFeatureAtLevel(
                    archetype.RemoveFeatures,
                    initialSelection,
                    FavoriteTerrainLevels[0]) != 1 ||
                CountFeature(archetype.RemoveFeatures, rankUpSelection) != 5 ||
                FavoriteTerrainLevels.Skip(1).Any(level =>
                    CountFeatureAtLevel(
                        archetype.RemoveFeatures,
                        rankUpSelection,
                        level) != 1)) ||
            retainedArchetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, initialSelection) != 0 ||
                CountFeature(archetype.RemoveFeatures, rankUpSelection) != 0)) {
            throw new InvalidOperationException(
                "Ranger Favored Terrain must be selected at levels 3/6/9/13/16/19, with matching removals for archetypes that replace it.");
        }
    }

    private static void ConfigureMasterHunter() {
        var instantEnemy = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.InstantEnemyAbility);
        var resource = AbilityResourceConfigurator.New(
                "ClassesRebornRangerMasterHunterInstantEnemyResource",
                BlueprintIds.RangerMasterHunterInstantEnemyResource)
            .SetLocalizedName("ClassesReborn.RangerMasterHunterInstantEnemy.Name")
            .SetLocalizedDescription(
                "ClassesReborn.RangerMasterHunterInstantEnemy.Description")
            .SetIcon(instantEnemy.Icon)
            .SetMax(5)
            .Configure();

        var ability = AbilityConfigurator.New(
                "ClassesRebornRangerMasterHunterInstantEnemyAbility",
                BlueprintIds.RangerMasterHunterInstantEnemyAbility)
            .CopyFrom(BlueprintIds.InstantEnemyAbility)
            .SetDisplayName("ClassesReborn.RangerMasterHunterInstantEnemy.Name")
            .SetDescription(
                "ClassesReborn.RangerMasterHunterInstantEnemy.Description")
            .SetIcon(instantEnemy.Icon)
            .SetType(AbilityType.Supernatural)
            .SetRange(instantEnemy.Range)
            .SetActionType(instantEnemy.ActionType)
            .SetIsFullRoundAction(instantEnemy.IsFullRoundAction)
            .AllowTargeting(
                point: instantEnemy.CanTargetPoint,
                enemies: instantEnemy.CanTargetEnemies,
                friends: instantEnemy.CanTargetFriends,
                self: instantEnemy.CanTargetSelf)
            .AddAbilityResourceLogic(
                amount: 1,
                isSpendResource: true,
                requiredResource:
                    BlueprintIds.RangerMasterHunterInstantEnemyResource)
            .Configure();

        ability.IgnoreMinimalRangeLimit = instantEnemy.IgnoreMinimalRangeLimit;
        ability.CustomRange = instantEnemy.CustomRange;
        ability.ShowNameForVariant = instantEnemy.ShowNameForVariant;
        ability.OnlyForAllyCaster = instantEnemy.OnlyForAllyCaster;
        ability.ShouldTurnToTarget = instantEnemy.ShouldTurnToTarget;
        ability.SpellResistance = instantEnemy.SpellResistance;
        ability.IgnoreSpellResistanceForAlly =
            instantEnemy.IgnoreSpellResistanceForAlly;
        ability.NeedEquipWeapons = instantEnemy.NeedEquipWeapons;
        ability.UseCurrentWeaponAsReasonItem =
            instantEnemy.UseCurrentWeaponAsReasonItem;
        ability.NotOffensive = instantEnemy.NotOffensive;
        ability.EffectOnAlly = instantEnemy.EffectOnAlly;
        ability.EffectOnEnemy = instantEnemy.EffectOnEnemy;
        ability.Animation = instantEnemy.Animation;
        ability.HasFastAnimation = instantEnemy.HasFastAnimation;
        ability.MinimalTransitionOut = instantEnemy.MinimalTransitionOut;
        ability.LocalizedDuration = instantEnemy.LocalizedDuration;
        ability.LocalizedSavingThrow = instantEnemy.LocalizedSavingThrow;
        ability.ResourceAssetIds = instantEnemy.ResourceAssetIds?.ToArray();

        var masterHunter = FeatureConfigurator.For(
                BlueprintIds.RangerMasterHunterFeature)
            .SetDescription("ClassesReborn.RangerMasterHunter.Description")
            .AddFacts(new() {
                BlueprintIds.RangerMasterHunterInstantEnemyAbility,
            })
            .AddAbilityResources(
                amount: 0,
                resource: BlueprintIds.RangerMasterHunterInstantEnemyResource,
                restoreAmount: true,
                restoreOnLevelUp: true)
            .AddComponent(new MasterHunterFavoredEnemyDefense())
            .Configure();

        ValidateMasterHunter(masterHunter, instantEnemy, ability, resource);
    }

    private static void ValidateMasterHunter(
        BlueprintFeature masterHunter,
        BlueprintAbility instantEnemy,
        BlueprintAbility ability,
        BlueprintAbilityResource resource) {
        var addedFacts = masterHunter.GetComponents<AddFacts>()
            .SelectMany(component =>
                component.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
            .Count(reference => reference?.Get() == ability);
        var resourceGrants = masterHunter.GetComponents<AddAbilityResources>()
            .Where(component => component.m_Resource?.Get() == resource)
            .ToArray();
        var resourceLogic = ability.GetComponents<AbilityResourceLogic>()
            .Where(component => component.m_RequiredResource?.Get() == resource)
            .ToArray();
        var defenses = masterHunter
            .GetComponents<MasterHunterFavoredEnemyDefense>()
            .ToArray();

        if (!resource.m_UseMax ||
            resource.m_Max != 5 ||
            addedFacts < 1 ||
            resourceGrants.Length < 1 ||
            resourceLogic.Length < 1 ||
            resourceLogic.Any(component =>
                component.Amount != 1 || !component.m_IsSpendResource) ||
            defenses.Length < 1 ||
            ability.Type != AbilityType.Supernatural) {
            throw new InvalidOperationException(
                $"Ranger Master Hunter must grant five supernatural Instant Enemy uses and +2 defenses against favored enemies (useFixedMax={resource.m_UseMax}, fixedMax={resource.m_Max}, facts={addedFacts}, grants={resourceGrants.Length}, logic={resourceLogic.Length}, defenses={defenses.Length}, type={ability.Type}).");
        }
    }

    private static void ConfigureNomadExceptionalBreed() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.NomadArchetype);
        var mountSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.NomadMountSelection);
        var icon = mountSelection.Icon;

        var level7Pet = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel7PetFeature",
                BlueprintIds.ExceptionalBreedLevel7PetFeature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level7.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Constitution,
                value: 4)
            .Configure();
        var level7 = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel7Feature",
                BlueprintIds.ExceptionalBreedLevel7Feature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level7.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddFeatureToPet(
                BlueprintIds.ExceptionalBreedLevel7PetFeature,
                PetType.AnimalCompanion)
            .Configure();

        var level12Pet = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel12PetFeature",
                BlueprintIds.ExceptionalBreedLevel12PetFeature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level12.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Speed,
                value: 10)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Dexterity,
                value: 4)
            .Configure();
        var level12 = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel12Feature",
                BlueprintIds.ExceptionalBreedLevel12Feature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level12.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddFeatureToPet(
                BlueprintIds.ExceptionalBreedLevel12PetFeature,
                PetType.AnimalCompanion)
            .Configure();

        var level17Pet = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel17PetFeature",
                BlueprintIds.ExceptionalBreedLevel17PetFeature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level17.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Strength,
                value: 4)
            .AddComponent(new BuffExtraAttack {
                Number = 1,
                Haste = false,
            })
            .Configure();
        var level17 = FeatureConfigurator.New(
                "ClassesRebornExceptionalBreedLevel17Feature",
                BlueprintIds.ExceptionalBreedLevel17Feature)
            .SetDisplayName("ClassesReborn.ExceptionalBreed.Name")
            .SetDescription("ClassesReborn.ExceptionalBreed.Level17.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddFeatureToPet(
                BlueprintIds.ExceptionalBreedLevel17PetFeature,
                PetType.AnimalCompanion)
            .Configure();

        var additions = archetype.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(additions, 7, level7);
        AddFeature(additions, 12, level12);
        AddFeature(additions, 17, level17);
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        ValidateExceptionalBreed(
            archetype,
            level7,
            level7Pet,
            level12,
            level12Pet,
            level17,
            level17Pet);
    }

    private static void ValidateExceptionalBreed(
        BlueprintArchetype archetype,
        BlueprintFeature level7,
        BlueprintFeature level7Pet,
        BlueprintFeature level12,
        BlueprintFeature level12Pet,
        BlueprintFeature level17,
        BlueprintFeature level17Pet) {
        var grants = new[] {
            (Level: 7, Feature: level7, PetFeature: level7Pet),
            (Level: 12, Feature: level12, PetFeature: level12Pet),
            (Level: 17, Feature: level17, PetFeature: level17Pet),
        };
        var petLinksAreValid = grants.All(grant => {
            var components = grant.Feature
                .GetComponents<AddFeatureToPet>()
                .ToArray();
            return components.Length == 1 &&
                   components[0].m_Feature?.Get() == grant.PetFeature &&
                   components[0].m_PetType == PetType.AnimalCompanion;
        });
        var level7Bonuses = level7Pet.GetComponents<AddStatBonus>().ToArray();
        var level12Bonuses = level12Pet.GetComponents<AddStatBonus>().ToArray();
        var level17Bonuses = level17Pet.GetComponents<AddStatBonus>().ToArray();
        var extraAttacks = level17Pet.GetComponents<BuffExtraAttack>().ToArray();

        if (grants.Any(grant =>
                CountFeature(archetype.AddFeatures, grant.Feature) != 1 ||
                CountFeatureAtLevel(
                    archetype.AddFeatures,
                    grant.Feature,
                    grant.Level) != 1) ||
            !petLinksAreValid ||
            level7Bonuses.Length != 2 ||
            level7Bonuses.Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.NaturalArmor) != 1 ||
            level7Bonuses.Count(component =>
                component.Stat == StatType.Constitution &&
                component.Value == 4 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            level12Bonuses.Length != 2 ||
            level12Bonuses.Count(component =>
                component.Stat == StatType.Speed &&
                component.Value == 10 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            level12Bonuses.Count(component =>
                component.Stat == StatType.Dexterity &&
                component.Value == 4 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            level17Bonuses.Length != 1 ||
            level17Bonuses[0].Stat != StatType.Strength ||
            level17Bonuses[0].Value != 4 ||
            level17Bonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            extraAttacks.Length != 1 ||
            extraAttacks[0].Number != 1 ||
            extraAttacks[0].Haste) {
            throw new InvalidOperationException(
                "Nomad Exceptional Breed levels 7/12/17 or horse bonuses are invalid.");
        }
    }

    private static void ConfigureFlamewardenEvasion(
        BlueprintProgression progression) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.FlamewardenArchetype);
        var evasion = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerEvasion);
        var improvedEvasion = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerImprovedEvasion);
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        var otherRemovalsBefore = CountOtherFeatures(
            removals,
            evasion,
            improvedEvasion);

        foreach (var entry in removals) {
            entry.m_Features?.RemoveAll(reference =>
                reference?.Get() == evasion ||
                reference?.Get() == improvedEvasion);
        }
        removals.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(archetype.RemoveFeatures, evasion) != 0 ||
            CountFeature(archetype.RemoveFeatures, improvedEvasion) != 0 ||
            CountOtherFeatures(
                archetype.RemoveFeatures,
                evasion,
                improvedEvasion) != otherRemovalsBefore ||
            CountFeatureAtLevel(progression.LevelEntries, evasion, 9) != 1 ||
            CountFeatureAtLevel(
                progression.LevelEntries,
                improvedEvasion,
                16) != 1) {
            throw new InvalidOperationException(
                "Flamewarden must retain Ranger Evasion at level 9 and Improved Evasion at level 16 without changing other archetype replacements.");
        }
    }

    private static void ConfigureStormwalkerRetainedFeatures(
        BlueprintProgression progression) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.StormwalkerArchetype);
        var quarry = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerQuarry);
        var improvedEvasion = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerImprovedEvasion);
        var improvedQuarry = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RangerImprovedQuarry);
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        var otherRemovalsBefore = CountOtherFeatures(
            removals,
            quarry,
            improvedEvasion,
            improvedQuarry);

        foreach (var entry in removals) {
            entry.m_Features?.RemoveAll(reference =>
                reference?.Get() == quarry ||
                reference?.Get() == improvedEvasion ||
                reference?.Get() == improvedQuarry);
        }
        removals.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(archetype.RemoveFeatures, quarry) != 0 ||
            CountFeature(archetype.RemoveFeatures, improvedEvasion) != 0 ||
            CountFeature(archetype.RemoveFeatures, improvedQuarry) != 0 ||
            CountOtherFeatures(
                archetype.RemoveFeatures,
                quarry,
                improvedEvasion,
                improvedQuarry) != otherRemovalsBefore ||
            CountFeatureAtLevel(progression.LevelEntries, quarry, 11) != 1 ||
            CountFeatureAtLevel(
                progression.LevelEntries,
                improvedEvasion,
                16) != 1 ||
            CountFeatureAtLevel(
                progression.LevelEntries,
                improvedQuarry,
                19) != 1) {
            throw new InvalidOperationException(
                "Stormwalker must retain Ranger Quarry at level 11, Improved Evasion at level 16, and Improved Quarry at level 19 without changing other archetype replacements.");
        }
    }

    private static void ConfigureStormwalkerWindTreaderAndResistance() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.StormwalkerArchetype);
        var immunity = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.StormwalkerImmunity);

        var windTreaderAbility = AbilityConfigurator.For(
                BlueprintIds.StormwalkerWeaponAbility)
            .SetDescription("ClassesReborn.WindTreader.Description")
            .SetActionType(UnitCommand.CommandType.Swift)
            .SetIsFullRoundAction(false)
            .Configure();
        FeatureConfigurator.For(BlueprintIds.StormwalkerWeaponFeature)
            .SetDescription("ClassesReborn.WindTreader.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.StormwalkerWeaponBuff)
            .SetDescription("ClassesReborn.WindTreader.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.StormwalkerWeaponBurstBuff)
            .SetDescription("ClassesReborn.WindTreader.Description")
            .Configure();

        var stormResistance = FeatureConfigurator.New(
                "ClassesRebornStormwalkerResistanceFeature",
                BlueprintIds.StormwalkerResistanceFeature)
            .SetDisplayName("ClassesReborn.StormwalkerResistance.Name")
            .SetDescription("ClassesReborn.StormwalkerResistance.Description")
            .SetIcon(immunity.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new ResistEnergyContext {
                Value = ContextValues.Constant(10),
                Type = DamageEnergyType.Electricity,
            })
            .Configure();

        var additions = archetype.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(additions, 7, stormResistance);
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        var resistances = stormResistance
            .GetComponents<ResistEnergyContext>()
            .ToArray();
        if (windTreaderAbility.ActionType != UnitCommand.CommandType.Swift ||
            windTreaderAbility.IsFullRoundAction ||
            CountFeature(archetype.AddFeatures, stormResistance) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, stormResistance, 7) != 1 ||
            resistances.Length != 1 ||
            resistances[0].Type != DamageEnergyType.Electricity ||
            resistances[0].Value.Value != 10) {
            throw new InvalidOperationException(
                "Stormwalker Wind Treader must be a swift action and Storm Resistance must grant electricity resistance 10 exactly once at level 7.");
        }
    }

    private static void ConfigureEspionageExpert() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.MasterSpyArchetype);
        var sourceFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MasterSpyFeature);
        var sneakAttack = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.SneakAttackFeature);
        var icon = sourceFeature.Icon ?? sneakAttack.Icon;

        var halfCharismaRank = ContextRankConfigs
            .StatBonus(StatType.Charisma, min: 0)
            .WithDivStepProgression(2);
        var calculatedAssault = FeatureConfigurator.New(
                "ClassesRebornCalculatedAssaultFeature",
                BlueprintIds.CalculatedAssaultFeature)
            .SetDisplayName("ClassesReborn.CalculatedAssault.Name")
            .SetDescription("ClassesReborn.CalculatedAssault.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddContextRankConfig(halfCharismaRank)
            .AddContextStatBonus(
                StatType.AdditionalAttackBonus,
                ContextValues.Rank(),
                ModifierDescriptor.Morale)
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Charisma,
            })
            .Configure();

        var fullCharismaRank = ContextRankConfigs.StatBonus(
            StatType.Charisma,
            min: 0);
        var calculatedAssaultMastery = FeatureConfigurator.New(
                "ClassesRebornCalculatedAssaultMasteryFeature",
                BlueprintIds.CalculatedAssaultMasteryFeature)
            .SetDisplayName("ClassesReborn.CalculatedAssault.Mastery.Name")
            .SetDescription("ClassesReborn.CalculatedAssault.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddContextRankConfig(fullCharismaRank)
            .AddContextStatBonus(
                StatType.AdditionalAttackBonus,
                ContextValues.Rank(),
                ModifierDescriptor.Morale)
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Charisma,
            })
            .Configure();

        var additions = archetype.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(additions, 7, sneakAttack);
        AddFeature(additions, 12, calculatedAssault);
        AddFeature(additions, 14, sneakAttack);
        AddFeature(additions, 20, calculatedAssaultMastery);
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(archetype.AddFeatures, sneakAttack) != 2 ||
            CountFeatureAtLevel(archetype.AddFeatures, sneakAttack, 7) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, sneakAttack, 14) != 1 ||
            CountFeature(archetype.AddFeatures, calculatedAssault) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, calculatedAssault, 12) != 1 ||
            CountFeature(archetype.AddFeatures, calculatedAssaultMastery) != 1 ||
            CountFeatureAtLevel(
                archetype.AddFeatures,
                calculatedAssaultMastery,
                20) != 1) {
            throw new InvalidOperationException(
                "Espionage Expert must gain Sneak Attack at levels 7/14 and Calculated Assault at levels 12/20.");
        }

        ValidateCalculatedAssault(
            calculatedAssault,
            ContextRankProgression.DivStep,
            2,
            "level-12");
        ValidateCalculatedAssault(
            calculatedAssaultMastery,
            ContextRankProgression.AsIs,
            0,
            "level-20");
    }

    private static void ValidateCalculatedAssault(
        BlueprintFeature feature,
        ContextRankProgression progression,
        int step,
        string label) {
        var rankConfigs = feature.GetComponents<ContextRankConfig>().ToArray();
        var bonuses = feature.GetComponents<AddContextStatBonus>().ToArray();
        var recalculations = feature
            .GetComponents<RecalculateOnStatChange>()
            .ToArray();
        if (rankConfigs.Length != 1 ||
            rankConfigs[0].m_BaseValueType != ContextRankBaseValueType.StatBonus ||
            rankConfigs[0].m_Stat != StatType.Charisma ||
            rankConfigs[0].m_Progression != progression ||
            (progression == ContextRankProgression.DivStep &&
             rankConfigs[0].m_StepLevel != step) ||
            !rankConfigs[0].m_UseMin ||
            rankConfigs[0].m_Min != 0 ||
            bonuses.Length != 1 ||
            bonuses[0].Stat != StatType.AdditionalAttackBonus ||
            bonuses[0].Descriptor != ModifierDescriptor.Morale ||
            recalculations.Length != 1 ||
            recalculations[0].Stat != StatType.Charisma) {
            throw new InvalidOperationException(
                $"Espionage Expert Calculated Assault {label} configuration is invalid.");
        }
    }

    private static void ConfigureFixedArcheryArchetype(
        BlueprintArchetype archetype,
        IReadOnlyDictionary<string, BlueprintFeatureSelection> generalSelections,
        IReadOnlyDictionary<string, BlueprintFeatureSelection> archerySelections) {
        var additions = archetype.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();

        foreach (var entry in GeneralSchedule) {
            AddFeature(
                removals,
                entry.Level,
                generalSelections[entry.SelectionId]);
        }
        foreach (var entry in ArcherySchedule) {
            AddFeature(
                additions,
                entry.Level,
                archerySelections[entry.SelectionId]);
        }

        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();
    }

    private static void ValidateSchedule(
        IEnumerable<LevelEntry> entries,
        IReadOnlyCollection<(int Level, string SelectionId)> schedule,
        IReadOnlyDictionary<string, BlueprintFeatureSelection> selections,
        string label) {
        foreach (var pair in selections) {
            var expectedLevels = schedule
                .Where(entry => entry.SelectionId == pair.Key)
                .Select(entry => entry.Level)
                .ToArray();
            if (CountFeature(entries, pair.Value) != expectedLevels.Length ||
                expectedLevels.Any(level =>
                    CountFeatureAtLevel(entries, pair.Value, level) != 1) ||
                entries.Any(entry =>
                    !expectedLevels.Contains(entry.Level) &&
                    CountFeatureAtLevel(entries, pair.Value, entry.Level) != 0)) {
                throw new InvalidOperationException(
                    $"{label} Combat Style schedule is invalid for {pair.Value.name}.");
            }
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
        List<LevelEntry> entries,
        BlueprintFeature feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference =>
                reference?.Get() == feature);
        }
        entries.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
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

    private static int CountOtherFeatures(
        IEnumerable<LevelEntry> entries,
        params BlueprintFeature[] excludedFeatures) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() is BlueprintFeature feature &&
            !excludedFeatures.Contains(feature)) ?? 0) ?? 0;
}
