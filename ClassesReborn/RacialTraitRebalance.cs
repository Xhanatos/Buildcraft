using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;

namespace ClassesReborn;

internal static class RacialTraitRebalance {
    internal static BlueprintFeature[] Configure(
        Sprite weaponIcon,
        Sprite armorIcon,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite unarmedIcon,
        Sprite combatIcon,
        Sprite magicIcon) {
        var traits = new List<BlueprintFeature>();

        ConfigureHumanTraits(
            traits,
            weaponIcon,
            initiativeIcon,
            faithIcon,
            combatIcon,
            magicIcon);
        ConfigureElfTraits(traits, initiativeIcon, faithIcon);
        ConfigureDwarfTraits(traits, weaponIcon, initiativeIcon, faithIcon, combatIcon);
        ConfigureGnomeTraits(traits, initiativeIcon, faithIcon, magicIcon);
        ConfigureHalflingTraits(traits, initiativeIcon, combatIcon);
        ConfigureHalfElfTraits(traits, initiativeIcon, faithIcon);
        ConfigureHalfOrcTraits(traits, weaponIcon, unarmedIcon, combatIcon);
        ConfigureAasimarTraits(traits, weaponIcon, faithIcon, magicIcon);
        ConfigureTieflingTraits(traits, armorIcon, weaponIcon, initiativeIcon, faithIcon, magicIcon);
        ConfigureOreadTraits(traits, faithIcon, magicIcon);
        ConfigureDhampirTraits(traits, weaponIcon, faithIcon, magicIcon);
        ConfigureKitsuneTraits(traits, initiativeIcon, faithIcon, magicIcon);
        ConfigureGoblinTraits(traits, weaponIcon, initiativeIcon, combatIcon);
        ConfigureMongrelTraits(
            traits,
            weaponIcon,
            initiativeIcon,
            faithIcon,
            combatIcon);

        var campaignTraits = traits
            .Where(trait => !AlternateRacialHeritageRebalance.IsAlternateHeritage(trait.AssetGuid.ToString()))
            .ToArray();
        Validate(campaignTraits);
        return campaignTraits;
    }

    private static void ConfigureGoblinTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite initiativeIcon,
        Sprite combatIcon) {
        var stealthIcon = FeatureRefs.SkillFocusStealth.Reference.Get().Icon;
        var sickenedIcon = FeatureRefs.StunningFistSickenedFeature.Reference.Get().Icon;
        var dodgeIcon = FeatureRefs.Dodge.Reference.Get().Icon;

        traits.Add(BaseFeature(
                "ClassesRebornColorThiefTrait",
                FutureContentIds.Get("Trait.Goblin.ColorThief"),
                "ClassesReborn.ColorThiefTrait",
                stealthIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddComponent(new ColorThiefStealthBonus { Bonus = 2 })
            .Configure());

        var foulBelchBuff = BuffConfigurator.New(
                "ClassesRebornFoulBelchSickenedBuff",
                FutureContentIds.Get("Trait.Goblin.FoulBelch.Buff"))
            .SetDisplayName("ClassesReborn.FoulBelchTrait.Name")
            .SetDescription("ClassesReborn.FoulBelchTrait.Description")
            .SetIcon(sickenedIcon)
            .SetStacking(StackingType.Replace)
            .AddCondition(UnitCondition.Sickened)
            .AddSpellDescriptorComponent(SpellDescriptor.Sickened)
            .Configure();
        var foulBelchResource = AbilityResourceConfigurator.New(
                "ClassesRebornFoulBelchResource",
                FutureContentIds.Get("Trait.Goblin.FoulBelch.Resource"))
            .SetLocalizedName("ClassesReborn.FoulBelchTrait.Name")
            .SetLocalizedDescription("ClassesReborn.FoulBelchTrait.Description")
            .SetIcon(sickenedIcon)
            .SetMax(1)
            .Configure();
        var foulBelchAbility = AbilityConfigurator.New(
                "ClassesRebornFoulBelchAbility",
                FutureContentIds.Get("Trait.Goblin.FoulBelch.Ability"))
            .SetDisplayName("ClassesReborn.FoulBelchTrait.Name")
            .SetDescription("ClassesReborn.FoulBelchTrait.Description")
            .SetIcon(sickenedIcon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Swift)
            .SetSpellResistance(false)
            .SetLocalizedDuration("ClassesReborn.FoulBelchTrait.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.FoulBelchTrait.SavingThrow")
            .AllowTargeting(point: false, enemies: true, friends: false, self: false)
            .AddAbilityResourceLogic(
                amount: 1,
                isSpendResource: true,
                requiredResource: foulBelchResource)
            .AddAbilityEffectRunAction(
                ActionsBuilder.New().Add<ContextActionFoulBelch>(action => {
                    action.m_SickenedBuff =
                        foulBelchBuff.ToReference<BlueprintBuffReference>();
                }))
            .Configure();
        traits.Add(BaseFeature(
                "ClassesRebornFoulBelchTrait",
                FutureContentIds.Get("Trait.Goblin.FoulBelch"),
                "ClassesReborn.FoulBelchTrait",
                sickenedIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddFacts(new() { foulBelchAbility })
            .AddAbilityResources(
                amount: 0,
                resource: foulBelchResource,
                restoreAmount: true)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornGoblinFoolhardinessTrait",
                FutureContentIds.Get("Trait.Goblin.Foolhardiness"),
                "ClassesReborn.GoblinFoolhardinessTrait",
                weaponIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddComponent(new GoblinFoolhardinessAttackBonus { Bonus = 1 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornBouncyTrait",
                FutureContentIds.Get("Trait.Goblin.Bouncy"),
                "ClassesReborn.BouncyTrait",
                initiativeIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SaveReflex,
                value: 1)
            .AddComponent(new BouncyTripDefense { Bonus = 2 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornUnderfootMenaceTrait",
                FutureContentIds.Get("Trait.Goblin.UnderfootMenace"),
                "ClassesReborn.UnderfootMenaceTrait",
                dodgeIcon ?? combatIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddComponent(new UnderfootMenaceArmorClass { Bonus = 2 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornVileChemistTrait",
                FutureContentIds.Get("Trait.Goblin.VileChemist"),
                "ClassesReborn.VileChemistTrait",
                BlueprintTool.Get<BlueprintFeature>(BlueprintIds.AcidBombsFeature)?.Icon ?? combatIcon,
                BlueprintIds.GoblinRace,
                "Goblin")
            .AddComponent(new VileChemistBombDamage())
            .Configure());
    }

    private static void ConfigureMongrelTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite combatIcon) {
        var mobilityIcon = FeatureRefs.Dodge.Reference.Get().Icon;

        traits.Add(BaseFeature(
                "ClassesRebornCrusadersDescendantTrait",
                FutureContentIds.Get("Trait.Mongrel.CrusadersDescendant"),
                "ClassesReborn.CrusadersDescendantTrait",
                weaponIcon,
                BlueprintIds.MongrelRace,
                "Mongrel")
            .AddComponent(new BackgroundEnemyBonus {
                m_EnemyType = FeatureRef(FeatureRefs.SubtypeDemon.ToString()),
                Bonus = 1,
                ApplyToAttack = false,
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornUnsettlingAppearanceTrait",
                FutureContentIds.Get("Trait.Mongrel.UnsettlingAppearance"),
                "ClassesReborn.UnsettlingAppearanceTrait",
                combatIcon,
                BlueprintIds.MongrelRace,
                "Mongrel")
            .AddComponent(new DemoralizeTraitBonus { Bonus = 2 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornTwistedBalanceTrait",
                FutureContentIds.Get("Trait.Mongrel.TwistedBalance"),
                "ClassesReborn.TwistedBalanceTrait",
                mobilityIcon,
                BlueprintIds.MongrelRace,
                "Mongrel")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillMobility,
                value: 1)
            .AddComponent(new BouncyTripDefense {
                Bonus = 1,
                BonusDescriptor = ModifierDescriptor.Trait,
            })
            .Configure());

        var manyBloodedSelection = ConfigureManyBloodedSelection(faithIcon);
        traits.Add(CreateChoiceProgression(
            "ManyBlooded",
            FutureContentIds.Get("Trait.Mongrel.ManyBlooded"),
            "ClassesReborn.ManyBloodedTrait",
            faithIcon,
            BlueprintIds.MongrelRace,
            "Mongrel",
            manyBloodedSelection));

        traits.Add(BaseFeature(
                "ClassesRebornHardenedMutationTrait",
                FutureContentIds.Get("Trait.Mongrel.HardenedMutation"),
                "ClassesReborn.HardenedMutationTrait",
                initiativeIcon,
                BlueprintIds.MongrelRace,
                "Mongrel")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Poison | SpellDescriptor.Disease,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());
    }

    private static BlueprintFeatureSelection ConfigureManyBloodedSelection(
        Sprite icon) {
        var races = new[] {
            ("Human", BlueprintIds.HumanRace),
            ("Elf", BlueprintIds.ElfRace),
            ("Dwarf", BlueprintIds.DwarfRace),
            ("Gnome", BlueprintIds.GnomeRace),
            ("Halfling", BlueprintIds.HalflingRace),
            ("Half-Elf", BlueprintIds.HalfElfRace),
            ("Half-Orc", BlueprintIds.HalfOrcRace),
            ("Kitsune", RaceRefs.KitsuneRace.ToString()),
            ("Goblin", BlueprintIds.GoblinRace),
        };
        var options = races.Select(entry => {
            var race = BlueprintTool.Get<BlueprintRace>(entry.Item2);
            var configurator = FeatureConfigurator.New(
                    $"ClassesRebornManyBlooded{entry.Item1.Replace("-", string.Empty)}",
                    FutureContentIds.Get($"Trait.Mongrel.ManyBlooded.{entry.Item1}"))
                .SetDisplayName(
                    $"ClassesReborn.RacialHeritage.{entry.Item1.Replace("-", string.Empty)}.Name")
                .SetDescription("ClassesReborn.ManyBloodedTrait.Description")
                .SetIcon(icon)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .AddComponent(new RacialHeritageMarker {
                    m_Race = race.ToReference<BlueprintRaceReference>(),
                });
            foreach (var racialFact in (race.m_Features ??
                         Array.Empty<BlueprintFeatureBaseReference>())
                     .Select(reference => reference?.Get())
                     .OfType<BlueprintUnitFact>()
                     .Distinct()) {
                configurator.AddComponent(new FeatureForPrerequisite {
                    FakeFact = racialFact.ToReference<BlueprintUnitFactReference>(),
                });
            }
            var option = configurator.Configure();
            option.Groups = Array.Empty<FeatureGroup>();
            return option;
        }).ToArray();

        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornManyBloodedSelection",
                FutureContentIds.Get("Trait.Mongrel.ManyBlooded.Selection"))
            .SetDisplayName("ClassesReborn.ManyBloodedTrait.Choice.Name")
            .SetDescription("ClassesReborn.ManyBloodedTrait.Description")
            .SetIcon(icon)
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

    private static void ConfigureKitsuneTraits(
        ICollection<BlueprintFeature> traits,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite magicIcon) {
        var kitsuneRace = RaceRefs.KitsuneRace.Reference.Get();
        var kitsuneRaceId = kitsuneRace.AssetGuid.ToString();
        var perceptionIcon = FeatureRefs.SkillFocusPerception.Reference.Get().Icon;
        var stealthIcon = FeatureRefs.SkillFocusStealth.Reference.Get().Icon;

        traits.Add(BaseFeature(
                "ClassesRebornCleverPredatorTrait",
                FutureContentIds.Get("Trait.Kitsune.CleverPredator"),
                "ClassesReborn.CleverPredatorTrait",
                perceptionIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddReplaceStatBaseAttribute(
                baseAttributeReplacement: StatType.Intelligence,
                replaceIfHigher: false,
                targetStat: StatType.SkillPerception)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornTwoFacesOneMindTrait",
                FutureContentIds.Get("Trait.Kitsune.TwoFacesOneMind"),
                "ClassesReborn.TwoFacesOneMindTrait",
                faithIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Polymorph |
                    SpellDescriptor.Charm |
                    SpellDescriptor.Compulsion,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());

        traits.Add(CreateSkillChoiceTrait(
            "HiddenTail",
            FutureContentIds.Get("Trait.Kitsune.HiddenTail"),
            FutureContentIds.Get("Trait.Kitsune.HiddenTail.Bonuses"),
            FutureContentIds.Get("Trait.Kitsune.HiddenTail.Selection"),
            FutureContentIds.Get("Trait.Kitsune.HiddenTail.Stealth"),
            FutureContentIds.Get("Trait.Kitsune.HiddenTail.Persuasion"),
            "ClassesReborn.HiddenTailTrait",
            stealthIcon,
            kitsuneRaceId,
            "Kitsune",
            StatType.SkillStealth,
            StatType.SkillPersuasion));

        traits.Add(BaseFeature(
                "ClassesRebornVulpineAmbusherTrait",
                FutureContentIds.Get("Trait.Kitsune.VulpineAmbusher"),
                "ClassesReborn.VulpineAmbusherTrait",
                initiativeIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.Initiative,
                value: 2)
            .AddComponent(new VulpineAmbusherAttackBonus { Bonus = 1 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornKeenKitsuneTrait",
                FutureContentIds.Get("Trait.Kitsune.KeenKitsune"),
                "ClassesReborn.KeenKitsuneTrait",
                magicIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddPrerequisiteNoFeature(FeatureRefs.KitsuneHeritageKeen.ToString())
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.Intelligence,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.Charisma,
                value: -2)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornSkilledKitsuneTrait",
                FutureContentIds.Get("Trait.Kitsune.SkilledKitsune"),
                "ClassesReborn.SkilledKitsuneTrait",
                magicIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddSkillPointPerCharacterLevel()
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornFastShifterTrait",
                FutureContentIds.Get("Trait.Kitsune.FastShifter"),
                "ClassesReborn.FastShifterTrait",
                initiativeIcon,
                kitsuneRaceId,
                "Kitsune")
            .AddChangeActivatableAbilitiesCommandType(
                activatableAbilities: new() {
                    ActivatableAbilityRefs.ChangeShapeKitsuneToggleAbility.ToString(),
                },
                newCommandType: UnitCommand.CommandType.Move)
            .Configure());
    }

    private static void ConfigureHumanTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite combatIcon,
        Sprite magicIcon) {
        var bredForWar = BaseFeature(
                "ClassesRebornBredForWarTrait",
                BlueprintIds.BredForWarTrait,
                "ClassesReborn.BredForWarTrait",
                combatIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillPersuasion,
                value: 1)
            .AddCMBBonus(
                checkFact: false,
                descriptor: ModifierDescriptor.Trait,
                value: ContextValues.Constant(1))
            .Configure();
        traits.Add(bredForWar);

        traits.Add(BaseFeature(
                "ClassesRebornKellidSuperstitiousTrait",
                BlueprintIds.KellidSuperstitiousTrait,
                "ClassesReborn.KellidSuperstitiousTrait",
                initiativeIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Arcane,
                value: 1,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornUlfenWeaponTrainingTrait",
                BlueprintIds.UlfenWeaponTrainingTrait,
                "ClassesReborn.UlfenWeaponTrainingTrait",
                weaponIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddComponent(new RacialWeaponDamageBonus {
                Categories = new[] {
                    WeaponCategory.BastardSword,
                    WeaponCategory.Battleaxe,
                    WeaponCategory.Greataxe,
                    WeaponCategory.Greatsword,
                    WeaponCategory.Handaxe,
                    WeaponCategory.LightHammer,
                    WeaponCategory.Longbow,
                    WeaponCategory.Shortbow,
                    WeaponCategory.Longsword,
                    WeaponCategory.Shortsword,
                    WeaponCategory.ThrowingAxe,
                    WeaponCategory.Warhammer,
                },
            })
            .Configure());

        var eyeForTalentStats = new[] {
            (StatType.Strength, "Strength"),
            (StatType.Dexterity, "Dexterity"),
            (StatType.Constitution, "Constitution"),
            (StatType.Intelligence, "Intelligence"),
            (StatType.Wisdom, "Wisdom"),
            (StatType.Charisma, "Charisma"),
        };
        var eyeForTalentOptions = eyeForTalentStats.Select(entry => {
            var petFeature = FeatureConfigurator.New(
                    $"ClassesRebornEyeForTalentPet{entry.Item2}",
                    FutureContentIds.Get($"Trait.Human.EyeForTalent.Pet.{entry.Item2}"))
                .SetDisplayName($"ClassesReborn.EyeForTalentTrait.{entry.Item2}.Name")
                .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
                .SetIcon(combatIcon)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Racial,
                    stat: entry.Item1,
                    value: 2)
                .Configure();
            petFeature.HideInUI = true;
            petFeature.HideInCharacterSheetAndLevelUp = true;

            return FeatureConfigurator.New(
                    $"ClassesRebornEyeForTalent{entry.Item2}",
                    FutureContentIds.Get($"Trait.Human.EyeForTalent.Option.{entry.Item2}"))
                .SetDisplayName($"ClassesReborn.EyeForTalentTrait.{entry.Item2}.Name")
                .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
                .SetIcon(combatIcon)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .AddFeatureToPet(petFeature, PetType.AnimalCompanion)
                .Configure();
        }).ToArray();
        var eyeForTalentPetHealth = FeatureConfigurator.New(
                "ClassesRebornEyeForTalentPetHealth",
                FutureContentIds.Get("Trait.Human.EyeForTalent.Pet.Health"))
            .SetDisplayName("ClassesReborn.EyeForTalentTrait.Name")
            .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
            .SetIcon(combatIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.HitPoints,
                value: 4)
            .Configure();
        eyeForTalentPetHealth.HideInUI = true;
        eyeForTalentPetHealth.HideInCharacterSheetAndLevelUp = true;

        var eyeForTalentHealthGrant = FeatureConfigurator.New(
                "ClassesRebornEyeForTalentHealthGrant",
                FutureContentIds.Get("Trait.Human.EyeForTalent.HealthGrant"))
            .SetDisplayName("ClassesReborn.EyeForTalentTrait.Name")
            .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
            .SetIcon(combatIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddFeatureToPet(eyeForTalentPetHealth, PetType.AnimalCompanion)
            .Configure();
        eyeForTalentHealthGrant.HideInUI = true;
        eyeForTalentHealthGrant.HideInCharacterSheetAndLevelUp = true;

        var eyeForTalentSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornEyeForTalentSelection",
                FutureContentIds.Get("Trait.Human.EyeForTalent.Selection"))
            .SetDisplayName("ClassesReborn.EyeForTalentTrait.Choice.Name")
            .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
            .SetIcon(combatIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        eyeForTalentSelection.m_AllFeatures = eyeForTalentOptions
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        eyeForTalentSelection.m_Features = eyeForTalentSelection.m_AllFeatures.ToArray();
        var eyeForTalentSecondSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornEyeForTalentSecondSelection",
                FutureContentIds.Get("Trait.Human.EyeForTalent.Selection.Second"))
            .SetDisplayName("ClassesReborn.EyeForTalentTrait.Choice.Name")
            .SetDescription("ClassesReborn.EyeForTalentTrait.Description")
            .SetIcon(combatIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        eyeForTalentSecondSelection.m_AllFeatures = eyeForTalentOptions
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        eyeForTalentSecondSelection.m_Features =
            eyeForTalentSecondSelection.m_AllFeatures.ToArray();
        traits.Add(CreateChoiceProgression(
            "EyeForTalent",
            FutureContentIds.Get("Trait.Human.EyeForTalent"),
            "ClassesReborn.EyeForTalentTrait",
            combatIcon,
            BlueprintIds.HumanRace,
            "Human",
            eyeForTalentHealthGrant,
            eyeForTalentSelection,
            eyeForTalentSecondSelection));

        traits.Add(BaseFeature(
                "ClassesRebornHeartOfTheFeyTrait",
                FutureContentIds.Get("Trait.Human.HeartOfTheFey"),
                "ClassesReborn.HeartOfTheFeyTrait",
                initiativeIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillPerception,
                value: 1)
            .AddComponent(new SourceCreatureSaveBonus {
                m_SourceType = FeatureRef(FeatureRefs.FeyType.ToString()),
                Bonus = 2,
            })
            .Configure());

        var skillFocusSelection = FeatureSelectionRefs.SkillFocusSelection.Reference.Get();
        traits.Add(CreateChoiceProgression(
            "FocusedStudy",
            FutureContentIds.Get("Trait.Human.FocusedStudy"),
            "ClassesReborn.FocusedStudyHeritage",
            skillFocusSelection.Icon,
            BlueprintIds.HumanRace,
            "Human",
            skillFocusSelection));

        traits.Add(BaseFeature(
                "ClassesRebornAwarenessHeritage",
                FutureContentIds.Get("Trait.Human.Awareness"),
                "ClassesReborn.AwarenessHeritage",
                faithIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SaveFortitude,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SaveReflex,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SaveWill,
                value: 1)
            .AddConcentrationBonus(
                checkFact: false,
                value: ContextValues.Constant(1))
            .Configure());

        var militaryOptions = new[] {
            FeatureRefs.BastardSwordProficiency.Reference.Get(),
            FeatureRefs.BattleaxeProficiency.Reference.Get(),
            FeatureRefs.DoubleAxeProficiency.Reference.Get(),
            FeatureRefs.DoubleSwordProficiency.Reference.Get(),
            FeatureRefs.DuelingSwordProficiency.Reference.Get(),
            FeatureRefs.DwarvenWaraxeProficiency.Reference.Get(),
            FeatureRefs.ElvenCurvedBladeProficiency.Reference.Get(),
            FeatureRefs.EstocProficiency.Reference.Get(),
            FeatureRefs.FalcataProficiency.Reference.Get(),
            FeatureRefs.FalchionProficiency.Reference.Get(),
            FeatureRefs.FauchardProficiency.Reference.Get(),
            FeatureRefs.FlailProficiency.Reference.Get(),
            FeatureRefs.GlaiveProficiency.Reference.Get(),
            FeatureRefs.GreataxeProficiency.Reference.Get(),
            FeatureRefs.GreatclubProficiency.Reference.Get(),
            FeatureRefs.GreatswordProficiency.Reference.Get(),
            FeatureRefs.HandCrossbowProficiency.Reference.Get(),
            FeatureRefs.HandaxeProficiency.Reference.Get(),
            FeatureRefs.HeavyFlailProficiency.Reference.Get(),
            FeatureRefs.HeavyPickProficiency.Reference.Get(),
            FeatureRefs.HeavyRepeatingCrossbowProficiency.Reference.Get(),
            FeatureRefs.HookedHammerProficiency.Reference.Get(),
            FeatureRefs.JavelinProficiency.Reference.Get(),
            FeatureRefs.KamaProficiency.Reference.Get(),
            FeatureRefs.KukriProficiency.Reference.Get(),
            FeatureRefs.LightHammerProficiency.Reference.Get(),
            FeatureRefs.LightPickProficiency.Reference.Get(),
            FeatureRefs.LightRepeatingCrossbowProficiency.Reference.Get(),
            FeatureRefs.LongbowProficiency.Reference.Get(),
            FeatureRefs.LongSpearProficiency.Reference.Get(),
            FeatureRefs.LongswordProficiency.Reference.Get(),
            FeatureRefs.NunchakuProficiency.Reference.Get(),
            FeatureRefs.RapierProficiency.Reference.Get(),
            FeatureRefs.SaiProficiency.Reference.Get(),
            FeatureRefs.SawtoothSabreProficiency.Reference.Get(),
            FeatureRefs.ScimitarProficiency.Reference.Get(),
            FeatureRefs.ScytheProficiency.Reference.Get(),
            FeatureRefs.ShortbowProficiency.Reference.Get(),
            FeatureRefs.ShortswordProficiency.Reference.Get(),
            FeatureRefs.ShurikenProficiency.Reference.Get(),
            FeatureRefs.SianghamProficiency.Reference.Get(),
            FeatureRefs.SlingStaffProficiency.Reference.Get(),
            FeatureRefs.StarknifeProficiency.Reference.Get(),
            FeatureRefs.ThrowingAxeProficiency.Reference.Get(),
            FeatureRefs.TongiProficiency.Reference.Get(),
            FeatureRefs.TridentProficiency.Reference.Get(),
            FeatureRefs.UrgroshProficiency.Reference.Get(),
            FeatureRefs.WarhammerProficiency.Reference.Get(),
        }.Distinct().ToArray();
        BlueprintFeatureSelection CreateMilitarySelection(string suffix) {
            var selection = FeatureSelectionConfigurator.New(
                    $"ClassesRebornMilitaryTradition{suffix}Selection",
                    FutureContentIds.Get($"Trait.Human.MilitaryTradition.{suffix}.Selection"))
                .SetDisplayName("ClassesReborn.MilitaryTraditionHeritage.Choice.Name")
                .SetDescription("ClassesReborn.MilitaryTraditionHeritage.Description")
                .SetIcon(weaponIcon)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .SetIgnorePrerequisites(false)
                .SetObligatory(true)
                .Configure();
            selection.m_AllFeatures = militaryOptions
                .Select(option => option.ToReference<BlueprintFeatureReference>())
                .ToArray();
            selection.m_Features = selection.m_AllFeatures.ToArray();
            return selection;
        }
        var militaryFirst = CreateMilitarySelection("First");
        var militarySecond = CreateMilitarySelection("Second");
        traits.Add(CreateChoiceProgression(
            "MilitaryTradition",
            FutureContentIds.Get("Trait.Human.MilitaryTradition"),
            "ClassesReborn.MilitaryTraditionHeritage",
            weaponIcon,
            BlueprintIds.HumanRace,
            "Human",
            militaryFirst,
            militarySecond));

        traits.Add(BaseFeature(
                "ClassesRebornUnstoppableMagicHeritage",
                FutureContentIds.Get("Trait.Human.UnstoppableMagic"),
                "ClassesReborn.UnstoppableMagicHeritage",
                magicIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddComponent(new RacialSpellPenetrationBonus { Bonus = 2 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornDimdwellerHeritage",
                FutureContentIds.Get("Trait.Human.Dimdweller"),
                "ClassesReborn.DimdwellerHeritage",
                FeatureRefs.SkillFocusStealth.Reference.Get().Icon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillPersuasion,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillPerception,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillStealth,
                value: 2)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornGiantAncestryHeritage",
                FutureContentIds.Get("Trait.Human.GiantAncestry"),
                "ClassesReborn.GiantAncestryHeritage",
                combatIcon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.AdditionalCMB,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.AdditionalCMD,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillStealth,
                value: -2)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornHeartOfTheSlumsHeritage",
                FutureContentIds.Get("Trait.Human.HeartOfTheSlums"),
                "ClassesReborn.HeartOfTheSlumsHeritage",
                FeatureRefs.SkillFocusThievery.Reference.Get().Icon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillThievery,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillStealth,
                value: 2)
            .AddComponent(new DiseaseSaveReroll())
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornPracticedHunterHeritage",
                FutureContentIds.Get("Trait.Human.PracticedHunter"),
                "ClassesReborn.PracticedHunterHeritage",
                FeatureRefs.SkillFocusLoreNature.Reference.Get().Icon,
                BlueprintIds.HumanRace,
                "Human")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillStealth,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillLoreNature,
                value: 2)
            .AddClassSkill(StatType.SkillStealth)
            .AddClassSkill(StatType.SkillLoreNature)
            .Configure());
    }

    private static void ConfigureElfTraits(
        ICollection<BlueprintFeature> traits,
        Sprite initiativeIcon,
        Sprite faithIcon) {
        traits.Add(CreateStatTrait(
            "ClassesRebornWarriorOfOldTrait",
            BlueprintIds.WarriorOfOldTrait,
            "ClassesReborn.WarriorOfOldTrait",
            initiativeIcon,
            BlueprintIds.ElfRace,
            "Elf",
            (StatType.Initiative, 2)));
        traits.Add(CreateStatTrait(
            "ClassesRebornForlornTrait",
            BlueprintIds.ForlornTrait,
            "ClassesReborn.ForlornTrait",
            faithIcon,
            BlueprintIds.ElfRace,
            "Elf",
            (StatType.SaveFortitude, 1)));
        traits.Add(BaseFeature(
                "ClassesRebornInsularTrait",
                BlueprintIds.InsularTrait,
                "ClassesReborn.InsularTrait",
                faithIcon,
                BlueprintIds.ElfRace,
                "Elf")
            .AddComponent(new InsularSaveBonus {
                m_ElfRace = RaceRef(BlueprintIds.ElfRace),
                m_HumanoidRaces = new[] {
                    RaceRef(BlueprintIds.HumanRace),
                    RaceRef(BlueprintIds.DwarfRace),
                    RaceRef(BlueprintIds.GnomeRace),
                    RaceRef(BlueprintIds.HalflingRace),
                    RaceRef(BlueprintIds.HalfElfRace),
                    RaceRef(BlueprintIds.HalfOrcRace),
                    RaceRef(BlueprintIds.GoblinRace),
                },
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornFleetFootedTrait",
                FutureContentIds.Get("Trait.Elf.FleetFooted"),
                "ClassesReborn.FleetFootedTrait",
                initiativeIcon,
                BlueprintIds.ElfRace,
                "Elf")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.Initiative,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillMobility,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.Speed,
                value: 5)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornDreamspeakerTrait",
                FutureContentIds.Get("Trait.Elf.Dreamspeaker"),
                "ClassesReborn.DreamspeakerTrait",
                faithIcon,
                BlueprintIds.ElfRace,
                "Elf")
            .AddComponent(new RacialSpellDcBonus {
                School = SpellSchool.Enchantment,
                Bonus = 1,
            })
            .Configure());
    }

    private static void ConfigureDwarfTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite combatIcon) {
        traits.Add(BaseFeature(
                "ClassesRebornRuthlessTrait",
                BlueprintIds.RuthlessTrait,
                "ClassesReborn.RuthlessTrait",
                weaponIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddCriticalConfirmationBonus(value: 1)
            .Configure());
        traits.Add(CreateStatTrait(
            "ClassesRebornGroundedTrait",
            BlueprintIds.GroundedTrait,
            "ClassesReborn.GroundedTrait",
            initiativeIcon,
            BlueprintIds.DwarfRace,
            "Dwarf",
            (StatType.SkillMobility, 2),
            (StatType.SaveReflex, 1)));

        var deepMarker = BaseFeature(
                "ClassesRebornDeepMarkerTrait",
                BlueprintIds.DeepMarkerTrait,
                "ClassesReborn.DeepMarkerTrait",
                faithIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillLoreNature,
                value: 1)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Fear,
                value: 1,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure();
        traits.Add(deepMarker);

        traits.Add(BaseFeature(
                "ClassesRebornTunnelFighterTrait",
                BlueprintIds.TunnelFighterTrait,
                "ClassesReborn.TunnelFighterTrait",
                combatIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddComponent(new TunnelFighterBonuses())
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornWarsmithTrait",
                BlueprintIds.WarsmithTrait,
                "ClassesReborn.WarsmithTrait",
                weaponIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddClassSkill(StatType.SkillKnowledgeWorld)
            .AddComponent(new WarsmithDamageBonus {
                m_ConstructType = FeatureRef(BlueprintIds.ConstructType),
                m_ElementalSubtype = FeatureRef(BlueprintIds.ElementalSubtype),
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornMagicResistantTrait",
                FutureContentIds.Get("Trait.Dwarf.MagicResistant"),
                "ClassesReborn.MagicResistantTrait",
                faithIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddSpellResistance(value: ContextValues.Rank())
            .AddContextRankConfig(
                ContextRankConfigs.CharacterLevel(max: 40).WithBonusValueProgression(5))
            .AddComponent(new RacialCasterLevelPenalty { Penalty = 2 })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornRelentlessTrait",
                FutureContentIds.Get("Trait.Dwarf.Relentless"),
                "ClassesReborn.RelentlessTrait",
                combatIcon,
                BlueprintIds.DwarfRace,
                "Dwarf")
            .AddComponent(new RelentlessManeuverBonus { Bonus = 2 })
            .Configure());
    }

    private static void ConfigureGnomeTraits(
        ICollection<BlueprintFeature> traits,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite magicIcon) {
        AbilityResourceConfigurator.New(
                "ClassesRebornAdrenalineRushResource",
                BlueprintIds.AdrenalineRushResource)
            .SetLocalizedName("ClassesReborn.AdrenalineRushTrait.Name")
            .SetLocalizedDescription("ClassesReborn.AdrenalineRushTrait.Description")
            .SetIcon(initiativeIcon)
            .SetMax(1)
            .Configure();
        BuffConfigurator.New(
                "ClassesRebornAdrenalineRushBuff",
                BlueprintIds.AdrenalineRushBuff)
            .SetDisplayName("ClassesReborn.AdrenalineRushTrait.Name")
            .SetDescription("ClassesReborn.AdrenalineRushTrait.Description")
            .SetIcon(initiativeIcon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddTemporaryHitPointsRandom(
                bonus: ContextValues.Constant(0),
                descriptor: ModifierDescriptor.Trait,
                dice: new DiceFormula(1, DiceType.D6),
                scaleBonusByCasterLevel: false)
            .Configure();
        traits.Add(BaseFeature(
                "ClassesRebornAdrenalineRushTrait",
                BlueprintIds.AdrenalineRushTrait,
                "ClassesReborn.AdrenalineRushTrait",
                initiativeIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddAbilityResources(
                amount: 0,
                resource: BlueprintIds.AdrenalineRushResource,
                restoreAmount: true)
            .AddComponent(new AdrenalineRushTrigger {
                m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                    BlueprintIds.AdrenalineRushResource),
                m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.AdrenalineRushBuff),
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornIllusionObsessionTrait",
                BlueprintIds.IllusionObsessionTrait,
                "ClassesReborn.IllusionObsessionTrait",
                magicIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddComponent(new RacialCasterLevelBonus { School = SpellSchool.Illusion })
            .Configure());
        traits.Add(CreateStatTrait(
            "ClassesRebornRapscallionTrait",
            BlueprintIds.RapscallionTrait,
            "ClassesReborn.RapscallionTrait",
            initiativeIcon,
            BlueprintIds.GnomeRace,
            "Gnome",
            (StatType.SkillMobility, 1),
            (StatType.Initiative, 1)));
        traits.Add(BaseFeature(
                "ClassesRebornAnimalFriendTrait",
                BlueprintIds.AnimalFriendTrait,
                "ClassesReborn.AnimalFriendTrait",
                faithIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddComponent(new AnimalFriendSaveBonus {
                m_AnimalType = FeatureRef(BlueprintIds.AnimalType),
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornPyromaniacTrait",
                FutureContentIds.Get("Trait.Gnome.Pyromaniac"),
                "ClassesReborn.PyromaniacTrait",
                magicIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddPrerequisiteNoFeature(FeatureRefs.PyromaniacGnome.ToString())
            .AddComponent(new RacialCasterLevelBonus {
                Descriptor = SpellDescriptor.Fire,
                Bonus = 1,
            })
            .Configure());

        var eternalHopeResource = AbilityResourceConfigurator.New(
                "ClassesRebornEternalHopeResource",
                FutureContentIds.Get("Trait.Gnome.EternalHope.Resource"))
            .SetLocalizedName("ClassesReborn.EternalHopeTrait.Name")
            .SetLocalizedDescription("ClassesReborn.EternalHopeTrait.Description")
            .SetIcon(initiativeIcon)
            .SetMax(1)
            .Configure();
        traits.Add(BaseFeature(
                "ClassesRebornEternalHopeTrait",
                FutureContentIds.Get("Trait.Gnome.EternalHope"),
                "ClassesReborn.EternalHopeTrait",
                initiativeIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Fear | SpellDescriptor.Emotion,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .AddAbilityResources(
                amount: 0,
                resource: eternalHopeResource,
                restoreAmount: true)
            .AddComponent(new EternalHopeReroll {
                m_Resource = eternalHopeResource.ToReference<BlueprintAbilityResourceReference>(),
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornFellMagicTrait",
                FutureContentIds.Get("Trait.Gnome.FellMagic"),
                "ClassesReborn.FellMagicTrait",
                magicIcon,
                BlueprintIds.GnomeRace,
                "Gnome")
            .AddComponent(new RacialSpellDcBonus {
                School = SpellSchool.Necromancy,
                Bonus = 1,
            })
            .Configure());
    }

    private static void ConfigureHalflingTraits(
        ICollection<BlueprintFeature> traits,
        Sprite initiativeIcon,
        Sprite combatIcon) {
        traits.Add(CreateSkillChoiceTrait(
            "WellInformed",
            BlueprintIds.WellInformedTrait,
            BlueprintIds.WellInformedBonuses,
            BlueprintIds.WellInformedSelection,
            BlueprintIds.WellInformedPersuasion,
            BlueprintIds.WellInformedWorld,
            "ClassesReborn.WellInformedTrait",
            initiativeIcon,
            BlueprintIds.HalflingRace,
            "Halfling",
            StatType.SkillPersuasion,
            StatType.SkillKnowledgeWorld));

        var athletics = FeatureConfigurator.New(
                "ClassesRebornIntrepidVolunteerAthletics",
                BlueprintIds.IntrepidVolunteerAthletics)
            .SetDisplayName("ClassesReborn.IntrepidVolunteerTrait.Athletics.Name")
            .SetDescription("ClassesReborn.IntrepidVolunteerTrait.Description")
            .SetIcon(combatIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddReplaceStatBaseAttribute(
                baseAttributeReplacement: StatType.Dexterity,
                replaceIfHigher: false,
                targetStat: StatType.SkillAthletics)
            .Configure();
        var maneuvers = FeatureConfigurator.New(
                "ClassesRebornIntrepidVolunteerManeuvers",
                BlueprintIds.IntrepidVolunteerManeuvers)
            .SetDisplayName("ClassesReborn.IntrepidVolunteerTrait.Maneuvers.Name")
            .SetDescription("ClassesReborn.IntrepidVolunteerTrait.Description")
            .SetIcon(combatIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddReplaceCombatManeuverStat(statType: StatType.Dexterity)
            .Configure();
        var intrepidSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornIntrepidVolunteerSelection",
                BlueprintIds.IntrepidVolunteerSelection)
            .SetDisplayName("ClassesReborn.IntrepidVolunteerTrait.Choice.Name")
            .SetDescription("ClassesReborn.IntrepidVolunteerTrait.Description")
            .SetIcon(combatIcon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(athletics, maneuvers)
            .Configure();
        traits.Add(CreateChoiceProgression(
            "IntrepidVolunteer",
            BlueprintIds.IntrepidVolunteerTrait,
            "ClassesReborn.IntrepidVolunteerTrait",
            combatIcon,
            BlueprintIds.HalflingRace,
            "Halfling",
            intrepidSelection));

        traits.Add(BaseFeature(
                "ClassesRebornSuccessfulShirkerTrait",
                BlueprintIds.SuccessfulShirkerTrait,
                "ClassesReborn.SuccessfulShirkerTrait",
                initiativeIcon,
                BlueprintIds.HalflingRace,
                "Halfling")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: StatType.SkillStealth,
                value: 1)
            .AddComponent(new DemoralizeTraitBonus { Bonus = 2 })
            .Configure());

        var jinxBuff = BuffConfigurator.New(
                "ClassesRebornJinxedDebuff",
                FutureContentIds.Get("Trait.Halfling.Jinxed.Buff"))
            .SetDisplayName("ClassesReborn.JinxedTrait.Name")
            .SetDescription("ClassesReborn.JinxedTrait.Description")
            .SetIcon(combatIcon)
            .SetStacking(StackingType.Replace)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Penalty,
                stat: StatType.SaveFortitude,
                value: -1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Penalty,
                stat: StatType.SaveReflex,
                value: -1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Penalty,
                stat: StatType.SaveWill,
                value: -1)
            .Configure();
        var jinxResource = AbilityResourceConfigurator.New(
                "ClassesRebornJinxedResource",
                FutureContentIds.Get("Trait.Halfling.Jinxed.Resource"))
            .SetLocalizedName("ClassesReborn.JinxedTrait.Name")
            .SetLocalizedDescription("ClassesReborn.JinxedTrait.Description")
            .SetIcon(combatIcon)
            .SetMax(1)
            .Configure();
        var jinxAbility = AbilityConfigurator.New(
                "ClassesRebornJinxedAbility",
                FutureContentIds.Get("Trait.Halfling.Jinxed.Ability"))
            .SetDisplayName("ClassesReborn.JinxedTrait.Name")
            .SetDescription("ClassesReborn.JinxedTrait.Description")
            .SetIcon(combatIcon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Close)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetSpellResistance(false)
            .SetLocalizedDuration("ClassesReborn.JinxedTrait.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.JinxedTrait.SavingThrow")
            .AllowTargeting(point: false, enemies: true, friends: false, self: false)
            .AddAbilityEffectRunAction(
                ActionsBuilder.New().ApplyBuff(jinxBuff, ContextDuration.Fixed(3)))
            .Configure();
        traits.Add(BaseFeature(
                "ClassesRebornJinxedTrait",
                FutureContentIds.Get("Trait.Halfling.Jinxed"),
                "ClassesReborn.JinxedTrait",
                combatIcon,
                BlueprintIds.HalflingRace,
                "Halfling")
            .AddFacts(new() { jinxAbility })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornLowBlowTrait",
                FutureContentIds.Get("Trait.Halfling.LowBlow"),
                "ClassesReborn.LowBlowTrait",
                combatIcon,
                BlueprintIds.HalflingRace,
                "Halfling")
            .AddComponent(new LowBlowConfirmationBonus { Bonus = 1 })
            .Configure());
    }

    private static void ConfigureHalfElfTraits(
        ICollection<BlueprintFeature> traits,
        Sprite initiativeIcon,
        Sprite faithIcon) {
        traits.Add(CreateStatTrait(
            "ClassesRebornElvenReflexesTrait",
            BlueprintIds.ElvenReflexesTrait,
            "ClassesReborn.ElvenReflexesTrait",
            initiativeIcon,
            BlueprintIds.HalfElfRace,
            "Half-Elf",
            (StatType.Initiative, 2)));
        traits.Add(BaseFeature(
                "ClassesRebornFailedApprenticeTrait",
                BlueprintIds.FailedApprenticeTrait,
                "ClassesReborn.FailedApprenticeTrait",
                faithIcon,
                BlueprintIds.HalfElfRace,
                "Half-Elf")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Arcane,
                value: 1,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornExperimentalRebelTrait",
                BlueprintIds.ExperimentalRebelTrait,
                "ClassesReborn.ExperimentalRebelTrait",
                faithIcon,
                BlueprintIds.HalfElfRace,
                "Half-Elf")
            .AddComponent(new ExperimentalRebelSaveBonus {
                m_ElfRace = RaceRef(BlueprintIds.ElfRace),
            })
            .Configure());

        var ancestralArmsSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornAncestralArmsSelection",
                FutureContentIds.Get("Trait.HalfElf.AncestralArms.Selection"))
            .SetDisplayName("ClassesReborn.AncestralArmsTrait.Choice.Name")
            .SetDescription("ClassesReborn.AncestralArmsTrait.Description")
            .SetIcon(initiativeIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(
                FeatureRefs.MartialWeaponProficiency.Reference.Get(),
                FeatureSelectionRefs.ExoticWeaponProficiencySelection.Reference.Get())
            .Configure();
        traits.Add(CreateChoiceProgression(
            "AncestralArms",
            FutureContentIds.Get("Trait.HalfElf.AncestralArms"),
            "ClassesReborn.AncestralArmsTrait",
            initiativeIcon,
            BlueprintIds.HalfElfRace,
            "Half-Elf",
            ancestralArmsSelection));

        traits.Add(BaseFeature(
                "ClassesRebornDualMindedTrait",
                FutureContentIds.Get("Trait.HalfElf.DualMinded"),
                "ClassesReborn.DualMindedTrait",
                faithIcon,
                BlueprintIds.HalfElfRace,
                "Half-Elf")
            .AddPrerequisiteNoFeature(FeatureRefs.DualMindedHalfElf.ToString())
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SaveWill,
                value: 2)
            .Configure());
    }

    private static void ConfigureHalfOrcTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite unarmedIcon,
        Sprite combatIcon) {
        BuffConfigurator.New(
                "ClassesRebornFinishTheFightMarkerBuff",
                BlueprintIds.FinishTheFightMarkerBuff)
            .SetDisplayName("ClassesReborn.FinishTheFightTrait.Name")
            .SetDescription("ClassesReborn.FinishTheFightTrait.Description")
            .SetIcon(combatIcon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .Configure();

        traits.Add(BaseFeature(
                "ClassesRebornCruelRagerTrait",
                BlueprintIds.CruelRagerTrait,
                "ClassesReborn.CruelRagerTrait",
                combatIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddComponent(new CruelRagerTrigger {
                m_RageBuffs = new[] {
                    BuffRef(BlueprintIds.StandardRageBuff),
                    BuffRef(BlueprintIds.FocusedRageBuff),
                    BuffRef(BlueprintIds.BloodragerStandardRageBuff),
                    BuffRef(BlueprintIds.BloodragerGreaterRageBuff),
                    BuffRef(BlueprintIds.BloodragerMightyRageBuff),
                },
                m_RageResources = new[] {
                    BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        BlueprintIds.FocusedRageResource),
                    BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        BlueprintIds.BarbarianRageResource),
                    BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                        BlueprintIds.BloodragerRageResource),
                },
            })
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornFinishTheFightTrait",
                BlueprintIds.FinishTheFightTrait,
                "ClassesReborn.FinishTheFightTrait",
                combatIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddComponent(new FinishTheFightTracker {
                m_Marker = BuffRef(BlueprintIds.FinishTheFightMarkerBuff),
            })
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornTuskedTrait",
                BlueprintIds.TuskedTrait,
                "ClassesReborn.TuskedTrait",
                unarmedIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddComponent(new TuskedNaturalAttackSizeIncrease())
            .Configure());
        traits.Add(CreateStatTrait(
            "ClassesRebornScrapperTrait",
            BlueprintIds.ScrapperTrait,
            "ClassesReborn.ScrapperTrait",
            combatIcon,
            BlueprintIds.HalfOrcRace,
            "Half-Orc",
            (StatType.SkillPersuasion, 1),
            (StatType.SkillPerception, 1)));
        traits.Add(BaseFeature(
                "ClassesRebornBruteOrcTrait",
                BlueprintIds.BruteOrcTrait,
                "ClassesReborn.BruteOrcTrait",
                weaponIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddComponent(new BruteThreatDamage())
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornSacredTattooTrait",
                FutureContentIds.Get("Trait.HalfOrc.SacredTattoo"),
                "ClassesReborn.SacredTattooTrait",
                combatIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddStatBonus(
                descriptor: ModifierDescriptor.Luck,
                stat: StatType.SaveFortitude,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Luck,
                stat: StatType.SaveReflex,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Luck,
                stat: StatType.SaveWill,
                value: 1)
            .Configure());

        var toothyId = FutureContentIds.Get("Trait.HalfOrc.Toothy");
        traits.Add(BaseFeature(
                "ClassesRebornToothyTrait",
                toothyId,
                "ClassesReborn.ToothyTrait",
                unarmedIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddAdditionalLimb(BlueprintIds.Bite1d4)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornShamansApprenticeTrait",
                FutureContentIds.Get("Trait.HalfOrc.ShamansApprentice"),
                "ClassesReborn.ShamansApprenticeTrait",
                combatIcon,
                BlueprintIds.HalfOrcRace,
                "Half-Orc")
            .AddFacts(new() { FeatureRefs.Endurance.Reference.Get() })
            .Configure());
    }

    private static void ConfigureAasimarTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite faithIcon,
        Sprite magicIcon) {
        traits.Add(BaseFeature(
                "ClassesRebornCelestialContactTrait",
                BlueprintIds.CelestialContactTrait,
                "ClassesReborn.CelestialContactTrait",
                magicIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddComponent(new RacialCasterLevelBonus { Descriptor = SpellDescriptor.Good })
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornMartyrsBloodTrait",
                BlueprintIds.MartyrsBloodTrait,
                "ClassesReborn.MartyrsBloodTrait",
                faithIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddComponent(new MartyrsBloodAttackBonus())
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornToxophiliteTrait",
                BlueprintIds.ToxophiliteTrait,
                "ClassesReborn.ToxophiliteTrait",
                weaponIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddComponent(new BowCriticalConfirmationBonus())
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornAdriftTrait",
                BlueprintIds.AdriftTrait,
                "ClassesReborn.AdriftTrait",
                faithIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Charm | SpellDescriptor.Compulsion,
                value: 1,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornSelectiveHealthTrait",
                BlueprintIds.SelectiveHealthTrait,
                "ClassesReborn.SelectiveHealthTrait",
                faithIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Disease,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornHeavenbornTrait",
                FutureContentIds.Get("Trait.Aasimar.Heavenborn"),
                "ClassesReborn.HeavenbornTrait",
                magicIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddComponent(new RacialCasterLevelBonus {
                Descriptor = SpellDescriptor.Good,
                NameFragments = new[] { "Light", "Radiance", "Sunbeam", "Sunburst" },
                Bonus = 1,
            })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornDeathlessSpiritTrait",
                FutureContentIds.Get("Trait.Aasimar.DeathlessSpirit"),
                "ClassesReborn.DeathlessSpiritTrait",
                faithIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Death | SpellDescriptor.NegativeLevel,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Racial)
            .AddDamageResistanceEnergy(
                type: Kingmaker.Enums.Damage.DamageEnergyType.NegativeEnergy,
                value: ContextValues.Constant(5))
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornCelestialCrusaderTrait",
                FutureContentIds.Get("Trait.Aasimar.CelestialCrusader"),
                "ClassesReborn.CelestialCrusaderTrait",
                weaponIcon,
                BlueprintIds.AasimarRace,
                "Aasimar")
            .AddComponent(new CelestialCrusaderBonuses {
                m_OutsiderType = FeatureRef(FeatureRefs.OutsiderType.ToString()),
                m_EvilSubtype = FeatureRef(FeatureRefs.SubtypeEvil.ToString()),
                Bonus = 1,
            })
            .Configure());
    }

    private static void ConfigureTieflingTraits(
        ICollection<BlueprintFeature> traits,
        Sprite armorIcon,
        Sprite weaponIcon,
        Sprite initiativeIcon,
        Sprite faithIcon,
        Sprite magicIcon) {
        traits.Add(BaseFeature(
                "ClassesRebornDarkMagicAffinityTrait",
                BlueprintIds.DarkMagicAffinityTrait,
                "ClassesReborn.DarkMagicAffinityTrait",
                magicIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddComponent(new RacialCasterLevelBonus { Descriptor = SpellDescriptor.Evil })
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornHardToPinDownTrait",
                BlueprintIds.HardToPinDownTrait,
                "ClassesReborn.HardToPinDownTrait",
                armorIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddComponent(new HardToPinDownArmorClass())
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornShadowStabberTrait",
                BlueprintIds.ShadowStabberTrait,
                "ClassesReborn.ShadowStabberTrait",
                weaponIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddComponent(new ShadowStabberDamage())
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornEverWaryTrait",
                BlueprintIds.EverWaryTrait,
                "ClassesReborn.EverWaryTrait",
                initiativeIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddComponent(new EverWaryArmorClass())
            .Configure());
        traits.Add(BaseFeature(
                "ClassesRebornBornDamnedTrait",
                BlueprintIds.BornDamnedTrait,
                "ClassesReborn.BornDamnedTrait",
                faithIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Curse | SpellDescriptor.Hex,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Trait)
            .Configure());

        var mawOption = FeatureConfigurator.New(
                "ClassesRebornMawOrClawMaw",
                FutureContentIds.Get("Trait.Tiefling.MawOrClaw.Maw"))
            .SetDisplayName("ClassesReborn.MawOrClawTrait.Maw.Name")
            .SetDescription("ClassesReborn.MawOrClawTrait.Description")
            .SetIcon(weaponIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddAdditionalLimb(ItemWeaponRefs.Bite1d6.ToString())
            .Configure();
        var clawOption = FeatureConfigurator.New(
                "ClassesRebornMawOrClawClaws",
                FutureContentIds.Get("Trait.Tiefling.MawOrClaw.Claws"))
            .SetDisplayName("ClassesReborn.MawOrClawTrait.Claws.Name")
            .SetDescription("ClassesReborn.MawOrClawTrait.Description")
            .SetIcon(weaponIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddAdditionalLimb(ItemWeaponRefs.Claw1d4.ToString())
            .AddAdditionalLimb(ItemWeaponRefs.Claw1d4.ToString())
            .Configure();
        var mawOrClawSelection = FeatureSelectionConfigurator.New(
                "ClassesRebornMawOrClawSelection",
                FutureContentIds.Get("Trait.Tiefling.MawOrClaw.Selection"))
            .SetDisplayName("ClassesReborn.MawOrClawTrait.Choice.Name")
            .SetDescription("ClassesReborn.MawOrClawTrait.Description")
            .SetIcon(weaponIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddToAllFeatures(mawOption, clawOption)
            .Configure();
        traits.Add(CreateChoiceProgression(
            "MawOrClaw",
            FutureContentIds.Get("Trait.Tiefling.MawOrClaw"),
            "ClassesReborn.MawOrClawTrait",
            weaponIcon,
            BlueprintIds.TieflingRace,
            "Tiefling",
            mawOrClawSelection));

        traits.Add(BaseFeature(
                "ClassesRebornFiendishSprinterTrait",
                FutureContentIds.Get("Trait.Tiefling.FiendishSprinter"),
                "ClassesReborn.FiendishSprinterTrait",
                initiativeIcon,
                BlueprintIds.TieflingRace,
                "Tiefling")
            .AddComponent(new FiendishSprinterChargeSpeed { Bonus = 10 })
            .Configure());
    }

    private static void ConfigureOreadTraits(
        ICollection<BlueprintFeature> traits,
        Sprite faithIcon,
        Sprite magicIcon) {
        BuffConfigurator.New(
                "ClassesRebornStoicDignityAllyBuff",
                BlueprintIds.StoicDignityAllyBuff)
            .SetDisplayName("ClassesReborn.StoicDignityTrait.Name")
            .SetDescription("ClassesReborn.StoicDignityTrait.Description")
            .SetIcon(faithIcon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new StoicDignitySaveBonus { IsSelf = false })
            .Configure();
        AbilityAreaEffectConfigurator.New(
                "ClassesRebornStoicDignityArea",
                BlueprintIds.StoicDignityArea)
            .SetTargetType(BlueprintAbilityAreaEffect.TargetType.Ally)
            .SetShape(AreaEffectShape.Cylinder)
            .SetSize(new Feet(10))
            .SetAffectEnemies(false)
            .SetAggroEnemies(false)
            .AddAbilityAreaEffectBuff(BlueprintIds.StoicDignityAllyBuff)
            .Configure();
        traits.Add(BaseFeature(
                "ClassesRebornStoicDignityTrait",
                BlueprintIds.StoicDignityTrait,
                "ClassesReborn.StoicDignityTrait",
                faithIcon,
                BlueprintIds.OreadRace,
                "Oread")
            .AddComponent(new AddAreaEffect {
                m_AreaEffect = BlueprintTool.GetRef<BlueprintAbilityAreaEffectReference>(
                    BlueprintIds.StoicDignityArea),
            })
            .AddComponent(new StoicDignitySaveBonus { IsSelf = true })
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornStoneInTheBloodTrait",
                FutureContentIds.Get("Trait.Oread.StoneInTheBlood"),
                "ClassesReborn.StoneInTheBloodTrait",
                faithIcon,
                BlueprintIds.OreadRace,
                "Oread")
            .AddComponent(new StoneInTheBloodHealing())
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornEarthInsightTrait",
                FutureContentIds.Get("Trait.Oread.EarthInsight"),
                "ClassesReborn.EarthInsightTrait",
                magicIcon,
                BlueprintIds.OreadRace,
                "Oread")
            .AddComponent(new RacialCasterLevelBonus {
                Descriptor = SpellDescriptor.Acid,
                NameFragments = new[] { "Acid", "Earth", "Stone", "Rock" },
                Bonus = 1,
            })
            .Configure());
    }

    private static void ConfigureDhampirTraits(
        ICollection<BlueprintFeature> traits,
        Sprite weaponIcon,
        Sprite faithIcon,
        Sprite magicIcon) {
        traits.Add(CreateSkillChoiceTrait(
            "AcknowledgedScion",
            BlueprintIds.AcknowledgedScionTrait,
            BlueprintIds.AcknowledgedScionBonuses,
            BlueprintIds.AcknowledgedScionSelection,
            BlueprintIds.AcknowledgedScionWorld,
            BlueprintIds.AcknowledgedScionReligion,
            "ClassesReborn.AcknowledgedScionTrait",
            faithIcon,
            BlueprintIds.DhampirRace,
            "Dhampir",
            StatType.SkillKnowledgeWorld,
            StatType.SkillLoreReligion));

        traits.Add(BaseFeature(
                "ClassesRebornUndeadSlayerTrait",
                BlueprintIds.UndeadSlayerTrait,
                "ClassesReborn.UndeadSlayerTrait",
                weaponIcon,
                BlueprintIds.DhampirRace,
                "Dhampir")
            .AddComponent(new UndeadSlayerBonuses {
                m_UndeadType = FeatureRef(BlueprintIds.UndeadType),
            })
            .Configure());

        var pairs = new[] {
            (BlueprintIds.HalfForgottenSecretsOption1, StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillKnowledgeArcana),
            (BlueprintIds.HalfForgottenSecretsOption2, StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillKnowledgeWorld),
            (BlueprintIds.HalfForgottenSecretsOption3, StatType.SkillKnowledgeArcana, StatType.SkillLoreNature, StatType.SkillKnowledgeArcana),
            (BlueprintIds.HalfForgottenSecretsOption4, StatType.SkillKnowledgeArcana, StatType.SkillLoreNature, StatType.SkillLoreNature),
            (BlueprintIds.HalfForgottenSecretsOption5, StatType.SkillKnowledgeArcana, StatType.SkillLoreReligion, StatType.SkillKnowledgeArcana),
            (BlueprintIds.HalfForgottenSecretsOption6, StatType.SkillKnowledgeArcana, StatType.SkillLoreReligion, StatType.SkillLoreReligion),
            (BlueprintIds.HalfForgottenSecretsOption7, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillKnowledgeWorld),
            (BlueprintIds.HalfForgottenSecretsOption8, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreNature),
            (BlueprintIds.HalfForgottenSecretsOption9, StatType.SkillKnowledgeWorld, StatType.SkillLoreReligion, StatType.SkillKnowledgeWorld),
            (BlueprintIds.HalfForgottenSecretsOption10, StatType.SkillKnowledgeWorld, StatType.SkillLoreReligion, StatType.SkillLoreReligion),
            (BlueprintIds.HalfForgottenSecretsOption11, StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillLoreNature),
            (BlueprintIds.HalfForgottenSecretsOption12, StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillLoreReligion),
        };
        var options = pairs.Select((pair, index) =>
            FeatureConfigurator.New(
                    $"ClassesRebornHalfForgottenSecretsOption{index + 1}",
                    pair.Item1)
                .SetDisplayName($"ClassesReborn.HalfForgottenSecretsTrait.Option{index + 1}.Name")
                .SetDescription("ClassesReborn.HalfForgottenSecretsTrait.Description")
                .SetIcon(magicIcon)
                .SetGroups(FeatureGroup.Trait)
                .SetRanks(1)
                .SetIsClassFeature(false)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Trait,
                    stat: pair.Item2,
                    value: 1)
                .AddStatBonus(
                    descriptor: ModifierDescriptor.Trait,
                    stat: pair.Item3,
                    value: 1)
                .AddClassSkill(pair.Item4)
                .Configure())
            .ToArray();
        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornHalfForgottenSecretsSelection",
                BlueprintIds.HalfForgottenSecretsSelection)
            .SetDisplayName("ClassesReborn.HalfForgottenSecretsTrait.Choice.Name")
            .SetDescription("ClassesReborn.HalfForgottenSecretsTrait.Description")
            .SetIcon(magicIcon)
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
        traits.Add(CreateChoiceProgression(
            "HalfForgottenSecrets",
            BlueprintIds.HalfForgottenSecretsTrait,
            "ClassesReborn.HalfForgottenSecretsTrait",
            magicIcon,
            BlueprintIds.DhampirRace,
            "Dhampir",
            selection));

        traits.Add(BaseFeature(
                "ClassesRebornDaybornTrait",
                FutureContentIds.Get("Trait.Dhampir.Dayborn"),
                "ClassesReborn.DaybornTrait",
                faithIcon,
                BlueprintIds.DhampirRace,
                "Dhampir")
            .AddConditionImmunity(UnitCondition.Dazzled)
            .Configure());

        traits.Add(BaseFeature(
                "ClassesRebornVampiricFangsTrait",
                FutureContentIds.Get("Trait.Dhampir.VampiricFangs"),
                "ClassesReborn.VampiricFangsTrait",
                weaponIcon,
                BlueprintIds.DhampirRace,
                "Dhampir")
            .AddAdditionalLimb(ItemWeaponRefs.Bite1d6.ToString())
            .Configure());
    }

    private static FeatureConfigurator BaseFeature(
        string name,
        string id,
        string localizationPrefix,
        Sprite icon,
        string raceId,
        string raceName) =>
        FeatureConfigurator.New(name, id)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddComponent(RacePrerequisite(raceId, raceName));

    private static BlueprintFeature CreateStatTrait(
        string name,
        string id,
        string localizationPrefix,
        Sprite icon,
        string raceId,
        string raceName,
        params (StatType Stat, int Value)[] bonuses) {
        var configurator = BaseFeature(
            name,
            id,
            localizationPrefix,
            icon,
            raceId,
            raceName);
        foreach (var bonus in bonuses) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: bonus.Stat,
                value: bonus.Value);
        }
        return configurator.Configure();
    }

    private static BlueprintProgression CreateSkillChoiceTrait(
        string name,
        string parentId,
        string bonusesId,
        string selectionId,
        string optionOneId,
        string optionTwoId,
        string localizationPrefix,
        Sprite icon,
        string raceId,
        string raceName,
        StatType firstSkill,
        StatType secondSkill) {
        var bonuses = FeatureConfigurator.New($"ClassesReborn{name}Bonuses", bonusesId)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: firstSkill,
                value: 1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Trait,
                stat: secondSkill,
                value: 1)
            .Configure();
        bonuses.HideInUI = true;
        bonuses.HideInCharacterSheetAndLevelUp = true;

        var firstOption = FeatureConfigurator.New(
                $"ClassesReborn{name}FirstClassSkill",
                optionOneId)
            .SetDisplayName($"{localizationPrefix}.FirstChoice.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddClassSkill(firstSkill)
            .Configure();
        var secondOption = FeatureConfigurator.New(
                $"ClassesReborn{name}SecondClassSkill",
                optionTwoId)
            .SetDisplayName($"{localizationPrefix}.SecondChoice.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddClassSkill(secondSkill)
            .Configure();
        var selection = FeatureSelectionConfigurator.New(
                $"ClassesReborn{name}ClassSkillSelection",
                selectionId)
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

        var progression = CreateChoiceProgression(
            name,
            parentId,
            localizationPrefix,
            icon,
            raceId,
            raceName,
            bonuses,
            selection);
        return progression;
    }

    private static BlueprintProgression CreateChoiceProgression(
        string name,
        string id,
        string localizationPrefix,
        Sprite icon,
        string raceId,
        string raceName,
        params BlueprintFeatureBase[] features) {
        var progression = ProgressionConfigurator.New($"ClassesReborn{name}Trait", id)
            .SetDisplayName($"{localizationPrefix}.Name")
            .SetDescription($"{localizationPrefix}.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Trait)
            .SetRanks(1)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(false)
            .AddComponent(RacePrerequisite(raceId, raceName))
            .Configure();
        progression.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = features
                    .Select(feature => feature.ToReference<BlueprintFeatureBaseReference>())
                    .ToList(),
            },
        };
        return progression;
    }

    private static RaceTraitPrerequisite RacePrerequisite(string raceId, string raceName) =>
        new() {
            m_Race = RaceRef(raceId),
            m_AdoptedSelection = BlueprintTool.GetRef<BlueprintFeatureSelectionReference>(
                BlueprintIds.AdoptedRacialTraitSelection),
            RaceName = raceName,
            Group = Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite.GroupType.All,
            CheckInProgression = true,
        };

    private static BlueprintRaceReference RaceRef(string id) =>
        BlueprintTool.GetRef<BlueprintRaceReference>(id);

    private static BlueprintFeatureReference FeatureRef(string id) =>
        BlueprintTool.GetRef<BlueprintFeatureReference>(id);

    private static BlueprintBuffReference BuffRef(string id) =>
        BlueprintTool.GetRef<BlueprintBuffReference>(id);

    private static void Validate(IReadOnlyCollection<BlueprintFeature> traits) {
        if (traits.Count != 55 ||
            traits.Select(trait => trait.AssetGuid).Distinct().Count() != traits.Count ||
            traits.Any(trait => trait.Ranks != 1 ||
                trait.GetComponents<RaceTraitPrerequisite>().Count() != 1)) {
            throw new InvalidOperationException(
                "The racial trait pool must contain 55 unique, single-rank traits and give every option exactly one race prerequisite.");
        }
    }
}
