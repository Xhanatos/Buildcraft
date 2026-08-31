using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

namespace ClassesReborn;

internal static class FighterRebalance {
    private static readonly int[] WeaponFocusLevels = { 1, 4, 7 };
    private static readonly int[] GreaterWeaponFocusLevels = { 8, 11, 14 };
    private static readonly int[] StrongGripLevels = { 2, 6, 10, 14, 18 };

    internal static void Configure() {
        ConfigureArmorTraining();
        ConfigureWeaponFocusProgression();
        ConfigureFearfulMight();
        ConfigureDragonheirMasteries();
        ConfigureStrongGrip();
        ConfigureTowerShieldDefense();
        ConfigureIAmYourShield();
    }

    private static void ConfigureArmorTraining() {
        var armorTraining = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FighterArmorTraining);
        armorTraining.m_Description = new LocalizedString {
            Key = "ClassesReborn.FighterArmorTraining.Description",
        };
        var maxDexBonuses = armorTraining.GetComponents<MaxDexBonusIncrease>().ToArray();
        var armorPenaltyReductions = armorTraining
            .GetComponents<ArmorCheckPenaltyIncrease>()
            .ToArray();

        foreach (var component in maxDexBonuses) {
            component.BonesPerRank = 2;
        }
        foreach (var component in armorPenaltyReductions) {
            component.BonesPerRank = 2;
        }

        if (maxDexBonuses.Length == 0 ||
            maxDexBonuses.Any(component => component.BonesPerRank != 2) ||
            armorPenaltyReductions.Length != 1 ||
            armorPenaltyReductions[0].BonesPerRank != 2) {
            throw new InvalidOperationException(
                "Every Armor Training rank must reduce armor check penalty by 2 and increase maximum Dexterity bonus by 2.");
        }
    }

    private static void ConfigureTowerShieldDefense() {
        FeatureConfigurator.For(BlueprintIds.TowerShieldDefenseFeature)
            .SetDescription("ClassesReborn.TowerShieldDefense.Description")
            .Configure();
        var buff = BlueprintTool.Get<BlueprintBuff>(BlueprintIds.TowerShieldDefenseBuff);
        var rankConfigs = buff.GetComponents<ContextRankConfig>().ToArray();
        foreach (var rankConfig in rankConfigs) {
            rankConfig.useShieldBonusAc = true;
        }

        if (rankConfigs.Length != 1 ||
            rankConfigs[0].m_BaseValueType != ContextRankBaseValueType.ShieldBonus ||
            !rankConfigs[0].userShieldBaseAc ||
            !rankConfigs[0].useShieldBonusAc ||
            !rankConfigs[0].useShieldFocusAc) {
            throw new InvalidOperationException(
                "Tower Shield Defense must include base, enhancement, and Shield Focus bonuses in touch AC.");
        }
    }

    private static void ConfigureStrongGrip() {
        var strongGrip = FeatureConfigurator.For(
                BlueprintIds.TwoHandedFighterStrongGrip)
            .SetDescription("ClassesReborn.TwoHandedFighterStrongGrip.Description")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Inherent,
                stat: StatType.Strength,
                value: 1)
            .Configure();

        // Retain an empty hidden blueprint at the old GUID so existing saves can
        // resolve previously granted Strong Arms facts without preserving their effect.
        var retiredStrongArms = FeatureConfigurator.New(
                "ClassesRebornTwoHandedFighterStrongArms",
                BlueprintIds.StrongArmsFeature)
            .SetIsClassFeature(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .SetRanks(3)
            .Configure();

        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TwoHandedFighterArchetype);
        var addFeatures = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(addFeatures, retiredStrongArms);
        archetype.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        var strengthBonuses = strongGrip
            .GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Strength)
            .ToArray();
        if (CountFeature(archetype.AddFeatures, retiredStrongArms) != 0 ||
            retiredStrongArms.GetComponents<AddStatBonus>().Any() ||
            CountFeature(archetype.AddFeatures, strongGrip) != StrongGripLevels.Length ||
            StrongGripLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, strongGrip, level) != 1) ||
            strengthBonuses.Length != 1 ||
            strengthBonuses[0].Value != 1 ||
            strengthBonuses[0].Descriptor != ModifierDescriptor.Inherent) {
            throw new InvalidOperationException(
                "Strong Grip must grant +1 inherent Strength per rank at Two-Handed Fighter levels 2/6/10/14/18, with Strong Arms removed.");
        }
    }

    private static void ConfigureFearfulMight() {
        var fearfulMight = FeatureConfigurator.For(
                BlueprintIds.DragonheirFearfulMightFeature)
            .SetDescription("ClassesReborn.FearfulMight.Description")
            .Configure();
        var bonuses = fearfulMight.GetComponents<AddContextStatBonus>().ToArray();
        foreach (var bonus in bonuses) {
            bonus.Multiplier = 2;
        }

        var rankConfigs = fearfulMight.GetComponents<ContextRankConfig>().ToArray();
        if (bonuses.Length != 1 ||
            bonuses[0].Stat != StatType.CheckIntimidate ||
            bonuses[0].Multiplier != 2 ||
            rankConfigs.Length != 1 ||
            rankConfigs[0].m_BaseValueType != ContextRankBaseValueType.ClassLevel ||
            rankConfigs[0].m_Progression != ContextRankProgression.StartPlusDivStep ||
            rankConfigs[0].m_StartLevel != 2 ||
            rankConfigs[0].m_StepLevel != 4) {
            throw new InvalidOperationException(
                "Fearful Might must grant +2 Intimidate per breakpoint at Fighter levels 2, 6, 10, 14, and 18.");
        }
    }

    private static void ConfigureDragonheirMasteries() {
        var armorMastery = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FighterArmorMastery);
        var weaponMastery = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FighterWeaponMastery);
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.DragonheirScionArchetype);
        var removeFeatures = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        var otherRemovalsBefore = removeFeatures.Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.Get() != armorMastery && reference?.Get() != weaponMastery) ?? 0);

        foreach (var entry in removeFeatures) {
            entry.m_Features?.RemoveAll(reference =>
                reference?.Get() == armorMastery || reference?.Get() == weaponMastery);
        }
        removeFeatures.RemoveAll(entry => entry.m_Features == null || entry.m_Features.Count == 0);
        archetype.RemoveFeatures = removeFeatures.OrderBy(entry => entry.Level).ToArray();

        var fighterProgression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.FighterProgression);
        var otherRemovalsAfter = archetype.RemoveFeatures.Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.Get() != armorMastery && reference?.Get() != weaponMastery) ?? 0);
        if (CountFeature(archetype.RemoveFeatures, armorMastery) != 0 ||
            CountFeature(archetype.RemoveFeatures, weaponMastery) != 0 ||
            otherRemovalsAfter != otherRemovalsBefore ||
            CountFeatureAtLevel(fighterProgression.LevelEntries, armorMastery, 19) != 1 ||
            CountFeatureAtLevel(fighterProgression.LevelEntries, weaponMastery, 20) != 1) {
            throw new InvalidOperationException(
                "Dragonheir Scion must retain base Fighter Armor Mastery at level 19 and Weapon Mastery at level 20 without changing its other feature replacements.");
        }
    }

    private static void ConfigureIAmYourShield() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.TowerShieldDefenseFeature).Icon;

        var resourceAmount = new ResourceAmountBuilder()
            .IncreaseByLevelStartPlusDivStep(
                new[] { BlueprintIds.FighterClass },
                otherClassLevelsMultiplier: 0f,
                startingLevel: 5,
                startingBonus: 2,
                levelsPerStep: 5,
                bonusPerStep: 2,
                minBonus: 0);
        var resource = AbilityResourceConfigurator.New(
                "ClassesRebornIAmYourShieldResource",
                BlueprintIds.IAmYourShieldResource)
            .SetLocalizedName("ClassesReborn.IAmYourShield.Name")
            .SetLocalizedDescription("ClassesReborn.IAmYourShield.Description")
            .SetIcon(icon)
            .SetMaxAmount(resourceAmount)
            .Configure();
        resource.m_MaxAmount.BaseValue = 2;

        var effectBuff = BuffConfigurator.New(
                "ClassesRebornIAmYourShieldEffectBuff",
                BlueprintIds.IAmYourShieldEffectBuff)
            .SetDisplayName("ClassesReborn.IAmYourShield.Name")
            .SetDescription("ClassesReborn.IAmYourShield.EffectDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Stack)
            .AddComponent(new IAmYourShieldArmorClassBonus())
            .Configure();

        var area = AbilityAreaEffectConfigurator.New(
                "ClassesRebornIAmYourShieldArea",
                BlueprintIds.IAmYourShieldArea)
            .SetTargetType(BlueprintAbilityAreaEffect.TargetType.Ally)
            .SetShape(AreaEffectShape.Cylinder)
            .SetSize(new Feet(5))
            .SetAffectEnemies(false)
            .SetAggroEnemies(false)
            .AddAbilityAreaEffectBuff(BlueprintIds.IAmYourShieldEffectBuff)
            .Configure();

        var sourceBuff = BuffConfigurator.New(
                "ClassesRebornIAmYourShieldSourceBuff",
                BlueprintIds.IAmYourShieldSourceBuff)
            .SetDisplayName("ClassesReborn.IAmYourShield.Name")
            .SetDescription("ClassesReborn.IAmYourShield.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddAreaEffect(BlueprintIds.IAmYourShieldArea)
            .Configure();

        var ability = ActivatableAbilityConfigurator.New(
                "ClassesRebornIAmYourShieldAbility",
                BlueprintIds.IAmYourShieldAbility)
            .SetDisplayName("ClassesReborn.IAmYourShield.Name")
            .SetDescription("ClassesReborn.IAmYourShield.Description")
            .SetIcon(icon)
            .SetBuff(BlueprintIds.IAmYourShieldSourceBuff)
            .SetDeactivateIfCombatEnded(true)
            .SetDeactivateIfOwnerDisabled(true)
            .SetDeactivateIfOwnerUnconscious(true)
            .SetActivationType(AbilityActivationType.WithUnitCommand)
            .SetActivateWithUnitCommand(UnitCommand.CommandType.Swift)
            .AddActivatableAbilityResourceLogic(
                requiredResource: BlueprintIds.IAmYourShieldResource,
                spendType: ActivatableAbilityResourceLogic.ResourceSpendType.NewRound)
            .AddComponent(new ActivatableAbilityRestrictionByShield {
                m_filterByShieldProficiencyGroup = true,
                m_ShiledProficiencyGroupEntries = ArmorProficiencyGroupFlag.TowerShield,
            })
            .Configure();

        var feature = FeatureConfigurator.New(
                "ClassesRebornIAmYourShieldFeature",
                BlueprintIds.IAmYourShieldFeature)
            .SetDisplayName("ClassesReborn.IAmYourShield.Name")
            .SetDescription("ClassesReborn.IAmYourShield.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddFacts(new() { BlueprintIds.IAmYourShieldAbility })
            .AddAbilityResources(
                amount: 0,
                resource: BlueprintIds.IAmYourShieldResource,
                restoreAmount: true,
                restoreOnLevelUp: true)
            .AddComponent(new PositiveConstitutionResourceBonus {
                m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                    BlueprintIds.IAmYourShieldResource),
            })
            .Configure();

        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TowerShieldSpecialistArchetype);
        var addFeatures = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(addFeatures, 1, feature);
        archetype.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        ValidateIAmYourShield(
            archetype,
            feature,
            ability,
            sourceBuff,
            area,
            effectBuff,
            resource);
    }

    private static void ValidateIAmYourShield(
        BlueprintArchetype archetype,
        BlueprintFeature feature,
        BlueprintActivatableAbility ability,
        BlueprintBuff sourceBuff,
        BlueprintAbilityAreaEffect area,
        BlueprintBuff effectBuff,
        BlueprintAbilityResource resource) {
        var resourceLogic = ability.GetComponents<ActivatableAbilityResourceLogic>().SingleOrDefault();
        var shieldRestriction = ability
            .GetComponents<ActivatableAbilityRestrictionByShield>()
            .SingleOrDefault();
        var constitutionBonus = feature
            .GetComponents<PositiveConstitutionResourceBonus>()
            .SingleOrDefault();

        if (CountFeatureAtLevel(archetype.AddFeatures, feature, 1) != 1 ||
            resource.m_MaxAmount.BaseValue != 2 ||
            !resource.m_MaxAmount.IncreasedByLevelStartPlusDivStep ||
            resource.m_MaxAmount.StartingLevel != 5 ||
            resource.m_MaxAmount.StartingIncrease != 2 ||
            resource.m_MaxAmount.LevelStep != 5 ||
            resource.m_MaxAmount.PerStepIncrease != 2 ||
            resourceLogic?.RequiredResource != resource ||
            resourceLogic.SpendType != ActivatableAbilityResourceLogic.ResourceSpendType.NewRound ||
            shieldRestriction?.m_filterByShieldProficiencyGroup != true ||
            shieldRestriction.m_ShiledProficiencyGroupEntries != ArmorProficiencyGroupFlag.TowerShield ||
            constitutionBonus?.m_Resource?.Get() != resource ||
            ability.Buff != sourceBuff ||
            area.m_TargetType != BlueprintAbilityAreaEffect.TargetType.Ally ||
            area.Size.Value != 5 ||
            sourceBuff.GetComponents<AddAreaEffect>().Count() != 1 ||
            effectBuff.GetComponents<IAmYourShieldArmorClassBonus>().Count() != 1) {
            throw new InvalidOperationException(
                "I Am Your Shield must be a level-1, tower-shield-only, round-based adjacent ally aura with the correct resource scaling.");
        }
    }

    private static void ConfigureWeaponFocusProgression() {
        var weaponFocus = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFocus);
        var greaterWeaponFocus = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.GreaterWeaponFocus);
        var weaponFocusSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornFighterWeaponFocusSelection",
                BlueprintIds.FighterWeaponFocusSelection)
            .SetDisplayName("ClassesReborn.FighterWeaponFocus.Name")
            .SetDescription("ClassesReborn.FighterWeaponFocus.Description")
            .SetIcon(weaponFocus.Icon)
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(BlueprintIds.WeaponFocus)
            .Configure();
        weaponFocusSelection.m_Features = weaponFocusSelection.m_AllFeatures.ToArray();

        var greaterWeaponFocusSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornFighterGreaterWeaponFocusSelection",
                BlueprintIds.FighterGreaterWeaponFocusSelection)
            .SetDisplayName("ClassesReborn.FighterGreaterWeaponFocus.Name")
            .SetDescription("ClassesReborn.FighterGreaterWeaponFocus.Description")
            .SetIcon(greaterWeaponFocus.Icon)
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(BlueprintIds.GreaterWeaponFocus)
            .Configure();
        greaterWeaponFocusSelection.m_Features =
            greaterWeaponFocusSelection.m_AllFeatures.ToArray();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.FighterProgression);
        var levelEntries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, weaponFocusSelection);
        RemoveFeature(levelEntries, greaterWeaponFocusSelection);
        foreach (var level in WeaponFocusLevels) {
            AddFeature(levelEntries, level, weaponFocusSelection);
        }
        foreach (var level in GreaterWeaponFocusLevels) {
            AddFeature(levelEntries, level, greaterWeaponFocusSelection);
        }
        progression.LevelEntries = levelEntries.OrderBy(entry => entry.Level).ToArray();

        var aldoriWeaponFocus = ConfigureAldoriDefenderWeaponFocus(
            weaponFocusSelection,
            weaponFocus);
        Validate(
            progression,
            weaponFocusSelection,
            weaponFocus,
            greaterWeaponFocusSelection,
            greaterWeaponFocus);
        ValidateAldoriDefender(
            weaponFocusSelection,
            weaponFocus,
            aldoriWeaponFocus);
    }

    private static BlueprintFeature ConfigureAldoriDefenderWeaponFocus(
        BlueprintFeatureSelection weaponFocusSelection,
        BlueprintFeature weaponFocus) {
        var fixedWeaponFocus = FeatureConfigurator.New(
                "ClassesRebornAldoriDefenderWeaponFocusDuelingSword",
                BlueprintIds.AldoriDefenderWeaponFocusDuelingSword)
            .SetDisplayName("ClassesReborn.AldoriDefenderWeaponFocus.Name")
            .SetDescription("ClassesReborn.AldoriDefenderWeaponFocus.Description")
            .SetIcon(weaponFocus.Icon)
            .SetIsClassFeature(true)
            .SetHideInCharacterSheetAndLevelUp(true)
            .AddParametrizedFeatures(new[] {
                new AddParametrizedFeatures.FeatureData {
                    m_Feature = BlueprintTool.GetRef<BlueprintParametrizedFeatureReference>(
                        BlueprintIds.WeaponFocus),
                    ParamWeaponCategory = WeaponCategory.DuelingSword,
                },
            })
            .Configure();

        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.AldoriDefenderArchetype);
        var addFeatures = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(addFeatures, 1, fixedWeaponFocus);
        archetype.AddFeatures = addFeatures.OrderBy(entry => entry.Level).ToArray();

        var removeFeatures = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(removeFeatures, 1, weaponFocusSelection);
        archetype.RemoveFeatures = removeFeatures.OrderBy(entry => entry.Level).ToArray();

        return fixedWeaponFocus;
    }

    private static void Validate(
        BlueprintProgression progression,
        BlueprintFeatureSelection weaponFocusSelection,
        BlueprintFeature weaponFocus,
        BlueprintFeatureSelection greaterWeaponFocusSelection,
        BlueprintFeature greaterWeaponFocus) {
        var totalWeaponFocusGrants = CountFeature(
            progression.LevelEntries,
            weaponFocusSelection);
        var totalGreaterWeaponFocusGrants = CountFeature(
            progression.LevelEntries,
            greaterWeaponFocusSelection);
        var unexpectedWeaponFocusGrants = progression.LevelEntries.Any(entry =>
            !WeaponFocusLevels.Contains(entry.Level) &&
            CountFeatureAtLevel(
                progression.LevelEntries,
                weaponFocusSelection,
                entry.Level) != 0);
        var unexpectedGreaterWeaponFocusGrants = progression.LevelEntries.Any(entry =>
            !GreaterWeaponFocusLevels.Contains(entry.Level) &&
            CountFeatureAtLevel(
                progression.LevelEntries,
                greaterWeaponFocusSelection,
                entry.Level) != 0);

        if (totalWeaponFocusGrants != WeaponFocusLevels.Length ||
            WeaponFocusLevels.Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    weaponFocusSelection,
                    level) != 1) ||
            unexpectedWeaponFocusGrants ||
            weaponFocusSelection.m_AllFeatures.Count(reference =>
                reference?.Get() == weaponFocus) != 1 ||
            weaponFocusSelection.m_Features.Count(reference =>
                reference?.Get() == weaponFocus) != 1 ||
            weaponFocusSelection.m_AllFeatures.Count() != 1 ||
            weaponFocusSelection.m_Features.Count() != 1 ||
            totalGreaterWeaponFocusGrants != GreaterWeaponFocusLevels.Length ||
            GreaterWeaponFocusLevels.Any(level =>
                CountFeatureAtLevel(
                    progression.LevelEntries,
                    greaterWeaponFocusSelection,
                    level) != 1) ||
            unexpectedGreaterWeaponFocusGrants ||
            greaterWeaponFocusSelection.m_AllFeatures.Count(reference =>
                reference?.Get() == greaterWeaponFocus) != 1 ||
            greaterWeaponFocusSelection.m_Features.Count(reference =>
                reference?.Get() == greaterWeaponFocus) != 1 ||
            greaterWeaponFocusSelection.m_AllFeatures.Count() != 1 ||
            greaterWeaponFocusSelection.m_Features.Count() != 1) {
            throw new InvalidOperationException(
                "Fighters must gain Weapon Focus at levels 1/4/7 and Greater Weapon Focus at levels 8/11/14.");
        }
    }

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }

    private static void ValidateAldoriDefender(
        BlueprintFeatureSelection weaponFocusSelection,
        BlueprintFeature weaponFocus,
        BlueprintFeature fixedWeaponFocus) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.AldoriDefenderArchetype);
        var parametrizedFeatures = fixedWeaponFocus
            .GetComponents<AddParametrizedFeatures>()
            .ToArray();
        var fixedGrants = parametrizedFeatures.Length == 1
            ? parametrizedFeatures[0].m_Features
            : Array.Empty<AddParametrizedFeatures.FeatureData>();

        if (CountFeature(archetype.AddFeatures, fixedWeaponFocus) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, fixedWeaponFocus, 1) != 1 ||
            CountFeature(archetype.RemoveFeatures, weaponFocusSelection) != 1 ||
            CountFeatureAtLevel(archetype.RemoveFeatures, weaponFocusSelection, 1) != 1 ||
            fixedGrants.Length != 1 ||
            fixedGrants[0].m_Feature?.Get() != weaponFocus ||
            fixedGrants[0].ParamWeaponCategory != WeaponCategory.DuelingSword) {
            throw new InvalidOperationException(
                "Aldori Defender must replace only the level-1 Fighter Weapon Focus selection with Weapon Focus (Dueling Sword).");
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

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
}
