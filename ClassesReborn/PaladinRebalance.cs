using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using System.Security.Cryptography;
using System.Text;

namespace ClassesReborn;

internal static class PaladinRebalance {
    private static readonly int[] PaladinMercyLevels = { 3, 6, 9, 12, 15, 18 };
    private static readonly int[] PaladinCombatPathFeatLevels = { 3, 9, 15 };
    private static readonly int[] TorturedCrusaderBonusCombatFeatLevels = {
        2, 8, 12, 16,
    };

    private static readonly string[] PowerOfFaithAbilityIds = {
        BlueprintIds.PowerOfFaithTier1Ability,
        BlueprintIds.PowerOfFaithTier2Ability,
        BlueprintIds.PowerOfFaithTier3Ability,
        BlueprintIds.PowerOfFaithTier4Ability,
        BlueprintIds.PowerOfFaithTier5Ability,
    };

    private static readonly string[] PowerOfFaithBonusBuffIds = {
        BlueprintIds.PowerOfFaithTier1Buff,
        BlueprintIds.PowerOfFaithTier3Buff,
        BlueprintIds.PowerOfFaithTier4Buff,
        BlueprintIds.PowerOfFaithTier4CasterBuff,
        BlueprintIds.PowerOfFaithTier5Buff,
        BlueprintIds.PowerOfFaithTier5CasterBuff,
    };

    private static readonly IReadOnlyDictionary<WeaponCategory, string>
        DeityWeaponFocusGrantIds = new Dictionary<WeaponCategory, string> {
            [WeaponCategory.Dagger] = "5371eabf28a54e79b6cffe298f94c822",
            [WeaponCategory.Falchion] = "eb5ed4fffb714d42926c8755da9366a6",
            [WeaponCategory.Flail] = "4ad212d2542d4aa0af9b7724af06196d",
            [WeaponCategory.Glaive] = "87dda3de6e3247b5b3b2e3fe1b26e06d",
            [WeaponCategory.Greataxe] = "b6b88a8059e84fb3b3bac060aacab673",
            [WeaponCategory.Greatsword] = "f92c8f9ab39c45559b948af6d94ec96f",
            [WeaponCategory.HeavyFlail] = "8fa3e4e934854e9787b4a5535f551d57",
            [WeaponCategory.HeavyMace] = "2782ca5841714c4c83547ddd3d093e8d",
            [WeaponCategory.LightMace] = "40ccb469c8794508bc91e306a1a3be03",
            [WeaponCategory.LightCrossbow] = "d08a27acd0384eb3b0e75764399c4f57",
            [WeaponCategory.Longbow] = "fb2aba259772428e9601317dc9cfbb4d",
            [WeaponCategory.Longsword] = "8cd729d8d19d4201a192933376b77628",
            [WeaponCategory.Quarterstaff] = "fccab1b031ab4b65b1878211ad060b66",
            [WeaponCategory.Rapier] = "7e237a3115cb4c5686e4358d0c44ed70",
            [WeaponCategory.SawtoothSabre] = "52af04e08bca4b558d13eff64c27aa14",
            [WeaponCategory.Scimitar] = "3480465d47a549ffa5d3b16f9389246e",
            [WeaponCategory.Scythe] = "8d736fec3aba404ca178906a4202a4f9",
            [WeaponCategory.Shortsword] = "f8859b8861a244dbb4a1b27807de819c",
            [WeaponCategory.Starknife] = "378745d5f82140e499d23b2b0efe9f95",
            [WeaponCategory.Trident] = "dd6883d33107451ca859c95bfd9e6291",
            [WeaponCategory.UnarmedStrike] = "89f84dcaa9e94a04b6b41c37583c50e2",
            [WeaponCategory.Warhammer] = "776a8464a7d44d8d979424b1f4fb2906",
        };

    internal static void Configure() {
        // Restore these first so an independent Paladin customization cannot
        // prevent the archetypes from receiving Divine Grace.
        var divineGrace = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.PaladinDivineGrace);
        RestoreFeature(
            BlueprintIds.MartyrArchetype,
            divineGrace,
            "Martyr");
        RestoreFeature(
            BlueprintIds.StonelordArchetype,
            divineGrace,
            "Stonelord");

        ConfigureLayOnHandsUses();
        ConfigureMercyOrCombatPath();
        ConfigureDeityWeaponFocus();
        ConfigureWarriorOfTheHolyLight();
    }

    private static void ConfigureMercyOrCombatPath() {
        var paladinClass = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.PaladinClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.PaladinProgression);
        var mercySelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.PaladinMercySelection);
        var fighterBonusFeats = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FighterBonusFeatSelection);
        var torturedCrusader = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.TorturedCrusaderArchetype);
        var torturedCrusaderBonusFeats = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.TorturedCrusaderBonusFeatSelection);
        var paladinBonusFeats = FeatureSelectionConfigurator.New(
                "ClassesRebornPaladinBonusCombatFeatSelection",
                BlueprintIds.PaladinBonusCombatFeatSelection)
            .CopyFrom(BlueprintIds.FighterBonusFeatSelection)
            .SetDisplayName("ClassesReborn.PaladinBonusCombatFeat.Name")
            .SetDescription("ClassesReborn.PaladinBonusCombatFeat.Description")
            .SetIsClassFeature(true)
            .Configure();

        var mercyPath = ProgressionConfigurator.New(
                "ClassesRebornPaladinMercyPathProgression",
                BlueprintIds.PaladinMercyPathProgression)
            .SetDisplayName("ClassesReborn.PaladinMercyPath.Name")
            .SetDescription("ClassesReborn.PaladinMercyPath.Description")
            .SetIcon(mercySelection.Icon)
            .SetClasses(BlueprintIds.PaladinClass)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(true)
            .Configure();
        mercyPath.LevelEntries = PaladinMercyLevels
            .Select(level => new LevelEntry {
                Level = level,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    mercySelection.ToReference<BlueprintFeatureBaseReference>(),
                },
            })
            .ToArray();

        var combatPath = ProgressionConfigurator.New(
                "ClassesRebornPaladinCombatPathProgression",
                BlueprintIds.PaladinCombatPathProgression)
            .SetDisplayName("ClassesReborn.PaladinCombatPath.Name")
            .SetDescription("ClassesReborn.PaladinCombatPath.Description")
            .SetIcon(fighterBonusFeats.Icon)
            .SetClasses(BlueprintIds.PaladinClass)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(true)
            .Configure();
        combatPath.LevelEntries = PaladinCombatPathFeatLevels
            .Select(level => new LevelEntry {
                Level = level,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    paladinBonusFeats.ToReference<BlueprintFeatureBaseReference>(),
                },
            })
            .ToArray();

        var pathSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornPaladinMercyOrCombatPathSelection",
                BlueprintIds.PaladinMercyOrCombatPathSelection)
            .SetDisplayName("ClassesReborn.PaladinPathChoice.Name")
            .SetDescription("ClassesReborn.PaladinPathChoice.Description")
            .SetIcon(mercySelection.Icon)
            .SetIsClassFeature(true)
            .AddToAllFeatures(mercyPath, combatPath)
            .Configure();

        var torturedCrusaderReplacesMercy =
            CountFeature(torturedCrusader.RemoveFeatures, mercySelection) > 0;
        var mercyReplacingArchetypes = paladinClass.Archetypes
            .Where(archetype =>
                CountFeature(archetype.RemoveFeatures, mercySelection) > 0)
            .ToHashSet();
        // Preserve the earlier Tortured Crusader decision: it keeps only its
        // four native Self-Sufficient feats and cannot trade Mercy for three
        // more combat feats. If it retained Mercy in the native blueprint,
        // grant the Mercy path directly so removing the base entries does not
        // accidentally take that progression away.
        mercyReplacingArchetypes.Add(torturedCrusader);

        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, paladinBonusFeats);
        RemoveFeature(levelEntries, mercySelection);
        RemoveFeature(levelEntries, pathSelection);
        AddFeature(levelEntries, 3, pathSelection);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        foreach (var archetype in paladinClass.Archetypes) {
            var removals = archetype.RemoveFeatures?.ToList()
                ?? new List<LevelEntry>();
            RemoveFeature(removals, paladinBonusFeats);
            RemoveFeature(removals, mercySelection);
            RemoveFeature(removals, pathSelection);
            if (mercyReplacingArchetypes.Contains(archetype)) {
                AddFeature(removals, 3, pathSelection);
            }
            archetype.RemoveFeatures = removals
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        var torturedCrusaderAdditions = torturedCrusader.AddFeatures?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(torturedCrusaderAdditions, mercyPath);
        if (!torturedCrusaderReplacesMercy) {
            AddFeature(torturedCrusaderAdditions, 3, mercyPath);
        }
        torturedCrusader.AddFeatures = torturedCrusaderAdditions
            .OrderBy(entry => entry.Level)
            .ToArray();

        var expectedChoices = fighterBonusFeats.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        var actualChoices = paladinBonusFeats.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        var torturedCrusaderTotal = CountFeature(
            torturedCrusader.AddFeatures,
            torturedCrusaderBonusFeats);

        if (!expectedChoices.SetEquals(actualChoices) ||
            paladinBonusFeats.m_AllFeatures.Length != expectedChoices.Count ||
            PaladinMercyLevels.Any(level =>
                CountFeatureAtLevel(
                    mercyPath.LevelEntries,
                    mercySelection,
                    level) != 1) ||
            CountFeature(mercyPath.LevelEntries, mercySelection) !=
                PaladinMercyLevels.Length ||
            PaladinCombatPathFeatLevels.Any(level =>
                CountFeatureAtLevel(
                    combatPath.LevelEntries,
                    paladinBonusFeats,
                    level) != 1) ||
            CountFeature(combatPath.LevelEntries, paladinBonusFeats) !=
                PaladinCombatPathFeatLevels.Length ||
            CountFeatureAtLevel(progression.LevelEntries, pathSelection, 3) != 1 ||
            CountFeature(progression.LevelEntries, pathSelection) != 1 ||
            CountFeature(progression.LevelEntries, mercySelection) != 0 ||
            CountFeature(progression.LevelEntries, paladinBonusFeats) != 0 ||
            pathSelection.m_AllFeatures.Length != 2 ||
            !pathSelection.m_AllFeatures.Any(reference =>
                reference.Get() == mercyPath) ||
            !pathSelection.m_AllFeatures.Any(reference =>
                reference.Get() == combatPath) ||
            paladinClass.Archetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, mercySelection) != 0 ||
                CountFeature(archetype.RemoveFeatures, paladinBonusFeats) != 0 ||
                CountFeature(archetype.RemoveFeatures, pathSelection) !=
                    (mercyReplacingArchetypes.Contains(archetype) ? 1 : 0)) ||
            TorturedCrusaderBonusCombatFeatLevels.Any(level =>
                CountFeatureAtLevel(
                    torturedCrusader.AddFeatures,
                    torturedCrusaderBonusFeats,
                    level) != 1) ||
            CountFeature(
                torturedCrusader.AddFeatures,
                torturedCrusaderBonusFeats) !=
                    TorturedCrusaderBonusCombatFeatLevels.Length ||
            CountFeature(torturedCrusader.AddFeatures, mercyPath) !=
                (torturedCrusaderReplacesMercy ? 0 : 1) ||
            torturedCrusaderTotal != 4) {
            throw new InvalidOperationException(
                "Paladins must choose the Mercy or Combat path at level 3, while Mercy-replacing archetypes must preserve their original tradeoffs.");
        }
    }

    internal static void ConfigureSpellListChanges() {
        if (Main.Settings.PaladinAid) {
            AddExistingSpellToPaladinList(BlueprintIds.AidAbility, 2);
        }
        if (Main.Settings.PaladinFreedomOfMovement) {
            AddExistingSpellToPaladinList(BlueprintIds.FreedomOfMovementAbility, 4);
        }
    }

    private static void AddExistingSpellToPaladinList(
        string abilityId,
        int spellLevel) {
        AbilityConfigurator.For(abilityId)
            .AddToSpellList(spellLevel, BlueprintIds.PaladinSpellList)
            .Configure();

        var spellList = BlueprintTool.Get<BlueprintSpellList>(
            BlueprintIds.PaladinSpellList);
        var ability = BlueprintTool.Get<BlueprintAbility>(abilityId);
        var requestedLevelCount = spellList.SpellsByLevel[spellLevel].Spells
            .Count(spell => spell == ability);
        var totalCount = spellList.SpellsByLevel.Sum(level =>
            level.Spells.Count(spell => spell == ability));
        if (requestedLevelCount != 1 || totalCount != 1) {
            throw new InvalidOperationException(
                $"{ability.name} must appear exactly once and only at level {spellLevel} on the Paladin spell list.");
        }
    }

    private static void ConfigureLayOnHandsUses() {
        var resourceIds = new[] {
            BlueprintIds.LayOnHandsResource,
            BlueprintIds.TorturedCrusaderLayOnHandsResource,
            BlueprintIds.LayOnHandsResourceTorturedCrusader,
        };

        var resources = resourceIds.Select(id => {
            var amount = new ResourceAmountBuilder()
                .IncreaseByLevel(new[] { BlueprintIds.PaladinClass }, 1)
                .IncreaseByStat(StatType.Charisma);
            var resource = AbilityResourceConfigurator.For(id)
                .SetMaxAmount(amount)
                .Configure();
            resource.m_MaxAmount.BaseValue = 0;
            return resource;
        }).ToArray();

        FeatureConfigurator.For(BlueprintIds.PaladinLayOnHandsFeature)
            .SetDescription("ClassesReborn.LayOnHands.Description")
            .Configure();
        AbilityConfigurator.For(BlueprintIds.LayOnHandsSelfAbility)
            .SetDescription("ClassesReborn.LayOnHands.Description")
            .Configure();
        AbilityConfigurator.For(BlueprintIds.LayOnHandsOthersAbility)
            .SetDescription("ClassesReborn.LayOnHands.Description")
            .Configure();
        AbilityConfigurator.For(BlueprintIds.LayOnHandsSelfOrTrothAbility)
            .SetDescription("ClassesReborn.LayOnHands.Troth.Description")
            .Configure();
        FeatureConfigurator.For(
                BlueprintIds.TorturedCrusaderLayOnHandsDescriptionFeature)
            .SetDescription("ClassesReborn.LayOnHands.TorturedCrusader.Description")
            .Configure();

        if (resources.Length != resourceIds.Length ||
            resources.Any(resource =>
                resource.m_MaxAmount.BaseValue != 0 ||
                !resource.m_MaxAmount.IncreasedByLevel ||
                resource.m_MaxAmount.LevelIncrease != 1 ||
                resource.m_MaxAmount.m_Class.Length != 1 ||
                resource.m_MaxAmount.m_Class[0].Get().AssetGuid.ToString() !=
                    BlueprintIds.PaladinClass ||
                resource.m_MaxAmount.IncreasedByLevelStartPlusDivStep ||
                !resource.m_MaxAmount.IncreasedByStat ||
                resource.m_MaxAmount.ResourceBonusStat != StatType.Charisma)) {
            throw new InvalidOperationException(
                "Every Paladin Lay on Hands resource must grant uses equal to Paladin level plus Charisma modifier.");
        }
    }

    private static void ConfigureWarriorOfTheHolyLight() {
        var powerOfFaithAbilities = PowerOfFaithAbilityIds
            .Select(BlueprintTool.Get<BlueprintAbility>)
            .ToArray();
        foreach (var ability in powerOfFaithAbilities) {
            ability.ActionType = UnitCommand.CommandType.Swift;
            ability.m_IsFullRoundAction = false;
        }

        powerOfFaithAbilities[0].m_Description = new LocalizedString {
            Key = "ClassesReborn.PowerOfFaith.Tier1.Description",
        };
        FeatureConfigurator.For(BlueprintIds.PowerOfFaithTier1Feature)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier1.Description")
            .Configure();
        FeatureConfigurator.For(BlueprintIds.PowerOfFaithTier1MainFeature)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier1.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.PowerOfFaithTier1AreaBuff)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier1.Description")
            .Configure();

        powerOfFaithAbilities[4].m_Description = new LocalizedString {
            Key = "ClassesReborn.PowerOfFaith.Tier5.Description",
        };
        FeatureConfigurator.For(BlueprintIds.PowerOfFaithTier5Feature)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier5.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.PowerOfFaithTier5AreaBuff)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier5.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.PowerOfFaithTier5Buff)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier5.Description")
            .Configure();
        BuffConfigurator.For(BlueprintIds.PowerOfFaithTier5CasterBuff)
            .SetDescription("ClassesReborn.PowerOfFaith.Tier5.Description")
            .Configure();

        var shiningLight = AbilityConfigurator.For(BlueprintIds.ShiningLightAbility)
            .SetDescription("ClassesReborn.ShiningLight.Description")
            .SetActionType(UnitCommand.CommandType.Swift)
            .SetIsFullRoundAction(false)
            .Configure();
        FeatureConfigurator.For(BlueprintIds.ShiningLightFeature)
            .SetDescription("ClassesReborn.ShiningLight.Description")
            .Configure();

        var bonusBuffs = PowerOfFaithBonusBuffIds
            .Select(BlueprintTool.Get<BlueprintBuff>)
            .ToArray();
        foreach (var buff in bonusBuffs) {
            var statBonuses = buff.GetComponents<AddStatBonus>().ToArray();
            var fearSaveBonuses = buff
                .GetComponents<SavingThrowBonusAgainstDescriptor>()
                .ToArray();
            var expectedStats = new[] {
                StatType.AC,
                StatType.AdditionalAttackBonus,
                StatType.AdditionalDamage,
            };

            if (statBonuses.Length != expectedStats.Length ||
                expectedStats.Any(stat =>
                    statBonuses.Count(component => component.Stat == stat) != 1) ||
                fearSaveBonuses.Length != 1) {
                throw new InvalidOperationException(
                    $"Unexpected Power of Faith bonus components on {buff.name}.");
            }

            foreach (var statBonus in statBonuses) {
                statBonus.Descriptor = ModifierDescriptor.Sacred;
            }
            fearSaveBonuses[0].ModifierDescriptor = ModifierDescriptor.Sacred;
        }

        if (powerOfFaithAbilities.Length != PowerOfFaithAbilityIds.Length ||
            powerOfFaithAbilities.Any(ability =>
                ability.ActionType != UnitCommand.CommandType.Swift ||
                ability.IsFullRoundAction) ||
            shiningLight.ActionType != UnitCommand.CommandType.Swift ||
            shiningLight.IsFullRoundAction ||
            bonusBuffs.Any(buff =>
                buff.GetComponents<AddStatBonus>().Any(component =>
                    component.Descriptor != ModifierDescriptor.Sacred) ||
                buff.GetComponents<SavingThrowBonusAgainstDescriptor>().Any(component =>
                    component.ModifierDescriptor != ModifierDescriptor.Sacred))) {
            throw new InvalidOperationException(
                "Warrior of the Holy Light action types or Power of Faith bonus descriptors are invalid.");
        }
    }

    private static void ConfigureDeityWeaponFocus() {
        var weaponFocus = BlueprintTool.Get<BlueprintParametrizedFeature>(
            BlueprintIds.WeaponFocus);
        var sourceController = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.WarpriestDeitySacredWeaponFeature);
        var sourceMappings = sourceController
            .GetComponents<AddFeatureIfHasFact>()
            .ToArray();

        if (sourceMappings.Length == 0) {
            throw new InvalidOperationException(
                "The Warpriest deity favored-weapon map is empty.");
        }

        var mappings = sourceMappings.SelectMany(source => {
            var deity = source.m_CheckedFact?.Get()
                ?? throw new InvalidOperationException(
                    "A Warpriest favored-weapon mapping has no deity fact.");
            var sacredWeaponFeature = source.m_Feature?.Get() as BlueprintFeature
                ?? throw new InvalidOperationException(
                    $"The favored-weapon mapping for {deity.name} has no feature.");
            var categories = sacredWeaponFeature
                .GetComponents<SacredWeaponFavoriteDamageOverride>()
                .Select(component => component.Category)
                .Distinct()
                .ToArray();
            if (categories.Length == 0) {
                throw new InvalidOperationException(
                    $"The favored-weapon mapping for {deity.name} has no weapon category.");
            }

            return categories.Select(category =>
                new DeityWeaponMapping(deity, category));
        }).ToArray();

        var duplicateMappings = mappings
            .GroupBy(mapping => (mapping.Deity, mapping.Category))
            .Where(group => group.Count() != 1)
            .Select(group => $"{group.Key.Deity.name}/{group.Key.Category}")
            .ToArray();
        if (duplicateMappings.Length != 0) {
            throw new InvalidOperationException(
                $"Duplicate deity favored-weapon mappings: {string.Join(", ", duplicateMappings)}.");
        }

        var grantFeatures = new Dictionary<WeaponCategory, BlueprintFeature>();
        foreach (var category in mappings
                     .Select(mapping => mapping.Category)
                     .Distinct()) {
            var guid = GetDeityWeaponFocusGrantId(category);

            grantFeatures[category] = FeatureConfigurator.New(
                    $"ClassesRebornPaladinDeityWeaponFocus{category}",
                    guid)
                .SetDisplayName("ClassesReborn.PaladinDeityWeaponFocus.Name")
                .SetDescription("ClassesReborn.PaladinDeityWeaponFocus.Description")
                .SetIcon(weaponFocus.Icon)
                .SetIsClassFeature(true)
                .SetHideInUI(true)
                .SetHideInCharacterSheetAndLevelUp(true)
                .AddParametrizedFeatures(new[] {
                    new AddParametrizedFeatures.FeatureData {
                        m_Feature = BlueprintTool.GetRef<BlueprintParametrizedFeatureReference>(
                            BlueprintIds.WeaponFocus),
                        ParamWeaponCategory = category,
                    },
                })
                .Configure();
        }

        var controllerConfigurator = FeatureConfigurator.New(
                "ClassesRebornPaladinDeityWeaponFocus",
                BlueprintIds.PaladinDeityWeaponFocus)
            .SetDisplayName("ClassesReborn.PaladinDeityWeaponFocus.Name")
            .SetDescription("ClassesReborn.PaladinDeityWeaponFocus.Description")
            .SetIcon(weaponFocus.Icon)
            .SetIsClassFeature(true);

        foreach (var mapping in mappings) {
            controllerConfigurator.AddFeatureIfHasFact(
                checkedFact: mapping.Deity,
                feature: grantFeatures[mapping.Category]);
        }

        var controller = controllerConfigurator.Configure();
        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.PaladinProgression);
        var levelEntries = progression.LevelEntries?.ToList()
            ?? new List<LevelEntry>();
        RemoveFeature(levelEntries, controller);
        AddFeature(levelEntries, 1, controller);
        progression.LevelEntries = levelEntries
            .OrderBy(entry => entry.Level)
            .ToArray();

        ValidateDeityWeaponFocus(
            progression,
            controller,
            weaponFocus,
            mappings,
            grantFeatures);
    }

    private static string GetDeityWeaponFocusGrantId(
        WeaponCategory category) {
        if (DeityWeaponFocusGrantIds.TryGetValue(category, out var guid)) {
            return guid;
        }

        using var hashAlgorithm = SHA256.Create();
        var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(
            $"ClassesReborn.PaladinDeityWeaponFocus.{category}"));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, guidBytes.Length);
        return new Guid(guidBytes).ToString("N");
    }

    private static void ValidateDeityWeaponFocus(
        BlueprintProgression progression,
        BlueprintFeature controller,
        BlueprintParametrizedFeature weaponFocus,
        IReadOnlyCollection<DeityWeaponMapping> sourceMappings,
        IReadOnlyDictionary<WeaponCategory, BlueprintFeature> grantFeatures) {
        var controllerMappings = controller
            .GetComponents<AddFeatureIfHasFact>()
            .ToArray();
        var mappingsMatch = sourceMappings.All(source =>
            controllerMappings.Count(candidate =>
                !candidate.Not &&
                candidate.m_CheckedFact?.Get() == source.Deity &&
                candidate.m_Feature?.Get() == grantFeatures[source.Category]) == 1);

        var grantsAreValid = grantFeatures.All(pair => {
            var components = pair.Value
                .GetComponents<AddParametrizedFeatures>()
                .ToArray();
            var grants = components.Length == 1
                ? components[0].m_Features
                : Array.Empty<AddParametrizedFeatures.FeatureData>();
            return grants.Length == 1 &&
                   grants[0].m_Feature?.Get() == weaponFocus &&
                   grants[0].ParamWeaponCategory == pair.Key;
        });

        if (CountFeature(progression.LevelEntries, controller) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, controller, 1) != 1 ||
            controllerMappings.Length != sourceMappings.Count ||
            !mappingsMatch ||
            !grantsAreValid) {
            throw new InvalidOperationException(
                "Every Paladin must gain Weapon Focus with the favored weapon of the selected deity at level 1.");
        }
    }

    private static void RestoreFeature(
        string archetypeId,
        BlueprintFeature feature,
        string archetypeName) {
        var archetype = BlueprintTool.Get<BlueprintArchetype>(archetypeId);
        var removals = archetype.RemoveFeatures?.ToList()
            ?? new List<LevelEntry>();
        var otherRemovalsBefore = CountOtherFeatures(removals, feature);

        foreach (var entry in removals) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
        removals.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        if (CountFeature(archetype.RemoveFeatures, feature) != 0 ||
            CountOtherFeatures(archetype.RemoveFeatures, feature) !=
                otherRemovalsBefore) {
            throw new InvalidOperationException(
                $"{archetypeName} must retain Divine Grace at Paladin level 2 without changing any other archetype feature replacement.");
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountOtherFeatures(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature excludedFeature) =>
        entries?.Sum(entry => entry.m_Features?.Count(reference =>
            reference?.Get() != excludedFeature) ?? 0) ?? 0;

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
        BlueprintFeature feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
        entries.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
    }

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry => entry.m_Features?.Count(reference =>
                reference?.Get() == feature) ?? 0) ?? 0;

    private sealed class DeityWeaponMapping {
        internal DeityWeaponMapping(
            BlueprintUnitFact deity,
            WeaponCategory category) {
            Deity = deity;
            Category = category;
        }

        internal BlueprintUnitFact Deity { get; }
        internal WeaponCategory Category { get; }
    }
}
