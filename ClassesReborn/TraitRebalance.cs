using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Spells;
using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;

namespace ClassesReborn;

internal static class TraitRebalance {
    internal static void Configure() {
        var weaponFocusIcon = BlueprintTool.Get<BlueprintParametrizedFeature>(BlueprintIds.WeaponFocus).Icon;
        var armorIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FighterArmorTraining).Icon;
        var initiativeIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ImprovedInitiativeFeature).Icon;
        var faithIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.PaladinDivineGrace).Icon;
        var unarmedIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ImprovedUnarmedStrike).Icon;
        var clawIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.RakingClawsFeature).Icon;
        var shieldIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ShieldFocus).Icon;
        // Dirty Fighting is custom content configured later in the blueprint pass and may
        // also be disabled independently. Use its native source icon so trait setup never
        // depends on that later feature existing.
        var dirtyFightingIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.CombatExpertise).Icon;
        // Trait setup runs before class rebalances, so every icon source here must be
        // a native blueprint. Reuse Divine Grace for faith and magic-themed traits.
        var magicIcon = faithIcon;

        var anatomist = FeatureConfigurator.New("ClassesRebornAnatomistTrait", BlueprintIds.AnatomistTrait)
            .SetDisplayName("ClassesReborn.AnatomistTrait.Name")
            .SetDescription("ClassesReborn.AnatomistTrait.Description")
            .SetIcon(weaponFocusIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddCriticalConfirmationBonus(value: 1)
            .Configure();

        var armorExpert = FeatureConfigurator.New("ClassesRebornArmorExpertTrait", BlueprintIds.ArmorExpertTrait)
            .SetDisplayName("ClassesReborn.ArmorExpertTrait.Name")
            .SetDescription("ClassesReborn.ArmorExpertTrait.Description")
            .SetIcon(armorIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddComponent(new ArmorCheckPenaltyIncrease { BonesPerRank = 1 })
            .Configure();

        var reactionary = CreateStatTrait("ClassesRebornReactionaryTrait", BlueprintIds.ReactionaryTrait,
            "ClassesReborn.ReactionaryTrait", initiativeIcon, (StatType.Initiative, 2));
        var resilient = CreateStatTrait("ClassesRebornResilientTrait", BlueprintIds.ResilientTrait,
            "ClassesReborn.ResilientTrait", faithIcon, (StatType.SaveFortitude, 1));
        var bullied = CreateComponentTrait("ClassesRebornBulliedTrait", BlueprintIds.BulliedTrait,
            "ClassesReborn.BulliedTrait", unarmedIcon,
            new TraitAttackOfOpportunityBonus { UnarmedOnly = true });
        var courageous = FeatureConfigurator.New("ClassesRebornCourageousTrait", BlueprintIds.CourageousTrait)
            .SetDisplayName("ClassesReborn.CourageousTrait.Name")
            .SetDescription("ClassesReborn.CourageousTrait.Description")
            .SetIcon(faithIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Fear,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure();
        var deftDodger = CreateStatTrait("ClassesRebornDeftDodgerTrait", BlueprintIds.DeftDodgerTrait,
            "ClassesReborn.DeftDodgerTrait", initiativeIcon, (StatType.SaveReflex, 1));
        var dirtyFighter = CreateComponentTrait("ClassesRebornDirtyFighterTrait", BlueprintIds.DirtyFighterTrait,
            "ClassesReborn.DirtyFighterTrait", dirtyFightingIcon, new DirtyFighterTraitDamage());
        var fencer = CreateComponentTrait("ClassesRebornFencerTrait", BlueprintIds.FencerTrait,
            "ClassesReborn.FencerTrait", weaponFocusIcon,
            new TraitAttackOfOpportunityBonus {
                FighterGroups = new[] { WeaponFighterGroup.BladesLight, WeaponFighterGroup.BladesHeavy },
            });
        var killer = CreateComponentTrait("ClassesRebornKillerTrait", BlueprintIds.KillerTrait,
            "ClassesReborn.KillerTrait", weaponFocusIcon, new KillerTraitDamage());
        var sharpNails = CreateComponentTrait(
            "ClassesRebornSharpNailsTrait",
            FutureContentIds.Get("Trait.SharpNails"),
            "ClassesReborn.SharpNailsTrait",
            clawIcon,
            new SharpNailsCriticalMultiplier());
        var shieldFighter = CreateComponentTrait(
            "ClassesRebornShieldFighterTrait",
            FutureContentIds.Get("Trait.ShieldFighter"),
            "ClassesReborn.ShieldFighterTrait",
            shieldIcon,
            new ShieldFighterTraitComponent());
        var birthmark = FeatureConfigurator.New("ClassesRebornBirthmarkTrait", BlueprintIds.BirthmarkTrait)
            .SetDisplayName("ClassesReborn.BirthmarkTrait.Name")
            .SetDescription("ClassesReborn.BirthmarkTrait.Description")
            .SetIcon(faithIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Charm | SpellDescriptor.Compulsion,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure();
        var devotee = CreateSkillChoiceTrait(
            "DevoteeOfTheGreen", BlueprintIds.DevoteeOfTheGreenTrait, BlueprintIds.DevoteeOfTheGreenBonuses,
            BlueprintIds.DevoteeOfTheGreenSelection, BlueprintIds.DevoteeOfTheGreenWorld,
            BlueprintIds.DevoteeOfTheGreenNature, "ClassesReborn.DevoteeOfTheGreenTrait", faithIcon,
            StatType.SkillKnowledgeWorld, StatType.SkillLoreNature);
        var easeOfFaith = CreateSkillTrait("ClassesRebornEaseOfFaithTrait", BlueprintIds.EaseOfFaithTrait,
            "ClassesReborn.EaseOfFaithTrait", faithIcon, StatType.SkillPersuasion);
        var historyOfHeresy = CreateComponentTrait("ClassesRebornHistoryOfHeresyTrait",
            BlueprintIds.HistoryOfHeresyTrait, "ClassesReborn.HistoryOfHeresyTrait", faithIcon,
            new HistoryOfHeresySaveBonus());
        var indomitableFaith = CreateStatTrait("ClassesRebornIndomitableFaithTrait",
            BlueprintIds.IndomitableFaithTrait, "ClassesReborn.IndomitableFaithTrait", faithIcon,
            (StatType.SaveWill, 1));
        var sacredConduit = CreateComponentTrait("ClassesRebornSacredConduitTrait",
            BlueprintIds.SacredConduitTrait, "ClassesReborn.SacredConduitTrait", magicIcon,
            new SacredConduitDcBonus());
        var scholar = CreateSkillChoiceTrait(
            "ScholarOfTheGreatBeyond", BlueprintIds.ScholarOfTheGreatBeyondTrait,
            BlueprintIds.ScholarOfTheGreatBeyondBonuses, BlueprintIds.ScholarOfTheGreatBeyondSelection,
            BlueprintIds.ScholarOfTheGreatBeyondWorld, BlueprintIds.ScholarOfTheGreatBeyondArcana,
            "ClassesReborn.ScholarOfTheGreatBeyondTrait", magicIcon,
            StatType.SkillKnowledgeWorld, StatType.SkillKnowledgeArcana);
        var dangerouslyCurious = CreateSkillTrait("ClassesRebornDangerouslyCuriousTrait",
            BlueprintIds.DangerouslyCuriousTrait, "ClassesReborn.DangerouslyCuriousTrait", initiativeIcon,
            StatType.SkillUseMagicDevice);
        var focusedMind = FeatureConfigurator.New("ClassesRebornFocusedMindTrait", BlueprintIds.FocusedMindTrait)
            .SetDisplayName("ClassesReborn.FocusedMindTrait.Name")
            .SetDescription("ClassesReborn.FocusedMindTrait.Description")
            .SetIcon(initiativeIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddConcentrationBonus(checkFact: false, value: ContextValues.Constant(2))
            .Configure();
        var skeptic = FeatureConfigurator.New("ClassesRebornSkepticTrait", BlueprintIds.SkepticTrait)
            .SetDisplayName("ClassesReborn.SkepticTrait.Name")
            .SetDescription("ClassesReborn.SkepticTrait.Description")
            .SetIcon(initiativeIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddSavingThrowBonusAgainstSchool(
                modifierDescriptor: ModifierDescriptor.Trait,
                school: SpellSchool.Illusion,
                value: 2)
            .Configure();
        var mathematicalProdigy = CreateSkillChoiceTrait(
            "MathematicalProdigy", BlueprintIds.MathematicalProdigyTrait,
            BlueprintIds.MathematicalProdigyBonuses, BlueprintIds.MathematicalProdigySelection,
            BlueprintIds.MathematicalProdigyArcana, BlueprintIds.MathematicalProdigyUseMagicDevice,
            "ClassesReborn.MathematicalProdigyTrait", initiativeIcon,
            StatType.SkillKnowledgeArcana, StatType.SkillUseMagicDevice);
        var bully = CreateSkillTrait("ClassesRebornBullyTrait", BlueprintIds.BullyTrait,
            "ClassesReborn.BullyTrait", dirtyFightingIcon, StatType.SkillPersuasion);
        var childOfTheStreets = CreateSkillTrait("ClassesRebornChildOfTheStreetsTrait",
            BlueprintIds.ChildOfTheStreetsTrait, "ClassesReborn.ChildOfTheStreetsTrait", initiativeIcon,
            StatType.SkillThievery);
        var fastTalker = CreateSkillTrait("ClassesRebornFastTalkerTrait", BlueprintIds.FastTalkerTrait,
            "ClassesReborn.FastTalkerTrait", initiativeIcon, StatType.SkillPersuasion);

        var fatesFavored = CreateComponentTrait(
            "ClassesRebornFatesFavoredTrait",
            FutureContentIds.Get("Trait.FatesFavored"),
            "ClassesReborn.FatesFavoredTrait",
            faithIcon,
            new FatesFavoredMarker());
        var pragmaticActivator = FeatureConfigurator.New(
                "ClassesRebornPragmaticActivatorTrait",
                FutureContentIds.Get("Trait.PragmaticActivator"))
            .SetDisplayName("ClassesReborn.PragmaticActivatorTrait.Name")
            .SetDescription("ClassesReborn.PragmaticActivatorTrait.Description")
            .SetIcon(magicIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddReplaceStatBaseAttribute(
                baseAttributeReplacement: StatType.Intelligence,
                replaceIfHigher: false,
                targetStat: StatType.SkillUseMagicDevice)
            .Configure();
        var bruisingIntellect = CreateComponentTrait(
            "ClassesRebornBruisingIntellectTrait",
            FutureContentIds.Get("Trait.BruisingIntellect"),
            "ClassesReborn.BruisingIntellectTrait",
            dirtyFightingIcon,
            new BruisingIntellectComponent());
        var twoWorldMagic = CreateTwoWorldMagicTrait(magicIcon);
        var giftedAdeptSpells = GetGiftedAdeptSpellVariants();
        var giftedAdept = ParametrizedFeatureConfigurator.New(
                "ClassesRebornGiftedAdeptTrait",
                FutureContentIds.Get("Trait.GiftedAdept"))
            .SetDisplayName("ClassesReborn.GiftedAdeptTrait.Name")
            .SetDescription("ClassesReborn.GiftedAdeptTrait.Description")
            .SetIcon(magicIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetHideNotAvailibleInUI(true)
            .SetParameterType(FeatureParameterType.Custom)
            .SetBlueprintParameterVariants(giftedAdeptSpells
                .Select(spell => (Blueprint<AnyBlueprintReference>)spell)
                .ToArray())
            .AddComponent(new GiftedAdeptSpellPrerequisite())
            .AddComponent(new GiftedAdeptCasterLevel())
            .Configure();

        var racialChoices = RacialTraitRebalance.Configure(
            weaponFocusIcon,
            armorIcon,
            initiativeIcon,
            faithIcon,
            unarmedIcon,
            dirtyFightingIcon,
            magicIcon);

        var adoptedSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornAdoptedRacialTraitSelection",
                BlueprintIds.AdoptedRacialTraitSelection)
            .SetDisplayName("ClassesReborn.AdoptedTrait.Choice.Name")
            .SetDescription("ClassesReborn.AdoptedTrait.Description")
            .SetIcon(faithIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        adoptedSelection.m_AllFeatures = racialChoices
            .Select(choice => choice.ToReference<BlueprintFeatureReference>())
            .ToArray();
        adoptedSelection.m_Features = adoptedSelection.m_AllFeatures.ToArray();

        var adopted = ProgressionConfigurator.New(
                "ClassesRebornAdoptedTrait",
                BlueprintIds.AdoptedTrait)
            .SetDisplayName("ClassesReborn.AdoptedTrait.Name")
            .SetDescription("ClassesReborn.AdoptedTrait.Description")
            .SetIcon(faithIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(false)
            .Configure();
        adopted.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    adoptedSelection.ToReference<BlueprintFeatureBaseReference>(),
                },
            },
        };

        var categories = new[] {
            new TraitCategoryDefinition(
                "Combat",
                "ClassesReborn.TraitCategory.Combat",
                weaponFocusIcon,
                anatomist, armorExpert, reactionary, resilient, bullied, courageous,
                deftDodger, dirtyFighter, fencer, killer, sharpNails, shieldFighter),
            new TraitCategoryDefinition(
                "Faith",
                "ClassesReborn.TraitCategory.Faith",
                faithIcon,
                birthmark, devotee, easeOfFaith, historyOfHeresy, indomitableFaith,
                sacredConduit, scholar, fatesFavored),
            new TraitCategoryDefinition(
                "Magic",
                "ClassesReborn.TraitCategory.Magic",
                magicIcon,
                dangerouslyCurious, focusedMind, skeptic, mathematicalProdigy,
                pragmaticActivator, twoWorldMagic, giftedAdept),
            new TraitCategoryDefinition(
                "Social",
                "ClassesReborn.TraitCategory.Social",
                initiativeIcon,
                bully, childOfTheStreets, fastTalker, bruisingIntellect, adopted),
            new TraitCategoryDefinition(
                "Racial",
                "ClassesReborn.TraitCategory.Racial",
                racialChoices.First().Icon,
                racialChoices),
        };
        var choices = categories.SelectMany(category => category.Choices).ToArray();
        // Each selection needs its own no-op feature. Reusing one blueprint would make
        // "None" unavailable in later slots after it had already been selected once.
        var noneChoices = Enumerable.Range(1, 4)
            .Select(slot => FeatureConfigurator.New(
                    $"ClassesRebornNoneTraitSelection{slot}",
                    FutureContentIds.Get($"Trait.None.Selection{slot}"))
                .SetDisplayName("ClassesReborn.NoneTrait.Name")
                .SetDescription("ClassesReborn.NoneTrait.Description")
                .SetIcon(reactionary.Icon)
                .SetGroups(FeatureGroup.Trait)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .Configure())
            .ToArray();
        var categorySelections = Enumerable.Range(1, 4)
            .Select(slot => categories
                .Select(category => CreateCategorySelection(slot, category))
                .ToArray())
            .ToArray();
        var selectionOne = CreateSelection("ClassesRebornTraitSelectionOne", BlueprintIds.TraitSelectionOne,
            categorySelections[0].Cast<BlueprintFeature>().Append(noneChoices[0]).ToArray(), reactionary.Icon);
        var selectionTwo = CreateSelection("ClassesRebornTraitSelectionTwo", BlueprintIds.TraitSelectionTwo,
            categorySelections[1].Cast<BlueprintFeature>().Append(noneChoices[1]).ToArray(), reactionary.Icon);
        BlueprintFeatureSelection selectionThree = null;
        BlueprintFeatureSelection selectionFour = null;
        BlueprintProgression additionalTraits = null;
        if (Main.Settings.AdditionalTraits) {
            selectionThree = CreateSelection("ClassesRebornTraitSelectionThree", BlueprintIds.TraitSelectionThree,
                categorySelections[2].Cast<BlueprintFeature>().Append(noneChoices[2]).ToArray(), reactionary.Icon);
            selectionFour = CreateSelection("ClassesRebornTraitSelectionFour", BlueprintIds.TraitSelectionFour,
                categorySelections[3].Cast<BlueprintFeature>().Append(noneChoices[3]).ToArray(), reactionary.Icon);
            additionalTraits = ProgressionConfigurator.New("ClassesRebornAdditionalTraits", BlueprintIds.AdditionalTraitsFeat)
                .SetDisplayName("ClassesReborn.AdditionalTraits.Name")
                .SetDescription("ClassesReborn.AdditionalTraits.Description")
                .SetIcon(reactionary.Icon)
                .SetGroups(FeatureGroup.Feat)
                .SetGiveFeaturesForPreviousLevels(true)
                .SetReapplyOnLevelUp(true)
                .SetIsClassFeature(true)
                .Configure();
            additionalTraits.LevelEntries = new[] {
                new LevelEntry {
                    Level = 1,
                    m_Features = new List<BlueprintFeatureBaseReference> {
                        selectionThree.ToReference<BlueprintFeatureBaseReference>(),
                        selectionFour.ToReference<BlueprintFeatureBaseReference>(),
                    },
                },
            };
            AddToSelection(BlueprintIds.BasicFeatSelection, additionalTraits);
        }

        Validate(selectionOne, selectionTwo, selectionThree, selectionFour, additionalTraits, choices,
            anatomist, armorExpert, reactionary, resilient, sharpNails, shieldFighter,
            adopted, adoptedSelection, racialChoices,
            noneChoices, categorySelections, categories, giftedAdept,
            devotee, scholar, mathematicalProdigy);
    }

    internal static void RefreshGiftedAdeptSpellVariants() {
        var giftedAdept = ResourcesLibrary.TryGetBlueprint<BlueprintParametrizedFeature>(
            FutureContentIds.Get("Trait.GiftedAdept"));
        if (giftedAdept == null) {
            return;
        }

        var spells = GetGiftedAdeptSpellVariants();
        giftedAdept.BlueprintParameterVariants = spells
            .Select(spell => spell.ToReference<AnyBlueprintReference>())
            .ToArray();
        giftedAdept.m_CachedItems = null;
        ValidateGiftedAdept(giftedAdept);
    }

    private static BlueprintAbility[] GetGiftedAdeptSpellVariants() {
        var spellbooks = new HashSet<BlueprintSpellbook>();
        foreach (var characterClass in
                 BlueprintRoot.Instance.Progression.CharacterClasses) {
            if (characterClass?.Spellbook != null) {
                spellbooks.Add(characterClass.Spellbook);
            }

            foreach (var archetype in characterClass?.Archetypes ??
                     Enumerable.Empty<BlueprintArchetype>()) {
                if (archetype?.ReplaceSpellbook != null) {
                    spellbooks.Add(archetype.ReplaceSpellbook);
                }
            }
        }

        return spellbooks
            .Select(spellbook => spellbook.SpellList)
            .Where(spellList => spellList != null)
            .SelectMany(spellList => spellList.SpellsByLevel)
            .Where(level => level.SpellLevel >= 1)
            .SelectMany(level => level.Spells)
            .Where(spell => spell != null)
            .Distinct()
            .OrderBy(spell => spell.AssetGuid.ToString())
            .ToArray();
    }

    private static void ValidateGiftedAdept(
        BlueprintParametrizedFeature giftedAdept) {
        var warpriestLevelOne = BlueprintTool.Get<BlueprintSpellList>(
                BlueprintIds.WarpriestSpellList)
            .SpellsByLevel
            .Single(level => level.SpellLevel == 1)
            .Spells
            .Select(spell => spell.AssetGuid)
            .ToHashSet();
        var variants = giftedAdept.BlueprintParameterVariants
            .Select(reference => reference?.Get())
            .OfType<BlueprintAbility>()
            .Select(spell => spell.AssetGuid)
            .ToHashSet();

        if (giftedAdept.ParameterType != FeatureParameterType.Custom ||
            giftedAdept.GetComponents<GiftedAdeptSpellPrerequisite>().Count() != 1 ||
            variants.Count == 0 ||
            !warpriestLevelOne.IsSubsetOf(variants)) {
            throw new InvalidOperationException(
                "Gifted Adept must offer castable spell parameters, including every level-1 Warpriest spell.");
        }
    }

    private static BlueprintFeature CreateStatTrait(
        string name, string id, string localizationPrefix, Sprite icon,
        params (StatType Stat, int Value)[] bonuses) {
        var configurator = FeatureConfigurator.New(name, id)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false);
        foreach (var bonus in bonuses) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: bonus.Stat,
                value: bonus.Value);
        }
        return configurator.Configure();
    }

    private static BlueprintFeature CreateSkillTrait(
        string name, string id, string localizationPrefix, Sprite icon, StatType skill) =>
        FeatureConfigurator.New(name, id)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(descriptor: ModifierDescriptor.Trait, stat: skill, value: 1)
            .AddClassSkill(skill)
            .Configure();

    private static BlueprintFeature CreateComponentTrait(
        string name, string id, string localizationPrefix, Sprite icon, BlueprintComponent component) =>
        FeatureConfigurator.New(name, id)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddComponent(component)
            .Configure();

    private static BlueprintFeatureSelection CreateTwoWorldMagicTrait(Sprite icon) {
        var classes = new[] {
            new SpellcastingTraitClass("Bard", CharacterClassRefs.BardClass.Reference.Get(), SpellListRefs.BardSpellList.Reference.Get()),
            new SpellcastingTraitClass("Cleric", CharacterClassRefs.ClericClass.Reference.Get(), SpellListRefs.ClericSpellList.Reference.Get()),
            new SpellcastingTraitClass("Druid", CharacterClassRefs.DruidClass.Reference.Get(), SpellListRefs.DruidSpellList.Reference.Get()),
            new SpellcastingTraitClass("Magus", CharacterClassRefs.MagusClass.Reference.Get(), SpellListRefs.MagusSpellList.Reference.Get()),
            new SpellcastingTraitClass("Shaman", CharacterClassRefs.ShamanClass.Reference.Get(), SpellListRefs.ShamanSpelllist.Reference.Get()),
            new SpellcastingTraitClass("Sorcerer", CharacterClassRefs.SorcererClass.Reference.Get(), SpellListRefs.WizardSpellList.Reference.Get()),
            new SpellcastingTraitClass("Wizard", CharacterClassRefs.WizardClass.Reference.Get(), SpellListRefs.WizardSpellList.Reference.Get()),
            new SpellcastingTraitClass("Arcanist", CharacterClassRefs.ArcanistClass.Reference.Get(), SpellListRefs.WizardSpellList.Reference.Get()),
            new SpellcastingTraitClass("Witch", CharacterClassRefs.WitchClass.Reference.Get(), SpellListRefs.WitchSpellList.Reference.Get()),
            new SpellcastingTraitClass("Oracle", CharacterClassRefs.OracleClass.Reference.Get(), SpellListRefs.ClericSpellList.Reference.Get()),
            new SpellcastingTraitClass(
                "Warpriest",
                BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.WarpriestClass),
                BlueprintTool.Get<BlueprintSpellList>(BlueprintIds.WarpriestSpellList)),
        };

        var allCantrips = classes
            .SelectMany(entry => entry.SpellList.SpellsByLevel
                .Where(level => level.SpellLevel == 0)
                .SelectMany(level => level.Spells))
            .Distinct()
            .ToArray();
        var options = new List<BlueprintParametrizedFeature>();
        foreach (var entry in classes) {
            var nativeCantrips = entry.SpellList.SpellsByLevel
                .Where(level => level.SpellLevel == 0)
                .SelectMany(level => level.Spells)
                .ToHashSet();
            var foreignCantrips = allCantrips
                .Where(spell => !nativeCantrips.Contains(spell))
                .ToArray();
            if (foreignCantrips.Length == 0) {
                continue;
            }

            var foreignListId = FutureContentIds.Get($"Trait.TwoWorldMagic.{entry.Name}.SpellList");
            var foreignList = SpellListConfigurator.New(
                    $"ClassesRebornTwoWorldMagic{entry.Name}SpellList",
                    foreignListId)
                .Configure();
            foreignList.SpellsByLevel = new[] {
                new SpellLevelList(0) {
                    m_Spells = foreignCantrips
                        .Select(spell => spell.ToReference<BlueprintAbilityReference>())
                        .ToList(),
                },
            };

            var classId = entry.CharacterClass.AssetGuid.ToString();
            options.Add(ParametrizedFeatureConfigurator.New(
                    $"ClassesRebornTwoWorldMagic{entry.Name}",
                    FutureContentIds.Get($"Trait.TwoWorldMagic.{entry.Name}"))
                .SetDisplayName($"ClassesReborn.TwoWorldMagicTrait.{entry.Name}.Name")
                .SetDescription("ClassesReborn.TwoWorldMagicTrait.Description")
                .SetIcon(icon)
                .SetGroups(FeatureGroup.Trait)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .SetParameterType(FeatureParameterType.LearnSpell)
                .SetSpellList(foreignListId)
                .SetSpellcasterClass(classId)
                .SetSpecificSpellLevel(true)
                .SetSpellLevel(0)
                .SetBlueprintParameterVariants(foreignCantrips
                    .Select(spell => (Blueprint<AnyBlueprintReference>)spell)
                    .ToArray())
                .AddPrerequisiteClassLevel(classId, 1)
                .AddLearnSpellParametrized(
                    specificSpellLevel: true,
                    spellcasterClass: classId,
                    spellLevel: 0,
                    spellList: foreignListId)
                .Configure());
        }

        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornTwoWorldMagicTrait",
                FutureContentIds.Get("Trait.TwoWorldMagic"))
            .SetDisplayName("ClassesReborn.TwoWorldMagicTrait.Name")
            .SetDescription("ClassesReborn.TwoWorldMagicTrait.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        selection.m_AllFeatures = options
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        return selection;
    }

    private sealed class SpellcastingTraitClass {
        internal readonly string Name;
        internal readonly BlueprintCharacterClass CharacterClass;
        internal readonly BlueprintSpellList SpellList;

        internal SpellcastingTraitClass(
            string name,
            BlueprintCharacterClass characterClass,
            BlueprintSpellList spellList) {
            Name = name;
            CharacterClass = characterClass;
            SpellList = spellList;
        }
    }

    private static BlueprintProgression CreateSkillChoiceTrait(
        string name, string parentId, string bonusesId, string selectionId,
        string optionOneId, string optionTwoId, string localizationPrefix, Sprite icon,
        StatType firstSkill, StatType secondSkill) {
        var bonuses = CreateStatTrait($"ClassesReborn{name}Bonuses", bonusesId, localizationPrefix, icon,
            (firstSkill, 1), (secondSkill, 1));
        bonuses.HideInUI = true;
        bonuses.HideInCharacterSheetAndLevelUp = true;

        var firstOption = FeatureConfigurator.New($"ClassesReborn{name}FirstClassSkill", optionOneId)
            .SetDisplayName($"{localizationPrefix}.FirstChoice.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddClassSkill(firstSkill)
            .Configure();
        var secondOption = FeatureConfigurator.New($"ClassesReborn{name}SecondClassSkill", optionTwoId)
            .SetDisplayName($"{localizationPrefix}.SecondChoice.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddClassSkill(secondSkill)
            .Configure();
        var selection = FeatureSelectionConfigurator.New($"ClassesReborn{name}ClassSkillSelection", selectionId)
            .SetDisplayName($"{localizationPrefix}.Choice.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(firstOption, secondOption)
            .Configure();
        var progression = ProgressionConfigurator.New($"ClassesReborn{name}Trait", parentId)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(false)
            .Configure();
        progression.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    bonuses.ToReference<BlueprintFeatureBaseReference>(),
                    selection.ToReference<BlueprintFeatureBaseReference>(),
                },
            },
        };
        return progression;
    }

    private static BlueprintFeatureSelection CreateSelection(
        string name, string id, BlueprintFeature[] choices, Sprite icon) {
        var selection = FeatureSelectionConfigurator.New(name, id)
            .SetDisplayName("ClassesReborn.TraitSelection.Name")
            .SetDescription("ClassesReborn.TraitSelection.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        selection.m_AllFeatures = choices
            .Select(choice => choice.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        return selection;
    }

    private static BlueprintFeatureSelection CreateCategorySelection(
        int slot, TraitCategoryDefinition category) {
        var selection = FeatureSelectionConfigurator.New(
                $"ClassesReborn{category.Key}TraitCategory{slot}",
                FutureContentIds.Get($"Trait.Category.{category.Key}.Selection{slot}"))
            .SetDisplayName($"{category.LocalizationPrefix}.Name")
            .SetDescription($"{category.LocalizationPrefix}.Description")
            .SetIcon(category.Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        selection.m_AllFeatures = category.Choices
            .Select(choice => choice.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        return selection;
    }

    private static void AddToSelection(string selectionId, BlueprintFeature feature) {
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
        selection.m_AllFeatures = AppendDistinct(selection.m_AllFeatures, feature);
        if (selection.m_Features?.Length > 0) {
            selection.m_Features = AppendDistinct(selection.m_Features, feature);
        }
    }

    private static BlueprintFeatureReference[] AppendDistinct(
        BlueprintFeatureReference[] references, BlueprintFeature feature) {
        references ??= Array.Empty<BlueprintFeatureReference>();
        return references.Any(reference => reference?.Get() == feature)
            ? references
            : references.Append(feature.ToReference<BlueprintFeatureReference>()).ToArray();
    }

    private static void Validate(
        BlueprintFeatureSelection selectionOne, BlueprintFeatureSelection selectionTwo,
        BlueprintFeatureSelection selectionThree, BlueprintFeatureSelection selectionFour,
        BlueprintProgression additionalTraits, BlueprintFeature[] choices,
        BlueprintFeature anatomist, BlueprintFeature armorExpert,
        BlueprintFeature reactionary, BlueprintFeature resilient,
        BlueprintFeature sharpNails, BlueprintFeature shieldFighter,
        BlueprintProgression adopted, BlueprintFeatureSelection adoptedSelection,
        BlueprintFeature[] racialChoices,
        BlueprintFeature[] noneChoices,
        BlueprintFeatureSelection[][] categorySelections,
        TraitCategoryDefinition[] categories,
        BlueprintParametrizedFeature giftedAdept,
        params BlueprintProgression[] choiceTraits) {
        var expectedOne = categorySelections[0].Cast<BlueprintFeature>().Append(noneChoices[0])
            .Select(feature => feature.AssetGuid).ToHashSet();
        var expectedTwo = categorySelections[1].Cast<BlueprintFeature>().Append(noneChoices[1])
            .Select(feature => feature.AssetGuid).ToHashSet();
        var expectedThree = categorySelections[2].Cast<BlueprintFeature>().Append(noneChoices[2])
            .Select(feature => feature.AssetGuid).ToHashSet();
        var expectedFour = categorySelections[3].Cast<BlueprintFeature>().Append(noneChoices[3])
            .Select(feature => feature.AssetGuid).ToHashSet();
        var first = selectionOne.m_AllFeatures.Select(reference => reference.deserializedGuid).ToHashSet();
        var second = selectionTwo.m_AllFeatures.Select(reference => reference.deserializedGuid).ToHashSet();
        var reactionaryBonus = reactionary.GetComponents<AddStatBonus>()
            .SingleOrDefault(component => component.Stat == StatType.Initiative);
        var resilientBonus = resilient.GetComponents<AddStatBonus>()
            .SingleOrDefault(component => component.Stat == StatType.SaveFortitude);
        var armorPenalty = armorExpert.GetComponents<ArmorCheckPenaltyIncrease>().SingleOrDefault();
        var adoptedValid = adopted.LevelEntries.Length == 1 &&
            adopted.LevelEntries[0].Level == 1 &&
            adopted.LevelEntries[0].Features.Count == 1 &&
            adopted.LevelEntries[0].Features.SingleOrDefault() == adoptedSelection &&
            adoptedSelection.Obligatory &&
            adoptedSelection.m_AllFeatures.Length == racialChoices.Length &&
            racialChoices.Select(feature => feature.AssetGuid).ToHashSet().SetEquals(
                adoptedSelection.m_AllFeatures.Select(reference => reference.deserializedGuid)) &&
            racialChoices.All(feature => feature.GetComponents<RaceTraitPrerequisite>()
                .SingleOrDefault()?.m_AdoptedSelection?.deserializedGuid == adoptedSelection.AssetGuid);
        var skillChoicesValid = choiceTraits.All(progression =>
            progression.LevelEntries.Length == 1 &&
            progression.LevelEntries[0].Level == 1 &&
            progression.LevelEntries[0].Features.Count == 2 &&
            progression.LevelEntries[0].Features.OfType<BlueprintFeatureSelection>().SingleOrDefault()
            is { Obligatory: true } nested &&
            nested.m_AllFeatures.Length == 2);
        var categoriesValid = categorySelections.Length == 4 &&
            categorySelections.All(slot => slot.Length == categories.Length) &&
            categorySelections.SelectMany(slot => slot)
                .Select(selection => selection.AssetGuid).Distinct().Count() == 4 * categories.Length &&
            categorySelections.SelectMany(slot => slot)
                .All(selection => selection.Obligatory &&
                    selection.Ranks == 1 &&
                    !(selection.Groups?.Any() ?? false)) &&
            categorySelections.SelectMany(slot => slot.Select((selection, index) => (selection, index)))
                .All(entry => categories[entry.index].Choices.Select(choice => choice.AssetGuid).ToHashSet()
                    .SetEquals(entry.selection.m_AllFeatures.Select(reference => reference.deserializedGuid))) &&
            categories.SelectMany(category => category.Choices)
                .Select(choice => choice.AssetGuid).ToHashSet()
                .SetEquals(choices.Select(choice => choice.AssetGuid));
        var additionalTraitsValid = !Main.Settings.AdditionalTraits ||
            selectionThree != null && selectionFour != null && additionalTraits != null &&
            expectedThree.SetEquals(selectionThree.m_AllFeatures.Select(reference => reference.deserializedGuid)) &&
            expectedFour.SetEquals(selectionFour.m_AllFeatures.Select(reference => reference.deserializedGuid)) &&
            selectionThree.m_AllFeatures.Length == categories.Length + 1 &&
            selectionFour.m_AllFeatures.Length == categories.Length + 1 &&
            selectionThree.Obligatory && selectionFour.Obligatory &&
            additionalTraits.LevelEntries.Length == 1 &&
            additionalTraits.LevelEntries[0].Level == 1 &&
            additionalTraits.LevelEntries[0].Features.Count == 2 &&
            additionalTraits.LevelEntries[0].Features.Contains(selectionThree) &&
            additionalTraits.LevelEntries[0].Features.Contains(selectionFour) &&
            BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.BasicFeatSelection)
                .m_AllFeatures.Count(reference => reference?.Get() == additionalTraits) == 1;

        if (noneChoices.Length != 4 || noneChoices.Select(feature => feature.AssetGuid).Distinct().Count() != 4 ||
            !expectedOne.SetEquals(first) || !expectedTwo.SetEquals(second) ||
            selectionOne.m_AllFeatures.Length != categories.Length + 1 ||
            selectionTwo.m_AllFeatures.Length != categories.Length + 1 ||
            !selectionOne.Obligatory || !selectionTwo.Obligatory ||
            choices.Concat(noneChoices).Any(feature => feature.Ranks != 1) ||
            anatomist.GetComponents<CriticalConfirmationBonus>().Count() != 1 ||
            sharpNails.GetComponents<SharpNailsCriticalMultiplier>().Count() != 1 ||
            shieldFighter.GetComponents<ShieldFighterTraitComponent>().Count() != 1 ||
            armorPenalty?.BonesPerRank != 1 ||
            reactionaryBonus?.Value != 2 || reactionaryBonus.Descriptor != ModifierDescriptor.Trait ||
            resilientBonus?.Value != 1 || resilientBonus.Descriptor != ModifierDescriptor.Trait ||
            !adoptedValid || !skillChoicesValid || !categoriesValid || !additionalTraitsValid) {
            throw new InvalidOperationException(
                $"Character creation and Additional Traits must grant obligatory, non-duplicating thematic category selections containing all {choices.Length} configured traits, an independent None option, and their requested effects.");
        }


        ValidateGiftedAdept(giftedAdept);
    }

    private sealed class TraitCategoryDefinition {
        internal readonly string Key;
        internal readonly string LocalizationPrefix;
        internal readonly Sprite Icon;
        internal readonly BlueprintFeature[] Choices;

        internal TraitCategoryDefinition(
            string key, string localizationPrefix, Sprite icon,
            params BlueprintFeature[] choices) {
            Key = key;
            LocalizationPrefix = localizationPrefix;
            Icon = icon;
            Choices = choices;
        }
    }
}

[HarmonyPatch(
    typeof(ApplyClassMechanics),
    nameof(ApplyClassMechanics.Apply),
    new[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
internal static class CharacterCreationTraitSelectionPatch {
    private static bool MissingSelectionLogged;

    [HarmonyPostfix]
    private static void Postfix(LevelUpState state, UnitDescriptor unit) {
        TryAddSelections(state);
    }

    internal static void TryAddSelections(LevelUpState state) {
        // ApplyClassMechanics and SelectRace are also used by AddClassLevels
        // while the game initializes NPC blueprints.  Only the controller
        // owned by the active character-creation UI may receive trait pages.
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !ReferenceEquals(Kingmaker.Game.Instance?.LevelUpController?.State, state) ||
            !Main.Settings.CharacterTraits || state == null || !state.IsFirstCharacterLevel ||
            state.IsPregen || state.SelectedRace == null) {
            return;
        }

        AddSelection(state, BlueprintIds.TraitSelectionOne);
        AddSelection(state, BlueprintIds.TraitSelectionTwo);
    }

    private static void AddSelection(LevelUpState state, string selectionId) {
        var selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(selectionId);
        if (selection == null) {
            if (!MissingSelectionLogged) {
                MissingSelectionLogged = true;
                Main.Log.Error(
                    "Character Trait selections were not created; skipping them to preserve character creation.");
            }
            return;
        }

        if (state.Selections.Any(existing => existing.Selection == selection)) {
            return;
        }

        state.AddSelection(null, new FeatureSource(state.SelectedRace), selection, 1);
    }
}

[HarmonyPatch(typeof(AddClassLevels), nameof(AddClassLevels.OnActivate))]
internal static class AddClassLevelsTraitSelectionGuardPatch {
    [ThreadStatic]
    private static int Depth;

    internal static bool IsActive => Depth > 0;

    [HarmonyPrefix]
    private static void Prefix() => Depth++;

    [HarmonyFinalizer]
    private static Exception Finalizer(Exception __exception) {
        if (Depth > 0) {
            Depth--;
        }

        return __exception;
    }
}

[HarmonyPatch(
    typeof(SelectRace),
    nameof(SelectRace.Apply),
    new[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
internal static class CharacterCreationRaceTraitSelectionPatch {
    [HarmonyPostfix]
    private static void Postfix(LevelUpState state, UnitDescriptor unit) =>
        CharacterCreationTraitSelectionPatch.TryAddSelections(state);
}
