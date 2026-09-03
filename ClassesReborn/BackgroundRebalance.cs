using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using BlueprintCore.Utils;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;

namespace ClassesReborn;

internal static class BackgroundRebalance {
    private static readonly HashSet<StatType> SkillStats = new() {
        StatType.SkillAthletics,
        StatType.SkillKnowledgeArcana,
        StatType.SkillKnowledgeWorld,
        StatType.SkillLoreNature,
        StatType.SkillLoreReligion,
        StatType.SkillMobility,
        StatType.SkillPerception,
        StatType.SkillPersuasion,
        StatType.SkillStealth,
        StatType.SkillThievery,
        StatType.SkillUseMagicDevice,
    };

    internal static void Configure() {
        var root = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.BackgroundsBaseSelection);
        var visited = new HashSet<BlueprintFeatureBase>();
        var convertedBackgroundWeapons = 0;
        var convertedDirectBonuses = 0;
        ConvertBackgroundTree(
            root,
            visited,
            ref convertedBackgroundWeapons,
            ref convertedDirectBonuses);
        ConfigureNativeBackgroundUpgrades();

        if (convertedBackgroundWeapons == 0) {
            throw new InvalidOperationException(
                "The background selection tree did not contain any background weapon-proficiency attack bonuses to convert to Trait bonuses.");
        }

        Main.Log.Log(
            $"Converted {convertedBackgroundWeapons} background weapon attack bonuses and {convertedDirectBonuses} direct background skill/attack bonus components to Trait bonuses across {visited.Count} background features and selections.");
    }

    internal static void ConfigureAddedBackgrounds() {
        var skillIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedInitiativeFeature).Icon;
        var combatIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.WeaponFocus).Icon;
        var faithIcon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.PaladinDivineGrace).Icon;
        var choices = new List<BlueprintFeature>();

        if (Main.Settings.SarkorianExile) {
            choices.Add(CreateSarkorianExile(skillIcon));
        }
        if (Main.Settings.WardstoneVeteran) {
            choices.Add(CreateWardstoneVeteran(faithIcon));
        }
        if (Main.Settings.RedeemedCultist) {
            choices.Add(CreateRedeemedCultist(faithIcon));
        }
        if (Main.Settings.KenabresWatchman) {
            choices.Add(CreateKenabresWatchman(combatIcon));
        }
        if (Main.Settings.NumerianSalvager) {
            choices.Add(CreateNumerianSalvager(skillIcon));
        }
        if (Main.Settings.TempleWeaponmaster) {
            choices.Add(CreateTempleWeaponmaster(combatIcon));
        }
        if (Main.Settings.CrusadeQuartermaster) {
            choices.Add(CreateCrusadeQuartermaster(skillIcon));
        }
        if (Main.Settings.WorldwoundCartographer) {
            choices.Add(CreateWorldwoundCartographer(skillIcon));
        }
        if (Main.Settings.WildRaised) {
            AddToBackgroundCategory(
                BlueprintIds.BackgroundsWandererSelection,
                CreateWildRaised(skillIcon));
        }
        if (Main.Settings.Knight) {
            AddToBackgroundCategory(
                BlueprintIds.BackgroundsNobleSelection,
                CreateKnight(combatIcon));
        }

        if (choices.Count > 0) {
            var selection = FeatureSelectionConfigurator.New(
                    "ClassesRebornAddedBackgroundSelection",
                    FutureContentIds.Get("Background.Selection"))
                .SetDisplayName("ClassesReborn.AddedBackgrounds.Name")
                .SetDescription("ClassesReborn.AddedBackgrounds.Description")
                .SetIcon(skillIcon)
                .SetRanks(1)
                .SetIgnorePrerequisites(false)
                .SetObligatory(true)
                .Configure();
            selection.m_AllFeatures = choices
                .Select(feature => feature.ToReference<BlueprintFeatureReference>())
                .ToArray();
            selection.m_Features = selection.m_AllFeatures.ToArray();

            // These are nested options added explicitly to the background tree.
            // Assigning FeatureGroup.None is not inert: BlueprintCore propagates
            // grouped features into matching global selections, and the zero-valued
            // group can leak them into unrelated heritage selections.
            var nestedFeatures = choices
                .Concat(choices.OfType<BlueprintFeatureSelection>()
                    .SelectMany(choice => choice.m_AllFeatures
                        .Select(reference => reference?.Get())
                        .OfType<BlueprintFeature>()))
                .Append(selection)
                .ToArray();
            if (nestedFeatures.Any(feature => feature.Groups?.Any() == true)) {
                throw new InvalidOperationException(
                    "Added backgrounds and their nested choices must not register with global feature groups.");
            }

            var root = BlueprintTool.Get<BlueprintFeatureSelection>(
                BlueprintIds.BackgroundsBaseSelection);
            root.m_AllFeatures = (root.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                .Where(reference => reference?.deserializedGuid != selection.AssetGuid)
                .Append(selection.ToReference<BlueprintFeatureReference>())
                .ToArray();
            root.m_Features = (root.m_Features ?? Array.Empty<BlueprintFeatureReference>())
                .Where(reference => reference?.deserializedGuid != selection.AssetGuid)
                .Append(selection.ToReference<BlueprintFeatureReference>())
                .ToArray();
        }
    }

    private static void ConfigureNativeBackgroundUpgrades() {
        var bountyHunter = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.BountyHunterBackground);
        bountyHunter.ComponentsArray = (bountyHunter.ComponentsArray ??
                Array.Empty<BlueprintComponent>())
            .Where(component => component is not AddBackgroundArmorProficiency)
            .ToArray();
        foreach (var facts in bountyHunter.GetComponents<AddFacts>()) {
            facts.m_Facts = (facts.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
                .Where(reference => reference?.Get()?.AssetGuid.ToString() !=
                    BlueprintIds.LightArmorProficiency)
                .ToArray();
        }
        foreach (var proficiencies in bountyHunter.GetComponents<AddProficiencies>()) {
            proficiencies.ArmorProficiencies =
                (proficiencies.ArmorProficiencies ?? Array.Empty<ArmorProficiencyGroup>())
                .Where(category => category != ArmorProficiencyGroup.Light)
                .ToArray();
        }
        FeatureConfigurator.For(BlueprintIds.BountyHunterBackground)
            .SetDescription("ClassesReborn.BountyHunterBackground.Description")
            .AddComponent(new ArmorCategoryBackgroundAcBonus {
                Category = ArmorProficiencyGroup.Medium,
                Bonus = 1,
            })
            .Configure();

        FeatureConfigurator.For(BlueprintIds.HealerBackground)
            .SetDescription("ClassesReborn.HealerBackground.Description")
            .AddComponent(new HealerBackgroundHealingBonus { Bonus = 1 })
            .Configure();
        FeatureConfigurator.For(BlueprintIds.MuggerBackground)
            .SetDescription("ClassesReborn.MuggerBackground.Description")
            .AddComponent(new MuggerStealthAttackBonus { Bonus = 1 })
            .Configure();
        FeatureConfigurator.For(BlueprintIds.PickpocketBackground)
            .SetDescription("ClassesReborn.PickpocketBackground.Description")
            .AddComponent(new FightingDefensivelyPenaltyReduction { Reduction = 1 })
            .Configure();

        var bountyHunterStillGrantsLightArmor =
            bountyHunter.GetComponents<AddFacts>().Any(facts =>
                (facts.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).Any(
                    reference => reference?.Get()?.AssetGuid.ToString() ==
                        BlueprintIds.LightArmorProficiency)) ||
            bountyHunter.GetComponents<AddProficiencies>().Any(proficiencies =>
                (proficiencies.ArmorProficiencies ??
                    Array.Empty<ArmorProficiencyGroup>()).Contains(
                        ArmorProficiencyGroup.Light));
        if (bountyHunter.GetComponents<AddBackgroundArmorProficiency>().Any() ||
            bountyHunterStillGrantsLightArmor ||
            bountyHunter.GetComponents<ArmorCategoryBackgroundAcBonus>().Count() != 1 ||
            BlueprintTool.Get<BlueprintFeature>(BlueprintIds.HealerBackground)
                .GetComponents<HealerBackgroundHealingBonus>().Count() != 1 ||
            BlueprintTool.Get<BlueprintFeature>(BlueprintIds.MuggerBackground)
                .GetComponents<MuggerStealthAttackBonus>().Count() != 1 ||
            BlueprintTool.Get<BlueprintFeature>(BlueprintIds.PickpocketBackground)
                .GetComponents<FightingDefensivelyPenaltyReduction>().Count() != 1) {
            throw new InvalidOperationException(
                "Native background upgrades were not configured exactly once.");
        }
    }

    private static BlueprintFeature CreateWildRaised(Sprite icon) {
        var feature = FeatureConfigurator.New(
                "ClassesRebornWildRaisedBackground",
                FutureContentIds.Get("Background.WildRaised"))
            .SetDisplayName("ClassesReborn.WildRaisedBackground.Name")
            .SetDescription("ClassesReborn.WildRaisedBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillLoreNature,
                value: 1)
            .AddClassSkill(StatType.SkillLoreNature)
            .AddComponent(new NaturalWeaponBackgroundAttackBonus { Bonus = 1 })
            .Configure();
        ValidateCategorizedBackground(
            feature,
            feature.GetComponents<NaturalWeaponBackgroundAttackBonus>().Count() == 1);
        return feature;
    }

    private static BlueprintFeature CreateKnight(Sprite icon) {
        var feature = FeatureConfigurator.New(
                "ClassesRebornKnightBackground",
                FutureContentIds.Get("Background.Knight"))
            .SetDisplayName("ClassesReborn.KnightBackground.Name")
            .SetDescription("ClassesReborn.KnightBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddComponent(new AddProficiencies {
                ArmorProficiencies = Array.Empty<ArmorProficiencyGroup>(),
                WeaponProficiencies = new[] { WeaponCategory.Longsword },
            })
            .AddComponent(new ArmorCategoryBackgroundAcBonus {
                Category = ArmorProficiencyGroup.Heavy,
                Bonus = 1,
            })
            .Configure();
        ValidateCategorizedBackground(
            feature,
            feature.GetComponents<AddProficiencies>()
                .SingleOrDefault()?.WeaponProficiencies.Contains(WeaponCategory.Longsword) == true &&
            feature.GetComponents<ArmorCategoryBackgroundAcBonus>().Count() == 1);
        return feature;
    }

    private static void ValidateCategorizedBackground(
        BlueprintFeature feature,
        bool mechanicsValid) {
        if (feature.Groups?.Any() == true || !mechanicsValid) {
            throw new InvalidOperationException(
                $"Background {feature.name} must remain nested in its native category and contain its requested mechanics.");
        }
    }

    private static void AddToBackgroundCategory(
        string selectionId,
        BlueprintFeature feature) {
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
        selection.m_AllFeatures = AppendDistinct(selection.m_AllFeatures, feature);
        if (selection.m_Features?.Length > 0) {
            selection.m_Features = AppendDistinct(selection.m_Features, feature);
        }
    }

    private static BlueprintFeatureReference[] AppendDistinct(
        BlueprintFeatureReference[] references,
        BlueprintFeature feature) {
        references ??= Array.Empty<BlueprintFeatureReference>();
        return references.Any(reference => reference?.Get() == feature)
            ? references
            : references.Append(feature.ToReference<BlueprintFeatureReference>()).ToArray();
    }

    private static BlueprintFeature CreateSarkorianExile(Sprite icon) =>
        CreateSkillBackground(
                "SarkorianExile",
                icon,
                StatType.SkillLoreNature,
                StatType.SkillMobility)
            .AddComponent(new BackgroundEnemyBonus {
                m_EnemyType = FeatureRefs.SubtypeDemon.Reference.Get()
                    .ToReference<BlueprintFeatureReference>(),
                ApplyToAttack = false,
            })
            .Configure();

    private static BlueprintFeature CreateWardstoneVeteran(Sprite icon) =>
        FeatureConfigurator.New(
                "ClassesRebornWardstoneVeteranBackground",
                FutureContentIds.Get("Background.WardstoneVeteran"))
            .SetDisplayName("ClassesReborn.WardstoneVeteranBackground.Name")
            .SetDescription("ClassesReborn.WardstoneVeteranBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.Initiative,
                value: 2)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Fear,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure();

    private static BlueprintFeature CreateRedeemedCultist(Sprite icon) =>
        CreateSkillBackground(
                "RedeemedCultist",
                icon,
                StatType.SkillLoreReligion,
                StatType.SkillUseMagicDevice)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Compulsion,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .AddComponent(new RedeemedCultistDemonSaveBonus {
                m_DemonType = FeatureRefs.SubtypeDemon.Reference.Get()
                    .ToReference<BlueprintFeatureReference>(),
            })
            .Configure();

    private static BlueprintFeature CreateKenabresWatchman(Sprite icon) {
        var armor = FeatureConfigurator.New(
                "ClassesRebornKenabresWatchmanLightArmor",
                FutureContentIds.Get("Background.KenabresWatchman.LightArmor"))
            .SetDisplayName("ClassesReborn.KenabresWatchmanBackground.LightArmor.Name")
            .SetDescription("ClassesReborn.KenabresWatchmanBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddComponent(new AddProficiencies {
                ArmorProficiencies = new[] { ArmorProficiencyGroup.Light },
                WeaponProficiencies = Array.Empty<WeaponCategory>(),
            })
            .Configure();
        var shield = FeatureConfigurator.New(
                "ClassesRebornKenabresWatchmanLightShield",
                FutureContentIds.Get("Background.KenabresWatchman.LightShield"))
            .SetDisplayName("ClassesReborn.KenabresWatchmanBackground.LightShield.Name")
            .SetDescription("ClassesReborn.KenabresWatchmanBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddComponent(new AddProficiencies {
                ArmorProficiencies = new[] { ArmorProficiencyGroup.LightShield },
                WeaponProficiencies = Array.Empty<WeaponCategory>(),
            })
            .Configure();
        return FeatureSelectionConfigurator.New(
                "ClassesRebornKenabresWatchmanBackground",
                FutureContentIds.Get("Background.KenabresWatchman"))
            .SetDisplayName("ClassesReborn.KenabresWatchmanBackground.Name")
            .SetDescription("ClassesReborn.KenabresWatchmanBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillPerception,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillPersuasion,
                value: 1)
            .AddClassSkill(StatType.SkillPerception)
            .AddClassSkill(StatType.SkillPersuasion)
            .AddToAllFeatures(armor, shield)
            .Configure();
    }

    private static BlueprintFeature CreateNumerianSalvager(Sprite icon) =>
        CreateSkillBackground(
                "NumerianSalvager",
                icon,
                StatType.SkillKnowledgeArcana,
                StatType.SkillUseMagicDevice)
            .AddComponent(new BackgroundEnemyBonus {
                m_EnemyType = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.ConstructType),
            })
            .Configure();

    private static BlueprintFeature CreateTempleWeaponmaster(Sprite icon) {
        var source = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.WarpriestDeitySacredWeaponFeature);
        var mappings = source.GetComponents<AddFeatureIfHasFact>()
            .SelectMany(mapping => {
                var deity = mapping.m_CheckedFact?.Get();
                var feature = mapping.m_Feature?.Get() as BlueprintFeature;
                return feature?.GetComponents<SacredWeaponFavoriteDamageOverride>()
                    .Select(component => (Deity: deity, component.Category))
                    ?? Enumerable.Empty<(BlueprintUnitFact Deity, WeaponCategory Category)>();
            })
            .Where(mapping => mapping.Deity != null)
            .Distinct()
            .ToArray();

        var grants = mappings.Select(mapping => mapping.Category)
            .Distinct()
            .ToDictionary(
                category => category,
                category => FeatureConfigurator.New(
                        $"ClassesRebornTempleWeaponmaster{category}",
                        FutureContentIds.Get($"Background.TempleWeaponmaster.{category}"))
                    .SetDisplayName("ClassesReborn.TempleWeaponmasterBackground.Name")
                    .SetDescription("ClassesReborn.TempleWeaponmasterBackground.Description")
                    .SetIcon(icon)
                    .SetHideInUI(true)
                    .SetHideInCharacterSheetAndLevelUp(true)
                    .AddComponent(new AddProficiencies {
                        ArmorProficiencies = Array.Empty<ArmorProficiencyGroup>(),
                        WeaponProficiencies = new[] { category },
                    })
                    .Configure());
        var configurator = FeatureConfigurator.New(
                "ClassesRebornTempleWeaponmasterBackground",
                FutureContentIds.Get("Background.TempleWeaponmaster"))
            .SetDisplayName("ClassesReborn.TempleWeaponmasterBackground.Name")
            .SetDescription("ClassesReborn.TempleWeaponmasterBackground.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillLoreReligion,
                value: 1)
            .AddClassSkill(StatType.SkillLoreReligion);
        foreach (var mapping in mappings) {
            configurator.AddFeatureIfHasFact(mapping.Deity, grants[mapping.Category]);
        }
        return configurator.Configure();
    }

    private static BlueprintFeature CreateCrusadeQuartermaster(Sprite icon) =>
        CreateSkillBackground(
                "CrusadeQuartermaster",
                icon,
                StatType.SkillKnowledgeWorld,
                StatType.SkillPersuasion)
            .AddComponent(new PersonalCarryingCapacityBonus { Bonus = 50 })
            .Configure();

    private static BlueprintFeature CreateWorldwoundCartographer(Sprite icon) =>
        CreateSkillBackground(
                "WorldwoundCartographer",
                icon,
                StatType.SkillPerception,
                StatType.SkillLoreNature)
            .AddComponent(new WorldwoundCartographerInitiative { Bonus = 4 })
            .Configure();

    private static FeatureConfigurator CreateSkillBackground(
        string name,
        Sprite icon,
        StatType first,
        StatType second) =>
        FeatureConfigurator.New(
                $"ClassesReborn{name}Background",
                FutureContentIds.Get($"Background.{name}"))
            .SetDisplayName($"ClassesReborn.{name}Background.Name")
            .SetDescription($"ClassesReborn.{name}Background.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: first,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: second,
                value: 1)
            .AddClassSkill(first)
            .AddClassSkill(second);

    private static void ConvertBackgroundTree(
        BlueprintFeatureBase feature,
        HashSet<BlueprintFeatureBase> visited,
        ref int convertedBackgroundWeapons,
        ref int convertedDirectBonuses) {
        if (feature == null || !visited.Add(feature)) {
            return;
        }

        if (feature is BlueprintScriptableObject blueprint) {
            foreach (var component in blueprint.ComponentsArray ??
                Array.Empty<BlueprintComponent>()) {
                if (component is AddBackgroundWeaponProficiency backgroundWeapon) {
                    backgroundWeapon.StackBonusType = ModifierDescriptor.Trait;
                    convertedBackgroundWeapons++;
                    continue;
                }

                if (TryConvertDirectBonus(component)) {
                    convertedDirectBonuses++;
                }
            }
        }

        if (feature is not BlueprintFeatureSelection selection) {
            return;
        }

        var references = (selection.m_AllFeatures ??
                Array.Empty<BlueprintFeatureReference>())
            .Concat(selection.m_Features ??
                Array.Empty<BlueprintFeatureReference>())
            .GroupBy(reference => reference?.deserializedGuid)
            .Select(group => group.First());
        foreach (var reference in references) {
            if (reference?.Get() is BlueprintFeatureBase child) {
                ConvertBackgroundTree(
                    child,
                    visited,
                    ref convertedBackgroundWeapons,
                    ref convertedDirectBonuses);
            }
        }
    }

    private static bool TryConvertDirectBonus(BlueprintComponent component) {
        var componentType = component.GetType();
        var descriptor = componentType.GetField(
            "Descriptor",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (descriptor?.FieldType != typeof(ModifierDescriptor)) {
            return false;
        }

        var isAttackBonus = componentType.Name.IndexOf(
            "AttackBonus",
            StringComparison.OrdinalIgnoreCase) >= 0;
        var stat = componentType.GetField(
            "Stat",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var isSkillBonus = stat?.FieldType == typeof(StatType) &&
            stat.GetValue(component) is StatType statType &&
            SkillStats.Contains(statType);
        var isGenericAttackBonus = stat?.FieldType == typeof(StatType) &&
            stat.GetValue(component) is StatType attackStat &&
            attackStat == StatType.AdditionalAttackBonus;
        if (!isAttackBonus && !isSkillBonus && !isGenericAttackBonus) {
            return false;
        }

        descriptor.SetValue(component, ModifierDescriptor.Trait);
        return true;
    }
}

[HarmonyPatch(typeof(ModifiableValueSkill), "UpdateInternalModifiers")]
internal static class BackgroundSkillTraitBonusPatch {
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions) {
        if (!Main.Settings.BackgroundTraitBonuses) {
            return instructions;
        }
        var result = instructions.ToList();
        var replacements = 0;
        for (var index = 0; index + 1 < result.Count; index++) {
            if (result[index].opcode != OpCodes.Ldc_I4_3 ||
                result[index + 1].operand is not MethodInfo method ||
                method.DeclaringType != typeof(ModifiableValue) ||
                method.Name != nameof(ModifiableValue.AddModifier)) {
                continue;
            }

            result[index] = new CodeInstruction(
                OpCodes.Ldc_I4,
                (int)ModifierDescriptor.Trait);
            replacements++;
        }

        if (replacements != 1) {
            throw new InvalidOperationException(
                $"Expected to replace exactly one hardcoded background skill bonus descriptor, but replaced {replacements}.");
        }
        return result;
    }
}
