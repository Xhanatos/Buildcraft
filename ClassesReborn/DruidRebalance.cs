using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;

namespace ClassesReborn;

internal static class DruidRebalance {
    private static readonly StatType[] PhysicalAbilityScores = {
        StatType.Strength,
        StatType.Dexterity,
        StatType.Constitution,
    };

    internal static void Configure() {
        ConfigureDefenderOfTheTrueWorld();

        var wildShape = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DruidWildShape);

        var mightyTransformationBuff = BuffConfigurator.New(
                "ClassesRebornMightyTransformationBuff",
                BlueprintIds.MightyTransformationBuff)
            .SetDisplayName("ClassesReborn.MightyTransformation.Name")
            .SetDescription("ClassesReborn.MightyTransformation.Description")
            .SetIcon(wildShape.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddComponent(new RaiseBAB {
                TargetValue = new ContextValue {
                    ValueType = ContextValueType.TargetProperty,
                    Property = UnitProperty.Level,
                },
            })
            .Configure();
        var mightyTransformation = FeatureConfigurator.New(
                "ClassesRebornMightyTransformationFeature",
                BlueprintIds.MightyTransformationFeature)
            .SetDisplayName("ClassesReborn.MightyTransformation.Name")
            .SetDescription("ClassesReborn.MightyTransformation.Description")
            .SetIcon(wildShape.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new BeastsOfLegendsShapeshiftBonus {
                m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.MightyTransformationBuff),
            })
            .Configure();

        var petFeature = CreatePetFeature(wildShape.Icon);
        var shapeshiftBuff = CreateStatBuff(
            "ClassesRebornBeastsOfLegendsShapeshiftBuff",
            BlueprintIds.BeastsOfLegendsShapeshiftBuff,
            4,
            wildShape.Icon);
        var summonBuff = CreateStatBuff(
            "ClassesRebornBeastsOfLegendsSummonBuff",
            BlueprintIds.BeastsOfLegendsSummonBuff,
            2,
            wildShape.Icon);

        var capstone = FeatureConfigurator.New(
                "ClassesRebornBeastsOfLegendsFeature",
                BlueprintIds.BeastsOfLegendsFeature)
            .SetDisplayName("ClassesReborn.BeastsOfLegends.Name")
            .SetDescription("ClassesReborn.BeastsOfLegends.Description")
            .SetIcon(wildShape.Icon)
            .SetIsClassFeature(true)
            .AddFeatureToPet(
                BlueprintIds.BeastsOfLegendsPetFeature,
                PetType.AnimalCompanion)
            .AddOnSpawnBuff(
                BlueprintIds.BeastsOfLegendsSummonBuff,
                isInfinity: true,
                ifHaveFact: BlueprintIds.BeastsOfLegendsFeature)
            .AddComponent(new BeastsOfLegendsShapeshiftBonus {
                m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.BeastsOfLegendsShapeshiftBuff),
            })
            .Configure();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.DruidProgression);
        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(levelEntries, 11, mightyTransformation);
        AddFeature(levelEntries, 20, capstone);
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var winterChild = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.WinterChildArchetype);
        var winterChildLevel20Features = winterChild.AddFeatures?
            .Where(entry => entry.Level == 20)
            .SelectMany(entry => entry.m_Features ?? new())
            .Select(reference => reference?.Get())
            .Where(feature => feature != null)
            .ToArray() ?? Array.Empty<BlueprintFeatureBase>();
        var winterChildRemovals = winterChild.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(winterChildRemovals, 20, capstone);
        winterChild.RemoveFeatures = winterChildRemovals
            .OrderBy(entry => entry.Level)
            .ToArray();

        Validate(
            progression,
            winterChild,
            winterChildLevel20Features,
            mightyTransformation,
            mightyTransformationBuff,
            capstone,
            petFeature,
            shapeshiftBuff,
            summonBuff);
    }

    private static void ConfigureDefenderOfTheTrueWorld() {
        var outsiderType = BlueprintTool.GetRef<BlueprintFeatureReference>(
            FeatureRefs.OutsiderType.ToString());
        var evilSubtype = BlueprintTool.GetRef<BlueprintFeatureReference>(
            FeatureRefs.SubtypeEvil.ToString());
        var druidClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
            BlueprintIds.DruidClass);

        var enemyOfTheFey = FeatureConfigurator.For(BlueprintIds.EnemyOfTheFey)
            .SetDescription("ClassesReborn.DefenderOfTheTrueWorld.EnemyOfTheFey.Description")
            .AddComponent(new EvilOutsiderWeaponBonus {
                m_OutsiderType = outsiderType,
                m_EvilSubtype = evilSubtype,
                FixedBonus = 2,
            })
            .Configure();
        var feyStalkerPet = FeatureConfigurator.For(BlueprintIds.FeyStalkerPetFeature)
            .SetDescription("ClassesReborn.DefenderOfTheTrueWorld.FeyStalker.Description")
            .AddComponent(new EvilOutsiderWeaponBonus {
                m_OutsiderType = outsiderType,
                m_EvilSubtype = evilSubtype,
                m_DruidClass = druidClass,
                ScaleAsFeyStalker = true,
            })
            .Configure();
        var feyStalkerSummon = BuffConfigurator.For(BlueprintIds.FeyStalkerSummonBuff)
            .SetDescription("ClassesReborn.DefenderOfTheTrueWorld.FeyStalker.Description")
            .AddComponent(new EvilOutsiderWeaponBonus {
                m_OutsiderType = outsiderType,
                m_EvilSubtype = evilSubtype,
                m_DruidClass = druidClass,
                ScaleAsFeyStalker = true,
            })
            .Configure();
        var feyBane = FeatureConfigurator.For(BlueprintIds.FeyBane)
            .SetDescription("ClassesReborn.DefenderOfTheTrueWorld.FeyBane.Description")
            .AddComponent(new EvilOutsiderFeybaneBonuses {
                m_OutsiderType = outsiderType,
                m_EvilSubtype = evilSubtype,
            })
            .Configure();

        if (enemyOfTheFey.GetComponents<EvilOutsiderWeaponBonus>().Count() != 1 ||
            feyStalkerPet.GetComponents<EvilOutsiderWeaponBonus>().Count() != 1 ||
            feyStalkerSummon.GetComponents<EvilOutsiderWeaponBonus>().Count() != 1 ||
            feyBane.GetComponents<EvilOutsiderFeybaneBonuses>().Count() != 1) {
            throw new InvalidOperationException(
                "Defender of the True World must extend Enemy of the Fey, Fey Stalker, and Feybane to evil outsiders exactly once.");
        }
    }

    private static BlueprintFeature CreatePetFeature(UnityEngine.Sprite icon) =>
        AddPhysicalAbilityBonuses(
                FeatureConfigurator.New(
                        "ClassesRebornBeastsOfLegendsPetFeature",
                        BlueprintIds.BeastsOfLegendsPetFeature)
                    .SetDisplayName("ClassesReborn.BeastsOfLegends.Name")
                    .SetDescription("ClassesReborn.BeastsOfLegends.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true),
                4)
            .Configure();

    private static BlueprintBuff CreateStatBuff(
        string name,
        string guid,
        int bonus,
        UnityEngine.Sprite icon) =>
        AddPhysicalAbilityBonuses(
                BuffConfigurator.New(name, guid)
                    .SetDisplayName("ClassesReborn.BeastsOfLegends.Name")
                    .SetDescription("ClassesReborn.BeastsOfLegends.Description")
                    .SetIcon(icon)
                    .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                    .SetStacking(StackingType.Replace),
                bonus)
            .Configure();

    private static FeatureConfigurator AddPhysicalAbilityBonuses(
        FeatureConfigurator configurator,
        int bonus) {
        foreach (var stat in PhysicalAbilityScores) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.Sacred,
                stat: stat,
                value: bonus);
        }

        return configurator;
    }

    private static BuffConfigurator AddPhysicalAbilityBonuses(
        BuffConfigurator configurator,
        int bonus) {
        foreach (var stat in PhysicalAbilityScores) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.Sacred,
                stat: stat,
                value: bonus);
        }

        return configurator;
    }

    private static void Validate(
        BlueprintProgression progression,
        BlueprintArchetype winterChild,
        BlueprintFeatureBase[] winterChildLevel20Features,
        BlueprintFeature mightyTransformation,
        BlueprintBuff mightyTransformationBuff,
        BlueprintFeature capstone,
        BlueprintFeature petFeature,
        BlueprintBuff shapeshiftBuff,
        BlueprintBuff summonBuff) {
        var mightyTransformationComponents = mightyTransformation
            .GetComponents<BeastsOfLegendsShapeshiftBonus>()
            .ToArray();
        var raiseBabComponents = mightyTransformationBuff
            .GetComponents<RaiseBAB>()
            .ToArray();
        var mightyTransformationGrants = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.Get() == mightyTransformation) ?? 0);
        var mightyTransformationAtLevel11 = progression.LevelEntries
            .Where(entry => entry.Level == 11)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == mightyTransformation) ?? 0);
        var totalGrants = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == capstone) ?? 0);
        var atLevel20 = progression.LevelEntries
            .Where(entry => entry.Level == 20)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == capstone) ?? 0);
        var petComponents = capstone.GetComponents<AddFeatureToPet>().ToArray();
        var spawnComponents = capstone.GetComponents<OnSpawnBuff>().ToArray();
        var shapeshiftComponents = capstone
            .GetComponents<BeastsOfLegendsShapeshiftBonus>()
            .ToArray();
        var winterChildRemovalCount = winterChild.RemoveFeatures?
            .Where(entry => entry.Level == 20)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == capstone) ?? 0) ?? 0;
        var totalWinterChildRemovals = winterChild.RemoveFeatures?
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == capstone) ?? 0) ?? 0;
        var currentWinterChildLevel20Features = winterChild.AddFeatures?
            .Where(entry => entry.Level == 20)
            .SelectMany(entry => entry.m_Features ?? new())
            .Select(reference => reference?.Get())
            .Where(feature => feature != null)
            .ToArray() ?? Array.Empty<BlueprintFeatureBase>();

        if (mightyTransformationGrants != 1 ||
            mightyTransformationAtLevel11 != 1 ||
            mightyTransformationComponents.Length != 1 ||
            mightyTransformationComponents[0].m_Buff?.Get() !=
                mightyTransformationBuff ||
            raiseBabComponents.Length != 1 ||
            raiseBabComponents[0].TargetValue.ValueType !=
                ContextValueType.TargetProperty ||
            raiseBabComponents[0].TargetValue.Property != UnitProperty.Level ||
            totalGrants != 1 || atLevel20 != 1 ||
            winterChildLevel20Features.Length == 0 ||
            currentWinterChildLevel20Features.Length != winterChildLevel20Features.Length ||
            winterChildLevel20Features.Any(feature =>
                currentWinterChildLevel20Features.Count(candidate => candidate == feature) !=
                winterChildLevel20Features.Count(candidate => candidate == feature)) ||
            winterChildRemovalCount != 1 || totalWinterChildRemovals != 1 ||
            petComponents.Length != 1 ||
            petComponents[0].m_Feature?.Get() != petFeature ||
            petComponents[0].m_PetType != PetType.AnimalCompanion ||
            spawnComponents.Length != 1 ||
            spawnComponents[0].m_buff?.Get() != summonBuff ||
            !spawnComponents[0].IsInfinity ||
            spawnComponents[0].m_IfHaveFact?.Get() != capstone ||
            spawnComponents[0].CheckDescriptor ||
            shapeshiftComponents.Length != 1 ||
            shapeshiftComponents[0].m_Buff?.Get() != shapeshiftBuff) {
            throw new InvalidOperationException(
                "Druids must gain Mighty Transformation at level 11, while Beasts of Legends must be granted once at level 20, be replaced by Winter Child's own level-20 feature, and use the configured companion, shapeshift, and summon effects.");
        }

        ValidateBonuses(petFeature, 4, "animal companion");
        ValidateBonuses(shapeshiftBuff, 4, "shapeshifted Druid");
        ValidateBonuses(summonBuff, 2, "summoned creature");
    }

    private static void ValidateBonuses(
        BlueprintUnitFact fact,
        int expectedValue,
        string recipient) {
        var bonuses = fact.GetComponents<AddStatBonus>().ToArray();
        if (bonuses.Length != PhysicalAbilityScores.Length ||
            PhysicalAbilityScores.Any(stat =>
                bonuses.Count(component =>
                    component.Stat == stat &&
                    component.Value == expectedValue &&
                    component.Descriptor == ModifierDescriptor.Sacred) != 1)) {
            throw new InvalidOperationException(
                $"Beasts of Legends {recipient} bonuses must be sacred +{expectedValue} Strength, Dexterity, and Constitution.");
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
}
