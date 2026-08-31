using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class InquisitorRebalance {
    internal static void Configure() {
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.InquisitorProgression);
        var judgment = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.JudgmentFeature);
        var judgmentResource = BlueprintTool.Get<BlueprintAbilityResource>(
            BlueprintIds.InquisitorJudgmentResource);
        var faithHunterJudgment = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FaithHunterJudgmentFeature);
        var trueJudgment = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.InquisitorTrueJudgmentFeature);

        FeatureConfigurator.For(trueJudgment)
            .SetDescription("ClassesReborn.TrueJudgment.Description")
            .AddIncreaseActivatableAbilityGroupSize(ActivatableAbilityGroup.Judgment)
            .Configure();

        var experiencedJudgement = FeatureConfigurator.New(
                "ClassesRebornExperiencedJudgementFeature",
                BlueprintIds.ExperiencedJudgementFeature)
            .SetDisplayName("ClassesReborn.ExperiencedJudgement.Name")
            .SetDescription("ClassesReborn.ExperiencedJudgement.Description")
            .SetIcon(judgment.Icon)
            .SetIsClassFeature(true)
            .AddIncreaseResourceAmount(BlueprintIds.InquisitorJudgmentResource, 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Insight,
                stat: StatType.Wisdom,
                value: 4)
            .Configure();

        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(levelEntries, 10, experiencedJudgement);
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var faithHunter = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.FaithHunterArchetype);
        var judge = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.JudgeArchetype);
        var tacticalLeader = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TacticalLeaderArchetype);
        var excludedArchetypes = new[] {
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.LivingGrimoireArchetype),
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.MonsterTacticianArchetype),
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.SacredHuntsmasterArchetype),
            BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.SanctifiedSlayerArchetype),
        };

        foreach (var archetype in excludedArchetypes) {
            var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
            AddFeature(removals, 10, experiencedJudgement);
            archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
        }

        Validate(
            progression,
            experiencedJudgement,
            judgment,
            judgmentResource,
            faithHunterJudgment,
            trueJudgment,
            faithHunter,
            judge,
            tacticalLeader,
            excludedArchetypes);
    }

    private static void Validate(
        BlueprintProgression progression,
        BlueprintFeature experiencedJudgement,
        BlueprintFeature judgment,
        BlueprintAbilityResource judgmentResource,
        BlueprintFeature faithHunterJudgment,
        BlueprintFeature trueJudgment,
        BlueprintArchetype faithHunter,
        BlueprintArchetype judge,
        BlueprintArchetype tacticalLeader,
        IReadOnlyCollection<BlueprintArchetype> excludedArchetypes) {
        var resourceBonuses = experiencedJudgement
            .GetComponents<IncreaseResourceAmount>()
            .ToArray();
        var statBonuses = experiencedJudgement
            .GetComponents<AddStatBonus>()
            .ToArray();
        var trueJudgmentGroupIncreases = trueJudgment
            .GetComponents<IncreaseActivatableAbilityGroupSize>()
            .ToArray();

        if (CountFeatureAtLevel(progression.LevelEntries, experiencedJudgement, 10) != 1 ||
            progression.LevelEntries.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == experiencedJudgement) ?? 0) != 1 ||
            resourceBonuses.Length != 1 ||
            resourceBonuses[0].m_Resource?.Get() != judgmentResource ||
            resourceBonuses[0].Value != 2 ||
            statBonuses.Length != 1 ||
            statBonuses[0].Stat != StatType.Wisdom ||
            statBonuses[0].Value != 4 ||
            statBonuses[0].Descriptor != ModifierDescriptor.Insight ||
            trueJudgmentGroupIncreases.Count(component =>
                component.Group == ActivatableAbilityGroup.Judgment) != 1 ||
            trueJudgmentGroupIncreases.Length != 1 ||
            excludedArchetypes.Any(archetype =>
                CountFeatureAtLevel(archetype.RemoveFeatures, judgment, 1) != 1 ||
                CountFeatureAtLevel(archetype.RemoveFeatures, experiencedJudgement, 10) != 1 ||
                archetype.RemoveFeatures.Sum(entry =>
                    entry.m_Features?.Count(reference =>
                        reference?.Get() == experiencedJudgement) ?? 0) != 1) ||
            CountFeatureAtLevel(faithHunter.RemoveFeatures, judgment, 1) != 1 ||
            CountFeatureAtLevel(faithHunter.AddFeatures, faithHunterJudgment, 1) != 1 ||
            CountFeatureAtLevel(faithHunter.RemoveFeatures, experiencedJudgement, 10) != 0 ||
            CountFeatureAtLevel(judge.RemoveFeatures, judgment, 1) != 0 ||
            CountFeatureAtLevel(judge.RemoveFeatures, experiencedJudgement, 10) != 0 ||
            CountFeatureAtLevel(tacticalLeader.RemoveFeatures, judgment, 1) != 0 ||
            CountFeatureAtLevel(tacticalLeader.RemoveFeatures, experiencedJudgement, 10) != 0) {
            throw new InvalidOperationException(
                "Experienced Judgement must grant +2 Judgement uses and +4 insight Wisdom at Inquisitor level 10, remain available to Faith Hunter, Judge, and Tactical Leader, and be removed from Living Grimoire, Monster Tactician, Sacred Huntsmaster, and Sanctified Slayer. True Judgment must allow a fourth simultaneous Judgment effect.");
        }
    }

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0);

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
