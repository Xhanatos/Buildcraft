using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;

namespace ClassesReborn;

internal static class TrapfindingRebalance {
    private static readonly (string Id, string DescriptionKey)[] TrapfindingFeatures = {
        (BlueprintIds.Trapfinding, "ClassesReborn.Trapfinding.Description"),
        (BlueprintIds.SlayerTalentTrapfinding, "ClassesReborn.Trapfinding.Slayer.Description"),
        (BlueprintIds.SlayerTrapfinding, "ClassesReborn.Trapfinding.Slayer.Description"),
        (BlueprintIds.OracleSeekerTrapfinding, "ClassesReborn.Trapfinding.OracleSeeker.Description"),
        (BlueprintIds.EspionageExpertTrapfinding, "ClassesReborn.Trapfinding.EspionageExpert.Description"),
        (BlueprintIds.SorcererSeekerTrapfinding, "ClassesReborn.Trapfinding.SorcererSeeker.Description"),
    };

    internal static void Configure() {
        var mechanicalFeatures = 0;

        foreach (var (id, descriptionKey) in TrapfindingFeatures) {
            var feature = BlueprintTool.Get<BlueprintFeature>(id);
            var existingBonuses = feature
                .GetComponents<AddContextStatBonus>()
                .Where(IsTrapfindingBonus)
                .ToArray();

            // SlayerTalentTrapfinding is a visible wrapper around the separate
            // SlayerTrapfinding mechanics feature. Keep its tooltip current,
            // but do not add a second scaling bonus to the wrapper.
            if (existingBonuses.Length == 0) {
                FeatureConfigurator.For(id)
                    .SetDescription(descriptionKey)
                    .Configure();
                continue;
            }

            var template = existingBonuses.FirstOrDefault(
                    component => component.Stat == StatType.SkillPerception)
                ?? existingBonuses[0];
            var configurator = FeatureConfigurator.For(id)
                .SetDescription(descriptionKey);

            if (!existingBonuses.Any(
                    component => component.Stat == StatType.SkillPerception)) {
                configurator.AddContextStatBonus(
                    StatType.SkillPerception,
                    template.Value,
                    template.Descriptor);
            }
            if (!existingBonuses.Any(
                    component => component.Stat == StatType.SkillThievery)) {
                configurator.AddContextStatBonus(
                    StatType.SkillThievery,
                    template.Value,
                    template.Descriptor);
            }

            var configured = configurator.Configure();
            var configuredBonuses = configured
                .GetComponents<AddContextStatBonus>()
                .Where(IsTrapfindingBonus)
                .ToArray();
            if (!configuredBonuses.Any(
                    component => component.Stat == StatType.SkillPerception) ||
                !configuredBonuses.Any(
                    component => component.Stat == StatType.SkillThievery)) {
                throw new InvalidOperationException(
                    $"Trapfinding feature {feature.name} must apply its scaling bonus to both Perception and Trickery.");
            }

            mechanicalFeatures++;
        }

        if (mechanicalFeatures < 5) {
            throw new InvalidOperationException(
                $"Expected at least five mechanical Trapfinding features, but configured {mechanicalFeatures}.");
        }
    }

    private static bool IsTrapfindingBonus(AddContextStatBonus component) =>
        (component.Stat == StatType.SkillPerception ||
         component.Stat == StatType.SkillThievery) &&
        component.Value.ValueType == ContextValueType.Rank;
}
