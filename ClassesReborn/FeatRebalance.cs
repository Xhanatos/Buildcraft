using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

namespace ClassesReborn;

internal static partial class FeatRebalance {
    private static readonly string[] ChannelEnergyFeatures = {
        "a79013ff4bcd4864cb669622a29ddafb", // Cleric
        "bd588bc544d2f8547a02bb82ad9f466a", // Warpriest
        "cb6d55dda5ab906459d18a435994a760", // Paladin
        "b8ec9dccc0e7ef74fb4072b0679c2aec", // Shaman Life Spirit
        "7d49d7f590dc9a948b3bd1c8b7979854", // Empyreal Sorcerer
        "332c43d3f25fb9a429a42067142c41d5", // Shieldbearer Warpriest
        "a9ab1bbc79ecb174d9a04699986ce8d5", // Hospitaler
        "4bf9a9afadca5304e89bf52f2ac2d236", // Oracle Life revelation
        "b40316f05d4772e4894688e6743602bd", // Hex Channeler Witch
        "b423fbf947bc51344bac21752c47471c", // Hex Channeler positive
        "7c8d5e2ab326fdb4cabafc1c84a5c8e2", // Hex Channeler negative
        "eb388d17f07e0b44d9f83ada0148cc69", // Witch Doctor
    };

    private static readonly string[] ManeuverAbilities = {
        "6fd05c4ecfebd6f4d873325de442fc17", // Trip
        "45d94c6db453cfc4a9b99b72d6afe6f6", // Disarm
        "fa9bfb9fd997faf49a108c4b17a00504", // Sunder Armor
        "7ab6f70c996fe9b4597b8332f0a3af5f", // Bull Rush
        "8b7364193036a8d4a80308fbe16c8187", // Dirty Trick: Blind
        "5f22daa9460c5844992bf751e1e8eb78", // Dirty Trick: Entangle
        "4921b86ee42c0b54e87a2f9b20521ab9", // Dirty Trick: Sicken
    };

    private static readonly string[] ImprovedManeuverFeats = {
        "0f15c6f70d8fb2b49aa6cc24239cc5fa", // Greater Trip action unlock
        "25bc9c439ac44fd44ac3b1e58890916f", // Greater Disarm action unlock
        "9719015edcbf142409592e2cbaab7fe1", // Greater Sunder action unlock
        "b3614622866fe7046b787a548bbd7f59", // Greater Bull Rush action unlock
        "ed699d64870044b43bb5a7fbe3f29494", // Improved Dirty Trick
        "ed699d64870044b43bb5a7fbe3f29494",
        "ed699d64870044b43bb5a7fbe3f29494",
    };

    internal static void Configure() {
        if (Main.Settings.ExtraHexWitch || Main.Settings.ExtraHexShaman) {
            ConfigureExtraHex();
        }
        if (Main.Settings.ExtraRevelation) {
            ConfigureExtraRevelation();
        }
        if (Main.Settings.HorseMaster) {
            ConfigureHorseMaster();
        }
        if (Main.Settings.ErastilsBlessing) {
            ConfigureErastilsBlessing();
        }
        if (Main.Settings.GuidedHand) {
            ConfigureGuidedHand();
        }
        if (Main.Settings.Hurtful) {
            ConfigureHurtful();
        }
        if (Main.Settings.DirtyFighting) {
            ConfigureDirtyFighting();
        }
        if (Main.Settings.SplitHex) {
            ConfigureSplitHex();
        }
        if (Main.Settings.CursingGaze) {
            ConfigureCursingGaze();
        }
        if (Main.Settings.HexStrike) {
            ConfigureHexStrike();
        }
        if (Main.Settings.ShieldBrace) {
            ConfigureShieldBrace();
        }
        if (Main.Settings.RakingClaws) {
            ConfigureRakingClaws();
        }
        ConfigureMythicMartialAbilities();
        ConfigureExpandedFeats();
        ValidateGeneralFeatRegistration();
    }

    private static void ConfigureRakingClaws() {
        var feat = FeatureConfigurator.For(BlueprintIds.RakingClawsFeature)
            .RemoveComponents(component =>
                component is PrerequisiteFeaturesFromList ||
                component is PrerequisiteStatValue prerequisite &&
                prerequisite.Stat == StatType.BaseAttackBonus)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 4)
            .Configure();

        var baseAttackPrerequisites = feat
            .GetComponents<PrerequisiteStatValue>()
            .Where(prerequisite =>
                prerequisite.Stat == StatType.BaseAttackBonus)
            .ToArray();
        if (baseAttackPrerequisites.Length != 1 ||
            baseAttackPrerequisites[0].Value != 4 ||
            feat.GetComponents<PrerequisiteFeaturesFromList>().Any()) {
            throw new InvalidOperationException(
                "Raking Claws must require only base attack bonus +4.");
        }
    }

    private static void ConfigureExtraHex() {
        if (Main.Settings.ExtraHexWitch) {
            CreateExtraSelectionFeat(
                BlueprintIds.WitchHexSelection,
                "ClassesRebornExtraHexWitch",
                BlueprintIds.ExtraHexWitchFeat,
                "ClassesReborn.ExtraHexWitch.Name",
                "ClassesReborn.ExtraHexWitch.Description");
            RemoveFromSelectionIfPresent(
                BlueprintIds.BasicFeatSelection,
                "d0b4c8245d504b8c9c6d3fccc1f8c5b6");
        }
        if (Main.Settings.ExtraHexShaman) {
            CreateExtraSelectionFeat(
                BlueprintIds.ShamanHexSelection,
                "ClassesRebornExtraHexShaman",
                BlueprintIds.ExtraHexShamanFeat,
                "ClassesReborn.ExtraHexShaman.Name",
                "ClassesReborn.ExtraHexShaman.Description");
            RemoveFromSelectionIfPresent(
                BlueprintIds.BasicFeatSelection,
                "b6054088b4ab4be286724127cbf48b35");
        }
    }

    private static void ConfigureExtraRevelation() {
        CreateExtraSelectionFeat(
            BlueprintIds.OracleRevelationSelection,
            "ClassesRebornExtraRevelation",
            BlueprintIds.ExtraRevelationFeat,
            "ClassesReborn.ExtraRevelation.Name",
            "ClassesReborn.ExtraRevelation.Description");
    }

    private static BlueprintFeatureSelection CreateExtraSelectionFeat(
        string sourceId,
        string name,
        string guid,
        string displayName,
        string description) {
        var source = BlueprintTool.Get<BlueprintFeatureSelection>(sourceId);
        var selection = FeatureSelectionConfigurator.New(name, guid)
            .SetDisplayName(displayName)
            .SetDescription(description)
            .SetIcon(source.Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetRanks(20)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(true)
            .AddPrerequisiteFeature(source)
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific)
            .Configure();
        selection.m_Features = source.m_Features.ToArray();
        selection.m_AllFeatures = source.m_AllFeatures.ToArray();
        selection.Mode = source.Mode;
        selection.IgnorePrerequisites = false;
        AddToSelection(BlueprintIds.BasicFeatSelection, selection);
        return selection;
    }

    private static void ConfigureHorseMaster() {
        var mountSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CavalierMountSelection);
        var expertTrainer = FeatureConfigurator.New(
                "ClassesRebornExpertTrainer",
                BlueprintIds.ExpertTrainerFeature)
            .SetDisplayName("ClassesReborn.ExpertTrainer.Name")
            .SetDescription("ClassesReborn.ExpertTrainer.Description")
            .SetIcon(mountSelection.Icon)
            .SetIsClassFeature(true)
            .Configure();
        var rank = FeatureConfigurator.New(
                "ClassesRebornHorseMasterRank",
                BlueprintIds.HorseMasterRank)
            .SetDisplayName("ClassesReborn.HorseMaster.Name")
            .SetDescription("ClassesReborn.Empty.Description")
            .SetRanks(20)
            .SetIsClassFeature(true)
            .SetHideInUI(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new ConstrainFeatureRank {
                TargetFeature = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.AnimalCompanionRank),
            })
            .Configure();

        var horseMaster = ProgressionConfigurator.New(
                "ClassesRebornHorseMaster",
                BlueprintIds.HorseMasterFeat)
            .SetDisplayName("ClassesReborn.HorseMaster.Name")
            .SetDescription("ClassesReborn.HorseMaster.Description")
            .SetIcon(mountSelection.Icon)
            .SetGroups(
                FeatureGroup.Feat,
                FeatureGroup.CombatFeat,
                FeatureGroup.MountedCombatFeat)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(true)
            .AddPrerequisiteStatValue(StatType.SkillMobility, 6)
            .AddPrerequisitePet()
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific)
            .Configure();
        horseMaster.LevelEntries = Enumerable.Range(1, 20)
            .Select(level => new LevelEntry {
                Level = level,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    rank.ToReference<BlueprintFeatureBaseReference>(),
                },
            })
            .ToArray();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.CavalierProgression);
        var levels = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(levels, expertTrainer);
        AddFeature(levels, 4, expertTrainer);
        progression.LevelEntries = levels.OrderBy(entry => entry.Level).ToArray();

        foreach (var archetypeId in new[] {
                     BlueprintIds.BeastRiderArchetype,
                     BlueprintIds.DiscipleOfThePikeArchetype,
                     BlueprintIds.KnightOfTheWallArchetype,
                 }) {
            var archetype = BlueprintTool.Get<BlueprintArchetype>(archetypeId);
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            RemoveFeature(removals, expertTrainer);
            AddFeature(removals, 4, expertTrainer);
            archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
        }

        AddAsFeat(horseMaster, combatFeat: true);
    }

    private static void ConfigureErastilsBlessing() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            "379c0da9f384e7547a70c259445377f5").Icon;
        var feat = FeatureConfigurator.New(
                "ClassesRebornErastilsBlessing",
                BlueprintIds.ErastilsBlessingFeat)
            .SetDisplayName("ClassesReborn.ErastilsBlessing.Name")
            .SetDescription("ClassesReborn.ErastilsBlessing.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteFeature(BlueprintIds.ErastilFeature)
            .AddPrerequisiteParametrizedWeaponFeature(
                BlueprintIds.WeaponFocus,
                WeaponCategory.Longbow)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
            .AddComponent(new WeaponCategoryAttackStatReplacement {
                ReplacementStat = StatType.Wisdom,
                Categories = new[] {
                    WeaponCategory.Longbow,
                    WeaponCategory.Shortbow,
                },
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureGuidedHand() {
        var sourceController = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.WarpriestDeitySacredWeaponFeature);
        var mappings = sourceController.GetComponents<AddFeatureIfHasFact>()
            .SelectMany(source => {
                var deity = source.m_CheckedFact?.Get();
                var feature = source.m_Feature?.Get() as BlueprintFeature;
                return feature?.GetComponents<SacredWeaponFavoriteDamageOverride>()
                    .Select(component => (Deity: deity, component.Category))
                    ?? Enumerable.Empty<(BlueprintUnitFact Deity, WeaponCategory Category)>();
            })
            .Where(mapping => mapping.Deity != null)
            .Distinct()
            .ToArray();
        if (mappings.Length == 0) {
            throw new InvalidOperationException(
                "Guided Hand could not read the deity favored-weapon map.");
        }

        var configurator = FeatureConfigurator.New(
                "ClassesRebornGuidedHand",
                BlueprintIds.GuidedHandFeat)
            .SetDisplayName("ClassesReborn.GuidedHand.Name")
            .SetDescription("ClassesReborn.GuidedHand.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.WarpriestDeitySacredWeaponFeature).Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetReapplyOnLevelUp(true)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.ClassSpecific)
            .AddComponent(new DeityFavoredWeaponAttackStatReplacement {
                ReplacementStat = StatType.Wisdom,
                m_Deities = mappings.Select(mapping =>
                        mapping.Deity.ToReference<BlueprintUnitFactReference>())
                    .ToArray(),
                Categories = mappings.Select(mapping => mapping.Category).ToArray(),
            });
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = ChannelEnergyFeatures.Select(
                    BlueprintTool.GetRef<BlueprintFeatureReference>)
                .ToArray(),
            Amount = 1,
            Group = Prerequisite.GroupType.All,
        });
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = mappings.Select(mapping => mapping.Deity)
                .Distinct()
                .Select(deity => deity.ToReference<BlueprintFeatureReference>())
                .ToArray(),
            Amount = 1,
            Group = Prerequisite.GroupType.All,
        });
        var feat = configurator.Configure();
        AddAsFeat(feat);
    }

    private static void ConfigureHurtful() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.PowerAttack).Icon;
        BuffConfigurator.New("ClassesRebornHurtfulBuff", BlueprintIds.HurtfulBuff)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new HurtfulTrigger())
            .Configure();
        ActivatableAbilityConfigurator.New(
                "ClassesRebornHurtfulAbility",
                BlueprintIds.HurtfulAbility)
            .SetDisplayName("ClassesReborn.Hurtful.Name")
            .SetDescription("ClassesReborn.Hurtful.Description")
            .SetIcon(icon)
            .SetBuff(BlueprintIds.HurtfulBuff)
            .SetIsOnByDefault(true)
            .SetDoNotTurnOffOnRest(true)
            .SetDeactivateImmediately(true)
            .SetActivationType(AbilityActivationType.Immediately)
            .Configure();
        var feat = FeatureConfigurator.New(
                "ClassesRebornHurtful",
                BlueprintIds.HurtfulFeat)
            .SetDisplayName("ClassesReborn.Hurtful.Name")
            .SetDescription("ClassesReborn.Hurtful.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteStatValue(StatType.Strength, 13)
            .AddPrerequisiteFeature(BlueprintIds.PowerAttack)
            .AddFeatureTagsComponent(
                FeatureTag.Melee | FeatureTag.Attack | FeatureTag.Skills)
            .AddFacts(new() { BlueprintIds.HurtfulAbility })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureDirtyFighting() {
        var configurator = FeatureConfigurator.New(
                "ClassesRebornDirtyFighting",
                BlueprintIds.DirtyFightingFeat)
            .SetDisplayName("ClassesReborn.DirtyFighting.Name")
            .SetDescription("ClassesReborn.DirtyFighting.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.CombatExpertise).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddFeatureTagsComponent(FeatureTag.Melee | FeatureTag.Attack)
            .AddComponent(new FeatureForPrerequisite {
                FakeFact = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.CombatExpertise),
            })
            .AddComponent(new FeatureForPrerequisite {
                FakeFact = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.ImprovedUnarmedStrike),
            })
            .AddComponent(new ReplaceStatForPrerequisites {
                OldStat = StatType.Dexterity,
                SpecificNumber = 13,
                Policy = ReplaceStatForPrerequisites.StatReplacementPolicy.SpecificNumber,
            })
            .AddComponent(new ReplaceStatForPrerequisites {
                OldStat = StatType.Intelligence,
                SpecificNumber = 13,
                Policy = ReplaceStatForPrerequisites.StatReplacementPolicy.SpecificNumber,
            })
            .AddComponent(new DirtyFightingBonus {
                m_Maneuvers = ManeuverAbilities.Select(
                        BlueprintTool.GetRef<BlueprintAbilityReference>)
                    .ToArray(),
                m_ImprovedFeats = ImprovedManeuverFeats.Select(
                        BlueprintTool.GetRef<BlueprintUnitFactReference>)
                    .ToArray(),
            });
        foreach (var abilityId in ManeuverAbilities) {
            configurator.AddFacts(new() { abilityId });
        }
        var feat = configurator.Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureSplitHex() {
        var icon = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.WitchHexSelection).Icon;
        BuffConfigurator.New(
                "ClassesRebornSplitHexBuff",
                BlueprintIds.SplitHexBuff)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new SplitHexToggle())
            .Configure();
        ActivatableAbilityConfigurator.New(
                "ClassesRebornSplitHexAbility",
                BlueprintIds.SplitHexAbility)
            .SetDisplayName("ClassesReborn.SplitHex.Name")
            .SetDescription("ClassesReborn.SplitHex.Description")
            .SetIcon(icon)
            .SetBuff(BlueprintIds.SplitHexBuff)
            .SetIsOnByDefault(true)
            .SetDoNotTurnOffOnRest(true)
            .SetDeactivateImmediately(true)
            .SetActivationType(AbilityActivationType.Immediately)
            .Configure();

        var excluded = GetHexAbilitiesFromMarker(BlueprintIds.WitchMajorHex)
            .Concat(GetHexAbilitiesFromMarker(BlueprintIds.WitchGrandHex))
            .Distinct()
            .ToArray();
        var feat = FeatureConfigurator.New(
                "ClassesRebornSplitHex",
                BlueprintIds.SplitHexFeat)
            .SetDisplayName("ClassesReborn.SplitHex.Name")
            .SetDescription("ClassesReborn.SplitHex.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WitchClass, 10)
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific)
            .AddFacts(new() { BlueprintIds.SplitHexAbility })
            .AddComponent(new SplitHexTrigger {
                m_ExcludedHexes = excluded.Select(ability =>
                        ability.ToReference<BlueprintAbilityReference>())
                    .ToArray(),
            })
            .Configure();
        AddAsFeat(feat);

        var allWitchHexes = BlueprintTool.Get<BlueprintFeatureSelection>(
                BlueprintIds.WitchHexSelection)
            .m_AllFeatures
            .Select(reference => reference.Get())
            .Where(feature => feature != null)
            .SelectMany(feature => feature.GetComponents<AddFacts>())
            .SelectMany(component => component.Facts)
            .OfType<BlueprintAbility>()
            .SelectMany(ExpandVariants)
            .Distinct()
            .ToArray();
        foreach (var ability in allWitchHexes) {
            if (ability.GetComponent<AbilityTargetNoSplitHexRepeat>() == null) {
                AbilityConfigurator.For(ability)
                    .AddComponent(new AbilityTargetNoSplitHexRepeat())
                    .Configure();
            }
        }
    }

    private static void ConfigureShieldBrace() {
        var configurator = FeatureConfigurator.New(
                "ClassesRebornShieldBrace",
                BlueprintIds.ShieldBraceFeat)
            .SetDisplayName("ClassesReborn.ShieldBrace.Name")
            .SetDescription("ClassesReborn.ShieldBrace.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ShieldFocus).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteFeature(BlueprintIds.ShieldFocus)
            .AddPrerequisiteProficiency(new[] {
                ArmorProficiencyGroup.LightShield,
                ArmorProficiencyGroup.HeavyShield,
                ArmorProficiencyGroup.TowerShield,
            }, Array.Empty<WeaponCategory>())
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Defense)
            .AddComponent(new ShieldBraceAttackPenalty());
        configurator.AddComponent(new PrerequisiteClassLevel {
            m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                BlueprintIds.FighterClass),
            Level = 1,
            Group = Prerequisite.GroupType.Any,
        });
        configurator.AddComponent(new PrerequisiteStatValue {
            Stat = StatType.BaseAttackBonus,
            Value = 3,
            Group = Prerequisite.GroupType.Any,
        });
        var feat = configurator.Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static IEnumerable<BlueprintAbility> GetHexAbilitiesFromMarker(
        string markerId) {
        var marker = BlueprintTool.Get<BlueprintFeature>(markerId);
        return marker.IsPrerequisiteFor
            .Select(reference => reference.Get())
            .Where(feature => feature != null)
            .SelectMany(feature => feature.GetComponents<AddFacts>())
            .SelectMany(component => component.Facts)
            .OfType<BlueprintAbility>()
            .SelectMany(ExpandVariants);
    }

    private static IEnumerable<BlueprintAbility> ExpandVariants(
        BlueprintAbility ability) {
        yield return ability;
        var variants = ability.GetComponent<AbilityVariants>();
        if (variants == null) {
            yield break;
        }
        foreach (var variant in variants.Variants) {
            if (variant != null) {
                yield return variant;
            }
        }
    }

    private static void AddAsFeat(BlueprintFeature feature, bool combatFeat = false) {
        AddToSelection(BlueprintIds.BasicFeatSelection, feature);
        if (combatFeat) {
            AddToSelection(BlueprintIds.FighterBonusFeatSelection, feature);
        }
    }

    private static void AddToSelection(
        string selectionId,
        BlueprintFeature feature) {
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
        selection.m_AllFeatures = AppendDistinct(selection.m_AllFeatures, feature);
        if (selection.m_Features?.Length > 0) {
            selection.m_Features = AppendDistinct(selection.m_Features, feature);
        }
    }

    private static BlueprintFeatureReference[] AppendDistinct(
        BlueprintFeatureReference[] references,
        BlueprintFeature feature) {
        references ??= Array.Empty<BlueprintFeatureReference>();
        return references.Any(reference => reference?.Get() == feature)
            ? references
            : references.Append(feature.ToReference<BlueprintFeatureReference>()).ToArray();
    }

    private static void RemoveFromSelectionIfPresent(
        string selectionId,
        string featureId) {
        var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(featureId);
        if (feature == null) {
            return;
        }
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
        selection.m_AllFeatures = selection.m_AllFeatures
            .Where(reference => reference?.Get() != feature)
            .ToArray();
        selection.m_Features = (selection.m_Features
                ?? Array.Empty<BlueprintFeatureReference>())
            .Where(reference => reference?.Get() != feature)
            .ToArray();
    }

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeatureBase feature) {
        var entry = entries.FirstOrDefault(candidate => candidate.Level == level);
        if (entry == null) {
            entry = new LevelEntry { Level = level };
            entries.Add(entry);
        }
        entry.m_Features ??= new List<BlueprintFeatureBaseReference>();
        if (!entry.Features.Contains(feature)) {
            entry.m_Features.Add(feature.ToReference<BlueprintFeatureBaseReference>());
        }
    }

    private static void RemoveFeature(
        List<LevelEntry> entries,
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }

    private static void ValidateGeneralFeatRegistration() {
        var basic = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.BasicFeatSelection);
        var fighter = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FighterBonusFeatSelection);
        var mythicAbilities = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.MythicAbilitySelection);
        var generalIds = new (string Id, bool Enabled)[] {
            (BlueprintIds.ExtraHexWitchFeat, Main.Settings.ExtraHexWitch),
            (BlueprintIds.ExtraHexShamanFeat, Main.Settings.ExtraHexShaman),
            (BlueprintIds.ExtraRevelationFeat, Main.Settings.ExtraRevelation),
            (BlueprintIds.HorseMasterFeat, Main.Settings.HorseMaster),
            (BlueprintIds.ErastilsBlessingFeat, Main.Settings.ErastilsBlessing),
            (BlueprintIds.GuidedHandFeat, Main.Settings.GuidedHand),
            (BlueprintIds.HurtfulFeat, Main.Settings.Hurtful),
            (BlueprintIds.DirtyFightingFeat, Main.Settings.DirtyFighting),
            (BlueprintIds.SplitHexFeat, Main.Settings.SplitHex),
            (FutureContentIds.Get("Feat.HexStrike"), Main.Settings.HexStrike),
            (BlueprintIds.ShieldBraceFeat, Main.Settings.ShieldBrace),
            (BlueprintIds.MightyHurlingFeat, Main.Settings.MightyHurling),
            (BlueprintIds.CrushingThrowFeat, Main.Settings.CrushingThrow),
            (BlueprintIds.BalancedGripSelection, Main.Settings.BalancedGrip),
            (BlueprintIds.TwoWeaponDefenseFeat, Main.Settings.TwoWeaponDefense),
            (BlueprintIds.ArmorOfThePitFeat, Main.Settings.ArmorOfThePit),
            (BlueprintIds.GreaterUnarmedStrikeFeat, Main.Settings.GreaterUnarmedStrike),
            (BlueprintIds.DervishDanceFeat, Main.Settings.DervishDance),
            (BlueprintIds.MadMagicFeat, Main.Settings.MadMagic),
            (BlueprintIds.CrusadersFlurryFeat, Main.Settings.CrusadersFlurry),
            (BlueprintIds.DesnasShootingStarFeat, Main.Settings.DesnasShootingStar),
            (BlueprintIds.BladedBrushFeat, Main.Settings.BladedBrush),
            (BlueprintIds.AsceticStyleFeat, Main.Settings.AsceticStyle),
            (BlueprintIds.AsceticFormFeat, Main.Settings.AsceticForm),
            (BlueprintIds.AsceticStrikeFeat, Main.Settings.AsceticStrike),
            (BlueprintIds.FeyFoundlingFeat, Main.Settings.FeyFoundling),
            (BlueprintIds.ViciousStompFeat, Main.Settings.ViciousStomp),
            (BlueprintIds.UnsanctionedKnowledgeFeat, Main.Settings.UnsanctionedKnowledge),
            (BlueprintIds.RimeSpellFeat, Main.Settings.RimeSpell),
            (Guids.EldritchHeritageFeat, Main.Settings.EldritchHeritage),
            (Guids.ImprovedEldritchHeritageFeat, Main.Settings.EldritchHeritage),
            (Guids.GreaterEldritchHeritageFeat, Main.Settings.EldritchHeritage),
            (FutureContentIds.Get("Feat.FeralCombatTraining"), Main.Settings.FeralCombatTraining),
            (FutureContentIds.Get("Feat.RacialHeritage"), Main.Settings.RacialHeritage),
            (FutureContentIds.Get("Feat.ArtfulDodge"), Main.Settings.ArtfulDodge),
            (FutureContentIds.Get("Feat.CutFromTheAir"), Main.Settings.CutFromTheAir),
        };
        var combatIds = new (string Id, bool Enabled)[] {
            (BlueprintIds.HorseMasterFeat, Main.Settings.HorseMaster),
            (BlueprintIds.ErastilsBlessingFeat, Main.Settings.ErastilsBlessing),
            (BlueprintIds.HurtfulFeat, Main.Settings.Hurtful),
            (BlueprintIds.DirtyFightingFeat, Main.Settings.DirtyFighting),
            (BlueprintIds.ShieldBraceFeat, Main.Settings.ShieldBrace),
            (BlueprintIds.MightyHurlingFeat, Main.Settings.MightyHurling),
            (BlueprintIds.CrushingThrowFeat, Main.Settings.CrushingThrow),
            (BlueprintIds.BalancedGripSelection, Main.Settings.BalancedGrip),
            (BlueprintIds.TwoWeaponDefenseFeat, Main.Settings.TwoWeaponDefense),
            (BlueprintIds.GreaterUnarmedStrikeFeat, Main.Settings.GreaterUnarmedStrike),
            (BlueprintIds.DervishDanceFeat, Main.Settings.DervishDance),
            (BlueprintIds.MadMagicFeat, Main.Settings.MadMagic),
            (BlueprintIds.BladedBrushFeat, Main.Settings.BladedBrush),
            (BlueprintIds.AsceticStyleFeat, Main.Settings.AsceticStyle),
            (BlueprintIds.AsceticFormFeat, Main.Settings.AsceticForm),
            (BlueprintIds.AsceticStrikeFeat, Main.Settings.AsceticStrike),
            (BlueprintIds.ViciousStompFeat, Main.Settings.ViciousStomp),
            (FutureContentIds.Get("Feat.FeralCombatTraining"), Main.Settings.FeralCombatTraining),
            (FutureContentIds.Get("Feat.ArtfulDodge"), Main.Settings.ArtfulDodge),
            (FutureContentIds.Get("Feat.CutFromTheAir"), Main.Settings.CutFromTheAir),
        };
        if (generalIds.Any(entry =>
                CountInSelection(basic, entry.Id) != (entry.Enabled ? 1 : 0)) ||
            combatIds.Any(entry =>
                CountInSelection(fighter, entry.Id) != (entry.Enabled ? 1 : 0)) ||
            new (string Id, bool Enabled)[] {
                (FutureContentIds.Get("MythicAbility.CursingGaze"), Main.Settings.CursingGaze),
                (FutureContentIds.Get("MythicAbility.Ricochet"), Main.Settings.Ricochet),
                (FutureContentIds.Get("MythicAbility.BashingBulwark"), Main.Settings.BashingBulwark),
                (FutureContentIds.Get("MythicAbility.ShieldedCasting"), Main.Settings.ShieldedCasting),
            }.Any(entry =>
                CountInSelection(mythicAbilities, entry.Id) !=
                (entry.Enabled ? 1 : 0))) {
            throw new InvalidOperationException(
                "One or more Buildcraft feat settings were not applied correctly.");
        }

        if (Main.Settings.GuidedHand) {
            var guidedHand = BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.GuidedHandFeat);
            var prerequisiteNames = guidedHand.ComponentsArray
                .Select(component => component.GetType().Name)
                .ToArray();
            if (prerequisiteNames.Any(name => name.Contains("ChannelSmite"))) {
                throw new InvalidOperationException(
                    "Guided Hand must not require Channel Smite.");
            }
        }

        if (Main.Settings.TwoWeaponDefense &&
            BlueprintTool.Get<BlueprintFeature>(BlueprintIds.TwoWeaponDefenseFeat)
                .GetComponents<TwoWeaponDefenseComponent>().Count() != 1) {
            throw new InvalidOperationException(
                "Two-Weapon Defense must use its conditional dodge AC component.");
        }

        if (Main.Settings.ArmorOfThePit) {
            var armorOfThePit = BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.ArmorOfThePitFeat);
            var armorBonuses = armorOfThePit.GetComponents<AddStatBonus>().ToArray();
            var heritagePrerequisite = armorOfThePit
                .GetComponents<PrerequisiteFeaturesFromList>()
                .SingleOrDefault();
            if (armorBonuses.Count(component =>
                    component.Stat == StatType.AC &&
                    component.Value == 2 &&
                    component.Descriptor == ModifierDescriptor.NaturalArmor) != 1 ||
                heritagePrerequisite?.Amount != 1 ||
                heritagePrerequisite.m_Features.Length !=
                    BlueprintIds.TieflingHeritages.Length) {
                throw new InvalidOperationException(
                    "Armor of the Pit must grant +2 natural armor and require one native Tiefling heritage.");
            }
        }

        if (Main.Settings.GreaterUnarmedStrike &&
            BlueprintTool.Get<BlueprintFeature>(BlueprintIds.GreaterUnarmedStrikeFeat)
                .GetComponents<GreaterUnarmedStrikeComponent>().Count() != 1) {
            throw new InvalidOperationException(
                "Greater Unarmed Strike must use its level-scaled damage component.");
        }
    }

    private static int CountInSelection(
        BlueprintFeatureSelection selection,
        string id) => selection.m_AllFeatures.Count(reference =>
            reference?.deserializedGuid.ToString() == id);
}
