using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class SorcererRebalance {
    private static readonly int[] BonusFeatLevels = { 3, 7, 11, 15, 19 };

    private static readonly string[] FeyBloodlineProgressionIds = {
        BlueprintIds.BloodlineFeyProgression,
        BlueprintIds.BloodlineFeyProgressionAlternate,
        BlueprintIds.SeekerBloodlineFeyProgression,
        BlueprintIds.CrossbloodedSecondaryBloodlineFeyProgression,
    };

    private static readonly string[] SerpentineBloodlineProgressionIds = {
        BlueprintIds.BloodlineSerpentineProgression,
        BlueprintIds.BloodlineSerpentineProgressionAlternate,
        BlueprintIds.SeekerBloodlineSerpentineProgression,
        BlueprintIds.CrossbloodedSecondaryBloodlineSerpentineProgression,
    };

    private static readonly (
        string AbilityId,
        string BaseFeatureId,
        string ExtraUseFeatureId,
        string FeatureId,
        string DescriptionKey)[] DraconicBreathWeapons = {
        ("1e65b0b2db777e24db96d8bc52cc9207", "2708d24e2b4200346994c82821c3b47b", "7459c25b2cc9cdd4d8367cb555f0fe5a", "73939e14b956b884688a6e1dccf9c043", "ClassesReborn.DraconicBreathWeapon.Black.Description"),
        ("60a3047f434f38544a2878c26955d3ad", "aa7704d95f479044f9779e0b3b489c87", "4675fb43f872c6546b43f55f564a9020", "a2a2caf3f73681643b0251c5561ce6ce", "ClassesReborn.DraconicBreathWeapon.Blue.Description"),
        ("531a57e0c19f80945b68bdb3e289279a", "807a97d06f5cdf245b9cc17d7b2429a9", "32031d5563b54e045a7faf714cb14875", "9d7bb2a6d590ba0498992f6ce825f2cc", "ClassesReborn.DraconicBreathWeapon.Brass.Description"),
        ("732291d7ac20b0949aae002622e00b34", "f97e345b9f474764fae2b7eff1c1a1c7", "8b1f74151e06fd54786e2354df6e7b28", "e86286b52aeefb540a67c3c1af235167", "ClassesReborn.DraconicBreathWeapon.Bronze.Description"),
        ("826ef8251d9243941b432f97d901e938", "8e339ab3898fdd14b879753eaaae933d", "ff5c413c97362bc40b27b27347d521f9", "63718b3248898134eba94a139ea07313", "ClassesReborn.DraconicBreathWeapon.Copper.Description"),
        ("598e33639b662784fb07c0e4c8978aa4", "2a711cd134b91d34ab027b50d721778b", "6d421317055dcf04093cbfdc1c4a4c48", "fcf0cb61b79b6fd47a6ed91f40820cea", "ClassesReborn.DraconicBreathWeapon.Gold.Description"),
        ("633b622267c097d4abe3ec6445c05152", "919e81dec12943c4d936bd90bacf5519", "754bc5933712bab4ea08710a1577cb27", "4d107a429575cb344bcba32a5b1a6abe", "ClassesReborn.DraconicBreathWeapon.Green.Description"),
        ("3f31704e595e78942b3640cdc9b95d8b", "97e25dac9d4da934e8c628a71a6a5792", "8526bb6390ec1064f9dfa903a5b98eeb", "9cbaf9563f8f6fb47810e0cbeb5e93ed", "ClassesReborn.DraconicBreathWeapon.Red.Description"),
        ("11d03ebc508d6834cad5992056ad01a4", "cd36514cf1f38f84a977a265cec113ae", "6772a8f320672c940b7bef018fd762f5", "a1cde01a9790834449d8f547ca52fc88", "ClassesReborn.DraconicBreathWeapon.Silver.Description"),
        ("84be529914c90664aa948d8266bb3fa6", "43bfe183b93ce0347a3f02c2b0438b77", "436e8e4d16f180a43a45c4e09cbbaf79", "a24420dcf53ad834783c0945882371d6", "ClassesReborn.DraconicBreathWeapon.White.Description"),
    };

    internal static void Configure() {
        ConfigureCelestialBody();
        ConfigureInfernalBody();
        ConfigureBloodlineDexterityProgressions();
        ConfigureDraconicBreathWeapons();

        ConfigureBonusFeatProgression();
    }

    private static void ConfigureBonusFeatProgression() {
        var sorcererProgression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.SorcererProgression);
        var genericBonusFeatSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SorcererBonusFeatSelection);
        var bloodlineFeatSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SorcererBloodlineFeatSelection);
        var sorcererLevelEntries = sorcererProgression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();

        // SorcererBonusFeat is the level-1 class marker, not the recurring
        // bloodline-specific feat selection. Keep that marker at level 1 and
        // expand SorcererFeatSelection, whose nested options resolve to the
        // feat list of the character's chosen bloodline.
        RemoveFeature(sorcererLevelEntries, genericBonusFeatSelection);
        AddFeature(sorcererLevelEntries, 1, genericBonusFeatSelection);
        RemoveFeature(sorcererLevelEntries, bloodlineFeatSelection);
        foreach (var level in BonusFeatLevels) {
            AddFeature(sorcererLevelEntries, level, bloodlineFeatSelection);
        }
        sorcererProgression.LevelEntries = sorcererLevelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        // Geomancer and Overwhelming Mage trade away every recurring
        // bloodline feat selection in the native Sorcerer progression. Keep
        // that exchange complete after expanding the base progression, and
        // ensure CanAddArchetype sees replacements at every new grant level.
        var geomancer = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.SorcererGeomancerArchetype);
        ConfigureBloodlineFeatReplacement(geomancer, bloodlineFeatSelection);
        ConfigureGeomancerFavoredTerrainProgression(geomancer);

        ConfigureBloodlineFeatReplacement(
            BlueprintTool.Get<BlueprintArchetype>(
                BlueprintIds.SorcererOverwhelmingMageArchetype),
            bloodlineFeatSelection);

        FeatureSelectionConfigurator.For(bloodlineFeatSelection.AssetGuid.ToString())
            .SetDescription("ClassesReborn.SorcererBloodlineBonusFeat.Description")
            .Configure();

        if (CountFeatureByGuid(
                sorcererProgression.LevelEntries,
                genericBonusFeatSelection.AssetGuid) != 1 ||
            CountFeatureAtLevelByGuid(
                sorcererProgression.LevelEntries,
                genericBonusFeatSelection.AssetGuid,
                1) != 1 ||
            sorcererProgression.LevelEntries.Any(entry =>
                entry.Level != 1 &&
                CountFeatureAtLevelByGuid(
                    sorcererProgression.LevelEntries,
                    genericBonusFeatSelection.AssetGuid,
                    entry.Level) != 0) ||
            CountFeatureByGuid(
                sorcererProgression.LevelEntries,
                bloodlineFeatSelection.AssetGuid) != BonusFeatLevels.Length ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevelByGuid(
                    sorcererProgression.LevelEntries,
                    bloodlineFeatSelection.AssetGuid,
                    level) != 1) ||
            sorcererProgression.LevelEntries.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                CountFeatureAtLevelByGuid(
                    sorcererProgression.LevelEntries,
                    bloodlineFeatSelection.AssetGuid,
                    entry.Level) != 0)) {
            throw new InvalidOperationException(
                "Each Sorcerer bloodline-specific feat selection must be granted at levels 3/7/11/15/19, while the generic Sorcerer Bonus Feat marker remains only at level 1.");
        }

        Main.Log.Log(
            "Expanded the Sorcerer bloodline feat selection to levels 3/7/11/15/19, retained the generic Sorcerer Bonus Feat marker at level 1, made Geomancer and Overwhelming Mage replace all five recurring selections, and granted Geomancer one Favored Terrain choice at each replacement level.");
    }

    private static void ConfigureGeomancerFavoredTerrainProgression(
        BlueprintArchetype geomancer) {
        var favoredTerrain = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SorcererGeomancerFavoredTerrainSelection);
        var favoredTerrainRankUp = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SorcererGeomancerFavoredTerrainRankUpSelection);
        var addFeatures = geomancer.AddFeatures?.ToList()
            ?? new List<LevelEntry>();

        // Preserve the native total of three new terrain selections and two
        // rank-ups, but distribute those five choices across the five levels
        // at which Geomancer replaces a Sorcerer bloodline bonus feat.
        RemoveFeature(addFeatures, favoredTerrain);
        RemoveFeature(addFeatures, favoredTerrainRankUp);
        AddFeature(addFeatures, 3, favoredTerrain);
        AddFeature(addFeatures, 7, favoredTerrain);
        AddFeature(addFeatures, 11, favoredTerrainRankUp);
        AddFeature(addFeatures, 15, favoredTerrain);
        AddFeature(addFeatures, 19, favoredTerrainRankUp);
        geomancer.AddFeatures = addFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var newTerrainLevels = new[] { 3, 7, 15 };
        var rankUpLevels = new[] { 11, 19 };
        if (CountFeature(geomancer.AddFeatures, favoredTerrain) !=
                newTerrainLevels.Length ||
            newTerrainLevels.Any(level =>
                CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrain,
                    level) != 1) ||
            CountFeature(geomancer.AddFeatures, favoredTerrainRankUp) !=
                rankUpLevels.Length ||
            rankUpLevels.Any(level =>
                CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrainRankUp,
                    level) != 1) ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrain,
                    level) +
                CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrainRankUp,
                    level) != 1) ||
            geomancer.AddFeatures.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                (CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrain,
                    entry.Level) != 0 ||
                 CountFeatureAtLevel(
                    geomancer.AddFeatures,
                    favoredTerrainRankUp,
                    entry.Level) != 0))) {
            throw new InvalidOperationException(
                "Geomancer must gain exactly one Favored Terrain selection at levels 3/7/11/15/19, with three new terrain choices and two rank-ups.");
        }
    }

    private static void ConfigureBloodlineFeatReplacement(
        BlueprintArchetype archetype,
        BlueprintFeatureSelection bloodlineFeatSelection) {
        var removeFeatures = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(removeFeatures, bloodlineFeatSelection);
        foreach (var level in BonusFeatLevels) {
            AddFeature(removeFeatures, level, bloodlineFeatSelection);
        }
        archetype.RemoveFeatures = removeFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeatureByGuid(
                archetype.RemoveFeatures,
                bloodlineFeatSelection.AssetGuid) != BonusFeatLevels.Length ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevelByGuid(
                    archetype.RemoveFeatures,
                    bloodlineFeatSelection.AssetGuid,
                    level) != 1) ||
            archetype.RemoveFeatures.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                CountFeatureAtLevelByGuid(
                    archetype.RemoveFeatures,
                    bloodlineFeatSelection.AssetGuid,
                    entry.Level) != 0)) {
            throw new InvalidOperationException(
                $"{archetype.name} must replace every Sorcerer bloodline feat selection at levels 3/7/11/15/19.");
        }
    }

    private static void ConfigureDraconicBreathWeapons() {
        var resource = BlueprintTool.Get<BlueprintAbilityResource>(
            BlueprintIds.BloodlineDraconicBreathWeaponResource);
        resource.m_MaxAmount.BaseValue = 3;

        foreach (var breathWeapon in DraconicBreathWeapons) {
            AbilityConfigurator.For(breathWeapon.AbilityId)
                .SetDescription(breathWeapon.DescriptionKey)
                .Configure();
            FeatureConfigurator.For(breathWeapon.BaseFeatureId)
                .SetDescription(breathWeapon.DescriptionKey)
                .Configure();
            FeatureConfigurator.For(breathWeapon.FeatureId)
                .SetDescription(breathWeapon.DescriptionKey)
                .Configure();
            var extraUse = FeatureConfigurator.For(
                    breathWeapon.ExtraUseFeatureId)
                .SetDescription(breathWeapon.DescriptionKey)
                .Configure();

            var resourceIncreases = extraUse
                .GetComponents<IncreaseResourceAmount>()
                .Where(component => component.m_Resource?.Get() == resource)
                .ToArray();
            if (resourceIncreases.Length != 1) {
                throw new InvalidOperationException(
                    $"Expected one draconic breath resource increase on {extraUse.name}.");
            }
            resourceIncreases[0].Value = 3;
        }

        if (resource.m_MaxAmount.BaseValue != 3 ||
            DraconicBreathWeapons.Any(breathWeapon => {
                var extraUse = BlueprintTool.Get<BlueprintFeature>(
                    breathWeapon.ExtraUseFeatureId);
                var resourceIncreases = extraUse
                    .GetComponents<IncreaseResourceAmount>()
                    .Where(component => component.m_Resource?.Get() == resource)
                    .ToArray();
                return resourceIncreases.Length != 1 ||
                    resourceIncreases[0].Value != 3;
            })) {
            throw new InvalidOperationException(
                "Sorcerer draconic breath weapons must have 3/6/9 daily uses at levels 9/17/20.");
        }
    }

    private static void ConfigureCelestialBody() {
        var controller = FeatureConfigurator.For(
                BlueprintIds.BloodlineCelestialResistancesController)
            .SetDisplayName("ClassesReborn.CelestialBody.Name")
            .SetDescription("ClassesReborn.CelestialBody.Description")
            .Configure();
        var level3 = FeatureConfigurator.For(
                BlueprintIds.BloodlineCelestialResistancesLevel3)
            .SetDisplayName("ClassesReborn.CelestialBody.Name")
            .SetDescription("ClassesReborn.CelestialBody.Description")
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 2)
            .Configure();
        var level9 = FeatureConfigurator.For(
                BlueprintIds.BloodlineCelestialResistancesLevel9)
            .SetDisplayName("ClassesReborn.CelestialBody.Name")
            .SetDescription("ClassesReborn.CelestialBody.Description")
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 4)
            .Configure();

        var classLevelGrants = controller
            .GetComponents<AddFeatureOnClassLevel>()
            .ToArray();
        var level3Bonuses = level3.GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Charisma)
            .ToArray();
        var level9Bonuses = level9.GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Charisma)
            .ToArray();
        if (classLevelGrants.Count(component =>
                component.m_Feature?.Get() == level3 &&
                component.Level == 9 &&
                component.BeforeThisLevel) != 1 ||
            classLevelGrants.Count(component =>
                component.m_Feature?.Get() == level9 &&
                component.Level == 9 &&
                !component.BeforeThisLevel) != 1 ||
            level3Bonuses.Length != 1 ||
            level3Bonuses[0].Value != 2 ||
            level3Bonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            level9Bonuses.Length != 1 ||
            level9Bonuses[0].Value != 4 ||
            level9Bonuses[0].Descriptor != ModifierDescriptor.UntypedStackable) {
            throw new InvalidOperationException(
                "Celestial Body must retain its level-9 resistance swap and grant +2/+4 Charisma.");
        }
    }

    private static void ConfigureInfernalBody() {
        var controller = FeatureConfigurator.For(
                BlueprintIds.BloodlineInfernalResistancesController)
            .SetDisplayName("ClassesReborn.InfernalBody.Name")
            .SetDescription("ClassesReborn.InfernalBody.Description")
            .Configure();
        var level3 = FeatureConfigurator.For(
                BlueprintIds.BloodlineInfernalResistancesLevel3)
            .SetDisplayName("ClassesReborn.InfernalBody.Name")
            .SetDescription("ClassesReborn.InfernalBody.Description")
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 2)
            .Configure();
        var level9 = FeatureConfigurator.For(
                BlueprintIds.BloodlineInfernalResistancesLevel9)
            .SetDisplayName("ClassesReborn.InfernalBody.Name")
            .SetDescription("ClassesReborn.InfernalBody.Description")
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 4)
            .Configure();

        var classLevelGrants = controller
            .GetComponents<AddFeatureOnClassLevel>()
            .ToArray();
        var level3Bonuses = level3.GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Charisma)
            .ToArray();
        var level9Bonuses = level9.GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Charisma)
            .ToArray();
        if (classLevelGrants.Count(component =>
                component.m_Feature?.Get() == level3 &&
                component.Level == 9 &&
                component.BeforeThisLevel) != 1 ||
            classLevelGrants.Count(component =>
                component.m_Feature?.Get() == level9 &&
                component.Level == 9 &&
                !component.BeforeThisLevel) != 1 ||
            level3Bonuses.Length != 1 ||
            level3Bonuses[0].Value != 2 ||
            level3Bonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            level9Bonuses.Length != 1 ||
            level9Bonuses[0].Value != 4 ||
            level9Bonuses[0].Descriptor != ModifierDescriptor.UntypedStackable) {
            throw new InvalidOperationException(
                "Infernal Body must retain its level-9 resistance swap and grant +2/+4 Charisma.");
        }
    }

    private static void ConfigureBloodlineDexterityProgressions() {
        ConfigureBloodlineDexterityProgression(
            FeyBloodlineProgressionIds,
            BlueprintIds.FeyAgilityLevel9,
            BlueprintIds.FeyAgilityLevel13,
            BlueprintIds.FeyAgilityLevel17,
            "ClassesReborn.FeyAgility.Name",
            "ClassesReborn.FeyAgility.Description");
        ConfigureBloodlineDexterityProgression(
            SerpentineBloodlineProgressionIds,
            BlueprintIds.SerpentineAgilityLevel9,
            BlueprintIds.SerpentineAgilityLevel13,
            BlueprintIds.SerpentineAgilityLevel17,
            "ClassesReborn.SerpentineAgility.Name",
            "ClassesReborn.SerpentineAgility.Description");
    }

    private static void ConfigureBloodlineDexterityProgression(
        IReadOnlyList<string> progressionIds,
        string level9FeatureId,
        string level13FeatureId,
        string level17FeatureId,
        string displayName,
        string description) {
        var icon = BlueprintTool.Get<BlueprintProgression>(progressionIds[0]).Icon;
        var features = new[] {
            FeatureConfigurator.New(
                    $"{displayName}.Level9",
                    level9FeatureId)
                .SetDisplayName(displayName)
                .SetDescription(description)
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Inherent,
                    stat: StatType.Dexterity,
                    value: 2)
                .Configure(),
            FeatureConfigurator.New(
                    $"{displayName}.Level13",
                    level13FeatureId)
                .SetDisplayName(displayName)
                .SetDescription(description)
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Inherent,
                    stat: StatType.Dexterity,
                    value: 4)
                .Configure(),
            FeatureConfigurator.New(
                    $"{displayName}.Level17",
                    level17FeatureId)
                .SetDisplayName(displayName)
                .SetDescription(description)
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Inherent,
                    stat: StatType.Dexterity,
                    value: 6)
                .Configure(),
        };
        var levels = new[] { 9, 13, 17 };

        foreach (var progressionId in progressionIds.Distinct()) {
            var progression = BlueprintTool.Get<BlueprintProgression>(progressionId);
            var levelEntries = progression.LevelEntries?.ToList()
                ?? new List<LevelEntry>();
            for (var index = 0; index < features.Length; index++) {
                AddFeature(levelEntries, levels[index], features[index]);
            }
            progression.LevelEntries = levelEntries
                .OrderBy(entry => entry.Level)
                .ToArray();

            if (features.Select((feature, index) =>
                    CountFeatureAtLevel(
                        progression.LevelEntries,
                        feature,
                        levels[index]))
                .Any(count => count != 1)) {
                throw new InvalidOperationException(
                    $"{displayName} must be granted at Sorcerer levels 9/13/17 on {progression.name}.");
            }
        }

        for (var index = 0; index < features.Length; index++) {
            var bonuses = features[index]
                .GetComponents<AddStatBonus>()
                .ToArray();
            if (bonuses.Length != 1 ||
                bonuses[0].Stat != StatType.Dexterity ||
                bonuses[0].Descriptor != ModifierDescriptor.Inherent ||
                bonuses[0].Value != (index + 1) * 2) {
                throw new InvalidOperationException(
                    $"{displayName} must grant +2/+4/+6 inherent Dexterity.");
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

    private static int CountFeatureByGuid(
        IEnumerable<LevelEntry> entries,
        BlueprintGuid featureGuid) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.deserializedGuid == featureGuid) ?? 0) ?? 0;

    private static int CountFeatureAtLevelByGuid(
        IEnumerable<LevelEntry> entries,
        BlueprintGuid featureGuid,
        int level) =>
        entries?.Where(entry => entry.Level == level).Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.deserializedGuid == featureGuid) ?? 0) ?? 0;
}
