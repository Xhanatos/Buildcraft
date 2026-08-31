using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class ClericRebalance {
    internal static void Configure() {
        ConfigureHolyDetermination();
        ConfigureDivineConduit();

        var crusader = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.CrusaderArchetype);
        var originalLevel20Feat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CrusaderBonusFeat20);
        var deitySelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.DeitySelection);
        var divinePower = BlueprintTool.Get<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(
            BlueprintIds.DivinePowerAbility);

        var deityAlignments = deitySelection.m_AllFeatures
            .Select(reference => reference?.Get())
            .Where(feature => feature != null)
            .Distinct()
            .Select(feature => (
                Feature: feature,
                Alignment: feature
                    .GetComponent<ForbidSpellbookOnAlignmentDeviation>()
                    ?.Alignment ?? AlignmentMaskType.None))
            .Where(entry => entry.Alignment != AlignmentMaskType.None)
            .ToArray();
        var goodDeities = deityAlignments
            .Where(entry => IsGood(entry.Alignment) && !IsEvil(entry.Alignment))
            .Select(entry => entry.Feature)
            .ToArray();
        var evilDeities = deityAlignments
            .Where(entry => IsEvil(entry.Alignment) && !IsGood(entry.Alignment))
            .Select(entry => entry.Feature)
            .ToArray();
        var neutralDeities = deityAlignments
            .Where(entry => IsGood(entry.Alignment) == IsEvil(entry.Alignment))
            .Select(entry => entry.Feature)
            .ToArray();

        var sacredAllowedDeities = goodDeities.Concat(neutralDeities).Distinct().ToArray();
        var profaneAllowedDeities = evilDeities.Concat(neutralDeities).Distinct().ToArray();
        var sacredWarSacred = CreateSacredWarChoice(
            "ClassesRebornSacredWarSacredFeature",
            BlueprintIds.SacredWarSacredFeature,
            "ClassesReborn.SacredWar.Sacred.Name",
            "ClassesReborn.SacredWar.Sacred.Description",
            ModifierDescriptor.Sacred,
            sacredAllowedDeities,
            divinePower.Icon);
        var sacredWarProfane = CreateSacredWarChoice(
            "ClassesRebornSacredWarProfaneFeature",
            BlueprintIds.SacredWarProfaneFeature,
            "ClassesReborn.SacredWar.Profane.Name",
            "ClassesReborn.SacredWar.Profane.Description",
            ModifierDescriptor.Profane,
            profaneAllowedDeities,
            divinePower.Icon);

        var sacredWar = FeatureSelectionConfigurator.New(
                "ClassesRebornSacredWarSelection",
                BlueprintIds.SacredWarSelection)
            .SetDisplayName("ClassesReborn.SacredWar.Name")
            .SetDescription("ClassesReborn.SacredWar.Description")
            .SetIcon(divinePower.Icon)
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(
                BlueprintIds.SacredWarSacredFeature,
                BlueprintIds.SacredWarProfaneFeature)
            .Configure();
        sacredWar.m_Features = sacredWar.m_AllFeatures.ToArray();

        var addFeatures = crusader.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(addFeatures, 20, sacredWar);
        crusader.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        var divineConduit = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.DivineConduitFeature);
        var removeFeatures = crusader.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(removeFeatures, 20, divineConduit);
        crusader.RemoveFeatures = removeFeatures.OrderBy(entry => entry.Level).ToArray();

        Validate(
            crusader,
            originalLevel20Feat,
            sacredWar,
            divineConduit,
            sacredWarSacred,
            sacredWarProfane,
            goodDeities,
            evilDeities,
            neutralDeities,
            sacredAllowedDeities,
            profaneAllowedDeities);

        ConfigureDemonbanePriest();
    }

    private static void ConfigureHolyDetermination() {
        var divinePower = BlueprintTool.Get<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(
            BlueprintIds.DivinePowerAbility);
        var charismaRank = ContextRankConfigs.StatBonus(StatType.Charisma, min: 0);
        var holyDetermination = FeatureConfigurator.New(
                "ClassesRebornHolyDeterminationFeature",
                BlueprintIds.HolyDeterminationFeature)
            .SetDisplayName("ClassesReborn.HolyDetermination.Name")
            .SetDescription("ClassesReborn.HolyDetermination.Description")
            .SetIcon(divinePower.Icon)
            .SetIsClassFeature(true)
            .AddContextRankConfig(charismaRank)
            .AddContextStatBonus(
                StatType.SaveWill,
                ContextValues.Rank(),
                ModifierDescriptor.Morale)
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Charisma,
            })
            .Configure();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ClericProgression);
        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(levelEntries, 10, holyDetermination);
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var rankConfigs = holyDetermination.GetComponents<ContextRankConfig>().ToArray();
        var bonuses = holyDetermination.GetComponents<AddContextStatBonus>().ToArray();
        var recalculations = holyDetermination
            .GetComponents<RecalculateOnStatChange>()
            .ToArray();
        var totalGrants = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == holyDetermination) ?? 0);
        if (totalGrants != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, holyDetermination, 10) != 1 ||
            rankConfigs.Length != 1 ||
            rankConfigs[0].m_BaseValueType != ContextRankBaseValueType.StatBonus ||
            rankConfigs[0].m_Stat != StatType.Charisma ||
            rankConfigs[0].m_Progression != ContextRankProgression.AsIs ||
            !rankConfigs[0].m_UseMin ||
            rankConfigs[0].m_Min != 0 ||
            bonuses.Length != 1 ||
            bonuses[0].Stat != StatType.SaveWill ||
            bonuses[0].Descriptor != ModifierDescriptor.Morale ||
            recalculations.Length != 1 ||
            recalculations[0].Stat != StatType.Charisma) {
            throw new InvalidOperationException(
                "Faithful Determination must grant a Charisma-based morale bonus to Will at Cleric level 10.");
        }
    }

    private static void ConfigureDivineConduit() {
        var cureAndInflictSpellIds = new[] {
            BlueprintIds.CureLightWoundsAbility,
            BlueprintIds.CureModerateWoundsAbility,
            BlueprintIds.CureSeriousWoundsAbility,
            BlueprintIds.CureCriticalWoundsAbility,
            BlueprintIds.CureLightWoundsMassAbility,
            BlueprintIds.CureModerateWoundsMassAbility,
            BlueprintIds.CureSeriousWoundsMassAbility,
            BlueprintIds.CureCriticalWoundsMassAbility,
            BlueprintIds.InflictLightWoundsAbility,
            BlueprintIds.InflictModerateWoundsAbility,
            BlueprintIds.InflictSeriousWoundsAbility,
            BlueprintIds.InflictCriticalWoundsAbility,
            BlueprintIds.InflictLightWoundsMassAbility,
            BlueprintIds.InflictModerateWoundsMassAbility,
            BlueprintIds.InflictSeriousWoundsMassAbility,
            BlueprintIds.InflictCriticalWoundsMassAbility,
        };
        var cureAndInflictSpells = cureAndInflictSpellIds
            .Select(BlueprintTool.Get<BlueprintAbility>)
            .ToArray();
        var cureCriticalWounds = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.CureCriticalWoundsAbility);

        var divineConduit = FeatureConfigurator.New(
                "ClassesRebornDivineConduitFeature",
                BlueprintIds.DivineConduitFeature)
            .SetDisplayName("ClassesReborn.DivineConduit.Name")
            .SetDescription("ClassesReborn.DivineConduit.Description")
            .SetIcon(cureCriticalWounds.Icon)
            .SetIsClassFeature(true)
            .AddAutoMetamagic(
                abilities: new() {
                    BlueprintIds.CureLightWoundsAbility,
                    BlueprintIds.CureModerateWoundsAbility,
                    BlueprintIds.CureSeriousWoundsAbility,
                    BlueprintIds.CureCriticalWoundsAbility,
                    BlueprintIds.CureLightWoundsMassAbility,
                    BlueprintIds.CureModerateWoundsMassAbility,
                    BlueprintIds.CureSeriousWoundsMassAbility,
                    BlueprintIds.CureCriticalWoundsMassAbility,
                    BlueprintIds.InflictLightWoundsAbility,
                    BlueprintIds.InflictModerateWoundsAbility,
                    BlueprintIds.InflictSeriousWoundsAbility,
                    BlueprintIds.InflictCriticalWoundsAbility,
                    BlueprintIds.InflictLightWoundsMassAbility,
                    BlueprintIds.InflictModerateWoundsMassAbility,
                    BlueprintIds.InflictSeriousWoundsMassAbility,
                    BlueprintIds.InflictCriticalWoundsMassAbility,
                },
                allowedAbilities: AutoMetamagic.AllowedType.SpellOnly,
                metamagic: Metamagic.Maximize,
                once: false)
            .AddBuffDescriptorImmunity(
                checkFact: false,
                descriptor: SpellDescriptor.Fear)
            .AddSpellImmunityToSpellDescriptor(
                descriptor: SpellDescriptor.Fear)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Charisma,
                value: 4)
            .Configure();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.ClericProgression);
        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(levelEntries, 20, divineConduit);
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var autoMetamagic = divineConduit.GetComponents<AutoMetamagic>().ToArray();
        var buffImmunities = divineConduit
            .GetComponents<BuffDescriptorImmunity>()
            .ToArray();
        var spellImmunities = divineConduit
            .GetComponents<SpellImmunityToSpellDescriptor>()
            .ToArray();
        var statBonuses = divineConduit.GetComponents<AddStatBonus>().ToArray();
        var grantedSpells = autoMetamagic.Length == 1
            ? autoMetamagic[0].Abilities.Select(reference => reference?.Get()).ToArray()
            : Array.Empty<BlueprintAbility>();
        var totalGrants = progression.LevelEntries.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == divineConduit) ?? 0);

        if (totalGrants != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, divineConduit, 20) != 1 ||
            cureAndInflictSpells.Length != 16 ||
            cureAndInflictSpells.Distinct().Count() != cureAndInflictSpells.Length ||
            autoMetamagic.Length != 1 ||
            autoMetamagic[0].m_AllowedAbilities != AutoMetamagic.AllowedType.SpellOnly ||
            autoMetamagic[0].Metamagic != Metamagic.Maximize ||
            autoMetamagic[0].Once ||
            autoMetamagic[0].CheckSpellbook ||
            grantedSpells.Length != cureAndInflictSpells.Length ||
            cureAndInflictSpells.Any(spell => grantedSpells.Count(candidate => candidate == spell) != 1) ||
            buffImmunities.Length != 1 ||
            buffImmunities[0].CheckFact ||
            buffImmunities[0].Descriptor != SpellDescriptor.Fear ||
            spellImmunities.Length != 1 ||
            spellImmunities[0].Descriptor != SpellDescriptor.Fear ||
            statBonuses.Length != 1 ||
            statBonuses[0].Stat != StatType.Charisma ||
            statBonuses[0].Value != 4 ||
            statBonuses[0].Descriptor != ModifierDescriptor.UntypedStackable) {
            throw new InvalidOperationException(
                "Divine Conduit must maximize exactly the sixteen Cure and Inflict spells, grant full fear immunity, and add +4 untyped Charisma at Cleric level 20.");
        }
    }

    private static void ConfigureDemonbanePriest() {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.DemonbanePriestArchetype);
        var bonusFeat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.DemonbanePriestTeamworkFeatSelection);
        var bonusFeatLevels = new[] { 4, 8, 12, 16 };

        var addFeatures = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(addFeatures, 12, bonusFeat);
        AddFeature(addFeatures, 16, bonusFeat);
        archetype.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        var totalGrants = archetype.AddFeatures.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == bonusFeat) ?? 0);
        if (totalGrants != bonusFeatLevels.Length ||
            bonusFeatLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, bonusFeat, level) != 1) ||
            archetype.AddFeatures.Any(entry =>
                !bonusFeatLevels.Contains(entry.Level) &&
                CountFeatureAtLevel(archetype.AddFeatures, bonusFeat, entry.Level) != 0)) {
            throw new InvalidOperationException(
                "Demonbane Priest must gain exactly four bonus feats at levels 4/8/12/16.");
        }
    }

    private static BlueprintFeature CreateSacredWarChoice(
        string name,
        string guid,
        string displayName,
        string description,
        ModifierDescriptor descriptor,
        IEnumerable<BlueprintFeature> allowedDeities,
        UnityEngine.Sprite icon) {
        var deityReferences = allowedDeities
            .Select(deity => BlueprintTool.GetRef<BlueprintFeatureReference>(
                deity.AssetGuid.ToString()))
            .ToArray();
        var charismaRank = ContextRankConfigs
            .StatBonus(StatType.Charisma, min: 0)
            .WithDivStepProgression(3);

        return FeatureConfigurator.New(name, guid)
            .SetDisplayName(displayName)
            .SetDescription(description)
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddContextRankConfig(charismaRank)
            .AddContextStatBonus(
                StatType.AdditionalAttackBonus,
                ContextValues.Rank(),
                descriptor)
            .AddContextStatBonus(
                StatType.AdditionalDamage,
                ContextValues.Rank(),
                descriptor)
            .AddComponent(new RecalculateOnStatChange {
                Stat = StatType.Charisma,
            })
            .AddComponent(new PrerequisiteFeaturesFromList {
                m_Features = deityReferences,
                m_RestrictIfNot = true,
                Amount = 1,
                Group = Prerequisite.GroupType.All,
                CheckInProgression = true,
                HideInUI = true,
            })
            .Configure();
    }

    private static void Validate(
        BlueprintArchetype crusader,
        BlueprintFeatureSelection originalLevel20Feat,
        BlueprintFeatureSelection sacredWar,
        BlueprintFeature divineConduit,
        BlueprintFeature sacredChoice,
        BlueprintFeature profaneChoice,
        BlueprintFeature[] goodDeities,
        BlueprintFeature[] evilDeities,
        BlueprintFeature[] neutralDeities,
        BlueprintFeature[] sacredAllowedDeities,
        BlueprintFeature[] profaneAllowedDeities) {
        if (goodDeities.Length == 0 || evilDeities.Length == 0 || neutralDeities.Length == 0 ||
            goodDeities.Intersect(evilDeities).Any() ||
            goodDeities.Intersect(neutralDeities).Any() ||
            evilDeities.Intersect(neutralDeities).Any()) {
            throw new InvalidOperationException(
                "Sacred War deity alignment groups must be nonempty and mutually exclusive.");
        }

        if (CountFeatureAtLevel(crusader.AddFeatures, originalLevel20Feat, 20) != 1 ||
            CountFeatureAtLevel(crusader.AddFeatures, sacredWar, 20) != 1 ||
            CountFeature(crusader.RemoveFeatures, divineConduit) != 1 ||
            CountFeatureAtLevel(crusader.RemoveFeatures, divineConduit, 20) != 1 ||
            sacredWar.m_AllFeatures.Count(reference => reference?.Get() == sacredChoice) != 1 ||
            sacredWar.m_AllFeatures.Count(reference => reference?.Get() == profaneChoice) != 1 ||
            sacredWar.m_Features.Count(reference => reference?.Get() == sacredChoice) != 1 ||
            sacredWar.m_Features.Count(reference => reference?.Get() == profaneChoice) != 1) {
            throw new InvalidOperationException(
                "Crusader must replace Divine Conduit with exactly one two-option Sacred War selection at level 20.");
        }

        ValidateChoice(
            sacredChoice,
            ModifierDescriptor.Sacred,
            sacredAllowedDeities.Length);
        ValidateChoice(
            profaneChoice,
            ModifierDescriptor.Profane,
            profaneAllowedDeities.Length);
    }

    private static void ValidateChoice(
        BlueprintFeature choice,
        ModifierDescriptor descriptor,
        int expectedDeityCount) {
        var rankConfigs = choice.GetComponents<ContextRankConfig>().ToArray();
        var bonuses = choice.GetComponents<AddContextStatBonus>().ToArray();
        var recalculations = choice.GetComponents<RecalculateOnStatChange>().ToArray();
        var prerequisites = choice.GetComponents<PrerequisiteFeaturesFromList>().ToArray();
        var expectedStats = new[] {
            StatType.AdditionalAttackBonus,
            StatType.AdditionalDamage,
        };

        if (rankConfigs.Length != 1 ||
            rankConfigs[0].m_BaseValueType != ContextRankBaseValueType.StatBonus ||
            rankConfigs[0].m_Stat != StatType.Charisma ||
            rankConfigs[0].m_Progression != ContextRankProgression.DivStep ||
            rankConfigs[0].m_StepLevel != 3 ||
            !rankConfigs[0].m_UseMin ||
            rankConfigs[0].m_Min != 0 ||
            bonuses.Length != expectedStats.Length ||
            expectedStats.Any(stat => bonuses.Count(component => component.Stat == stat) != 1) ||
            bonuses.Any(component => component.Descriptor != descriptor) ||
            recalculations.Length != 1 ||
            recalculations[0].Stat != StatType.Charisma ||
            prerequisites.Length != 1 ||
            !prerequisites[0].m_RestrictIfNot ||
            prerequisites[0].Amount != 1 ||
            prerequisites[0].m_Features.Length != expectedDeityCount) {
            throw new InvalidOperationException(
                $"Sacred War {descriptor} choice configuration is invalid.");
        }
    }

    private static bool IsGood(AlignmentMaskType alignment) =>
        (alignment & AlignmentMaskType.Good) != AlignmentMaskType.None;

    private static bool IsEvil(AlignmentMaskType alignment) =>
        (alignment & AlignmentMaskType.Evil) != AlignmentMaskType.None;

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

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
}
