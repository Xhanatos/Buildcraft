using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class HunterRebalance {
    internal static void Configure() {
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.HunterProgression);
        var animalCompanionSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.HunterAnimalCompanionSelection);
        var preciseCompanion = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.HunterPreciseCompanion);
        var masterHunter = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MasterHunterFeature);
        var colludingScoundrel = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ColludingScoundrelArchetype);
        var sneakAttack = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.SneakAttackFeature);
        var divineHound = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.DivineHoundArchetype);
        var judgmentFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.JudgmentFeature);
        var everlastingJudgment = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.EverlastingJudgmentFeature);
        var forester = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ForesterArchetype);
        var combatFeat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FighterBonusFeatSelection);
        var uncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var tandemExecutioner = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TandemExecutionerArchetype);
        var tandemTechniques = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.TandemExecutionerTechniquesSelection);

        var tandemHunterMark = BuffConfigurator.New(
                "ClassesRebornTandemExecutionHunterMarkBuff",
                BlueprintIds.TandemExecutionHunterMarkBuff)
            .SetDisplayName("ClassesReborn.TandemExecution.Name")
            .SetDescription("ClassesReborn.TandemExecution.EffectDescription")
            .SetIcon(tandemTechniques.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddUniqueBuff()
            .AddCombatStateTrigger(
                ActionsBuilder.New(),
                ActionsBuilder.New().RemoveSelf())
            .Configure();

        var tandemPetMark = BuffConfigurator.New(
                "ClassesRebornTandemExecutionPetMarkBuff",
                BlueprintIds.TandemExecutionPetMarkBuff)
            .SetDisplayName("ClassesReborn.TandemExecution.Name")
            .SetDescription("ClassesReborn.TandemExecution.EffectDescription")
            .SetIcon(tandemTechniques.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddUniqueBuff()
            .AddCombatStateTrigger(
                ActionsBuilder.New(),
                ActionsBuilder.New().RemoveSelf())
            .Configure();

        var tandemPetFeature = FeatureConfigurator.New(
                "ClassesRebornTandemExecutionPetFeature",
                BlueprintIds.TandemExecutionPetFeature)
            .SetDisplayName("ClassesReborn.TandemExecution.Name")
            .SetDescription("ClassesReborn.TandemExecution.EffectDescription")
            .SetIcon(tandemTechniques.Icon)
            .SetIsClassFeature(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new TandemExecutionController {
                IsPet = true,
                m_MyPetBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionerMyPetBuff),
                m_HunterMark = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionHunterMarkBuff),
                m_PetMark = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionPetMarkBuff),
            })
            .Configure();

        var tandemExecution = FeatureConfigurator.New(
                "ClassesRebornTandemExecutionFeature",
                BlueprintIds.TandemExecutionFeature)
            .SetDisplayName("ClassesReborn.TandemExecution.Name")
            .SetDescription("ClassesReborn.TandemExecution.Description")
            .SetIcon(tandemTechniques.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new TandemExecutionController {
                IsPet = false,
                m_MyPetBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionerMyPetBuff),
                m_HunterMark = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionHunterMarkBuff),
                m_PetMark = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.TandemExecutionPetMarkBuff),
            })
            .AddFeatureToPet(
                BlueprintIds.TandemExecutionPetFeature,
                PetType.AnimalCompanion)
            .Configure();

        var tandemExecutionerFeatures = tandemExecutioner.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(tandemExecutionerFeatures, 1, tandemTechniques);
        AddFeature(tandemExecutionerFeatures, 10, tandemExecution);
        tandemExecutioner.AddFeatures = tandemExecutionerFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        FeatureConfigurator.For(preciseCompanion)
            .SetDescription("ClassesReborn.HunterPreciseCompanion.Description")
            .Configure();

        var nurturedGrowthPet = FeatureConfigurator.New(
                "ClassesRebornNurturedGrowthPetFeature",
                BlueprintIds.NurturedGrowthPetFeature)
            .SetDisplayName("ClassesReborn.NurturedGrowth.Name")
            .SetDescription("ClassesReborn.NurturedGrowth.Description")
            .SetIcon(animalCompanionSelection.Icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Strength,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Dexterity,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Constitution,
                value: 2)
            .Configure();

        var nurturedGrowth = FeatureConfigurator.New(
                "ClassesRebornNurturedGrowthFeature",
                BlueprintIds.NurturedGrowthFeature)
            .SetDisplayName("ClassesReborn.NurturedGrowth.Name")
            .SetDescription("ClassesReborn.NurturedGrowth.Description")
            .SetIcon(animalCompanionSelection.Icon)
            .SetIsClassFeature(true)
            .AddFeatureToPet(
                BlueprintIds.NurturedGrowthPetFeature,
                PetType.AnimalCompanion)
            .Configure();

        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(levelEntries, 5, preciseCompanion);
        AddFeature(levelEntries, 13, nurturedGrowth);
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var colludingScoundrelFeatures = colludingScoundrel.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        foreach (var level in new[] { 5, 10, 15 }) {
            AddFeature(colludingScoundrelFeatures, level, sneakAttack);
        }
        colludingScoundrel.AddFeatures = colludingScoundrelFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var judgmentBonusBuff = BuffConfigurator.New(
                "ClassesRebornDivineHoundJudgmentBonusBuff",
                BlueprintIds.DivineHoundJudgmentBonusBuff)
            .SetDisplayName("ClassesReborn.EternalJudgment.Name")
            .SetDescription("ClassesReborn.EternalJudgment.EffectDescription")
            .SetIcon(judgmentFeature.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Dodge,
                stat: StatType.AC,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.BaseAttackBonus,
                value: 2)
            .Configure();

        var judgmentReferences = BlueprintIds.JudgmentBuffs
            .Select(BlueprintTool.GetRef<BlueprintBuffReference>)
            .ToArray();
        var judgmentController = FeatureConfigurator.New(
                "ClassesRebornDivineHoundJudgmentController",
                BlueprintIds.DivineHoundJudgmentController)
            .SetDisplayName("ClassesReborn.EternalJudgment.Name")
            .SetDescription("ClassesReborn.EternalJudgment.EffectDescription")
            .SetIcon(judgmentFeature.Icon)
            .SetIsClassFeature(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new DivineHoundJudgmentBonusController {
                m_BonusBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.DivineHoundJudgmentBonusBuff),
                m_JudgmentBuffs = judgmentReferences,
            })
            .Configure();

        var eternalJudgment = FeatureConfigurator.New(
                "ClassesRebornDivineHoundEternalJudgmentFeature",
                BlueprintIds.DivineHoundEternalJudgmentFeature)
            .SetDisplayName("ClassesReborn.EternalJudgment.Name")
            .SetDescription("ClassesReborn.EternalJudgment.Description")
            .SetIcon(judgmentFeature.Icon)
            .SetIsClassFeature(true)
            .AddFacts(new() {
                BlueprintIds.EverlastingJudgmentFeature,
                BlueprintIds.DivineHoundJudgmentController,
            })
            .AddFeatureToPet(
                BlueprintIds.DivineHoundJudgmentController,
                PetType.AnimalCompanion)
            .Configure();

        var divineHoundFeatures = divineHound.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(divineHoundFeatures, 20, eternalJudgment);
        divineHound.AddFeatures = divineHoundFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var foresterFeatures = forester.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(foresterFeatures, combatFeat);
        foreach (var level in new[] { 2, 6, 10, 14, 18 }) {
            AddFeature(foresterFeatures, level, combatFeat);
        }
        AddFeature(foresterFeatures, 2, uncannyDodge);
        forester.AddFeatures = foresterFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var foresterRemovals = forester.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        AddFeature(foresterRemovals, 13, nurturedGrowth);
        forester.RemoveFeatures = foresterRemovals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var bonusBuff = BuffConfigurator.New(
                "ClassesRebornMasterHunterAnimalFocusBonusBuff",
                BlueprintIds.MasterHunterAnimalFocusBonusBuff)
            .SetDisplayName("ClassesReborn.MasterHunter.Name")
            .SetDescription("ClassesReborn.MasterHunter.EffectDescription")
            .SetIcon(masterHunter.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Dodge,
                stat: StatType.AC,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.BaseAttackBonus,
                value: 2)
            .Configure();

        var focusReferences = BlueprintIds.HunterAnimalFocusEffects
            .Select(BlueprintTool.GetRef<BlueprintUnitFactReference>)
            .ToArray();
        var controller = FeatureConfigurator.New(
                "ClassesRebornMasterHunterAnimalFocusController",
                BlueprintIds.MasterHunterAnimalFocusController)
            .SetDisplayName("ClassesReborn.MasterHunter.Name")
            .SetDescription("ClassesReborn.MasterHunter.EffectDescription")
            .SetIcon(masterHunter.Icon)
            .SetIsClassFeature(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new MasterHunterAnimalFocusBonusController {
                m_BonusBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.MasterHunterAnimalFocusBonusBuff),
                m_AnimalFocusEffects = focusReferences,
            })
            .Configure();

        FeatureConfigurator.For(masterHunter)
            .SetDescription("ClassesReborn.MasterHunter.Description")
            .AddFacts(new() { BlueprintIds.MasterHunterAnimalFocusController })
            .AddFeatureToPet(
                BlueprintIds.MasterHunterAnimalFocusController,
                PetType.AnimalCompanion)
            .Configure();

        Validate(
            progression,
            preciseCompanion,
            nurturedGrowth,
            nurturedGrowthPet,
            colludingScoundrel,
            sneakAttack,
            divineHound,
            eternalJudgment,
            everlastingJudgment,
            judgmentController,
            judgmentBonusBuff,
            forester,
            combatFeat,
            uncannyDodge,
            tandemExecutioner,
            tandemTechniques,
            tandemExecution,
            tandemPetFeature,
            tandemHunterMark,
            tandemPetMark,
            masterHunter,
            controller,
            bonusBuff);
    }

    private static void Validate(
        BlueprintProgression progression,
        BlueprintFeatureSelection preciseCompanion,
        BlueprintFeature nurturedGrowth,
        BlueprintFeature nurturedGrowthPet,
        BlueprintArchetype colludingScoundrel,
        BlueprintFeature sneakAttack,
        BlueprintArchetype divineHound,
        BlueprintFeature eternalJudgment,
        BlueprintFeature everlastingJudgment,
        BlueprintFeature judgmentController,
        BlueprintBuff judgmentBonusBuff,
        BlueprintArchetype forester,
        BlueprintFeatureSelection combatFeat,
        BlueprintFeature uncannyDodge,
        BlueprintArchetype tandemExecutioner,
        BlueprintFeatureSelection tandemTechniques,
        BlueprintFeature tandemExecution,
        BlueprintFeature tandemPetFeature,
        BlueprintBuff tandemHunterMark,
        BlueprintBuff tandemPetMark,
        BlueprintFeature masterHunter,
        BlueprintFeature controller,
        BlueprintBuff bonusBuff) {
        var controllerComponents = controller
            .GetComponents<MasterHunterAnimalFocusBonusController>()
            .ToArray();
        var nurturedGrowthPetComponents = nurturedGrowth
            .GetComponents<AddFeatureToPet>()
            .ToArray();
        var nurturedGrowthBonuses = nurturedGrowthPet
            .GetComponents<AddStatBonus>()
            .ToArray();
        var judgmentControllerComponents = judgmentController
            .GetComponents<DivineHoundJudgmentBonusController>()
            .ToArray();
        var eternalJudgmentPetComponents = eternalJudgment
            .GetComponents<AddFeatureToPet>()
            .ToArray();
        var eternalJudgmentFacts = eternalJudgment.GetComponents<AddFacts>()
            .SelectMany(component => component.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
            .ToArray();
        var judgmentBonuses = judgmentBonusBuff.GetComponents<AddStatBonus>().ToArray();
        var expectedJudgments = BlueprintIds.JudgmentBuffs
            .Select(BlueprintTool.Get<BlueprintBuff>)
            .ToArray();
        var petComponents = masterHunter.GetComponents<AddFeatureToPet>().ToArray();
        var addedFacts = masterHunter.GetComponents<AddFacts>()
            .SelectMany(component => component.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
            .Count(reference => reference?.Get() == controller);
        var bonuses = bonusBuff.GetComponents<AddStatBonus>().ToArray();
        var expectedFocuses = BlueprintIds.HunterAnimalFocusEffects
            .Select(BlueprintTool.Get<BlueprintUnitFact>)
            .ToArray();
        var tandemControllers = tandemExecution
            .GetComponents<TandemExecutionController>()
            .ToArray();
        var tandemPetControllers = tandemPetFeature
            .GetComponents<TandemExecutionController>()
            .ToArray();
        var tandemPetGrants = tandemExecution
            .GetComponents<AddFeatureToPet>()
            .ToArray();
        var tandemHunterMarkers = tandemHunterMark
            .GetComponents<Kingmaker.UnitLogic.FactLogic.UniqueBuff>()
            .ToArray();
        var tandemPetMarkers = tandemPetMark
            .GetComponents<Kingmaker.UnitLogic.FactLogic.UniqueBuff>()
            .ToArray();

        if (CountFeatureAtLevel(progression, preciseCompanion, 2) != 1 ||
            CountFeatureAtLevel(progression, preciseCompanion, 5) != 1 ||
            progression.LevelEntries.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == preciseCompanion) ?? 0) != 2 ||
            preciseCompanion.m_Features.Length != 2 ||
            preciseCompanion.m_Features.Distinct().Count() != 2 ||
            CountFeatureAtLevel(progression, nurturedGrowth, 13) != 1 ||
            progression.LevelEntries.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == nurturedGrowth) ?? 0) != 1 ||
            nurturedGrowthPetComponents.Length != 1 ||
            nurturedGrowthPetComponents[0].m_Feature?.Get() != nurturedGrowthPet ||
            nurturedGrowthPetComponents[0].m_PetType != PetType.AnimalCompanion ||
            nurturedGrowthBonuses.Length != 4 ||
            nurturedGrowthBonuses.Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.NaturalArmor) != 1 ||
            new[] { StatType.Strength, StatType.Dexterity, StatType.Constitution }
                .Any(stat => nurturedGrowthBonuses.Count(component =>
                    component.Stat == stat &&
                    component.Value == 2 &&
                    component.Descriptor == ModifierDescriptor.UntypedStackable) != 1) ||
            new[] { 5, 10, 15 }.Any(level =>
                CountFeatureAtLevel(colludingScoundrel.AddFeatures, sneakAttack, level) != 1) ||
            colludingScoundrel.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == sneakAttack) ?? 0) != 3 ||
            CountFeatureAtLevel(divineHound.AddFeatures, eternalJudgment, 20) != 1 ||
            divineHound.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == eternalJudgment) ?? 0) != 1 ||
            CountFeatureAtLevel(divineHound.RemoveFeatures, masterHunter, 20) != 1 ||
            eternalJudgmentFacts.Count(reference =>
                reference?.Get() == everlastingJudgment) != 1 ||
            eternalJudgmentFacts.Count(reference =>
                reference?.Get() == judgmentController) != 1 ||
            eternalJudgmentFacts.Length != 2 ||
            eternalJudgmentPetComponents.Length != 1 ||
            eternalJudgmentPetComponents[0].m_Feature?.Get() != judgmentController ||
            eternalJudgmentPetComponents[0].m_PetType != PetType.AnimalCompanion ||
            judgmentControllerComponents.Length != 1 ||
            judgmentControllerComponents[0].m_BonusBuff?.Get() != judgmentBonusBuff ||
            judgmentControllerComponents[0].m_JudgmentBuffs.Length != expectedJudgments.Length ||
            expectedJudgments.Any(expected =>
                judgmentControllerComponents[0].m_JudgmentBuffs.Count(reference =>
                    reference?.Get() == expected) != 1) ||
            judgmentBonuses.Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.Dodge) != 1 ||
            judgmentBonuses.Count(component =>
                component.Stat == StatType.BaseAttackBonus &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            judgmentBonuses.Length != 2 ||
            new[] { 2, 6, 10, 14, 18 }.Any(level =>
                CountFeatureAtLevel(forester.AddFeatures, combatFeat, level) != 1) ||
            forester.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == combatFeat) ?? 0) != 5 ||
            CountFeatureAtLevel(forester.AddFeatures, uncannyDodge, 2) != 1 ||
            forester.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == uncannyDodge) ?? 0) != 1 ||
            CountFeatureAtLevel(forester.RemoveFeatures, nurturedGrowth, 13) != 1 ||
            forester.RemoveFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == nurturedGrowth) ?? 0) != 1 ||
            new[] { 1, 4, 7, 10, 13, 16, 19 }.Any(level =>
                CountFeatureAtLevel(tandemExecutioner.AddFeatures, tandemTechniques, level) != 1) ||
            tandemExecutioner.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == tandemTechniques) ?? 0) != 7 ||
            CountFeatureAtLevel(tandemExecutioner.AddFeatures, tandemExecution, 10) != 1 ||
            tandemExecutioner.AddFeatures.Sum(entry =>
                entry.m_Features?.Count(reference =>
                    reference?.Get() == tandemExecution) ?? 0) != 1 ||
            tandemControllers.Length != 1 ||
            tandemControllers[0].IsPet ||
            tandemPetControllers.Length != 1 ||
            !tandemPetControllers[0].IsPet ||
            tandemPetGrants.Count(component =>
                component.m_Feature?.Get() == tandemPetFeature &&
                component.m_PetType == PetType.AnimalCompanion) != 1 ||
            tandemHunterMarkers.Length != 1 ||
            tandemPetMarkers.Length != 1 ||
            controllerComponents.Length != 1 ||
            controllerComponents[0].m_BonusBuff?.Get() != bonusBuff ||
            controllerComponents[0].m_AnimalFocusEffects.Length != expectedFocuses.Length ||
            expectedFocuses.Any(expected =>
                controllerComponents[0].m_AnimalFocusEffects.Count(reference =>
                    reference?.Get() == expected) != 1) ||
            addedFacts != 1 ||
            petComponents.Count(component =>
                component.m_Feature?.Get() == controller &&
                component.m_PetType == PetType.AnimalCompanion) != 1 ||
            bonuses.Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.Dodge) != 1 ||
            bonuses.Count(component =>
                component.Stat == StatType.BaseAttackBonus &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.UntypedStackable) != 1 ||
            bonuses.Length != 2) {
            throw new InvalidOperationException(
                "Hunter changes must grant Precise Companion at levels 2 and 5, Nurtured Growth at level 13, Colluding Scoundrel Sneak Attack at levels 5, 10, and 15, Eternal Judgment to Divine Hound at level 20, the configured Forester tradeoffs, seven Tandem Executioner Techniques plus Tandem Execution, and the Master Hunter Animal Focus bonuses at level 20.");
        }
    }

    private static int CountFeatureAtLevel(
        BlueprintProgression progression,
        BlueprintFeature feature,
        int level) =>
        CountFeatureAtLevel(progression.LevelEntries, feature, level);

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

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }
}
