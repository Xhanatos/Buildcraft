using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class MonkRebalance {
    internal static void Configure() {
        var uncannyDodge = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.UncannyDodgeChecker);
        var selectableUncannyDodge = FeatureConfigurator.New(
                "ClassesRebornMonkBonusFeatUncannyDodge",
                BlueprintIds.MonkBonusFeatUncannyDodge)
            .SetDisplayName("ClassesReborn.MonkBonusFeatUncannyDodge.Name")
            .SetDescription("ClassesReborn.MonkBonusFeatUncannyDodge.Description")
            .SetIcon(uncannyDodge.Icon)
            .SetRanks(1)
            .SetHideInCharacterSheetAndLevelUp(false)
            .AddFacts(new() { BlueprintIds.UncannyDodgeChecker })
            .Configure();
        var selections = BlueprintIds.MonkBonusFeatSelectionsFromLevel6
            .Select(BlueprintTool.Get<BlueprintFeatureSelection>)
            .ToArray();

        foreach (var selection in selections) {
            selection.m_AllFeatures = AddVisibleSelectionFeature(
                selection.m_AllFeatures,
                uncannyDodge,
                selectableUncannyDodge);
            selection.m_Features = AddVisibleSelectionFeature(
                selection.m_Features,
                uncannyDodge,
                selectableUncannyDodge);
        }

        var selectableUncannyDodgeFacts = selectableUncannyDodge
            .GetComponents<AddFacts>()
            .SelectMany(component =>
                component.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
            .ToArray();

        var perfectSelfFeatures = new[] {
            BlueprintIds.MonkKiPerfectSelf,
            BlueprintIds.DrunkenMonkKiPerfectSelf,
        }
            .Select(id => FeatureConfigurator.For(id)
                .SetDescription("ClassesReborn.KiPerfectSelf.Description")
                .AddComponent(new PerfectSelfForceDamage())
                .Configure())
            .ToArray();

        var strengthOfStone = FeatureConfigurator.For(
                BlueprintIds.StrengthOfStoneFeature)
            .SetDescription("ClassesReborn.StrengthOfStone.Description")
            .Configure();
        var strengthOfStoneLevel9 = FeatureConfigurator.New(
                "ClassesRebornStrengthOfStoneLevel9",
                BlueprintIds.StrengthOfStoneLevel9)
            .SetDisplayName("ClassesReborn.StrengthOfStone.Name")
            .SetDescription("ClassesReborn.StrengthOfStone.Level9.Description")
            .SetIcon(strengthOfStone.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new StrengthOfStoneUnarmedDamage())
            .Configure();
        var strengthOfStoneLevel15 = FeatureConfigurator.New(
                "ClassesRebornStrengthOfStoneLevel15",
                BlueprintIds.StrengthOfStoneLevel15)
            .SetDisplayName("ClassesReborn.StrengthOfStone.Name")
            .SetDescription("ClassesReborn.StrengthOfStone.Level15.Description")
            .SetIcon(strengthOfStone.Icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Inherent,
                stat: StatType.Strength,
                value: 4)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Inherent,
                stat: StatType.Constitution,
                value: 4)
            .Configure();

        var studentOfStone = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.StudentOfStoneArchetype);
        var studentOfStoneFeatures = studentOfStone.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(studentOfStoneFeatures, strengthOfStoneLevel9);
        RemoveFeature(studentOfStoneFeatures, strengthOfStoneLevel15);
        AddFeature(studentOfStoneFeatures, 9, strengthOfStoneLevel9);
        AddFeature(studentOfStoneFeatures, 15, strengthOfStoneLevel15);
        studentOfStone.AddFeatures = studentOfStoneFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var flurryOfBlows = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MonkFlurryOfBlows);
        var masteringFundamentalsLevel7 = FeatureConfigurator.New(
                "ClassesRebornMasteringFundamentalsLevel7",
                BlueprintIds.MasteringFundamentalsLevel7)
            .SetDisplayName("ClassesReborn.MasteringFundamentals.Name")
            .SetDescription("ClassesReborn.MasteringFundamentals.Level7.Description")
            .SetIcon(flurryOfBlows.Icon)
            .SetIsClassFeature(true)
            .AddWeaponSizeChange(WeaponCategory.UnarmedStrike, true, 1)
            .Configure();
        var masteringFundamentalsLevel11 = FeatureConfigurator.New(
                "ClassesRebornMasteringFundamentalsLevel11",
                BlueprintIds.MasteringFundamentalsLevel11)
            .SetDisplayName("ClassesReborn.MasteringFundamentals.Name")
            .SetDescription("ClassesReborn.MasteringFundamentals.Level11.Description")
            .SetIcon(flurryOfBlows.Icon)
            .SetIsClassFeature(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Dodge,
                stat: StatType.AC,
                value: 2)
            .Configure();
        var masteringFundamentalsExtraAttack = FeatureConfigurator.New(
                "ClassesRebornMasteringFundamentalsExtraAttack",
                BlueprintIds.MasteringFundamentalsExtraAttack)
            .SetDisplayName("ClassesReborn.MasteringFundamentals.Name")
            .SetDescription("ClassesReborn.MasteringFundamentals.Level15.Description")
            .SetIcon(flurryOfBlows.Icon)
            .SetIsClassFeature(true)
            .SetHideInUI(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new BuffExtraAttack {
                Number = 1,
                Haste = false,
            })
            .Configure();
        var masteringFundamentalsLevel15 = FeatureConfigurator.New(
                "ClassesRebornMasteringFundamentalsLevel15",
                BlueprintIds.MasteringFundamentalsLevel15)
            .SetDisplayName("ClassesReborn.MasteringFundamentals.Name")
            .SetDescription("ClassesReborn.MasteringFundamentals.Level15.Description")
            .SetIcon(flurryOfBlows.Icon)
            .SetIsClassFeature(true)
            .AddMonkNoArmorAndMonkWeaponFeatureUnlock(
                isSohei: false,
                isZenArcher: false,
                newFact: BlueprintIds.MasteringFundamentalsExtraAttack)
            .Configure();

        var traditionalMonk = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TraditionalMonkArchetype);
        var traditionalMonkFeatures = traditionalMonk.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        foreach (var feature in new[] {
            masteringFundamentalsLevel7,
            masteringFundamentalsLevel11,
            masteringFundamentalsLevel15,
        }) {
            RemoveFeature(traditionalMonkFeatures, feature);
        }
        AddFeature(traditionalMonkFeatures, 7, masteringFundamentalsLevel7);
        AddFeature(traditionalMonkFeatures, 11, masteringFundamentalsLevel11);
        AddFeature(traditionalMonkFeatures, 15, masteringFundamentalsLevel15);
        traditionalMonk.AddFeatures = traditionalMonkFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var rapidShot = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.RapidShotFeature);
        var manyshot = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ManyshotFeature);
        var deflectArrows = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.DeflectArrowsFeature);
        var improvedInitiative = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedInitiativeFeature);
        var zenArcherBonusFeatSelections = new[] {
            BlueprintIds.ZenArcherBonusFeatSelectionLevel1,
            BlueprintIds.ZenArcherBonusFeatSelectionLevel6,
            BlueprintIds.ZenArcherBonusFeatSelectionLevel10,
        }
            .Select(BlueprintTool.Get<BlueprintFeatureSelection>)
            .ToArray();
        foreach (var selection in zenArcherBonusFeatSelections) {
            selection.m_AllFeatures = RemoveSelectionFeatures(
                selection.m_AllFeatures,
                rapidShot,
                manyshot,
                deflectArrows);
            selection.m_Features = RemoveSelectionFeatures(
                selection.m_Features,
                rapidShot,
                manyshot,
                deflectArrows);
            selection.m_AllFeatures = AddSelectionFeature(
                selection.m_AllFeatures,
                improvedInitiative);
            selection.m_Features = AddSelectionFeature(
                selection.m_Features,
                improvedInitiative);
        }

        var kiArrows = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ZenArcherKiArrowsFeature);
        var zenArcherDamageIncrease = FeatureConfigurator.New(
                "ClassesRebornZenArcherUnarmedDamageIncrease",
                BlueprintIds.ZenArcherUnarmedDamageIncrease)
            .SetDisplayName("ClassesReborn.ZenArcherDamageIncrease.Name")
            .SetDescription("ClassesReborn.ZenArcherDamageIncrease.Description")
            .SetIcon(kiArrows.Icon)
            .SetIsClassFeature(true)
            .AddWeaponSizeChange(WeaponCategory.UnarmedStrike, true, 1)
            .AddWeaponSizeChange(WeaponCategory.Longbow, true, 1)
            .AddWeaponSizeChange(WeaponCategory.Shortbow, true, 1)
            .Configure();

        var zenArcherFlurryUnlock = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ZenArcherFlurryOfBlowsUnlock);
        var zenArcherFlurryComponent = zenArcherFlurryUnlock
            .GetComponents<MonkNoArmorAndMonkWeaponFeatureUnlock>()
            .Single();
        var oneWithTheArrowExtraAttack = FeatureConfigurator.New(
                "ClassesRebornOneWithTheArrowExtraAttack",
                BlueprintIds.OneWithTheArrowExtraAttack)
            .SetDisplayName("ClassesReborn.OneWithTheArrow.Name")
            .SetDescription("ClassesReborn.OneWithTheArrow.Description")
            .SetIcon(kiArrows.Icon)
            .SetIsClassFeature(true)
            .SetHideInUI(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddComponent(new BuffExtraAttack {
                Number = 1,
                Haste = false,
            })
            .Configure();
        var oneWithTheArrow = FeatureConfigurator.New(
                "ClassesRebornOneWithTheArrow",
                BlueprintIds.OneWithTheArrow)
            .SetDisplayName("ClassesReborn.OneWithTheArrow.Name")
            .SetDescription("ClassesReborn.OneWithTheArrow.Description")
            .SetIcon(kiArrows.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new MonkNoArmorAndMonkWeaponFeatureUnlock {
                m_NewFact = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.OneWithTheArrowExtraAttack),
                IsZenArcher = true,
                m_BowWeaponTypes = zenArcherFlurryComponent.m_BowWeaponTypes.ToArray(),
                m_RapidshotBuff = zenArcherFlurryComponent.m_RapidshotBuff,
                IsSohei = false,
            })
            .AddComponent(new WeaponCriticalEdgeIncreaseStackable {
                IncludeCategories = new[] {
                    WeaponCategory.Longbow,
                    WeaponCategory.Shortbow,
                },
                ExcludeCategories = Array.Empty<WeaponCategory>(),
                IncludeAttackTypes = Array.Empty<AttackType>(),
                ExcludeAttackTypes = Array.Empty<AttackType>(),
                Value = 1,
            })
            .Configure();

        var evasion = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.MonkEvasion);
        var improvedEvasion = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MonkImprovedEvasion);
        var zenArcher = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ZenArcherArchetype);
        var zenArcherRemovals = zenArcher.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(zenArcherRemovals, evasion);
        RemoveFeature(zenArcherRemovals, improvedEvasion);
        RemoveFeature(zenArcherRemovals, perfectSelfFeatures[0]);
        AddFeature(zenArcherRemovals, 20, perfectSelfFeatures[0]);
        zenArcher.RemoveFeatures = zenArcherRemovals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var zenArcherFeatures = zenArcher.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(zenArcherFeatures, deflectArrows);
        RemoveFeature(zenArcherFeatures, zenArcherDamageIncrease);
        RemoveFeature(zenArcherFeatures, oneWithTheArrow);
        AddFeature(zenArcherFeatures, 7, deflectArrows);
        AddFeature(zenArcherFeatures, 15, zenArcherDamageIncrease);
        AddFeature(zenArcherFeatures, 20, oneWithTheArrow);
        zenArcher.AddFeatures = zenArcherFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var quarterstaffType = BlueprintTool.Get<BlueprintWeaponType>(
            BlueprintIds.QuarterstaffWeaponType);
        var quarterstaffTraining = FeatureConfigurator.New(
                "ClassesRebornQuarterstaffWeaponTraining",
                BlueprintIds.QuarterstaffWeaponTraining)
            .SetDisplayName("ClassesReborn.QuarterstaffWeaponTraining.Name")
            .SetDescription("ClassesReborn.QuarterstaffWeaponTraining.Description")
            .SetIcon(quarterstaffType.Icon)
            .SetIsClassFeature(true)
            .SetRanks(4)
            .AddComponent(new WeaponCategoryAttackBonus {
                Category = WeaponCategory.Quarterstaff,
                AttackBonus = 1,
                Descriptor = ModifierDescriptor.WeaponTraining,
            })
            .AddComponent(new WeaponTypeDamageBonus {
                m_WeaponType = BlueprintTool.GetRef<BlueprintWeaponTypeReference>(
                    BlueprintIds.QuarterstaffWeaponType),
                DamageBonus = 1,
            })
            .AddComponent(new WeaponTraining())
            .Configure();

        var quarterstaffMaster = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.QuarterstaffMasterArchetype);
        var quarterstaffMasterFeatures = quarterstaffMaster.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(quarterstaffMasterFeatures, quarterstaffTraining);
        foreach (var level in new[] { 7, 10, 13, 16 }) {
            AddFeature(quarterstaffMasterFeatures, level, quarterstaffTraining);
        }
        quarterstaffMaster.AddFeatures = quarterstaffMasterFeatures
            .OrderBy(entry => entry.Level)
            .ToArray();

        var monkProgression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.MonkProgression);
        var fastMovement = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.MonkFastMovement);
        var sensei = BlueprintTool.Get<BlueprintArchetype>(BlueprintIds.SenseiArchetype);
        var senseiRemovals = sensei.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        var otherSenseiRemovalsBefore = CountOtherFeatures(
            senseiRemovals,
            evasion,
            fastMovement);
        RemoveFeature(senseiRemovals, evasion);
        RemoveFeature(senseiRemovals, fastMovement);
        sensei.RemoveFeatures = senseiRemovals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var attackBonuses = quarterstaffTraining
            .GetComponents<WeaponCategoryAttackBonus>()
            .ToArray();
        var damageBonuses = quarterstaffTraining
            .GetComponents<WeaponTypeDamageBonus>()
            .ToArray();
        var strengthOfStoneDamage = strengthOfStoneLevel9
            .GetComponents<StrengthOfStoneUnarmedDamage>()
            .ToArray();
        var strengthOfStoneStats = strengthOfStoneLevel15
            .GetComponents<AddStatBonus>()
            .ToArray();
        var masteringFundamentalsSizeChanges = masteringFundamentalsLevel7
            .GetComponents<WeaponSizeChange>()
            .ToArray();
        var masteringFundamentalsArmorBonuses = masteringFundamentalsLevel11
            .GetComponents<AddStatBonus>()
            .ToArray();
        var masteringFundamentalsUnlocks = masteringFundamentalsLevel15
            .GetComponents<MonkNoArmorAndMonkWeaponFeatureUnlock>()
            .ToArray();
        var masteringFundamentalsExtraAttacks = masteringFundamentalsExtraAttack
            .GetComponents<BuffExtraAttack>()
            .ToArray();
        var zenArcherSizeChanges = zenArcherDamageIncrease
            .GetComponents<WeaponSizeChange>()
            .ToArray();
        var oneWithTheArrowUnlocks = oneWithTheArrow
            .GetComponents<MonkNoArmorAndMonkWeaponFeatureUnlock>()
            .ToArray();
        var oneWithTheArrowCriticalEdges = oneWithTheArrow
            .GetComponents<WeaponCriticalEdgeIncreaseStackable>()
            .ToArray();
        var oneWithTheArrowExtraAttacks = oneWithTheArrowExtraAttack
            .GetComponents<BuffExtraAttack>()
            .ToArray();

        if (selections.Length != BlueprintIds.MonkBonusFeatSelectionsFromLevel6.Length ||
            selections.Distinct().Count() != selections.Length ||
            selections.Any(selection =>
                selection.m_AllFeatures.Count(reference =>
                    reference?.Get() == selectableUncannyDodge) != 1 ||
                selection.m_Features.Count(reference =>
                    reference?.Get() == selectableUncannyDodge) != 1 ||
                selection.m_AllFeatures.Any(reference =>
                    reference?.Get() == uncannyDodge) ||
                selection.m_Features.Any(reference =>
                    reference?.Get() == uncannyDodge)) ||
            selectableUncannyDodge.HideInCharacterSheetAndLevelUp ||
            selectableUncannyDodge.Ranks != 1 ||
            selectableUncannyDodgeFacts.Count(reference =>
                reference?.Get() == uncannyDodge) != 1 ||
            selectableUncannyDodgeFacts.Length != 1 ||
            perfectSelfFeatures.Length != 2 ||
            perfectSelfFeatures.Distinct().Count() != 2 ||
            perfectSelfFeatures.Any(feature =>
                feature.GetComponents<PerfectSelfForceDamage>().Count() != 1) ||
            CountFeatureAtLevel(studentOfStone.AddFeatures, strengthOfStone, 3) != 1 ||
            CountFeature(studentOfStone.AddFeatures, strengthOfStoneLevel9) != 1 ||
            CountFeatureAtLevel(
                studentOfStone.AddFeatures,
                strengthOfStoneLevel9,
                9) != 1 ||
            CountFeature(studentOfStone.AddFeatures, strengthOfStoneLevel15) != 1 ||
            CountFeatureAtLevel(
                studentOfStone.AddFeatures,
                strengthOfStoneLevel15,
                15) != 1 ||
            strengthOfStoneDamage.Length != 1 ||
            strengthOfStoneStats.Length != 2 ||
            strengthOfStoneStats.Count(component =>
                component.Stat == StatType.Strength &&
                component.Value == 4 &&
                component.Descriptor == ModifierDescriptor.Inherent) != 1 ||
            strengthOfStoneStats.Count(component =>
                component.Stat == StatType.Constitution &&
                component.Value == 4 &&
                component.Descriptor == ModifierDescriptor.Inherent) != 1 ||
            CountFeatureAtLevel(
                traditionalMonk.AddFeatures,
                masteringFundamentalsLevel7,
                7) != 1 ||
            CountFeatureAtLevel(
                traditionalMonk.AddFeatures,
                masteringFundamentalsLevel11,
                11) != 1 ||
            CountFeatureAtLevel(
                traditionalMonk.AddFeatures,
                masteringFundamentalsLevel15,
                15) != 1 ||
            CountFeature(traditionalMonk.AddFeatures, masteringFundamentalsLevel7) != 1 ||
            CountFeature(traditionalMonk.AddFeatures, masteringFundamentalsLevel11) != 1 ||
            CountFeature(traditionalMonk.AddFeatures, masteringFundamentalsLevel15) != 1 ||
            masteringFundamentalsSizeChanges.Length != 1 ||
            masteringFundamentalsSizeChanges[0].Category != WeaponCategory.UnarmedStrike ||
            !masteringFundamentalsSizeChanges[0].CheckWeaponCategory ||
            masteringFundamentalsSizeChanges[0].SizeCategoryChange != 1 ||
            masteringFundamentalsArmorBonuses.Length != 1 ||
            masteringFundamentalsArmorBonuses[0].Stat != StatType.AC ||
            masteringFundamentalsArmorBonuses[0].Value != 2 ||
            masteringFundamentalsArmorBonuses[0].Descriptor != ModifierDescriptor.Dodge ||
            masteringFundamentalsUnlocks.Length != 1 ||
            masteringFundamentalsUnlocks[0].m_NewFact?.Get() !=
                masteringFundamentalsExtraAttack ||
            masteringFundamentalsUnlocks[0].IsSohei ||
            masteringFundamentalsUnlocks[0].IsZenArcher ||
            masteringFundamentalsExtraAttacks.Length != 1 ||
            masteringFundamentalsExtraAttacks[0].Number != 1 ||
            masteringFundamentalsExtraAttacks[0].Haste ||
            zenArcherBonusFeatSelections.Length != 3 ||
            zenArcherBonusFeatSelections.Distinct().Count() != 3 ||
            zenArcherBonusFeatSelections.Any(selection =>
                selection.m_AllFeatures.Any(reference =>
                    reference?.Get() == rapidShot ||
                    reference?.Get() == manyshot ||
                    reference?.Get() == deflectArrows) ||
                selection.m_Features.Any(reference =>
                    reference?.Get() == rapidShot ||
                    reference?.Get() == manyshot ||
                    reference?.Get() == deflectArrows) ||
                selection.m_AllFeatures.Count(reference =>
                    reference?.Get() == improvedInitiative) != 1 ||
                selection.m_Features.Count(reference =>
                    reference?.Get() == improvedInitiative) != 1) ||
            CountFeatureAtLevel(monkProgression.LevelEntries, improvedEvasion, 9) != 1 ||
            CountFeature(zenArcher.RemoveFeatures, evasion) != 0 ||
            CountFeature(zenArcher.RemoveFeatures, improvedEvasion) != 0 ||
            CountFeature(zenArcher.RemoveFeatures, perfectSelfFeatures[0]) != 1 ||
            CountFeatureAtLevel(
                zenArcher.RemoveFeatures,
                perfectSelfFeatures[0],
                20) != 1 ||
            CountFeature(zenArcher.AddFeatures, deflectArrows) != 1 ||
            CountFeatureAtLevel(zenArcher.AddFeatures, deflectArrows, 7) != 1 ||
            CountFeature(zenArcher.AddFeatures, zenArcherDamageIncrease) != 1 ||
            CountFeatureAtLevel(
                zenArcher.AddFeatures,
                zenArcherDamageIncrease,
                15) != 1 ||
            CountFeature(zenArcher.AddFeatures, oneWithTheArrow) != 1 ||
            CountFeatureAtLevel(zenArcher.AddFeatures, oneWithTheArrow, 20) != 1 ||
            zenArcherSizeChanges.Length != 3 ||
            new[] {
                WeaponCategory.UnarmedStrike,
                WeaponCategory.Longbow,
                WeaponCategory.Shortbow,
            }.Any(category => zenArcherSizeChanges.Count(component =>
                component.Category == category &&
                component.CheckWeaponCategory &&
                component.SizeCategoryChange == 1) != 1) ||
            oneWithTheArrowUnlocks.Length != 1 ||
            oneWithTheArrowUnlocks[0].m_NewFact?.Get() != oneWithTheArrowExtraAttack ||
            !oneWithTheArrowUnlocks[0].IsZenArcher ||
            oneWithTheArrowUnlocks[0].IsSohei ||
            oneWithTheArrowUnlocks[0].m_RapidshotBuff?.Get() !=
                zenArcherFlurryComponent.m_RapidshotBuff?.Get() ||
            oneWithTheArrowUnlocks[0].m_BowWeaponTypes.Length !=
                zenArcherFlurryComponent.m_BowWeaponTypes.Length ||
            oneWithTheArrowUnlocks[0].m_BowWeaponTypes.Any(reference =>
                !zenArcherFlurryComponent.m_BowWeaponTypes.Any(sourceReference =>
                    sourceReference?.Get() == reference?.Get())) ||
            oneWithTheArrowCriticalEdges.Length != 1 ||
            oneWithTheArrowCriticalEdges[0].Value != 1 ||
            oneWithTheArrowCriticalEdges[0].IncludeCategories.Length != 2 ||
            !oneWithTheArrowCriticalEdges[0].IncludeCategories.Contains(
                WeaponCategory.Longbow) ||
            !oneWithTheArrowCriticalEdges[0].IncludeCategories.Contains(
                WeaponCategory.Shortbow) ||
            oneWithTheArrowCriticalEdges[0].ExcludeCategories.Length != 0 ||
            oneWithTheArrowCriticalEdges[0].IncludeAttackTypes.Length != 0 ||
            oneWithTheArrowCriticalEdges[0].ExcludeAttackTypes.Length != 0 ||
            oneWithTheArrowExtraAttacks.Length != 1 ||
            oneWithTheArrowExtraAttacks[0].Number != 1 ||
            oneWithTheArrowExtraAttacks[0].Haste ||
            quarterstaffTraining.Ranks != 4 ||
            attackBonuses.Length != 1 ||
            attackBonuses[0].Category != WeaponCategory.Quarterstaff ||
            attackBonuses[0].AttackBonus != 1 ||
            attackBonuses[0].Descriptor != ModifierDescriptor.WeaponTraining ||
            damageBonuses.Length != 1 ||
            damageBonuses[0].WeaponType != quarterstaffType ||
            damageBonuses[0].DamageBonus != 1 ||
            quarterstaffTraining.GetComponents<WeaponTraining>().Count() != 1 ||
            (quarterstaffTraining.Groups?.Contains(FeatureGroup.WeaponTraining) ?? false) ||
            CountFeature(quarterstaffMaster.AddFeatures, quarterstaffTraining) != 4 ||
            new[] { 7, 10, 13, 16 }.Any(level =>
                CountFeatureAtLevel(
                    quarterstaffMaster.AddFeatures,
                    quarterstaffTraining,
                    level) != 1) ||
            quarterstaffMaster.AddFeatures.Any(entry =>
                !new[] { 7, 10, 13, 16 }.Contains(entry.Level) &&
                (entry.m_Features?.Any(reference =>
                    reference?.Get() == quarterstaffTraining) ?? false)) ||
            CountFeatureAtLevel(monkProgression.LevelEntries, evasion, 2) != 1 ||
            CountFeatureAtLevel(monkProgression.LevelEntries, fastMovement, 3) != 1 ||
            CountFeature(sensei.RemoveFeatures, evasion) != 0 ||
            CountFeature(sensei.RemoveFeatures, fastMovement) != 0 ||
            CountOtherFeatures(sensei.RemoveFeatures, evasion, fastMovement) !=
                otherSenseiRemovalsBefore) {
            throw new InvalidOperationException(
                "A visible Uncanny Dodge wrapper must appear exactly once in every Monk Bonus Feat pool available from level 6 onward and grant the hidden multiclass-safe checker; both versions of Ki Power: Perfect Self must add 1d10 force damage to unarmed hits; Student of Stone must retain Strength of Stone at level 3, add 1d6 bludgeoning damage to unarmed hits at level 9, and gain +4 inherent Strength and Constitution at level 15; Traditional Monk must gain Mastering the fundamentals at levels 7, 11, and 15 for one unarmed damage size increase, +2 dodge AC, and one additional Flurry attack respectively; Zen Archer must regain Evasion and Improved Evasion, exclude Rapid Shot, Manyshot, and Deflect Arrows from its bonus-feat pools, gain Deflect Arrows automatically at level 7, offer Improved Initiative in every bonus-feat pool, improve unarmed and Ki Arrows damage dice at level 15, and replace Perfect Self with One with the Arrow at level 20; Quarterstaff Master must gain four ranks of quarterstaff-only Weapon Training at levels 7, 10, 13, and 16; and Sensei must retain Evasion at level 2 and Fast Movement at level 3 without changing its other replacements.");
        }
    }

    private static BlueprintFeatureReference[] AddVisibleSelectionFeature(
        BlueprintFeatureReference[] references,
        BlueprintFeature hiddenFeature,
        BlueprintFeature visibleFeature) {
        var result = references?
            .Where(reference =>
                reference?.Get() != hiddenFeature && reference?.Get() != visibleFeature)
            .ToList() ?? new List<BlueprintFeatureReference>();
        result.Add(BlueprintTool.GetRef<BlueprintFeatureReference>(
            visibleFeature.AssetGuid.ToString()));
        return result.ToArray();
    }

    private static BlueprintFeatureReference[] RemoveSelectionFeatures(
        BlueprintFeatureReference[] references,
        params BlueprintFeature[] excludedFeatures) =>
        references?
            .Where(reference => !excludedFeatures.Contains(reference?.Get()))
            .ToArray() ?? Array.Empty<BlueprintFeatureReference>();

    private static BlueprintFeatureReference[] AddSelectionFeature(
        BlueprintFeatureReference[] references,
        BlueprintFeature feature) {
        var result = references?
            .Where(reference => reference?.Get() != feature)
            .ToList() ?? new List<BlueprintFeatureReference>();
        result.Add(BlueprintTool.GetRef<BlueprintFeatureReference>(
            feature.AssetGuid.ToString()));
        return result.ToArray();
    }

    private static int CountOtherFeatures(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase firstExcluded,
        BlueprintFeatureBase secondExcluded) =>
        entries.Sum(entry => entry.m_Features?.Count(reference => {
            var feature = reference?.Get();
            return feature != firstExcluded && feature != secondExcluded;
        }) ?? 0);

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature) =>
        entries.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0);

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeatureBase feature,
        int level) =>
        entries
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0);

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
        entries.RemoveAll(entry => entry.m_Features == null || entry.m_Features.Count == 0);
    }
}
