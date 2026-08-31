using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace ClassesReborn;

internal static class RogueRebalance {
    private static readonly int[] FinesseTrainingLevels = { 3, 7, 11, 15, 19 };
    private static readonly int[] UndergroundChemistTalentLevels = {
        2, 4, 6, 8, 10, 12, 14, 16, 18, 20,
    };
    private static readonly int[] UndergroundChemistBombLevels = { 1, 5, 9, 13, 17 };
    private static readonly string[] UndergroundChemistDiscoveries = {
        BlueprintIds.AcidBombsFeature,
        BlueprintIds.BlindingBombsFeature,
        BlueprintIds.ChokingBombFeature,
        BlueprintIds.ExplosiveBombsFeature,
        BlueprintIds.FastBombsFeature,
        BlueprintIds.ForceBombsFeature,
        BlueprintIds.TanglefootBombsFeature,
    };
    private static readonly int[] SlipperyLevels = { 7, 12, 17 };
    private static readonly StatType[] ProfessionalCraftSkills = {
        StatType.SkillAthletics,
        StatType.SkillMobility,
        StatType.SkillThievery,
        StatType.SkillStealth,
        StatType.SkillKnowledgeArcana,
        StatType.SkillKnowledgeWorld,
        StatType.SkillLoreNature,
        StatType.SkillLoreReligion,
        StatType.SkillPerception,
        StatType.SkillPersuasion,
        StatType.SkillUseMagicDevice,
    };

    internal static void Configure() {
        ConfigureRepeatableCombatTrick();
        RequestedTalentRebalance.ConfigureRogueTalents();
        ConfigureSylvanTricksterUncannyDodge();

        var rogueClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.RogueClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.RogueProgression);
        var finesseTraining = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FinesseTrainingSelection);

        ConfigureFinesseTraining(rogueClass, progression, finesseTraining);
        ConfigureUndergroundChemist(finesseTraining);
        ConfigureKnifeMasterBladeSense();
        var slippery = ConfigureSlippery();
        var professionalCraft = ConfigureProfessionalCraft();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        foreach (var level in SlipperyLevels) {
            AddFeature(levelEntries, level, slippery);
        }
        AddFeature(levelEntries, 20, professionalCraft);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        Validate(progression, finesseTraining, slippery, professionalCraft);
    }

    private static void ConfigureSylvanTricksterUncannyDodge() {
        var rogueClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.RogueClass);
        var sylvanTrickster = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.SylvanTricksterArchetype);
        var talentSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SylvanTricksterTalentSelection);
        var uncannyDodgeTalent = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RogueUncannyDodgeTalent);

        if (!talentSelection.m_AllFeatures.Any(reference =>
                reference?.Get() == uncannyDodgeTalent)) {
            talentSelection.m_AllFeatures = talentSelection.m_AllFeatures
                .Append(BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.RogueUncannyDodgeTalent))
                .ToArray();
        }

        var existingPrerequisites = uncannyDodgeTalent
            .GetComponents<PrerequisiteArchetypeLevel>()
            .Where(prerequisite =>
                prerequisite.CharacterClass == rogueClass &&
                prerequisite.Archetype == sylvanTrickster)
            .ToArray();
        if (existingPrerequisites.Length == 0) {
            uncannyDodgeTalent = FeatureConfigurator.For(
                    BlueprintIds.RogueUncannyDodgeTalent)
                .AddComponent(new PrerequisiteArchetypeLevel {
                    Group = Prerequisite.GroupType.Any,
                    m_CharacterClass =
                        BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                            BlueprintIds.RogueClass),
                    m_Archetype = BlueprintTool.GetRef<BlueprintArchetypeReference>(
                        BlueprintIds.SylvanTricksterArchetype),
                    Level = 4,
                })
                .Configure();
        }

        var finalPrerequisites = uncannyDodgeTalent
            .GetComponents<PrerequisiteArchetypeLevel>()
            .Where(prerequisite =>
                prerequisite.CharacterClass == rogueClass &&
                prerequisite.Archetype == sylvanTrickster)
            .ToArray();
        if (talentSelection.m_AllFeatures.Count(reference =>
                reference?.Get() == uncannyDodgeTalent) != 1 ||
            finalPrerequisites.Length != 1 ||
            finalPrerequisites[0].Level != 4 ||
            finalPrerequisites[0].Group != Prerequisite.GroupType.Any) {
            throw new InvalidOperationException(
                "Sylvan Trickster Rogue Talents must offer Uncanny Dodge from archetype level 4 onward.");
        }
    }

    private static void ConfigureUndergroundChemist(
        BlueprintFeatureSelection finesseTraining) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.UndergroundChemistArchetype);
        var rogueTalents = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.RogueTalentSelection);
        var undergroundTalents = FeatureSelectionConfigurator.For(
                BlueprintIds.UndergroundChemistTalentSelection)
            .SetDisplayName("ClassesReborn.UndergroundChemistTalent.Name")
            .SetDescription("ClassesReborn.UndergroundChemistTalent.Description")
            .Configure();
        var bombs = FeatureConfigurator.For(BlueprintIds.AlchemistBombsFeature)
            .AddComponent(new IncreaseResourcesByClass {
                m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                    BlueprintIds.AlchemistBombsResource),
                m_CharacterClass = null,
                m_Archetype = BlueprintTool.GetRef<BlueprintArchetypeReference>(
                    BlueprintIds.UndergroundChemistArchetype),
                Stat = StatType.Unknown,
                BaseValue = 0,
            })
            .Configure();

        var discoveryFeatures = UndergroundChemistDiscoveries
            .Select(BlueprintTool.Get<BlueprintFeature>)
            .ToArray();
        var choices = rogueTalents.m_AllFeatures
            .Concat(discoveryFeatures.Select(feature =>
                BlueprintTool.GetRef<BlueprintFeatureReference>(
                    feature.AssetGuid.ToString())))
            .GroupBy(reference => reference.deserializedGuid)
            .Select(group => group.First())
            .ToArray();
        undergroundTalents.m_AllFeatures = choices;
        undergroundTalents.m_Features = choices.ToArray();

        ConfigureUndergroundChemistDiscoveryPrerequisites(discoveryFeatures);

        var additions = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(additions, undergroundTalents);
        RemoveFeature(additions, bombs);
        foreach (var level in UndergroundChemistTalentLevels) {
            AddFeature(additions, level, undergroundTalents);
        }
        foreach (var level in UndergroundChemistBombLevels) {
            AddFeature(additions, level, bombs);
        }
        archetype.AddFeatures = additions.OrderBy(entry => entry.Level).ToArray();

        var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(removals, rogueTalents);
        RemoveFeature(removals, finesseTraining);
        foreach (var level in UndergroundChemistTalentLevels) {
            AddFeature(removals, level, rogueTalents);
        }
        foreach (var level in FinesseTrainingLevels) {
            AddFeature(removals, level, finesseTraining);
        }
        archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();

        ValidateUndergroundChemist(
            archetype,
            rogueTalents,
            undergroundTalents,
            finesseTraining,
            bombs,
            discoveryFeatures);
    }

    private static void ConfigureUndergroundChemistDiscoveryPrerequisites(
        IEnumerable<BlueprintFeature> discoveries) {
        var alchemistClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.AlchemistClass);
        var rogueClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.RogueClass);
        var undergroundChemist = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.UndergroundChemistArchetype);

        foreach (var discovery in discoveries) {
            var alchemistLevelPrerequisites = discovery
                .GetComponents<PrerequisiteClassLevel>()
                .Where(prerequisite => prerequisite.CharacterClass == alchemistClass)
                .ToArray();
            foreach (var prerequisite in alchemistLevelPrerequisites) {
                prerequisite.Group = Prerequisite.GroupType.Any;
                FeatureConfigurator.For(discovery.AssetGuid.ToString())
                    .AddComponent(new PrerequisiteArchetypeLevel {
                        Group = Prerequisite.GroupType.Any,
                        m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                            BlueprintIds.RogueClass),
                        m_Archetype = BlueprintTool.GetRef<BlueprintArchetypeReference>(
                            BlueprintIds.UndergroundChemistArchetype),
                        Level = prerequisite.Level,
                    })
                    .Configure();
            }

            var translatedPrerequisites = discovery
                .GetComponents<PrerequisiteArchetypeLevel>()
                .Where(prerequisite =>
                    prerequisite.CharacterClass == rogueClass &&
                    prerequisite.Archetype == undergroundChemist)
                .ToArray();
            if (translatedPrerequisites.Length != alchemistLevelPrerequisites.Length ||
                translatedPrerequisites.Any(prerequisite =>
                    prerequisite.Group != Prerequisite.GroupType.Any)) {
                throw new InvalidOperationException(
                    $"Discovery {discovery.name} did not receive the matching Underground Chemist level prerequisite.");
            }
        }
    }

    private static void ValidateUndergroundChemist(
        BlueprintArchetype archetype,
        BlueprintFeatureSelection rogueTalents,
        BlueprintFeatureSelection undergroundTalents,
        BlueprintFeatureSelection finesseTraining,
        BlueprintFeature bombs,
        IReadOnlyCollection<BlueprintFeature> discoveries) {
        var expectedChoiceIds = rogueTalents.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .Concat(discoveries.Select(feature => feature.AssetGuid))
            .ToHashSet();
        var actualChoiceIds = undergroundTalents.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        var resourceScalers = bombs.GetComponents<IncreaseResourcesByClass>()
            .Where(component =>
                component.Resource?.AssetGuid.ToString() == BlueprintIds.AlchemistBombsResource &&
                component.CharacterClass == null &&
                component.Archetype?.AssetGuid.ToString() ==
                    BlueprintIds.UndergroundChemistArchetype)
            .ToArray();

        if (!expectedChoiceIds.SetEquals(actualChoiceIds) ||
            undergroundTalents.m_AllFeatures.Length != expectedChoiceIds.Count ||
            undergroundTalents.m_Features.Length != expectedChoiceIds.Count ||
            UndergroundChemistTalentLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, undergroundTalents, level) != 1 ||
                CountFeatureAtLevel(archetype.RemoveFeatures, rogueTalents, level) != 1) ||
            CountFeature(archetype.AddFeatures, undergroundTalents) !=
                UndergroundChemistTalentLevels.Length ||
            CountFeature(archetype.RemoveFeatures, rogueTalents) !=
                UndergroundChemistTalentLevels.Length ||
            UndergroundChemistBombLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, bombs, level) != 1) ||
            CountFeature(archetype.AddFeatures, bombs) !=
                UndergroundChemistBombLevels.Length ||
            FinesseTrainingLevels.Any(level =>
                CountFeatureAtLevel(archetype.RemoveFeatures, finesseTraining, level) != 1) ||
            CountFeature(archetype.RemoveFeatures, finesseTraining) !=
                FinesseTrainingLevels.Length ||
            resourceScalers.Length != 1 ||
            resourceScalers[0].Stat != StatType.Unknown ||
            resourceScalers[0].BaseValue != 0) {
            throw new InvalidOperationException(
                "Underground Chemist must replace all Rogue Talents and Finesse Training, gain ten expanded talent selections, and gain five Alchemist Bomb ranks with Rogue-level daily uses.");
        }
    }

    private static void ConfigureRepeatableCombatTrick() {
        var combatTrick = FeatureSelectionConfigurator.For(BlueprintIds.CombatTrick)
            .SetRanks(20)
            .Configure();

        var nativeCombatTrick = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CombatTrick);
        if (combatTrick != nativeCombatTrick || nativeCombatTrick.Ranks != 20) {
            throw new InvalidOperationException(
                "The native Rogue Combat Trick selection must be repeatable for 20 ranks.");
        }
    }

    private static void ConfigureFinesseTraining(
        BlueprintCharacterClass rogueClass,
        BlueprintProgression progression,
        BlueprintFeatureSelection finesseTraining) {
        var archetypesTradingFinesseTraining = rogueClass.Archetypes
            .Where(archetype => CountFeature(
                archetype.RemoveFeatures,
                finesseTraining) > 0)
            .ToArray();

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(levelEntries, 7, finesseTraining);
        AddFeature(levelEntries, 15, finesseTraining);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        foreach (var archetype in archetypesTradingFinesseTraining) {
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            AddFeature(removals, 7, finesseTraining);
            AddFeature(removals, 15, finesseTraining);
            archetype.RemoveFeatures = removals
                .OrderBy(entry => entry.Level)
                .ToArray();

            if (CountFeatureAtLevel(archetype.RemoveFeatures, finesseTraining, 7) != 1 ||
                CountFeatureAtLevel(archetype.RemoveFeatures, finesseTraining, 15) != 1) {
                throw new InvalidOperationException(
                    $"Rogue archetype {archetype.name} must preserve its Finesse Training tradeoff at levels 7 and 15.");
            }
        }
    }

    private static BlueprintFeature ConfigureSlippery() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FastStealth).Icon;
        return FeatureConfigurator.New(
                "ClassesRebornRogueSlipperyFeature",
                BlueprintIds.SlipperyFeature)
            .SetDisplayName("ClassesReborn.Slippery.Name")
            .SetDescription("ClassesReborn.Slippery.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetRanks(3)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Dodge,
                stat: StatType.AC,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SaveReflex,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SkillStealth,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.SkillMobility,
                value: 1)
            .Configure();
    }

    private static void ConfigureKnifeMasterBladeSense() {
        var bladeSense = FeatureConfigurator.For(BlueprintIds.KnifeMasterBladeSense)
            .SetDescription("ClassesReborn.KnifeMasterBladeSense.Description")
            .AddComponent(new ACBonusAgainstWeaponGroup {
                ArmorClassBonus = 1,
                FighterGroup = WeaponFighterGroup.BladesHeavy,
                Descriptor = ModifierDescriptor.Dodge,
            })
            .Configure();

        var bladeBonuses = bladeSense
            .GetComponents<ACBonusAgainstWeaponGroup>()
            .ToArray();
        if (bladeSense.Ranks != 6 ||
            bladeBonuses.Length != 2 ||
            bladeBonuses.Count(component =>
                component.FighterGroup == WeaponFighterGroup.BladesLight &&
                component.ArmorClassBonus == 1 &&
                component.Descriptor == ModifierDescriptor.Dodge) != 1 ||
            bladeBonuses.Count(component =>
                component.FighterGroup == WeaponFighterGroup.BladesHeavy &&
                component.ArmorClassBonus == 1 &&
                component.Descriptor == ModifierDescriptor.Dodge) != 1) {
            throw new InvalidOperationException(
                "Knife Master Blade Sense must grant its rank-scaled Dodge AC bonus against both light and heavy blades.");
        }
    }

    private static BlueprintFeature ConfigureProfessionalCraft() {
        var masterStrike = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MasterStrike);
        var configurator = FeatureConfigurator.New(
                "ClassesRebornRogueProfessionalCraftFeature",
                BlueprintIds.ProfessionalCraftFeature)
            .SetDisplayName("ClassesReborn.ProfessionalCraft.Name")
            .SetDescription("ClassesReborn.ProfessionalCraft.Description")
            .SetIcon(masterStrike.Icon)
            .SetIsClassFeature(true);

        foreach (var skill in ProfessionalCraftSkills) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: skill,
                value: 2);
        }

        return configurator
            .AddComponent(new AttackBonusConditional {
                CheckWielder = false,
                Descriptor = ModifierDescriptor.UntypedStackable,
                Bonus = ContextValues.Constant(2),
                Conditions = new ConditionsChecker {
                    Operation = Operation.And,
                    Conditions = new Kingmaker.ElementsSystem.Condition[] {
                        new ContextConditionIsFlatFooted(),
                    },
                },
            })
            .Configure();
    }

    private static void Validate(
        BlueprintProgression progression,
        BlueprintFeatureSelection finesseTraining,
        BlueprintFeature slippery,
        BlueprintFeature professionalCraft) {
        var slipperyBonuses = slippery.GetComponents<AddStatBonus>().ToArray();
        var professionalSkillBonuses = professionalCraft
            .GetComponents<AddStatBonus>()
            .ToArray();
        var professionalAttackBonuses = professionalCraft
            .GetComponents<AttackBonusConditional>()
            .ToArray();

        if (FinesseTrainingLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, finesseTraining, level) != 1) ||
            CountFeature(progression.LevelEntries, finesseTraining) !=
                FinesseTrainingLevels.Length ||
            SlipperyLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, slippery, level) != 1) ||
            CountFeature(progression.LevelEntries, slippery) != SlipperyLevels.Length ||
            slippery.Ranks != 3 ||
            slipperyBonuses.Length != 4 ||
            slipperyBonuses.Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 1 &&
                component.Descriptor == ModifierDescriptor.Dodge) != 1 ||
            slipperyBonuses.Count(component =>
                component.Stat == StatType.SaveReflex &&
                component.Value == 1 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            slipperyBonuses.Count(component =>
                component.Stat == StatType.SkillStealth &&
                component.Value == 1 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            slipperyBonuses.Count(component =>
                component.Stat == StatType.SkillMobility &&
                component.Value == 1 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, professionalCraft, 20) != 1 ||
            CountFeature(progression.LevelEntries, professionalCraft) != 1 ||
            professionalSkillBonuses.Length != ProfessionalCraftSkills.Length ||
            ProfessionalCraftSkills.Any(skill =>
                professionalSkillBonuses.Count(component =>
                    component.Stat == skill &&
                    component.Value == 2 &&
                    component.Descriptor == ModifierDescriptor.UntypedStackable) != 1) ||
            professionalAttackBonuses.Length != 1 ||
            professionalAttackBonuses[0].Bonus.ValueType != ContextValueType.Simple ||
            professionalAttackBonuses[0].Bonus.Value != 2 ||
            professionalAttackBonuses[0].Descriptor !=
                ModifierDescriptor.UntypedStackable ||
            professionalAttackBonuses[0].Conditions.Conditions.Length != 1 ||
            professionalAttackBonuses[0].Conditions.Conditions[0] is not
                ContextConditionIsFlatFooted) {
            throw new InvalidOperationException(
                "Rogue changes must grant Finesse Training at levels 3/7/11/15/19, Slippery ranks at 7/12/17, and Professional Craft at level 20 with the requested bonuses.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0) ?? 0;

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
        BlueprintFeatureBase feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
        entries.RemoveAll(entry => entry.m_Features == null || entry.m_Features.Count == 0);
    }
}

[HarmonyPatch(typeof(BindAbilitiesToClass), "GetLevel")]
internal static class UndergroundChemistBombScalingPatch {
    [HarmonyPostfix]
    private static void AddUndergroundChemistLevels(
        BindAbilitiesToClass __instance,
        UnitDescriptor unit,
        ref int __result) {
        if (!Main.Settings.Rogue ||
            __instance.Fact?.Blueprint?.AssetGuid.ToString() !=
                BlueprintIds.AlchemistBombsFeature ||
            unit?.Progression?.Classes == null) {
            return;
        }

        var undergroundChemistData = unit.Progression.Classes.FirstOrDefault(data =>
            data.CharacterClass?.AssetGuid.ToString() == BlueprintIds.RogueClass &&
            data.Archetypes.Any(archetype =>
                archetype.AssetGuid.ToString() ==
                    BlueprintIds.UndergroundChemistArchetype));
        if (undergroundChemistData != null) {
            __result += undergroundChemistData.Level;
        }
    }
}
