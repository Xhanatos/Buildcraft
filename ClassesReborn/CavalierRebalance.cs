using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class CavalierRebalance {
    private static readonly int[] BonusFeatLevels = { 2, 6, 10, 14, 18 };

    internal static void Configure() {
        ConfigureBraggart();
        ConfigureOrderChallengeBonuses();
        ConfigureByMyHonor();
        ConfigureFearmonger();
        ConfigureGloriousCharge();
        ConfigureFrightfulGaze();
        ConfigureKnightOfTheWallShieldBonuses();
        ConfigureStandardBearer();

        var cavalierClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.CavalierClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.CavalierProgression);
        var bonusFeat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CavalierBonusFeatSelection);
        var gendarme = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.GendarmeArchetype);

        // Replace the original 6/12/18 schedule with five evenly spaced grants.
        var progressionEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(progressionEntries, bonusFeat);
        foreach (var level in BonusFeatLevels) {
            AddFeature(progressionEntries, level, bonusFeat);
        }
        progression.LevelEntries = progressionEntries.OrderBy(entry => entry.Level).ToArray();

        // Gendarme replaces every normal Cavalier bonus feat with its own selection.
        // Move that removal schedule alongside the base progression so the archetype
        // does not accidentally inherit the newly added selections.
        var gendarmeRemoveFeatures = gendarme.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(gendarmeRemoveFeatures, bonusFeat);
        foreach (var level in BonusFeatLevels) {
            AddFeature(gendarmeRemoveFeatures, level, bonusFeat);
        }
        gendarme.RemoveFeatures = gendarmeRemoveFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        Validate(cavalierClass, progression, bonusFeat, gendarme);
    }

    private static void ConfigureBraggart() {
        var braggart = FeatureConfigurator.For(BlueprintIds.CavalierBraggart)
            .SetDescription("ClassesReborn.CavalierBraggart.Description")
            .Configure();
        var fearAttackBonuses = braggart
            .GetComponents<AttackBonusAgainstFactOwner>()
            .ToArray();

        foreach (var component in fearAttackBonuses) {
            component.Descriptor = ModifierDescriptor.Profane;
        }

        if (fearAttackBonuses.Length != 3 ||
            fearAttackBonuses.Any(component =>
                component.Descriptor != ModifierDescriptor.Profane)) {
            throw new InvalidOperationException(
                "Braggart must have three profane attack bonuses against fear effects.");
        }
    }

    private static void ConfigureOrderChallengeBonuses() {
        var shroudChallenge = FeatureConfigurator.For(BlueprintIds.CavalierShroudChallenge)
            .SetDescription("ClassesReborn.CavalierShroudChallenge.Description")
            .Configure();
        var shroudAttackBonuses = shroudChallenge
            .GetComponents<AttackBonusAgainstFactOwner>()
            .ToArray();
        foreach (var component in shroudAttackBonuses) {
            component.Descriptor = ModifierDescriptor.Sacred;
        }
        if (shroudAttackBonuses.Length != 1 ||
            shroudAttackBonuses[0].Descriptor != ModifierDescriptor.Sacred) {
            throw new InvalidOperationException(
                "Order of the Shroud challenge must grant a sacred attack bonus.");
        }

        FeatureConfigurator.For(BlueprintIds.CavalierStarChallenge)
            .SetDescription("ClassesReborn.CavalierStarChallenge.Description")
            .Configure();
        var starChallengeBuff = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.CavalierStarChallengeBuff);
        var starSaveBonuses = starChallengeBuff
            .GetComponents<AddContextStatBonus>()
            .ToArray();
        foreach (var component in starSaveBonuses) {
            component.Descriptor = ModifierDescriptor.Sacred;
        }
        var expectedSaves = new[] {
            StatType.SaveFortitude,
            StatType.SaveReflex,
            StatType.SaveWill,
        };
        if (starSaveBonuses.Length != expectedSaves.Length ||
            expectedSaves.Any(save =>
                starSaveBonuses.Count(component => component.Stat == save) != 1) ||
            starSaveBonuses.Any(component =>
                component.Descriptor != ModifierDescriptor.Sacred)) {
            throw new InvalidOperationException(
                "Order of the Star challenge must grant sacred bonuses to all three saves.");
        }
    }

    private static void ConfigureByMyHonor() {
        FeatureConfigurator.For(BlueprintIds.CavalierByMyHonorSelection)
            .SetDescription("ClassesReborn.CavalierByMyHonor.Description")
            .Configure();
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CavalierByMyHonorSelection);

        var choices = selection.m_AllFeatures
            .Select(reference => reference?.Get())
            .Where(feature => feature != null)
            .Distinct()
            .ToArray();
        foreach (var choice in choices) {
            FeatureConfigurator.For(choice.AssetGuid.ToString())
                .SetDescription("ClassesReborn.CavalierByMyHonor.Description")
                .Configure();
        }

        var saveBuffs = new[] {
            (Id: BlueprintIds.CavalierByMyHonorFortitudeBuff, Stat: StatType.SaveFortitude),
            (Id: BlueprintIds.CavalierByMyHonorReflexBuff, Stat: StatType.SaveReflex),
            (Id: BlueprintIds.CavalierByMyHonorWillBuff, Stat: StatType.SaveWill),
        };
        foreach (var saveBuff in saveBuffs) {
            var buff = BlueprintTool.Get<BlueprintBuff>(saveBuff.Id);
            var bonuses = buff.GetComponents<AddContextStatBonus>().ToArray();
            if (bonuses.Length != 1 || bonuses[0].Stat != saveBuff.Stat) {
                throw new InvalidOperationException(
                    $"By My Honor {saveBuff.Stat} buff is invalid.");
            }
            bonuses[0].Descriptor = ModifierDescriptor.Luck;
            if (bonuses[0].Descriptor != ModifierDescriptor.Luck) {
                throw new InvalidOperationException(
                    $"By My Honor {saveBuff.Stat} bonus must be a luck bonus.");
            }
        }

        if (choices.Length != 15) {
            throw new InvalidOperationException(
                "By My Honor must retain all fifteen alignment and saving-throw choices.");
        }
    }

    private static void ConfigureFearmonger() {
        var fearmonger = FeatureConfigurator.For(BlueprintIds.FearmongerFeature)
            .SetDescription("ClassesReborn.CavalierFearmonger.Description")
            .Configure();
        var fearsomeLeader = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.FearsomeLeaderArchetype);
        var intimidationBonuses = fearmonger
            .GetComponents<AddContextStatBonus>()
            .Where(component => component.Stat == StatType.CheckIntimidate)
            .ToArray();
        var rankConfigs = fearmonger.GetComponents<ContextRankConfig>().ToArray();

        foreach (var component in intimidationBonuses) {
            component.Multiplier = 2;
        }

        if (CountFeatureAtLevel(fearsomeLeader.AddFeatures, fearmonger, 3) != 1 ||
            intimidationBonuses.Length != 1 ||
            intimidationBonuses[0].Multiplier != 2 ||
            rankConfigs.Length != 1 ||
            rankConfigs[0].m_Progression != ContextRankProgression.DivStep ||
            rankConfigs[0].m_StepLevel != 3) {
            throw new InvalidOperationException(
                "Fearmonger must grant +2 Intimidate per three-level breakpoint starting at level 3.");
        }
    }

    private static void ConfigureGloriousCharge() {
        var chargeIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.CavalierCharge).Icon;
        var gloriousChargeBuff = BuffConfigurator.New(
                "ClassesRebornGloriousChargeBuff",
                BlueprintIds.GloriousChargeBuff)
            .SetDisplayName("ClassesReborn.GloriousCharge.Name")
            .SetDescription("ClassesReborn.GloriousCharge.Description")
            .SetIcon(chargeIcon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new DerivativeStatBonus {
                BaseStat = StatType.Charisma,
                DerivativeStat = StatType.SaveFortitude,
                Descriptor = ModifierDescriptor.Morale,
            })
            .AddComponent(new DerivativeStatBonus {
                BaseStat = StatType.Charisma,
                DerivativeStat = StatType.SaveReflex,
                Descriptor = ModifierDescriptor.Morale,
            })
            .AddComponent(new DerivativeStatBonus {
                BaseStat = StatType.Charisma,
                DerivativeStat = StatType.SaveWill,
                Descriptor = ModifierDescriptor.Morale,
            })
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Charisma,
            })
            .Configure();

        var gloriousCharge = FeatureConfigurator.New(
                "ClassesRebornGloriousChargeFeature",
                BlueprintIds.GloriousChargeFeature)
            .SetDisplayName("ClassesReborn.GloriousCharge.Name")
            .SetDescription("ClassesReborn.GloriousCharge.Description")
            .SetIcon(chargeIcon)
            .SetIsClassFeature(true)
            .AddComponent(new GloriousChargeTrigger {
                m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.GloriousChargeBuff),
                DurationRounds = 2,
            })
            .Configure();

        var gendarme = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.GendarmeArchetype);
        var gendarmeAddFeatures = gendarme.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(gendarmeAddFeatures, 9, gloriousCharge);
        gendarme.AddFeatures = gendarmeAddFeatures.OrderBy(entry => entry.Level).ToArray();

        var saveBonuses = gloriousChargeBuff
            .GetComponents<DerivativeStatBonus>()
            .ToArray();
        var expectedSaves = new[] {
            StatType.SaveFortitude,
            StatType.SaveReflex,
            StatType.SaveWill,
        };
        var triggers = gloriousCharge.GetComponents<GloriousChargeTrigger>().ToArray();
        var recalculations = gloriousChargeBuff
            .GetComponents<RecalculateOnStatChange>()
            .ToArray();
        if (CountFeatureAtLevel(gendarme.AddFeatures, gloriousCharge, 9) != 1 ||
            triggers.Length != 1 ||
            triggers[0].m_Buff?.Get() != gloriousChargeBuff ||
            triggers[0].DurationRounds != 2 ||
            saveBonuses.Length != expectedSaves.Length ||
            expectedSaves.Any(save =>
                saveBonuses.Count(component => component.DerivativeStat == save) != 1) ||
            saveBonuses.Any(component =>
                component.BaseStat != StatType.Charisma ||
                component.Descriptor != ModifierDescriptor.Morale) ||
            recalculations.Length != 1 ||
            recalculations[0].Stat != StatType.Charisma) {
            throw new InvalidOperationException(
                "Glorious Charge level, duration, or Charisma-based save bonuses are invalid.");
        }
    }

    private static void ConfigureFrightfulGaze() {
        var featureIds = new[] {
            BlueprintIds.GhostRiderFrightfulGazeFeature,
            BlueprintIds.GhostRiderFrightfulGazeMindAffectingFeature,
            BlueprintIds.GhostRiderFrightfulGazeNoMindAffectingFeature,
        };
        foreach (var featureId in featureIds) {
            FeatureConfigurator.For(featureId)
                .SetDescription("ClassesReborn.CavalierFrightfulGaze.Description")
                .Configure();
        }

        var abilityIds = new[] {
            BlueprintIds.GhostRiderFrightfulGazeMindAffectingAbility,
            BlueprintIds.GhostRiderFrightfulGazeNoMindAffectingAbility,
        };
        var abilities = abilityIds
            .Select(abilityId => AbilityConfigurator.For(abilityId)
                .SetDescription("ClassesReborn.CavalierFrightfulGaze.Description")
                .SetActionType(UnitCommand.CommandType.Swift)
                .Configure())
            .ToArray();

        if (abilities.Length != 2 ||
            abilities.Distinct().Count() != 2 ||
            abilities.Any(ability =>
                ability.ActionType != UnitCommand.CommandType.Swift)) {
            throw new InvalidOperationException(
                "Both versions of Frightful Gaze must be swift actions.");
        }
    }

    private static void ConfigureKnightOfTheWallShieldBonuses() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.KnightOfTheWallArchetype);
        var affectedFeatures = new[] {
            (
                Feature: FeatureConfigurator.For(BlueprintIds.KnightOfTheWallDeflectiveShield)
                    .SetDescription("ClassesReborn.KnightOfTheWallDeflectiveShield.Description")
                    .Configure(),
                Level: 4,
                Name: "Deflective Shield"),
            (
                Feature: FeatureConfigurator.For(BlueprintIds.KnightOfTheWallSoulShield)
                    .SetDescription("ClassesReborn.KnightOfTheWallSoulShield.Description")
                    .Configure(),
                Level: 9,
                Name: "Soul Shield"),
        };

        foreach (var affectedFeature in affectedFeatures) {
            var rankConfigs = affectedFeature.Feature
                .GetComponents<ContextRankConfig>()
                .ToArray();
            foreach (var rankConfig in rankConfigs) {
                rankConfig.useShieldBonusAc = true;
            }

            if (CountFeatureAtLevel(
                    archetype.AddFeatures,
                    affectedFeature.Feature,
                    affectedFeature.Level) != 1 ||
                rankConfigs.Length != 1 ||
                !rankConfigs[0].userShieldBaseAc ||
                !rankConfigs[0].useShieldBonusAc ||
                !rankConfigs[0].useShieldFocusAc) {
                throw new InvalidOperationException(
                    $"Knight of the Wall {affectedFeature.Name} must include base, enhancement, and Shield Focus bonuses.");
            }
        }
    }

    private static void ConfigureStandardBearer() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.StandardBearerArchetype);
        var bannerOfSolaceFeature = FeatureConfigurator.For(
                BlueprintIds.StandardBearerBannerOfSolaceFeature)
            .SetDescription("ClassesReborn.StandardBearerBannerOfSolace.Description")
            .Configure();
        var bannerOfSolaceAbility = AbilityConfigurator.For(
                BlueprintIds.StandardBearerBannerOfSolaceAbility)
            .SetDescription("ClassesReborn.StandardBearerBannerOfSolace.Description")
            .SetActionType(UnitCommand.CommandType.Swift)
            .SetIsFullRoundAction(false)
            .Configure();

        var awesomePennonFeature = FeatureConfigurator.For(
                BlueprintIds.StandardBearerAwesomePennonFeature)
            .SetDescription("ClassesReborn.StandardBearerAwesomePennon.Description")
            .Configure();
        var awesomePennonBuff = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.StandardBearerAwesomePennonBuff);
        var attackBonuses = awesomePennonBuff
            .GetComponents<AddContextStatBonus>()
            .Where(component => component.Stat == StatType.AdditionalAttackBonus)
            .ToArray();
        var savingThrowBonuses = awesomePennonBuff
            .GetComponents<SavingThrowBonusAgainstDescriptor>()
            .ToArray();

        foreach (var attackBonus in attackBonuses) {
            attackBonus.Descriptor = ModifierDescriptor.Sacred;
        }
        foreach (var savingThrowBonus in savingThrowBonuses) {
            savingThrowBonus.ModifierDescriptor = ModifierDescriptor.Sacred;
        }

        if (CountFeatureAtLevel(archetype.AddFeatures, bannerOfSolaceFeature, 11) != 1 ||
            bannerOfSolaceAbility.ActionType != UnitCommand.CommandType.Swift ||
            bannerOfSolaceAbility.IsFullRoundAction ||
            CountFeatureAtLevel(archetype.AddFeatures, awesomePennonFeature, 20) != 1 ||
            attackBonuses.Length != 1 ||
            attackBonuses[0].Descriptor != ModifierDescriptor.Sacred ||
            savingThrowBonuses.Length != 1 ||
            savingThrowBonuses[0].ModifierDescriptor != ModifierDescriptor.Sacred) {
            throw new InvalidOperationException(
                "Standard Bearer Banner of Solace or Awesome Pennon configuration is invalid.");
        }
    }

    private static void Validate(
        BlueprintCharacterClass cavalierClass,
        BlueprintProgression progression,
        BlueprintFeatureSelection bonusFeat,
        BlueprintArchetype gendarme) {
        if (cavalierClass.Progression != progression ||
            CountFeature(progression.LevelEntries, bonusFeat) != BonusFeatLevels.Length ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, bonusFeat, level) != 1) ||
            progression.LevelEntries.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                CountFeatureAtLevel(progression.LevelEntries, bonusFeat, entry.Level) != 0)) {
            throw new InvalidOperationException(
                "Cavalier Bonus Feat progression must be exactly levels 2/6/10/14/18.");
        }

        if (CountFeature(gendarme.RemoveFeatures, bonusFeat) != BonusFeatLevels.Length ||
            BonusFeatLevels.Any(level =>
                CountFeatureAtLevel(gendarme.RemoveFeatures, bonusFeat, level) != 1) ||
            gendarme.RemoveFeatures.Any(entry =>
                !BonusFeatLevels.Contains(entry.Level) &&
                CountFeatureAtLevel(gendarme.RemoveFeatures, bonusFeat, entry.Level) != 0)) {
            throw new InvalidOperationException(
                "Gendarme must replace every Cavalier Bonus Feat on the new schedule.");
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
