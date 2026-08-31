using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;

namespace ClassesReborn;

internal static partial class FeatRebalance {
    private const string StandardRageBuff = "da8ce41ac3cd74742b80984ccc3c9613";
    private const string FocusedRageBuff = "3513326cd64f475781799685c57fa452";
    private const string InspiredRageBuff = "345d36cd45f5614409824209f26d0130";
    private const string RageSpellBuff = "6928adfa56f0dcc468162efde545786b";
    private const string InfectiousRageBuff = "2ff155ab5a6316e4e809f42148ef4d09";
    private const string BloodragerStandardRageBuff = "5eac31e457999334b98f98b60fc73b2f";
    private const string BloodragerMightyRageBuff = "b8a8d387f4bd46b5b283089f5ff0ec61";

    private static void ConfigureExpandedFeats() {
        if (Main.Settings.MightyHurling) {
            ConfigureMightyHurling();
        }
        if (Main.Settings.CrushingThrow) {
            ConfigureCrushingThrow();
        }
        if (Main.Settings.BalancedGrip) {
            ConfigureBalancedGrip();
        }
        if (Main.Settings.TwoWeaponDefense) {
            ConfigureTwoWeaponDefense();
        }
        if (Main.Settings.ArmorOfThePit) {
            ConfigureArmorOfThePit();
        }
        if (Main.Settings.GreaterUnarmedStrike) {
            ConfigureGreaterUnarmedStrike();
        }
        if (Main.Settings.DervishDance) {
            ConfigureDervishDance();
        }
        if (Main.Settings.QuickStudy) {
            ConfigureQuickStudy();
        }
        ArcanistExploitRebalance.Configure();
        if (Main.Settings.MadMagic) {
            ConfigureMadMagic();
        }
        if (Main.Settings.CrusadersFlurry) {
            ConfigureCrusadersFlurry();
        }
        if (Main.Settings.RimeSpell) {
            ConfigureRimeSpell();
        }
        if (Main.Settings.DesnasShootingStar) {
            ConfigureDesnasShootingStar();
        }
        if (Main.Settings.BladedBrush) {
            ConfigureBladedBrush();
        }
        if (Main.Settings.AsceticStyle || Main.Settings.AsceticForm ||
            Main.Settings.AsceticStrike) {
            ConfigureAsceticStyleChain();
        }
        if (Main.Settings.FeyFoundling) {
            ConfigureFeyFoundling();
        }
        if (Main.Settings.ViciousStomp) {
            ConfigureViciousStomp();
        }
        if (Main.Settings.UnsanctionedKnowledge) {
            ConfigureUnsanctionedKnowledge();
        }
        if (Main.Settings.EldritchHeritage) {
            EldritchHeritageRebalance.Configure();
            EldritchHeritageRebalance.ConfigureDelayed();
            AddAsFeat(BlueprintTool.Get<BlueprintFeatureSelection>(
                Guids.EldritchHeritageFeat));
            AddAsFeat(BlueprintTool.Get<BlueprintFeatureSelection>(
                Guids.ImprovedEldritchHeritageFeat));
            AddAsFeat(BlueprintTool.Get<BlueprintFeatureSelection>(
                Guids.GreaterEldritchHeritageFeat));
        }
        if (Main.Settings.FeralCombatTraining) {
            ConfigureFeralCombatTraining();
        }
        if (Main.Settings.RacialHeritage) {
            ConfigureRacialHeritage();
        }
        if (Main.Settings.ArtfulDodge) {
            ConfigureArtfulDodge();
        }
        if (Main.Settings.CutFromTheAir) {
            ConfigureCutFromTheAir();
        }
        ConfigureRequestedFeats();
    }

    private static void ConfigureFeralCombatTraining() {
        var feat = ParametrizedFeatureConfigurator.New(
                "ClassesRebornFeralCombatTraining",
                FutureContentIds.Get("Feat.FeralCombatTraining"))
            .SetDisplayName("ClassesReborn.FeralCombatTraining.Name")
            .SetDescription("ClassesReborn.FeralCombatTraining.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.ImprovedUnarmedStrike).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(20)
            .SetReapplyOnLevelUp(true)
            .SetParameterType(FeatureParameterType.WeaponCategory)
            .SetWeaponSubCategory(WeaponSubCategory.Natural)
            .SetRequireProficiency(true)
            .SetPrerequisite(BlueprintIds.WeaponFocus)
            .AddPrerequisiteFeature(BlueprintIds.ImprovedUnarmedStrike)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Melee | FeatureTag.ClassSpecific)
            .AddComponent(new FeralCombatTrainingComponent())
            .AddComponent(new AddMechanicsFeature {
                m_Feature = AddMechanicsFeature.MechanicsFeatureType.IterativeNaturalAttacks,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureRacialHeritage() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.WeaponFocus).Icon;
        var races = new[] {
            ("Aasimar", RaceRefs.AasimarRace.Reference.Get()),
            ("Dhampir", RaceRefs.DhampirRace.Reference.Get()),
            ("Dwarf", RaceRefs.DwarfRace.Reference.Get()),
            ("Elf", RaceRefs.ElfRace.Reference.Get()),
            ("Gnome", RaceRefs.GnomeRace.Reference.Get()),
            ("Half-Elf", RaceRefs.HalfElfRace.Reference.Get()),
            ("Half-Orc", RaceRefs.HalfOrcRace.Reference.Get()),
            ("Halfling", RaceRefs.HalflingRace.Reference.Get()),
            ("Kitsune", RaceRefs.KitsuneRace.Reference.Get()),
            ("Oread", RaceRefs.OreadRace.Reference.Get()),
            ("Tiefling", RaceRefs.TieflingRace.Reference.Get()),
        };
        if (Main.Settings.GoblinRace) {
            races = races.Append((
                "Goblin",
                BlueprintTool.Get<BlueprintRace>(BlueprintIds.GoblinRace)))
                .ToArray();
        }
        if (Main.Settings.MongrelRace) {
            races = races.Append((
                "Mongrel",
                BlueprintTool.Get<BlueprintRace>(BlueprintIds.MongrelRace)))
                .ToArray();
        }
        var options = races.Select(entry => FeatureConfigurator.New(
                $"ClassesRebornRacialHeritage{entry.Item1.Replace("-", string.Empty)}",
                FutureContentIds.Get($"Feat.RacialHeritage.{entry.Item1}"))
            .SetDisplayName($"ClassesReborn.RacialHeritage.{entry.Item1.Replace("-", string.Empty)}.Name")
            .SetDescription("ClassesReborn.RacialHeritage.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .AddComponent(new RacialHeritageMarker {
                m_Race = entry.Item2.ToReference<BlueprintRaceReference>(),
            })
            .Configure()).ToArray();

        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornRacialHeritage",
                FutureContentIds.Get("Feat.RacialHeritage"))
            .SetDisplayName("ClassesReborn.RacialHeritage.Name")
            .SetDescription("ClassesReborn.RacialHeritage.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat)
            .SetRanks(1)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddComponent(new CharacterRacePrerequisite {
                m_Race = RaceRefs.HumanRace.Reference.Get()
                    .ToReference<BlueprintRaceReference>(),
                RaceName = "Human",
            })
            .Configure();
        selection.m_AllFeatures = options
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        if (options.Any(option => option.Groups?.Any() == true)) {
            throw new InvalidOperationException(
                "Racial Heritage's nested race choices must not register as standalone feature-group options.");
        }
        AddAsFeat(selection);
    }

    private static void ConfigureArtfulDodge() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornArtfulDodge",
                FutureContentIds.Get("Feat.ArtfulDodge"))
            .SetDisplayName("ClassesReborn.ArtfulDodge.Name")
            .SetDescription("ClassesReborn.ArtfulDodge.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.CombatExpertise).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(1)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Intelligence, 13)
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific)
            .AddComponent(new ReplaceStatForPrerequisites {
                OldStat = StatType.Dexterity,
                NewStat = StatType.Intelligence,
                Policy = ReplaceStatForPrerequisites.StatReplacementPolicy.NewStat,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureCutFromTheAir() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornCutFromTheAir",
                FutureContentIds.Get("Feat.CutFromTheAir"))
            .SetDisplayName("ClassesReborn.CutFromTheAir.Name")
            .SetDescription("ClassesReborn.CutFromTheAir.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.CombatReflexes).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(1)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Strength, 13)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 5)
            .AddPrerequisiteFeature(BlueprintIds.PowerAttack)
            .AddPrerequisiteFeature(BlueprintIds.WeaponFocus)
            .AddFeatureTagsComponent(FeatureTag.Defense | FeatureTag.Melee)
            .AddComponent(new CutFromTheAirComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static readonly WeaponCategory[] ThrownWeaponCategories = {
        WeaponCategory.ThrowingAxe,
        WeaponCategory.Dart,
        WeaponCategory.Javelin,
        WeaponCategory.Shuriken,
    };

    private static void ConfigureMightyHurling() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornMightyHurling",
                BlueprintIds.MightyHurlingFeat)
            .SetDisplayName("ClassesReborn.MightyHurling.Name")
            .SetDescription("ClassesReborn.MightyHurling.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.DeadlyAimFeature).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Strength, 13)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 1)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
            .AddComponent(new WeaponCategoryAttackStatReplacement {
                ReplacementStat = StatType.Strength,
                Categories = ThrownWeaponCategories,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureCrushingThrow() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornCrushingThrow",
                BlueprintIds.CrushingThrowFeat)
            .SetDisplayName("ClassesReborn.CrushingThrow.Name")
            .SetDescription("ClassesReborn.CrushingThrow.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.PowerAttack).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Strength, 15)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 6)
            .AddPrerequisiteFeature(BlueprintIds.MightyHurlingFeat)
            .AddPrerequisiteFeature(BlueprintIds.PowerAttack)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Damage | FeatureTag.Ranged)
            .AddComponent(new CrushingThrowComponent {
                Categories = ThrownWeaponCategories,
                m_PowerAttackBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.PowerAttackBuff),
                m_MythicPowerAttack = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.MythicPowerAttack),
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureBalancedGrip() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFinesse).Icon;
        var slashing = CreateBalancedGripOption(
            "ClassesRebornBalancedGripSlashing",
            BlueprintIds.BalancedGripSlashingFeat,
            "ClassesReborn.BalancedGrip.Slashing.Name",
            WeaponSubCategory.OneHandedSlashing,
            icon);
        var piercing = CreateBalancedGripOption(
            "ClassesRebornBalancedGripPiercing",
            BlueprintIds.BalancedGripPiercingFeat,
            "ClassesReborn.BalancedGrip.Piercing.Name",
            WeaponSubCategory.OneHandedPiercing,
            icon);

        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornBalancedGrip",
                BlueprintIds.BalancedGripSelection)
            .SetDisplayName("ClassesReborn.BalancedGrip.Name")
            .SetDescription("ClassesReborn.BalancedGrip.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetRanks(20)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Dexterity, 13)
            .AddPrerequisiteStatValue(StatType.BaseAttackBonus, 1)
            .AddPrerequisiteFeature(BlueprintIds.WeaponFinesse)
            .AddPrerequisiteFeature(BlueprintIds.WeaponFocus)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .Configure();
        selection.m_Features = new[] {
            slashing.ToReference<BlueprintFeatureReference>(),
            piercing.ToReference<BlueprintFeatureReference>(),
        };
        selection.m_AllFeatures = selection.m_Features.ToArray();
        AddAsFeat(selection, combatFeat: true);
    }

    private static BlueprintParametrizedFeature CreateBalancedGripOption(
        string blueprintName,
        string id,
        string displayName,
        WeaponSubCategory subCategory,
        UnityEngine.Sprite icon) =>
        ParametrizedFeatureConfigurator.New(blueprintName, id)
            .SetDisplayName(displayName)
            .SetDescription("ClassesReborn.BalancedGrip.Description")
            .SetIcon(icon)
            .SetParameterType(FeatureParameterType.WeaponCategory)
            .SetWeaponSubCategory(subCategory)
            .SetRequireProficiency(true)
            .SetPrerequisite(BlueprintIds.WeaponFocus)
            .SetReapplyOnLevelUp(true)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new BalancedGripComponent {
                m_MythicWeaponFinesse =
                    BlueprintTool.GetRef<BlueprintUnitFactReference>(
                        BlueprintIds.MythicWeaponFinesse),
            })
            .Configure();

    private static void ConfigureTwoWeaponDefense() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornTwoWeaponDefense",
                BlueprintIds.TwoWeaponDefenseFeat)
            .SetDisplayName("ClassesReborn.TwoWeaponDefense.Name")
            .SetDescription("ClassesReborn.TwoWeaponDefense.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.TwoWeaponFighting).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Dexterity, 15)
            .AddPrerequisiteFeature(BlueprintIds.TwoWeaponFighting)
            .AddFeatureTagsComponent(FeatureTag.Defense)
            .AddComponent(new TwoWeaponDefenseComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureArmorOfThePit() {
        var configurator = FeatureConfigurator.New(
                "ClassesRebornArmorOfThePit",
                BlueprintIds.ArmorOfThePitFeat)
            .SetDisplayName("ClassesReborn.ArmorOfThePit.Name")
            .SetDescription("ClassesReborn.ArmorOfThePit.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.TieflingHeritages[2]).Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetReapplyOnLevelUp(true)
            .AddFeatureTagsComponent(FeatureTag.Defense)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 2);
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = BlueprintIds.TieflingHeritages
                .Select(BlueprintTool.GetRef<BlueprintFeatureReference>)
                .ToArray(),
            Amount = 1,
            CheckInProgression = true,
        });
        AddAsFeat(configurator.Configure());
    }

    private static void ConfigureGreaterUnarmedStrike() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornGreaterUnarmedStrike",
                BlueprintIds.GreaterUnarmedStrikeFeat)
            .SetDisplayName("ClassesReborn.GreaterUnarmedStrike.Name")
            .SetDescription("ClassesReborn.GreaterUnarmedStrike.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.ImprovedUnarmedStrike).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteCharacterLevel(3)
            .AddPrerequisiteFeature(BlueprintIds.ImprovedUnarmedStrike)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Damage |
                                     FeatureTag.Melee)
            .AddComponent(new GreaterUnarmedStrikeComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureDervishDance() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornDervishDance",
                BlueprintIds.DervishDanceFeat)
            .SetDisplayName("ClassesReborn.DervishDance.Name")
            .SetDescription("ClassesReborn.DervishDance.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFinesse).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteStatValue(StatType.Dexterity, 13)
            .AddPrerequisiteStatValue(StatType.SkillMobility, 2)
            .AddPrerequisiteFeature(BlueprintIds.WeaponFinesse)
            .AddPrerequisiteProficiency(
                Array.Empty<ArmorProficiencyGroup>(),
                new[] { WeaponCategory.Scimitar })
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new WeaponCategoryAttackAndDamageStatReplacement {
                ReplacementStat = StatType.Dexterity,
                Categories = new[] { WeaponCategory.Scimitar },
                RequireFreeOffHand = true,
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureQuickStudy() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ArcanistExploitSelection).Icon;
        var ability = AbilityConfigurator.New(
                "ClassesRebornQuickStudyAbility",
                BlueprintIds.QuickStudyAbility)
            .SetDisplayName("ClassesReborn.QuickStudy.Name")
            .SetDescription("ClassesReborn.QuickStudy.Description")
            .SetIcon(icon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetIsFullRoundAction(true)
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddAbilityResourceLogic(
                amount: 1,
                isSpendResource: true,
                requiredResource: BlueprintIds.ArcanistReservoirResource)
            .AddComponent(new QuickStudyComponent {
                AnySpellLevel = true,
                CharacterClass = new[] {
                    BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                        BlueprintIds.ArcanistClass),
                    BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                        BlueprintIds.WizardClass),
                },
                Archetypes = new[] {
                    BlueprintTool.GetRef<BlueprintArchetypeReference>(
                        BlueprintIds.ExploiterWizardArchetype),
                },
            })
            .Configure();
        ability.Hidden = true;
        ability.ShowNameForVariant = true;

        var feature = FeatureConfigurator.New(
                "ClassesRebornQuickStudy",
                BlueprintIds.QuickStudyFeature)
            .SetDisplayName("ClassesReborn.QuickStudy.Name")
            .SetDescription("ClassesReborn.QuickStudy.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.ArcanistExploit)
            .SetIsClassFeature(true)
            .AddFacts(new() { BlueprintIds.QuickStudyAbility })
            .Configure();
        AddToSelection(BlueprintIds.ArcanistExploitSelection, feature);
    }

    private static void ConfigureMadMagic() {
        var exceptions = new[] {
            StandardRageBuff,
            FocusedRageBuff,
            InspiredRageBuff,
            RageSpellBuff,
            InfectiousRageBuff,
            BloodragerStandardRageBuff,
            BlueprintIds.BloodragerGreaterBloodrageBuff,
            BloodragerMightyRageBuff,
        }.Select(BlueprintTool.GetRef<BlueprintBuffReference>).ToArray();

        var feat = FeatureConfigurator.New(
                "ClassesRebornMadMagic",
                BlueprintIds.MadMagicFeat)
            .SetDisplayName("ClassesReborn.MadMagic.Name")
            .SetDescription("ClassesReborn.MadMagic.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.BloodragerRageFeature).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteFeature(BlueprintIds.BloodragerRageFeature)
            .AddFeatureTagsComponent(FeatureTag.Magic | FeatureTag.ClassSpecific)
            .AddComponent(new AddConditionExceptions {
                Condition = UnitCondition.SpellcastingForbidden,
                Exception = new UnitConditionExceptionsFromBuff {
                    Exceptions = exceptions,
                },
            })
            .AddComponent(new MadMagicGreaterBloodrageDC {
                m_GreaterBloodrage = BlueprintTool.GetRef<BlueprintUnitFactReference>(
                    BlueprintIds.BloodragerGreaterBloodrage),
                m_GreaterBloodrageBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.BloodragerGreaterBloodrageBuff),
            })
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureCrusadersFlurry() {
        var configurator = FeatureConfigurator.New(
                "ClassesRebornCrusadersFlurry",
                BlueprintIds.CrusadersFlurryFeat)
            .SetDisplayName("ClassesReborn.CrusadersFlurry.Name")
            .SetDescription("ClassesReborn.CrusadersFlurry.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.MonkFlurryOfBlows).Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteFeature(BlueprintIds.WeaponFocus)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.ClassSpecific)
            .AddComponent(new CrusadersFlurryComponent());
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = new[] {
                BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.MonkFlurryUnlock),
                BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.ScaledFistFlurryUnlock),
            },
            Amount = 1,
        });
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = ChannelEnergyFeatures.Select(
                    BlueprintTool.GetRef<BlueprintFeatureReference>)
                .ToArray(),
            Amount = 1,
        });
        AddAsFeat(configurator.Configure());
    }

    private static void ConfigureRimeSpell() {
        var entangledSource = BlueprintTool.Get<BlueprintBuff>(
            "c53b286bb06a0544c85ca0f8bcc86950");
        BuffConfigurator.New(
                "ClassesRebornRimeEntangledBuff",
                BlueprintIds.RimeEntangledBuff)
            .SetDisplayName("ClassesReborn.RimeSpell.Entangled.Name")
            .SetDescription("ClassesReborn.RimeSpell.Entangled.Description")
            .SetIcon(entangledSource.Icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .AddComponent(new SpellDescriptorComponent {
                Descriptor = SpellDescriptor.Cold | SpellDescriptor.MovementImpairing,
            })
            .AddComponent(new AddCondition { Condition = UnitCondition.Entangled })
            .AddComponent(new RemoveWhenCombatEnded())
            .Configure();

        var feat = FeatureConfigurator.New(
                "ClassesRebornRimeSpell",
                BlueprintIds.RimeSpellFeat)
            .SetDisplayName("ClassesReborn.RimeSpell.Name")
            .SetDescription("ClassesReborn.RimeSpell.Description")
            .SetIcon(entangledSource.Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.WizardFeat)
            .SetReapplyOnLevelUp(true)
            .AddFeatureTagsComponent(FeatureTag.Magic | FeatureTag.Metamagic)
            .AddComponent(new AddMetamagicFeat {
                Metamagic = RimeMetamagicExtension.Rime,
            })
            .AddComponent(new RimeSpellTrigger {
                m_EntangledBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.RimeEntangledBuff),
            })
            .Configure();
        AddAsFeat(feat);
        RimeMetamagicExtension.EnableOnColdSpells();
    }

    private static void ConfigureDesnasShootingStar() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornDesnasShootingStar",
                BlueprintIds.DesnasShootingStarFeat)
            .SetDisplayName("ClassesReborn.DesnasShootingStar.Name")
            .SetDescription("ClassesReborn.DesnasShootingStar.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DesnaFeature).Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteFeature(BlueprintIds.DesnaFeature)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
            .AddComponent(new WeaponCategoryAttackAndDamageStatReplacement {
                ReplacementStat = StatType.Charisma,
                Categories = new[] { WeaponCategory.Starknife },
            })
            .Configure();
        AddAsFeat(feat);
    }

    private static void ConfigureBladedBrush() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornBladedBrush",
                BlueprintIds.BladedBrushFeat)
            .SetDisplayName("ClassesReborn.BladedBrush.Name")
            .SetDescription("ClassesReborn.BladedBrush.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ShelynFeature).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteFeature(BlueprintIds.ShelynFeature)
            .AddPrerequisiteParametrizedWeaponFeature(
                BlueprintIds.WeaponFocus,
                WeaponCategory.Glaive)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new WeaponCategoryAttackStatReplacement {
                ReplacementStat = StatType.Dexterity,
                Categories = new[] { WeaponCategory.Glaive },
            })
            .AddComponent(new BladedBrushComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureAsceticStyleChain() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ImprovedUnarmedStrike).Icon;
        var style = CreateAsceticFeat(
            "ClassesRebornAsceticStyle",
            BlueprintIds.AsceticStyleFeat,
            "ClassesReborn.AsceticStyle.Name",
            "ClassesReborn.AsceticStyle.Description",
            icon,
            BlueprintIds.WeaponFocus,
            1,
            new AsceticStyleComponent());
        var form = CreateAsceticFeat(
            "ClassesRebornAsceticForm",
            BlueprintIds.AsceticFormFeat,
            "ClassesReborn.AsceticForm.Name",
            "ClassesReborn.AsceticForm.Description",
            icon,
            BlueprintIds.AsceticStyleFeat,
            5,
            new AsceticFormComponent());
        var strike = CreateAsceticFeat(
            "ClassesRebornAsceticStrike",
            BlueprintIds.AsceticStrikeFeat,
            "ClassesReborn.AsceticStrike.Name",
            "ClassesReborn.AsceticStrike.Description",
            icon,
            BlueprintIds.AsceticFormFeat,
            7,
            new AsceticStrikeComponent());
        if (Main.Settings.AsceticStyle) {
            AddAsFeat(style, combatFeat: true);
        }
        if (Main.Settings.AsceticForm) {
            AddAsFeat(form, combatFeat: true);
        }
        if (Main.Settings.AsceticStrike) {
            AddAsFeat(strike, combatFeat: true);
        }
    }

    private static BlueprintParametrizedFeature CreateAsceticFeat(
        string blueprintName,
        string id,
        string displayName,
        string description,
        UnityEngine.Sprite icon,
        string prerequisite,
        int requiredLevel,
        BlueprintComponent component) {
        var configurator = ParametrizedFeatureConfigurator.New(blueprintName, id)
            .SetDisplayName(displayName)
            .SetDescription(description)
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .SetParameterType(FeatureParameterType.WeaponCategory)
            .SetWeaponSubCategory(WeaponSubCategory.Monk)
            .SetRequireProficiency(true)
            .SetPrerequisite(prerequisite)
            .SetReapplyOnLevelUp(true)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(component);
        configurator.AddComponent(new PrerequisiteClassLevel {
            m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                BlueprintIds.MonkClass),
            Level = requiredLevel,
            Group = Prerequisite.GroupType.Any,
        });
        configurator.AddComponent(new PrerequisiteStatValue {
            Stat = StatType.BaseAttackBonus,
            Value = requiredLevel,
            Group = Prerequisite.GroupType.Any,
        });
        return configurator.Configure();
    }

    private static void ConfigureFeyFoundling() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornFeyFoundling",
                BlueprintIds.FeyFoundlingFeat)
            .SetDisplayName("ClassesReborn.FeyFoundling.Name")
            .SetDescription("ClassesReborn.FeyFoundling.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.DesnaFeature).Icon)
            .SetGroups(FeatureGroup.Feat)
            .AddFeatureTagsComponent(FeatureTag.Defense)
            .AddComponent(new PrerequisiteFirstCharacterLevel())
            .AddComponent(new FeyFoundlingHealing())
            .AddComponent(new FeyFoundlingColdIronVulnerability())
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Death,
                value: 2,
                modifierDescriptor: ModifierDescriptor.UntypedStackable)
            .Configure();
        AddAsFeat(feat);
    }

    private static void ConfigureViciousStomp() {
        var feat = FeatureConfigurator.New(
                "ClassesRebornViciousStomp",
                BlueprintIds.ViciousStompFeat)
            .SetDisplayName("ClassesReborn.ViciousStomp.Name")
            .SetDescription("ClassesReborn.ViciousStomp.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.ImprovedUnarmedStrike).Icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.CombatFeat)
            .AddPrerequisiteFeature(BlueprintIds.CombatReflexes)
            .AddPrerequisiteFeature(BlueprintIds.ImprovedUnarmedStrike)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
            .AddComponent(new ViciousStompComponent())
            .Configure();
        AddAsFeat(feat, combatFeat: true);
    }

    private static void ConfigureUnsanctionedKnowledge() {
        var selections = Enumerable.Range(1, 4)
            .Select(CreateUnsanctionedKnowledgeSelection)
            .ToArray();
        var progression = ProgressionConfigurator.New(
                "ClassesRebornUnsanctionedKnowledge",
                BlueprintIds.UnsanctionedKnowledgeFeat)
            .SetDisplayName("ClassesReborn.UnsanctionedKnowledge.Name")
            .SetDescription("ClassesReborn.UnsanctionedKnowledge.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.WarpriestDeitySacredWeaponFeature).Icon)
            .SetGroups(FeatureGroup.Feat)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(true)
            .AddPrerequisiteClassSpellLevel(
                BlueprintIds.PaladinClass,
                requiredSpellLevel: 1)
            .AddPrerequisiteStatValue(StatType.Intelligence, 13)
            .Configure();
        progression.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = selections.Select(selection =>
                        selection.ToReference<BlueprintFeatureBaseReference>())
                    .ToList(),
            },
        };
        AddAsFeat(progression);
    }

    private static BlueprintFeatureSelection CreateUnsanctionedKnowledgeSelection(
        int spellLevel) {
        var bard = CreateUnsanctionedKnowledgeSpellFeature(
            spellLevel,
            "Bard",
            BlueprintIds.BardSpellList,
            BlueprintIds.UnsanctionedKnowledgeBardFeatures[spellLevel - 1]);
        var cleric = CreateUnsanctionedKnowledgeSpellFeature(
            spellLevel,
            "Cleric",
            BlueprintIds.ClericSpellList,
            BlueprintIds.UnsanctionedKnowledgeClericFeatures[spellLevel - 1]);
        var inquisitor = CreateUnsanctionedKnowledgeSpellFeature(
            spellLevel,
            "Inquisitor",
            BlueprintIds.InquisitorSpellList,
            BlueprintIds.UnsanctionedKnowledgeInquisitorFeatures[spellLevel - 1]);
        var ids = new[] {
            BlueprintIds.UnsanctionedKnowledgeSelection1,
            BlueprintIds.UnsanctionedKnowledgeSelection2,
            BlueprintIds.UnsanctionedKnowledgeSelection3,
            BlueprintIds.UnsanctionedKnowledgeSelection4,
        };
        return FeatureSelectionConfigurator.New(
                $"ClassesRebornUnsanctionedKnowledgeSelection{spellLevel}",
                ids[spellLevel - 1])
            .SetDisplayName($"ClassesReborn.UnsanctionedKnowledge.Level{spellLevel}.Name")
            .SetDescription("ClassesReborn.UnsanctionedKnowledge.Description")
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            .AddToAllFeatures(bard, cleric, inquisitor)
            .Configure();
    }

    private static BlueprintParametrizedFeature CreateUnsanctionedKnowledgeSpellFeature(
        int spellLevel,
        string sourceName,
        string spellListId,
        string id) {
        var spellList = BlueprintTool.Get<BlueprintSpellList>(spellListId);
        var spells = spellList.SpellsByLevel
            .First(level => level.SpellLevel == spellLevel)
            .Spells
            .Select(spell => (Blueprint<AnyBlueprintReference>)spell)
            .ToArray();
        return ParametrizedFeatureConfigurator.New(
                $"ClassesRebornUnsanctionedKnowledge{sourceName}{spellLevel}",
                id)
            .SetDisplayName($"ClassesReborn.UnsanctionedKnowledge.{sourceName}.Name")
            .SetDescription("ClassesReborn.UnsanctionedKnowledge.Description")
            .SetIsClassFeature(true)
            .SetParameterType(FeatureParameterType.LearnSpell)
            .SetSpellList(spellListId)
            .SetSpellcasterClass(BlueprintIds.PaladinClass)
            .SetSpecificSpellLevel(true)
            .SetSpellLevel(spellLevel)
            .SetBlueprintParameterVariants(spells)
            .AddLearnSpellParametrized(
                specificSpellLevel: true,
                spellcasterClass: BlueprintIds.PaladinClass,
                spellLevel: spellLevel,
                spellList: spellListId)
            .Configure();
    }
}
