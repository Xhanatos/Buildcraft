using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

/// <summary>
/// Exposes Owlcat's native Mongrel race for player creation and turns its
/// native rules into named racial features so alternate heritages can replace
/// them cleanly.
/// </summary>
internal static class MongrelRaceRebalance {
    // Owlcat's Mongrel blueprint is marked as Human because it was never
    // selectable. Once exposed alongside the real Human race, that duplicate
    // identity makes ProgressionRoot.HumanRace throw on every animation tick.
    // Catfolk is the only defined race identity unused by Wrath's player races.
    private const Race MongrelRaceIdentity = Race.Catfolk;
    private const string ShinyIrisEquipmentEntity =
        "a47bac4deb099fc4b86a2e01bb425cc5";
    private static readonly string[] MaleMutationEquipmentEntities = {
        "65d4388247109a841827b6ddd312a426", // Mongrel ears 01
        "b5b2e84af4b3f4e429efaba22fb53789", // Mongrel ears 02
        "7ae57d4a307b8cc47acd57cf56c99b5e", // Mongrel horns 01
        "fed897cada6bd7e4196260cd6543e31b", // Mongrel horns 02
        "e900a1a0ecd6d754ea05b63fb38334bb", // Mongrel horns 03
        "3c1269cc50173ec4bbcaca741205ccd5", // Mongrel horns 04
        "7ee2612f73b04724a8f145430ce67c2d", // Mongrel horns 05
        ShinyIrisEquipmentEntity,
    };
    private static readonly string[] FemaleMutationEquipmentEntities = {
        "be14de00304a17c4cb88dc78c377eb2a", // Mongrel ears 01
        "88fcdcdc8e9e8d24f9a7d7c91a6054a5", // Mongrel ears 02
        "784bb887aceb5a045996974a71e78875", // Mongrel horns 01
        "ce26bc3e359a0b84c9095b2a012bc877", // Mongrel horns 02
        "10d6c6e9b22b3a64bb8fdca64b77ed2a", // Mongrel horns 03
        "67a781ca294d40441b5ae7a81330ca83", // Mongrel horns 04
        "fe2d8bf332b38cf459ea989490228099", // Mongrel horns 05
        ShinyIrisEquipmentEntity,
    };

    internal static string ResilienceId =>
        FutureContentIds.Get("Race.Mongrel.NaturalArmor");
    internal static string UndergroundSurvivorId =>
        FutureContentIds.Get("Race.Mongrel.UndergroundSurvivor");
    internal static string SoundMimicryId =>
        FutureContentIds.Get("Race.Mongrel.SoundMimicry");

    internal static void Configure() {
        var race = BlueprintTool.Get<BlueprintRace>(BlueprintIds.MongrelRace);
        var human = BlueprintTool.Get<BlueprintRace>(BlueprintIds.HumanRace);

        AssignUniqueRaceIdentity(race);
        race.SelectableRaceStat = false;
        RemoveNativeNaturalArmor(race);
        var baseFeatures = ConfigureVisibleRaceFeatures(race);
        if (Main.Settings.MongrelAlternateHeritages) {
            ConfigureHeritages();
        }
        ConfigureAppearance(race, human);
        if (Main.Settings.MongrelRace) {
            ExposePlayableRace(race);
        }
        ValidateNativeRace(race, baseFeatures);
    }

    private static void AssignUniqueRaceIdentity(BlueprintRace race) {
        race.RaceId = MongrelRaceIdentity;
        foreach (var presetReference in race.m_Presets ??
                     Array.Empty<BlueprintRaceVisualPresetReference>()) {
            var preset = presetReference?.Get();
            if (preset != null) {
                preset.RaceId = MongrelRaceIdentity;
            }
        }
    }

    private static void RemoveNativeNaturalArmor(BlueprintRace race) {
        race.ComponentsArray = (race.ComponentsArray ??
                Array.Empty<BlueprintComponent>())
            .Where(component => component is not AddStatBonus statBonus ||
                statBonus.Stat != StatType.AC || statBonus.Value != 2 ||
                statBonus.Descriptor != ModifierDescriptor.NaturalArmor)
            .ToArray();
    }

    private static BlueprintFeature[] ConfigureVisibleRaceFeatures(
        BlueprintRace race) {
        var resilience = FeatureConfigurator.New(
                "ClassesRebornMongrelNaturalArmorFeature",
                ResilienceId)
            .SetDisplayName("ClassesReborn.Mongrel.NaturalArmor.Name")
            .SetDescription("ClassesReborn.Mongrel.NaturalArmor.Description")
            .SetIcon(FeatureRefs.ImprovedNaturalArmor.Reference.Get().Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 2)
            .Configure();
        var undergroundSurvivor = FeatureConfigurator.New(
                "ClassesRebornMongrelUndergroundSurvivorFeature",
                UndergroundSurvivorId)
            .SetDisplayName("ClassesReborn.Mongrel.UndergroundSurvivor.Name")
            .SetDescription("ClassesReborn.Mongrel.UndergroundSurvivor.Description")
            .SetIcon(FeatureRefs.SkillFocusStealth.Reference.Get().Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillStealth,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillThievery,
                value: 2)
            .Configure();
        var soundMimicry = FeatureConfigurator.New(
                "ClassesRebornMongrelSoundMimicryFeature",
                SoundMimicryId)
            .SetDisplayName("ClassesReborn.Mongrel.SoundMimicry.Name")
            .SetDescription("ClassesReborn.Mongrel.SoundMimicry.Description")
            .SetIcon(FeatureRefs.Alertness.Reference.Get().Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillPersuasion,
                value: 1)
            .AddClassSkill(StatType.SkillPersuasion)
            .Configure();

        var additions = new[] { resilience, undergroundSurvivor, soundMimicry };
        var additionIds = additions.Select(feature => feature.AssetGuid).ToHashSet();
        race.m_Features = (race.m_Features ??
                Array.Empty<BlueprintFeatureBaseReference>())
            .Where(reference => !additionIds.Contains(reference.deserializedGuid))
            .Concat(additions.Select(feature =>
                feature.ToReference<BlueprintFeatureBaseReference>()))
            .ToArray();
        return additions;
    }

    private static void ConfigureHeritages() {
        var naturalArmorIcon = FeatureRefs.ImprovedNaturalArmor.Reference.Get().Icon;
        var perceptionIcon = FeatureRefs.SkillFocusPerception.Reference.Get().Icon;
        var movementIcon = FeatureRefs.AcrobaticMovement.Reference.Get().Icon;
        var weaponIcon = FeatureRefs.MartialWeaponProficiency.Reference.Get().Icon;
        var athleticsIcon = FeatureRefs.Dodge.Reference.Get().Icon;
        var biteIcon = FeatureRefs
            .BloodlineSerpentineSerpentsFangBiteFeatureAddLevel1
            .Reference.Get().Icon;

        FeatureConfigurator.New(
                "ClassesRebornMongrelChitinPlatedHeritage",
                FutureContentIds.Get("Heritage.Mongrel.ChitinPlated"))
            .SetDisplayName("ClassesReborn.Mongrel.ChitinPlated.Name")
            .SetDescription("ClassesReborn.Mongrel.ChitinPlated.Description")
            .SetIcon(naturalArmorIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 3)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillMobility,
                value: -2)
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornMongrelKeenEaredHeritage",
                FutureContentIds.Get("Heritage.Mongrel.KeenEared"))
            .SetDisplayName("ClassesReborn.Mongrel.KeenEared.Name")
            .SetDescription("ClassesReborn.Mongrel.KeenEared.Description")
            .SetIcon(perceptionIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillPerception,
                value: 4)
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornMongrelHoovedRunnerHeritage",
                FutureContentIds.Get("Heritage.Mongrel.HoovedRunner"))
            .SetDisplayName("ClassesReborn.Mongrel.HoovedRunner.Name")
            .SetDescription("ClassesReborn.Mongrel.HoovedRunner.Description")
            .SetIcon(movementIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.Speed,
                value: 5)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillMobility,
                value: 2)
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornMongrelFirstCrusadeScionHeritage",
                FutureContentIds.Get("Heritage.Mongrel.FirstCrusadeScion"))
            .SetDisplayName("ClassesReborn.Mongrel.FirstCrusadeScion.Name")
            .SetDescription("ClassesReborn.Mongrel.FirstCrusadeScion.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFocus).Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Fear,
                value: 2,
                modifierDescriptor: ModifierDescriptor.Racial)
            .AddComponent(new EvilOutsiderDamageBonus {
                m_OutsiderType = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    FeatureRefs.OutsiderType.ToString()),
                m_EvilSubtype = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    FeatureRefs.SubtypeEvil.ToString()),
                Bonus = 1,
            })
            .Configure();

        ConfigureAdaptiveLineage(weaponIcon);

        FeatureConfigurator.New(
                "ClassesRebornMongrelCliffbornHeritage",
                FutureContentIds.Get("Heritage.Mongrel.Cliffborn"))
            .SetDisplayName("ClassesReborn.Mongrel.Cliffborn.Name")
            .SetDescription("ClassesReborn.Mongrel.Cliffborn.Description")
            .SetIcon(athleticsIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillAthletics,
                value: 2)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillMobility,
                value: 2)
            .AddComponent(new BouncyTripDefense { Bonus = 2 })
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornMongrelFangedOffshootHeritage",
                FutureContentIds.Get("Heritage.Mongrel.FangedOffshoot"))
            .SetDisplayName("ClassesReborn.Mongrel.FangedOffshoot.Name")
            .SetDescription("ClassesReborn.Mongrel.FangedOffshoot.Description")
            .SetIcon(biteIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddAdditionalLimb(BlueprintIds.Bite1d4)
            .AddComponent(new DemoralizeTraitBonus {
                Bonus = 2,
                BonusDescriptor = ModifierDescriptor.Racial,
            })
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornMongrelCrushingLimbsHeritage",
                FutureContentIds.Get("Heritage.Mongrel.CrushingLimbs"))
            .SetDisplayName("ClassesReborn.Mongrel.CrushingLimbs.Name")
            .SetDescription("ClassesReborn.Mongrel.CrushingLimbs.Description")
            .SetIcon(biteIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddAdditionalLimb(ItemWeaponRefs.Slam1d4.ToString())
            .Configure();
    }

    private static void ConfigureAdaptiveLineage(UnityEngine.Sprite icon) {
        BlueprintParametrizedFeature CreateWeaponChoice(
            string suffix,
            WeaponSubCategory category) =>
            ParametrizedFeatureConfigurator.New(
                    $"ClassesRebornMongrelAdaptiveLineage{suffix}",
                    FutureContentIds.Get($"Heritage.Mongrel.AdaptiveLineage.{suffix}"))
                .SetDisplayName($"ClassesReborn.Mongrel.AdaptiveLineage.{suffix}.Name")
                .SetDescription("ClassesReborn.Mongrel.AdaptiveLineage.Description")
                .SetIcon(icon)
                .SetRanks(1)
                .SetReapplyOnLevelUp(true)
                .SetParameterType(FeatureParameterType.WeaponCategory)
                .SetWeaponSubCategory(category)
                .SetRequireProficiency(false)
                .AddComponent(new AdaptiveLineageWeaponTraining { Bonus = 1 })
                .Configure();

        var martial = CreateWeaponChoice("Martial", WeaponSubCategory.Martial);
        var exotic = CreateWeaponChoice("Exotic", WeaponSubCategory.Exotic);
        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornMongrelAdaptiveLineageSelection",
                FutureContentIds.Get("Heritage.Mongrel.AdaptiveLineage.Selection"))
            .SetDisplayName("ClassesReborn.Mongrel.AdaptiveLineage.Choice.Name")
            .SetDescription("ClassesReborn.Mongrel.AdaptiveLineage.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        selection.m_AllFeatures = new[] { martial, exotic }
            .Select(feature => feature.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();

        var progression = ProgressionConfigurator.New(
                "ClassesRebornMongrelAdaptiveLineageHeritage",
                FutureContentIds.Get("Heritage.Mongrel.AdaptiveLineage"))
            .SetDisplayName("ClassesReborn.Mongrel.AdaptiveLineage.Name")
            .SetDescription("ClassesReborn.Mongrel.AdaptiveLineage.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(false)
            .Configure();
        progression.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = new List<BlueprintFeatureBaseReference> {
                    selection.ToReference<BlueprintFeatureBaseReference>(),
                },
            },
        };
    }

    private static void ConfigureAppearance(
        BlueprintRace race,
        BlueprintRace human) {
        if (human.MaleOptions == null || human.FemaleOptions == null) {
            throw new InvalidOperationException(
                "Human customization options are unavailable for Mongrel character creation.");
        }

        // Human-compatible heads, hair, and facial options remain available,
        // but Mongrels receive independent option containers. This is important:
        // mutating the Human containers directly would add these deformities to
        // Human character creation as well.
        race.MaleOptions = CreateMongrelOptions(
            human.MaleOptions,
            MaleMutationEquipmentEntities);
        race.FemaleOptions = CreateMongrelOptions(
            human.FemaleOptions,
            FemaleMutationEquipmentEntities);

        Main.Log.Log(
            $"Mongrel appearance customization enabled with " +
            $"{MaleMutationEquipmentEntities.Length} male and " +
            $"{FemaleMutationEquipmentEntities.Length} female mutation choices.");
    }

    private static Kingmaker.Blueprints.CharGen.CustomizationOptions
        CreateMongrelOptions(
            Kingmaker.Blueprints.CharGen.CustomizationOptions source,
            IReadOnlyCollection<string> mutationAssetIds) {
        var mutations = mutationAssetIds
            .Select(assetId => new EquipmentEntityLink { AssetId = assetId });
        var horns = (source.Horns ?? Array.Empty<EquipmentEntityLink>())
            .Concat(mutations)
            .GroupBy(link => link.AssetId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        return new Kingmaker.Blueprints.CharGen.CustomizationOptions {
            m_Heads = source.m_Heads?.ToArray() ??
                Array.Empty<EquipmentEntityLink>(),
            m_HeadsCache = null,
            m_Eyebrows = source.m_Eyebrows?.ToArray() ??
                Array.Empty<EquipmentEntityLink>(),
            m_EyebrowsCache = null,
            m_Hair = source.m_Hair?.ToArray() ??
                Array.Empty<EquipmentEntityLink>(),
            m_HairCache = null,
            Beards = source.Beards?.ToArray() ??
                Array.Empty<EquipmentEntityLink>(),
            Horns = horns,
            TailSkinColors = source.TailSkinColors?.ToArray() ??
                Array.Empty<EquipmentEntityLink>(),
        };
    }

    private static void ExposePlayableRace(BlueprintRace race) {
        var races = (BlueprintRoot.Instance.Progression.m_CharacterRaces ??
                Array.Empty<BlueprintRaceReference>())
            .Where(reference => reference?.deserializedGuid != race.AssetGuid)
            .ToList();
        races.Add(race.ToReference<BlueprintRaceReference>());
        BlueprintRoot.Instance.Progression.m_CharacterRaces = races.ToArray();
    }

    private static void ValidateNativeRace(
        BlueprintRace race,
        IReadOnlyCollection<BlueprintFeature> baseFeatures) {
        var statBonuses = race.GetComponents<AddStatBonus>().ToArray();
        var playerRaceCount = BlueprintRoot.Instance.Progression.m_CharacterRaces
            .Count(reference => reference?.deserializedGuid == race.AssetGuid);
        var humanIdentityCount = BlueprintRoot.Instance.Progression.m_CharacterRaces
            .Count(reference => reference?.Get()?.RaceId == Race.Human);
        var mongrelIdentityCount = BlueprintRoot.Instance.Progression.m_CharacterRaces
            .Count(reference => reference?.Get()?.RaceId == MongrelRaceIdentity);
        var listedFeatureIds = (race.m_Features ??
                Array.Empty<BlueprintFeatureBaseReference>())
            .Select(reference => reference.deserializedGuid)
            .ToArray();

        if (race.Size != Size.Medium ||
            race.RaceId != MongrelRaceIdentity ||
            race.SelectableRaceStat ||
            race.m_Presets == null || race.m_Presets.Length < 3 ||
            race.m_Presets.Any(reference => {
                var preset = reference?.Get();
                return preset == null || preset.Skin == null ||
                    preset.RaceId != MongrelRaceIdentity ||
                    preset.MaleSkeleton == null || preset.FemaleSkeleton == null;
            }) ||
            race.MaleOptions?.m_Heads?.Length < 1 ||
            race.FemaleOptions?.m_Heads?.Length < 1 ||
            !ContainsAllMutations(
                race.MaleOptions?.Horns,
                MaleMutationEquipmentEntities) ||
            !ContainsAllMutations(
                race.FemaleOptions?.Horns,
                FemaleMutationEquipmentEntities) ||
            statBonuses.Count(component =>
                component.Stat == StatType.Strength &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            statBonuses.Count(component =>
                component.Stat == StatType.Dexterity &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            statBonuses.Any(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.NaturalArmor) ||
            baseFeatures.Count != 3 ||
            baseFeatures.Any(feature =>
                listedFeatureIds.Count(id => id == feature.AssetGuid) != 1) ||
            baseFeatures.Single(feature =>
                    feature.AssetGuid.ToString() == ResilienceId)
                .GetComponents<AddStatBonus>()
                .Count(component => component.Stat == StatType.AC &&
                    component.Value == 2 &&
                    component.Descriptor == ModifierDescriptor.NaturalArmor) != 1 ||
            playerRaceCount != (Main.Settings.MongrelRace ? 1 : 0) ||
            humanIdentityCount != 1 ||
            mongrelIdentityCount != (Main.Settings.MongrelRace ? 1 : 0)) {
            throw new InvalidOperationException(
                "Mongrel must be a single optional playable Medium race with a unique identity, complete Human-compatible visuals, fixed +2 Strength and Dexterity, and exactly three modular base racial features.");
        }
    }

    private static bool ContainsAllMutations(
        IEnumerable<EquipmentEntityLink> options,
        IEnumerable<string> expectedAssetIds) {
        var actual = (options ?? Array.Empty<EquipmentEntityLink>())
            .Select(link => link?.AssetId)
            .Where(assetId => !string.IsNullOrEmpty(assetId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return expectedAssetIds.All(actual.Contains);
    }
}
