using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class ShamanRebalance {
    private static readonly int[] AdditionalHexLevels = { 6, 14 };
    private static readonly int[] CharmingSpiritsLevels = { 7, 15 };
    private static readonly int[] FinalHexLevels = {
        2, 4, 6, 8, 10, 12, 14, 16, 18, 20,
    };

    internal static void Configure() {
        ConfigureWardBonuses();
        ConfigureSpiritManifestations();
        ConfigureNatureSpirit();

        var shamanClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.ShamanClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ShamanProgression);
        var hexSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.ShamanHexSelection);
        var unswornShaman = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.UnswornShamanArchetype);
        var charmingSpirits = FeatureConfigurator.New(
                "ClassesRebornShamanCharmingSpiritsFeature",
                BlueprintIds.CharmingSpiritsFeature)
            .SetDisplayName("ClassesReborn.CharmingSpirits.Name")
            .SetDescription("ClassesReborn.CharmingSpirits.Description")
            .SetIcon(hexSelection.Icon)
            .SetIsClassFeature(true)
            .SetRanks(2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 2)
            .Configure();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        foreach (var level in AdditionalHexLevels) {
            AddFeature(levelEntries, level, hexSelection);
        }
        RemoveFeature(levelEntries, charmingSpirits);
        foreach (var level in CharmingSpiritsLevels) {
            AddFeature(levelEntries, level, charmingSpirits);
        }
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        var unswornRemovals = unswornShaman.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        foreach (var level in AdditionalHexLevels) {
            AddFeature(unswornRemovals, level, hexSelection);
        }
        unswornShaman.RemoveFeatures = unswornRemovals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var charismaBonuses = charmingSpirits.GetComponents<AddStatBonus>().ToArray();
        if (FinalHexLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, hexSelection, level) != 1) ||
            CountFeature(progression.LevelEntries, hexSelection) != FinalHexLevels.Length ||
            CharmingSpiritsLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, charmingSpirits, level) != 1) ||
            CountFeature(progression.LevelEntries, charmingSpirits) !=
                CharmingSpiritsLevels.Length ||
            charmingSpirits.Ranks != CharmingSpiritsLevels.Length ||
            charismaBonuses.Length != 1 ||
            charismaBonuses[0].Stat != StatType.Charisma ||
            charismaBonuses[0].Value != 2 ||
            charismaBonuses[0].Descriptor != ModifierDescriptor.UntypedStackable ||
            AdditionalHexLevels.Any(level =>
                CountFeatureAtLevel(
                    unswornShaman.RemoveFeatures,
                    hexSelection,
                    level) != 1) ||
            shamanClass.Archetypes
                .Where(archetype => archetype != unswornShaman)
                .Any(archetype =>
                    AdditionalHexLevels.Any(level =>
                        CountFeatureAtLevel(
                            archetype.RemoveFeatures,
                            hexSelection,
                            level) != 0)) ||
            shamanClass.Archetypes.Any(archetype =>
                CharmingSpiritsLevels.Any(level =>
                    CountFeatureAtLevel(archetype.RemoveFeatures, charmingSpirits, level) != 0))) {
            throw new InvalidOperationException(
                "Every Shaman except Unsworn Shaman must inherit Hex selections at levels 6 and 14, while every Shaman must inherit Charming Spirits at levels 7 and 15.");
        }
    }

    private static void ConfigureWardBonuses() {
        FeatureConfigurator.For(BlueprintIds.ShamanBattleWardFeature)
            .SetDescription("ClassesReborn.ShamanBattleWard.Description")
            .Configure();
        AbilityConfigurator.For(BlueprintIds.ShamanBattleWardAbility)
            .SetDescription("ClassesReborn.ShamanBattleWard.Description")
            .Configure();

        for (var index = 0; index < BlueprintIds.ShamanBattleWardBuffs.Length; index++) {
            var buff = BuffConfigurator.For(BlueprintIds.ShamanBattleWardBuffs[index])
                .SetDescription("ClassesReborn.ShamanBattleWard.Description")
                .Configure();
            var armorBonuses = buff.GetComponents<AddStatBonus>()
                .Where(component => component.Stat == StatType.AC)
                .ToArray();
            foreach (var bonus in armorBonuses) {
                bonus.Descriptor = ModifierDescriptor.Dodge;
            }

            if (armorBonuses.Length != 1 ||
                armorBonuses[0].Value != index + 1 ||
                armorBonuses[0].Descriptor != ModifierDescriptor.Dodge) {
                throw new InvalidOperationException(
                    $"Battle Ward stage {index + 1} must grant a +{index + 1} Dodge bonus to AC.");
            }
        }

        FeatureConfigurator.For(BlueprintIds.ShamanBoneWardFeature)
            .SetDescription("ClassesReborn.ShamanBoneWard.Description")
            .Configure();
        AbilityConfigurator.For(BlueprintIds.ShamanBoneWardAbility)
            .SetDescription("ClassesReborn.ShamanBoneWard.Description")
            .Configure();
        var boneWard = BuffConfigurator.For(BlueprintIds.ShamanBoneWardBuff)
            .SetDescription("ClassesReborn.ShamanBoneWard.Description")
            .Configure();
        var boneWardBonuses = boneWard.GetComponents<AddContextStatBonus>()
            .Where(component => component.Stat == StatType.AC)
            .ToArray();
        foreach (var bonus in boneWardBonuses) {
            bonus.Descriptor = ModifierDescriptor.Dodge;
        }

        if (boneWardBonuses.Length != 1 ||
            boneWardBonuses[0].Descriptor != ModifierDescriptor.Dodge) {
            throw new InvalidOperationException(
                "Bone Ward must grant a rank-scaled Dodge bonus to AC.");
        }
    }

    private static void ConfigureSpiritManifestations() {
        ConfigureElementalManifestation(
            BlueprintIds.ShamanFlameManifestation,
            "ClassesReborn.ShamanFlameManifestation.Description",
            DamageEnergyType.Fire);
        ConfigureElementalManifestation(
            BlueprintIds.ShamanFrostManifestation,
            "ClassesReborn.ShamanFrostManifestation.Description",
            DamageEnergyType.Cold);
        ConfigureElementalManifestation(
            BlueprintIds.ShamanStoneManifestation,
            "ClassesReborn.ShamanStoneManifestation.Description",
            DamageEnergyType.Acid);
        ConfigureElementalManifestation(
            BlueprintIds.ShamanWavesManifestation,
            "ClassesReborn.ShamanWavesManifestation.Description",
            DamageEnergyType.Cold);
        ConfigureElementalManifestation(
            BlueprintIds.ShamanWindManifestation,
            "ClassesReborn.ShamanWindManifestation.Description",
            DamageEnergyType.Electricity);
    }

    private static void ConfigureElementalManifestation(
        string featureId,
        string descriptionKey,
        DamageEnergyType energyType) {
        var original = BlueprintTool.Get<BlueprintFeature>(featureId);
        var alreadyImmune = original.GetComponents<AddEnergyDamageImmunity>()
            .Any(component => component.EnergyType == energyType);
        var configurator = FeatureConfigurator.For(featureId)
            .SetDescription(descriptionKey)
            .RemoveComponents(component =>
                component is AddDamageResistanceEnergy resistance &&
                resistance.Type == energyType)
            .AddComponent(new ElementalManifestationDamage {
                EnergyType = energyType,
            });
        if (!alreadyImmune) {
            configurator.AddEnergyDamageImmunity(energyType);
        }

        var manifestation = configurator.Configure();
        var immunities = manifestation.GetComponents<AddEnergyDamageImmunity>()
            .Where(component => component.EnergyType == energyType)
            .ToArray();
        var resistances = manifestation.GetComponents<AddDamageResistanceEnergy>()
            .Where(component => component.Type == energyType)
            .ToArray();
        var damageComponents = manifestation
            .GetComponents<ElementalManifestationDamage>()
            .Where(component => component.EnergyType == energyType)
            .ToArray();
        if (immunities.Length != 1 || resistances.Length != 0 ||
            damageComponents.Length != 1) {
            throw new InvalidOperationException(
                $"{manifestation.name} must grant {energyType} immunity and exactly one 1d6 {energyType} attack-damage component.");
        }
    }

    private static void ConfigureNatureSpirit() {
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ShamanNatureSpiritProgression);
        var greaterFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ShamanNatureSpiritGreaterFeature);
        var companionSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.ShamanNatureSpiritTrueSelection);
        var companionProgression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ShamanAnimalCompanionProgression);
        var companionRank = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.AnimalCompanionRank);
        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();

        RemoveFeature(levelEntries, greaterFeature);
        RemoveFeature(levelEntries, companionSelection);
        RemoveFeature(levelEntries, companionProgression);
        RemoveFeature(levelEntries, companionRank);
        AddFeature(levelEntries, 8, companionSelection);
        AddFeature(levelEntries, 8, companionProgression);
        AddFeature(levelEntries, 8, companionRank);
        AddFeature(levelEntries, 16, greaterFeature);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeatureAtLevel(progression.LevelEntries, companionSelection, 8) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, companionProgression, 8) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, companionRank, 8) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, greaterFeature, 16) != 1 ||
            CountFeature(progression.LevelEntries, companionSelection) != 1 ||
            CountFeature(progression.LevelEntries, companionProgression) != 1 ||
            CountFeature(progression.LevelEntries, companionRank) != 1 ||
            CountFeature(progression.LevelEntries, greaterFeature) != 1) {
            throw new InvalidOperationException(
                "Nature Spirit must grant its animal companion bundle at level 8 and its greater fast-healing feature at level 16.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0) ?? 0;

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeatureBase feature) {
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
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }
}
