using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.Configurators.CharGen;
using BlueprintCore.Blueprints.Configurators.Visual;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class GoblinRaceRebalance {
    private const string GoblinBodyEquipmentEntity =
        "c416ef4dcb3359c41b0cb2c7cc1a7e7f";
    private const string GoblinHeadEquipmentEntity =
        "6214b16cb1f1fe645b2177bd3af813c9";
    private const string HalflingMaleHeadPaletteSource =
        "1e4cafa72c2a2f5468c83868873f31ec";
    private const string HalflingMaleBodyPaletteSource =
        "d509ad2a15110a34cb793fec7c26214c";
    private const string KitsuneMaleHeadColorSource =
        "f899a03df5f1be5448a40693a7ce3572";
    private const string GoblinGreenRamp = "CR_Armor_Green2";
    private const string GoblinRedEyeRamp = "CR_Armor_Red1";
    private static readonly string[] HalflingVisualPresets = {
        "639cebe8cbc098345af429d62fd79578",
        "4031e194544440c47a50ed30073bcf09",
        "f6db3ede6c67b84438e2b5609fcaf693",
    };

    internal static void Configure() {
        var race = BlueprintTool.Get<BlueprintRace>(BlueprintIds.GoblinRace);
        if (Main.Settings.GoblinRace) {
            ExposePlayableRace(race);
        }
        if (Main.Settings.GoblinAlternateHeritages) {
            ConfigureHeritages(race);
        }
        ConfigureAppearance(race);
        ValidateNativeRace(race);
    }

    private static void ConfigureAppearance(BlueprintRace race) {
        var goblinBody = CreateGoblinBodyWithPlayerPalettes();
        var skin = KingmakerEquipmentEntityConfigurator.New(
                "ClassesRebornGoblinBody",
                FutureContentIds.Get("Race.Goblin.Body"))
            .SetMaleArray(goblinBody.AssetId)
            .SetFemaleArray(goblinBody.AssetId)
            .SetRaceDependent(false)
            .Configure();

        race.m_Presets = HalflingVisualPresets
            .Select((sourcePresetId, index) => {
                var sourcePreset = BlueprintTool.Get<BlueprintRaceVisualPreset>(
                    sourcePresetId);
                var preset = RaceVisualPresetConfigurator.New(
                        $"ClassesRebornGoblinVisualPreset{index + 1}",
                        FutureContentIds.Get($"Race.Goblin.VisualPreset.{index + 1}"))
                    .CopyFrom(sourcePresetId)
                    .SetRaceId(Race.Goblin)
                    .SetSkin(skin)
                    .Configure();

                // Owlcat only supplied modular male Goblin body and head assets.
                // Both gender choices must therefore use the matching small male
                // skeleton; using the copied female Halfling skeleton stretches the
                // Goblin head and ears vertically.
                preset.MaleSkeleton = sourcePreset.MaleSkeleton;
                preset.FemaleSkeleton = sourcePreset.MaleSkeleton;
                return preset.ToReference<BlueprintRaceVisualPresetReference>();
            })
            .ToArray();

        foreach (var presetReference in race.m_Presets) {
            RaceVisualPresetConfigurator.For(presetReference)
                .SetSkin(skin)
                .Configure();
        }

        race.MaleOptions ??= new();
        race.FemaleOptions ??= new();
        var goblinHead = CreateGoblinHeadWithPlayerPalettes();
        SetGoblinHead(race.MaleOptions, goblinHead);
        SetGoblinHead(race.FemaleOptions, goblinHead);
    }

    private static EquipmentEntityLink CreateGoblinBodyWithPlayerPalettes() {
        var goblinBody = new EquipmentEntityLink {
            AssetId = GoblinBodyEquipmentEntity,
        };
        var paletteSource = new EquipmentEntityLink {
            AssetId = HalflingMaleBodyPaletteSource,
        };
        EnablePlayerPalettes(goblinBody, paletteSource, "body");
        return goblinBody;
    }

    private static EquipmentEntityLink CreateGoblinHeadWithPlayerPalettes() {
        var goblinHead = new EquipmentEntityLink {
            AssetId = GoblinHeadEquipmentEntity,
        };
        var paletteSource = new EquipmentEntityLink {
            AssetId = HalflingMaleHeadPaletteSource,
        };
        EnablePlayerPalettes(goblinHead, paletteSource, "head and eyes");
        return goblinHead;
    }

    private static void EnablePlayerPalettes(
        EquipmentEntityLink goblinLink,
        EquipmentEntityLink paletteSourceLink,
        string assetLabel) {
        var goblinEntity = goblinLink.Load(
            ignorePreloadWarning: true,
            hold: true);
        var sourceEntity = paletteSourceLink.Load(
            ignorePreloadWarning: true,
            hold: true);
        var colorSourceEntity = new EquipmentEntityLink {
            AssetId = KitsuneMaleHeadColorSource,
        }.Load(ignorePreloadWarning: true, hold: true);
        if (goblinEntity == null || sourceEntity == null ||
            sourceEntity.PrimaryColorsProfile == null) {
            Main.Log.Error(
                $"Goblin {assetLabel} color support could not be loaded.");
            return;
        }

        var primaryProfile = UnityEngine.Object.Instantiate(
            sourceEntity.PrimaryColorsProfile);
        primaryProfile.name = $"Buildcraft_Goblin_{assetLabel}_PrimaryColors";
        primaryProfile.Ramps = sourceEntity.PrimaryColorsProfile.Ramps.ToList();
        AddNamedRamp(
            primaryProfile.Ramps,
            colorSourceEntity?.SecondaryRamps,
            GoblinGreenRamp);
        goblinEntity.PrimaryColorsProfile = primaryProfile;

        if (sourceEntity.SecondaryColorsProfile != null) {
            var secondaryProfile = UnityEngine.Object.Instantiate(
                sourceEntity.SecondaryColorsProfile);
            secondaryProfile.name =
                $"Buildcraft_Goblin_{assetLabel}_SecondaryColors";
            secondaryProfile.Ramps =
                sourceEntity.SecondaryColorsProfile.Ramps.ToList();
            AddNamedRamp(
                secondaryProfile.Ramps,
                colorSourceEntity?.SecondaryRamps,
                GoblinRedEyeRamp);
            goblinEntity.SecondaryColorsProfile = secondaryProfile;
        }
        goblinEntity.PrimaryColorsAvailableForPlayer = true;
        goblinEntity.SecondaryColorsAvailableForPlayer =
            goblinEntity.SecondaryColorsProfile != null;

        foreach (var pair in goblinEntity.BodyParts.Zip(
                     sourceEntity.BodyParts,
                     (goblinPart, sourcePart) =>
                         (Goblin: goblinPart, Source: sourcePart))) {
            var goblinPart = pair.Goblin;
            var sourcePart = pair.Source;
            foreach (var goblinTexture in goblinPart.Textures) {
                var sourceTexture = sourcePart.Textures.FirstOrDefault();
                if (sourceTexture == null) {
                    continue;
                }

                goblinTexture.UseRamp1Mask = sourceTexture.UseRamp1Mask;
                goblinTexture.UseRamp2Mask = sourceTexture.UseRamp2Mask;
                goblinTexture.UseDefaultMask1 = sourceTexture.UseDefaultMask1;
                goblinTexture.UseDefaultMask2 = sourceTexture.UseDefaultMask2;
                goblinTexture.DefaultMask1 = sourceTexture.DefaultMask1;
                goblinTexture.DefaultMask2 = sourceTexture.DefaultMask2;
                goblinTexture.Ramps = sourceTexture.Ramps;
            }
        }

        Main.Log.Log(
            $"Goblin {assetLabel} recoloring enabled with " +
            $"{goblinEntity.PrimaryRamps.Count} primary and " +
            $"{goblinEntity.SecondaryRamps.Count} secondary colors.");
    }

    private static void AddNamedRamp(
        ICollection<UnityEngine.Texture2D> destination,
        IEnumerable<UnityEngine.Texture2D> source,
        string rampName) {
        var ramp = source?.FirstOrDefault(candidate =>
            candidate != null && candidate.name == rampName);
        if (ramp == null) {
            Main.Log.Error($"Goblin color ramp {rampName} could not be loaded.");
            return;
        }
        if (!destination.Contains(ramp)) {
            destination.Add(ramp);
        }
    }

    private static void SetGoblinHead(
        Kingmaker.Blueprints.CharGen.CustomizationOptions options,
        EquipmentEntityLink goblinHead) {
        options.m_Heads = new[] { goblinHead };
        options.m_HeadsCache = null;
    }

    private static void ConfigureHeritages(BlueprintRace race) {
        var caveCrawlerIcon = FeatureRefs.CaveExplorersArmorFeature.Reference.Get().Icon;
        var cityScavengerIcon = FeatureRefs.UndergroundScavengerFeature.Reference.Get().Icon;
        var eatAnythingIcon = FeatureRefs.GreatFortitude.Reference.Get().Icon;
        var hardHeadBigTeethIcon = FeatureRefs
            .BloodlineSerpentineSerpentsFangBiteFeatureAddLevel1
            .Reference.Get().Icon;
        var overSizedEarsIcon = FeatureRefs.Alertness.Reference.Get().Icon;
        var treeRunnerIcon = FeatureRefs.AcrobaticMovement.Reference.Get().Icon;

        FeatureConfigurator.New(
                "ClassesRebornGoblinCaveCrawlerHeritage",
                FutureContentIds.Get("Heritage.Goblin.CaveCrawler"))
            .SetDisplayName("ClassesReborn.Goblin.CaveCrawler.Name")
            .SetDescription("ClassesReborn.Goblin.CaveCrawler.Description")
            .SetIcon(caveCrawlerIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.Speed,
                value: -10)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillAthletics,
                value: 8)
            .Configure();

        CreateSkillHeritage(
            "CityScavenger",
            cityScavengerIcon,
            (StatType.SkillPerception, 2),
            (StatType.SkillLoreNature, 2));

        FeatureConfigurator.New(
                "ClassesRebornGoblinEatAnythingHeritage",
                FutureContentIds.Get("Heritage.Goblin.EatAnything"))
            .SetDisplayName("ClassesReborn.Goblin.EatAnything.Name")
            .SetDescription("ClassesReborn.Goblin.EatAnything.Description")
            .SetIcon(eatAnythingIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: StatType.SkillLoreNature,
                value: 4)
            .AddSavingThrowBonusAgainstDescriptor(
                spellDescriptor: SpellDescriptor.Sickened |
                    SpellDescriptor.Nauseated,
                value: 4,
                modifierDescriptor: ModifierDescriptor.Racial)
            .Configure();

        FeatureConfigurator.New(
                "ClassesRebornGoblinHardHeadBigTeethHeritage",
                FutureContentIds.Get("Heritage.Goblin.HardHeadBigTeeth"))
            .SetDisplayName("ClassesReborn.Goblin.HardHeadBigTeeth.Name")
            .SetDescription("ClassesReborn.Goblin.HardHeadBigTeeth.Description")
            .SetIcon(hardHeadBigTeethIcon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .AddAdditionalLimb(BlueprintIds.Bite1d4)
            .Configure();

        CreateSkillHeritage(
            "OverSizedEars",
            overSizedEarsIcon,
            (StatType.SkillPerception, 4));
        CreateSkillHeritage(
            "TreeRunner",
            treeRunnerIcon,
            (StatType.SkillAthletics, 4),
            (StatType.SkillMobility, 4));
    }

    private static void CreateSkillHeritage(
        string key,
        UnityEngine.Sprite icon,
        params (StatType Stat, int Bonus)[] bonuses) {
        var configurator = FeatureConfigurator.New(
                $"ClassesRebornGoblin{key}Heritage",
                FutureContentIds.Get($"Heritage.Goblin.{key}"))
            .SetDisplayName($"ClassesReborn.Goblin.{key}.Name")
            .SetDescription($"ClassesReborn.Goblin.{key}.Description")
            .SetIcon(icon)
            .SetRanks(1)
            .SetIsClassFeature(false);
        foreach (var bonus in bonuses) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.Racial,
                stat: bonus.Stat,
                value: bonus.Bonus);
        }
        configurator.Configure();
    }

    private static void ExposePlayableRace(BlueprintRace race) {
        var races = (BlueprintRoot.Instance.Progression.m_CharacterRaces ??
                Array.Empty<BlueprintRaceReference>())
            .Where(reference => reference?.deserializedGuid != race.AssetGuid)
            .ToList();
        races.Add(race.ToReference<BlueprintRaceReference>());
        BlueprintRoot.Instance.Progression.m_CharacterRaces = races.ToArray();
    }

    private static void ValidateNativeRace(BlueprintRace race) {
        var statBonuses = race.GetComponents<AddStatBonus>().ToArray();
        var playerRaceCount = BlueprintRoot.Instance.Progression.m_CharacterRaces
            .Count(reference => reference?.deserializedGuid == race.AssetGuid);
        if (race.Size != Size.Small ||
            race.m_Presets == null || race.m_Presets.Length < 3 ||
            race.m_Presets.Any(reference => reference?.Get()?.Skin == null) ||
            race.m_Presets.Any(reference => {
                var preset = reference?.Get();
                return preset == null ||
                    preset.MaleSkeleton != preset.FemaleSkeleton;
            }) ||
            race.MaleOptions?.Heads?.Length != 1 ||
            race.MaleOptions.Heads[0].AssetId != GoblinHeadEquipmentEntity ||
            race.FemaleOptions?.Heads?.Length != 1 ||
            race.FemaleOptions.Heads[0].AssetId != GoblinHeadEquipmentEntity ||
            statBonuses.Count(component =>
                component.Stat == StatType.Dexterity &&
                component.Value == 4 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            statBonuses.Count(component =>
                component.Stat == StatType.Strength &&
                component.Value == -2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            statBonuses.Count(component =>
                component.Stat == StatType.Charisma &&
                component.Value == -2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            (Main.Settings.GoblinRace && playerRaceCount != 1)) {
            throw new InvalidOperationException(
                "Goblin must be a single playable Small race with valid body presets, a Goblin head, and +4 Dexterity, -2 Strength, -2 Charisma modifiers.");
        }
    }
}
