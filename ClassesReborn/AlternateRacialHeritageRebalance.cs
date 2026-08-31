using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class AlternateRacialHeritageRebalance {
    private sealed class StackingDefinition {
        internal BlueprintRace Race;
        internal BlueprintFeatureSelection PrimarySelection;
        internal BlueprintFeatureSelection[] FollowUpSelections;
        internal BlueprintFeature Standard;
        internal BlueprintFeature[] Options;
    }

    private static readonly Dictionary<string, StackingDefinition> StackingBySelection =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<string>> ReplacementKeysByOption =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<string>> DeclaredReplacementKeys =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> OriginalOptionIdByWrapper =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> SourceSelectionIdByWrapper =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<string>> GeneratedSourceIdsByWrapper =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<string>> WrapperIdsByGeneratedSource =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AlternateHeritageIds = new(
        new[] {
            "Trait.Kitsune.KeenKitsune",
            "Trait.Kitsune.SkilledKitsune",
            "Trait.Kitsune.FastShifter",
            "Trait.Human.EyeForTalent",
            "Trait.Human.HeartOfTheFey",
            "Trait.Human.FocusedStudy",
            "Trait.Human.Awareness",
            "Trait.Human.MilitaryTradition",
            "Trait.Human.UnstoppableMagic",
            "Trait.Human.Dimdweller",
            "Trait.Human.GiantAncestry",
            "Trait.Human.HeartOfTheSlums",
            "Trait.Human.PracticedHunter",
            "Trait.Elf.FleetFooted",
            "Trait.Elf.Dreamspeaker",
            "Trait.Dwarf.MagicResistant",
            "Trait.Dwarf.Relentless",
            "Trait.Gnome.Pyromaniac",
            "Trait.Gnome.EternalHope",
            "Trait.Gnome.FellMagic",
            "Trait.Halfling.Jinxed",
            "Trait.Halfling.LowBlow",
            "Trait.HalfElf.AncestralArms",
            "Trait.HalfElf.DualMinded",
            "Trait.HalfOrc.SacredTattoo",
            "Trait.HalfOrc.Toothy",
            "Trait.HalfOrc.ShamansApprentice",
            "Trait.Aasimar.Heavenborn",
            "Trait.Aasimar.DeathlessSpirit",
            "Trait.Aasimar.CelestialCrusader",
            "Trait.Tiefling.MawOrClaw",
            "Trait.Tiefling.FiendishSprinter",
            "Trait.Oread.StoneInTheBlood",
            "Trait.Oread.EarthInsight",
            "Trait.Dhampir.Dayborn",
            "Trait.Dhampir.VampiricFangs",
            "Heritage.Goblin.CaveCrawler",
            "Heritage.Goblin.CityScavenger",
            "Heritage.Goblin.EatAnything",
            "Heritage.Goblin.HardHeadBigTeeth",
            "Heritage.Goblin.OverSizedEars",
            "Heritage.Goblin.TreeRunner",
            "Heritage.Mongrel.ChitinPlated",
            "Heritage.Mongrel.KeenEared",
            "Heritage.Mongrel.HoovedRunner",
            "Heritage.Mongrel.FirstCrusadeScion",
            "Heritage.Mongrel.AdaptiveLineage",
            "Heritage.Mongrel.Cliffborn",
            "Heritage.Mongrel.FangedOffshoot",
            "Heritage.Mongrel.CrushingLimbs",
        }.Select(FutureContentIds.Get),
        StringComparer.OrdinalIgnoreCase);

    internal static string HumanSelectionId => SelectionId("Human");
    internal static string EyeForTalentId =>
        FutureContentIds.Get("Trait.Human.EyeForTalent");

    internal static bool IsAlternateHeritage(string id) =>
        AlternateHeritageIds.Contains(id);

    internal static void Configure() {
        StackingBySelection.Clear();
        ReplacementKeysByOption.Clear();
        DeclaredReplacementKeys.Clear();
        OriginalOptionIdByWrapper.Clear();
        SourceSelectionIdByWrapper.Clear();
        GeneratedSourceIdsByWrapper.Clear();
        WrapperIdsByGeneratedSource.Clear();

        var standard = FeatureConfigurator.New(
                "ClassesRebornStandardAlternateRacialHeritage",
                FutureContentIds.Get("Heritage.Standard"))
            .SetDisplayName("ClassesReborn.StandardAlternateRacialHeritage.Name")
            .SetDescription("ClassesReborn.StandardAlternateRacialHeritage.Description")
            .SetIcon(FeatureRefs.KeenSenses.Reference.Get().Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .Configure();
        ConfigureHuman(standard);
        ConfigureElf(standard);
        ConfigureDwarf(standard);
        ConfigureGnome(standard);
        ConfigureHalfling(standard);
        ConfigureHalfElf(standard);
        ConfigureHalfOrc(standard);
        ConfigureKitsune(standard);
        ConfigureAasimar(standard);
        ConfigureTiefling(standard);
        ConfigureOread(standard);
        ConfigureDhampir(standard);
        ConfigureGoblin(standard);
        ConfigureMongrel(standard);
    }

    private static void ConfigureHuman(BlueprintFeature standard) {
        if (!Main.Settings.HumanAlternateHeritages) {
            return;
        }

        var eyeForTalent = PrepareOption(EyeForTalentId);
        DeclareReplacement(eyeForTalent, "Human.BonusFeat");
        var heartOfTheFey = PrepareOption(
            FutureContentIds.Get("Trait.Human.HeartOfTheFey"),
            FeatureRefs.HumanSkilled.ToString());
        var focusedStudy = PrepareOption(
            FutureContentIds.Get("Trait.Human.FocusedStudy"));
        var awareness = PrepareOption(
            FutureContentIds.Get("Trait.Human.Awareness"));
        var militaryTradition = PrepareOption(
            FutureContentIds.Get("Trait.Human.MilitaryTradition"));
        var unstoppableMagic = PrepareOption(
            FutureContentIds.Get("Trait.Human.UnstoppableMagic"));
        foreach (var option in new[] {
                     focusedStudy,
                     awareness,
                     militaryTradition,
                     unstoppableMagic,
                 }) {
            DeclareReplacement(option, "Human.BonusFeat");
        }
        AddAlternateSelection(
            BlueprintIds.HumanRace,
            "Human",
            standard,
            eyeForTalent,
            heartOfTheFey,
            focusedStudy,
            awareness,
            militaryTradition,
            unstoppableMagic,
            PrepareOption(
                FutureContentIds.Get("Trait.Human.Dimdweller"),
                FeatureRefs.HumanSkilled.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Human.GiantAncestry"),
                FeatureRefs.HumanSkilled.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Human.HeartOfTheSlums"),
                FeatureRefs.HumanSkilled.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Human.PracticedHunter"),
                FeatureRefs.HumanSkilled.ToString()));
    }

    private static void ConfigureElf(BlueprintFeature standard) {
        if (!Main.Settings.ElfAlternateHeritages) {
            return;
        }

        var selection = FeatureSelectionRefs.ElvenHeritageSelection.Reference.Get();
        AddOptions(
            selection,
            PrepareOption(
                FutureContentIds.Get("Trait.Elf.FleetFooted"),
                FeatureRefs.KeenSenses.ToString(),
                FeatureRefs.ElvenWeaponFamiliarity.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Elf.Dreamspeaker"),
                FeatureRefs.ElvenImmunities.ToString()));
        RegisterStacking(BlueprintIds.ElfRace, "Elf", selection, standard);
    }

    private static void ConfigureDwarf(BlueprintFeature standard) {
        if (!Main.Settings.DwarfAlternateHeritages) {
            return;
        }

        var selection = FeatureSelectionRefs.DwarfHeritageSelection.Reference.Get();
        AddOptions(
            selection,
            PrepareOption(
                FutureContentIds.Get("Trait.Dwarf.MagicResistant"),
                FeatureRefs.Hardy.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Dwarf.Relentless"),
                FeatureRefs.Stability.ToString()));
        RegisterStacking(BlueprintIds.DwarfRace, "Dwarf", selection, standard);
    }

    private static void ConfigureGnome(BlueprintFeature standard) {
        if (!Main.Settings.GnomeAlternateHeritages) {
            return;
        }

        var selection = FeatureSelectionRefs.GnomeHeritageSelection.Reference.Get();
        AddOptions(
            selection,
            PrepareOption(
                FutureContentIds.Get("Trait.Gnome.EternalHope"),
                FeatureRefs.DwarfDefensiveTrainingGiants.ToString(),
                FeatureRefs.HatredReptilian.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Gnome.FellMagic"),
                FeatureRefs.GnomeMagic.ToString()));
        RegisterStacking(BlueprintIds.GnomeRace, "Gnome", selection, standard);
    }

    private static void ConfigureHalfling(BlueprintFeature standard) {
        if (!Main.Settings.HalflingAlternateHeritages) {
            return;
        }

        var selection = FeatureSelectionRefs.HalflingHeritageSelection.Reference.Get();
        AddOptions(
            selection,
            PrepareOption(
                FutureContentIds.Get("Trait.Halfling.Jinxed"),
                FeatureRefs.HalflingLuck.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Halfling.LowBlow"),
                FeatureRefs.KeenSenses.ToString()));
        RegisterStacking(BlueprintIds.HalflingRace, "Halfling", selection, standard);
    }

    private static void ConfigureHalfElf(BlueprintFeature standard) {
        if (!Main.Settings.HalfElfAlternateHeritages) {
            return;
        }

        // Dual-Minded is already a native Wrath heritage option. Ancestral Arms
        // is added alongside it, so choosing it inherently gives up Adaptability.
        var selection = FeatureSelectionRefs.HalfElfHeritageSelection.Reference.Get();
        var ancestralArms = PrepareOption(
            FutureContentIds.Get("Trait.HalfElf.AncestralArms"));
        DeclareReplacement(ancestralArms, "HalfElf.Adaptability");
        var dualMinded = FeatureRefs.DualMindedHalfElf.Reference.Get();
        DeclareReplacement(dualMinded, "HalfElf.Adaptability");
        AddOptions(selection, ancestralArms);
        RegisterStacking(BlueprintIds.HalfElfRace, "Half-Elf", selection, standard);
    }

    private static void ConfigureHalfOrc(BlueprintFeature standard) {
        if (!Main.Settings.HalfOrcAlternateHeritages) {
            return;
        }

        var selection = FeatureSelectionRefs.HalfOrcHeritageSelection.Reference.Get();
        AddOptions(
            selection,
            PrepareOption(
                FutureContentIds.Get("Trait.HalfOrc.SacredTattoo"),
                FeatureRefs.HalfOrcFerocity.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.HalfOrc.Toothy"),
                FeatureRefs.HalfOrcFerocity.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.HalfOrc.ShamansApprentice"),
                FeatureRefs.Intimidating.ToString()));
        RegisterStacking(BlueprintIds.HalfOrcRace, "Half-Orc", selection, standard);
    }

    private static void ConfigureKitsune(BlueprintFeature standard) {
        if (!Main.Settings.KitsuneAlternateHeritages) {
            return;
        }

        // Keen Kitsune is already part of Wrath's native Kitsune heritage
        // selection. Skilled and Fast Shifter are an independent exchange so
        // either remains compatible with Classic or Keen ability modifiers.
        AddAlternateSelection(
            RaceRefs.KitsuneRace.Reference.Get().AssetGuid.ToString(),
            "Kitsune",
            standard,
            PrepareOption(
                FutureContentIds.Get("Trait.Kitsune.SkilledKitsune"),
                FeatureRefs.AgileKitsune.ToString(),
                FeatureRefs.KitsuneMagic.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Kitsune.FastShifter"),
                FeatureRefs.KitsuneMagic.ToString()));
    }

    private static void ConfigureAasimar(BlueprintFeature standard) {
        if (!Main.Settings.AasimarAlternateHeritages) {
            return;
        }

        var ancestrySelection = FeatureSelectionRefs.AasimarHeritageSelection.Reference.Get();
        var heritageRefs = ancestrySelection.m_AllFeatures.ToArray();
        var heavenborn = PrepareOption(
            FutureContentIds.Get("Trait.Aasimar.Heavenborn"),
            CollectAddedFacts(ancestrySelection));
        heavenborn = AddSkillBonusSuppression(heavenborn, heritageRefs);
        DeclareReplacement(heavenborn, "Aasimar.AncestrySkillBonuses");
        var celestialCrusader = PrepareOption(
            FutureContentIds.Get("Trait.Aasimar.CelestialCrusader"),
            FeatureRefs.CelestialResistance.ToString());
        celestialCrusader = AddSkillBonusSuppression(celestialCrusader, heritageRefs);
        DeclareReplacement(celestialCrusader, "Aasimar.AncestrySkillBonuses");

        AddAlternateSelection(
            BlueprintIds.AasimarRace,
            "Aasimar",
            standard,
            heavenborn,
            PrepareOption(
                FutureContentIds.Get("Trait.Aasimar.DeathlessSpirit"),
                FeatureRefs.CelestialResistance.ToString()),
            celestialCrusader);
    }

    private static void ConfigureTiefling(BlueprintFeature standard) {
        if (!Main.Settings.TieflingAlternateHeritages) {
            return;
        }

        var ancestrySelection = FeatureSelectionRefs.TieflingHeritageSelection.Reference.Get();
        var fiendishSprinter = PrepareOption(
            FutureContentIds.Get("Trait.Tiefling.FiendishSprinter"));
        fiendishSprinter = AddSkillBonusSuppression(
            fiendishSprinter,
            ancestrySelection.m_AllFeatures.ToArray());
        DeclareReplacement(fiendishSprinter, "Tiefling.AncestrySkillBonuses");

        AddAlternateSelection(
            BlueprintIds.TieflingRace,
            "Tiefling",
            standard,
            PrepareOption(
                FutureContentIds.Get("Trait.Tiefling.MawOrClaw"),
                CollectAddedFacts(ancestrySelection)),
            fiendishSprinter);
    }

    private static void ConfigureOread(BlueprintFeature standard) {
        if (!Main.Settings.OreadAlternateHeritages) {
            return;
        }

        AddAlternateSelection(
            BlueprintIds.OreadRace,
            "Oread",
            standard,
            PrepareOption(
                FutureContentIds.Get("Trait.Oread.StoneInTheBlood"),
                FeatureRefs.AcidAffinityOread.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Oread.EarthInsight"),
                FeatureRefs.AcidAffinityOread.ToString()));
    }

    private static void ConfigureDhampir(BlueprintFeature standard) {
        if (!Main.Settings.DhampirAlternateHeritages) {
            return;
        }

        // Wrath omits the tabletop Dhampir spell-like ability and light
        // sensitivity. The closest implemented racial exchanges are used so
        // these options remain genuine trade-offs instead of free additions.
        AddAlternateSelection(
            BlueprintIds.DhampirRace,
            "Dhampir",
            standard,
            PrepareOption(
                FutureContentIds.Get("Trait.Dhampir.Dayborn"),
                FeatureRefs.ResistLevelDrainDhampir.ToString()),
            PrepareOption(
                FutureContentIds.Get("Trait.Dhampir.VampiricFangs"),
                FeatureRefs.UndeadResistanceDhampir.ToString()));
    }

    private static void ConfigureGoblin(BlueprintFeature standard) {
        if (!Main.Settings.GoblinAlternateHeritages) {
            return;
        }

        var caveCrawler = PrepareOption(
            FutureContentIds.Get("Heritage.Goblin.CaveCrawler"));
        DeclareReplacement(caveCrawler, "Goblin.FastMovement");

        var replacedSkillFeatures = new[] {
            BlueprintIds.GoblinStealthy,
            FeatureRefs.KeenSenses.ToString(),
        };
        AddAlternateSelection(
            BlueprintIds.GoblinRace,
            "Goblin",
            standard,
            caveCrawler,
            PrepareOption(
                FutureContentIds.Get("Heritage.Goblin.CityScavenger"),
                replacedSkillFeatures),
            PrepareOption(
                FutureContentIds.Get("Heritage.Goblin.EatAnything"),
                replacedSkillFeatures),
            PrepareOption(
                FutureContentIds.Get("Heritage.Goblin.HardHeadBigTeeth"),
                replacedSkillFeatures),
            PrepareOption(
                FutureContentIds.Get("Heritage.Goblin.OverSizedEars"),
                replacedSkillFeatures),
            PrepareOption(
                FutureContentIds.Get("Heritage.Goblin.TreeRunner"),
                replacedSkillFeatures));
    }

    private static void ConfigureMongrel(BlueprintFeature standard) {
        if (!Main.Settings.MongrelAlternateHeritages) {
            return;
        }

        AddAlternateSelection(
            BlueprintIds.MongrelRace,
            "Mongrel",
            standard,
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.ChitinPlated"),
                MongrelRaceRebalance.ResilienceId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.KeenEared"),
                MongrelRaceRebalance.SoundMimicryId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.HoovedRunner"),
                MongrelRaceRebalance.UndergroundSurvivorId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.FirstCrusadeScion"),
                MongrelRaceRebalance.SoundMimicryId,
                MongrelRaceRebalance.UndergroundSurvivorId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.AdaptiveLineage"),
                MongrelRaceRebalance.SoundMimicryId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.Cliffborn"),
                MongrelRaceRebalance.UndergroundSurvivorId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.FangedOffshoot"),
                MongrelRaceRebalance.ResilienceId,
                MongrelRaceRebalance.SoundMimicryId),
            PrepareOption(
                FutureContentIds.Get("Heritage.Mongrel.CrushingLimbs"),
                MongrelRaceRebalance.ResilienceId));
    }

    private static BlueprintFeature PrepareOption(string id, params string[] removedFeatures) {
        var configurator = FeatureConfigurator.For(id)
            .RemoveComponents(component => component is RaceTraitPrerequisite);
        foreach (var removedFeature in removedFeatures.Distinct(StringComparer.OrdinalIgnoreCase)) {
            configurator.AddRemoveFeatureOnApply(removedFeature);
        }
        var feature = configurator.Configure();
        feature.Groups = Array.Empty<FeatureGroup>();
        NormalizeBonusDescriptors(feature);
        return feature;
    }

    private static void NormalizeBonusDescriptors(BlueprintFeature feature) {
        foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>()) {
            if (component is RacialCasterLevelBonus casterLevel) {
                casterLevel.BonusDescriptor = ModifierDescriptor.Racial;
            } else if (component is RacialSpellDcBonus spellDc) {
                spellDc.BonusDescriptor = ModifierDescriptor.Racial;
            } else if (component is SourceCreatureSaveBonus sourceSave) {
                sourceSave.BonusDescriptor = ModifierDescriptor.Racial;
            } else if (component is RelentlessManeuverBonus maneuver) {
                maneuver.BonusDescriptor = ModifierDescriptor.Racial;
            }

            foreach (var fieldName in new[] { "Descriptor", "ModifierDescriptor" }) {
                var field = component.GetType().GetField(fieldName);
                if (field?.FieldType == typeof(ModifierDescriptor) &&
                    (ModifierDescriptor)field.GetValue(component) == ModifierDescriptor.Trait) {
                    field.SetValue(component, ModifierDescriptor.Racial);
                }
            }
        }
    }

    private static BlueprintFeature AddSkillBonusSuppression(
        BlueprintFeature feature,
        BlueprintFeatureReference[] heritages) =>
        FeatureConfigurator.For(feature.AssetGuid.ToString())
            .AddComponent(new SuppressHeritageSkillBonuses {
                m_Heritages = heritages,
            })
            .Configure();

    private static string[] CollectAddedFacts(BlueprintFeatureSelection selection) =>
        selection.m_AllFeatures
            .Select(reference => reference.Get())
            .Where(feature => feature != null)
            .SelectMany(feature => feature.GetComponents<AddFacts>())
            .SelectMany(component => component.m_Facts ??
                Array.Empty<BlueprintUnitFactReference>())
            .Select(reference => reference?.deserializedGuid.ToString())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static void AddOptions(
        BlueprintFeatureSelection selection,
        params BlueprintFeature[] options) {
        selection.m_AllFeatures = AppendUnique(selection.m_AllFeatures, options);
        selection.m_Features = AppendUnique(selection.m_Features, options);
    }

    private static BlueprintFeatureReference[] AppendUnique(
        BlueprintFeatureReference[] existing,
        IEnumerable<BlueprintFeature> additions) {
        var result = (existing ?? Array.Empty<BlueprintFeatureReference>()).ToList();
        foreach (var addition in additions) {
            if (result.Any(reference => reference?.deserializedGuid == addition.AssetGuid)) {
                continue;
            }
            result.Add(addition.ToReference<BlueprintFeatureReference>());
        }
        return result.ToArray();
    }

    private static void AddAlternateSelection(
        string raceId,
        string raceName,
        BlueprintFeature standard,
        params BlueprintFeature[] alternatives) {
        var icon = alternatives.FirstOrDefault()?.Icon ?? standard.Icon;
        var selection = FeatureSelectionConfigurator.New(
                $"ClassesReborn{raceName.Replace("-", string.Empty)}AlternateRacialHeritageSelection",
                SelectionId(raceName))
            .SetDisplayName("ClassesReborn.AlternateRacialHeritage.Name")
            .SetDescription("ClassesReborn.AlternateRacialHeritage.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Racial)
            .SetRanks(1)
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            // Match Owlcat's native racial-heritage selections.  Keeping the
            // primary page optional lets automatic NPC level-up skip it while
            // the character-creation roadmap still presents it to the player.
            .SetObligatory(false)
            .Configure();
        selection.m_AllFeatures = new[] { standard }
            .Concat(alternatives)
            .Select(feature => feature.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        if (!selection.Groups.Contains(FeatureGroup.Racial) ||
            !selection.IsClassFeature) {
            throw new InvalidOperationException(
                $"{raceName} alternate heritages must use Wrath's native racial roadmap group.");
        }

        var race = BlueprintTool.Get<BlueprintRace>(raceId);
        var features = (race.m_Features ?? Array.Empty<BlueprintFeatureBaseReference>())
            .Where(reference => reference?.deserializedGuid != selection.AssetGuid)
            .ToList();
        features.Add(selection.ToReference<BlueprintFeatureBaseReference>());
        race.m_Features = features.ToArray();

        RegisterStacking(raceId, raceName, selection, standard);
    }

    private static void DeclareReplacement(
        BlueprintFeature option,
        params string[] replacementKeys) {
        var id = option.AssetGuid.ToString();
        if (!DeclaredReplacementKeys.TryGetValue(id, out var keys)) {
            keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DeclaredReplacementKeys[id] = keys;
        }
        keys.UnionWith(replacementKeys.Where(key => !string.IsNullOrWhiteSpace(key)));
    }

    private static void RegisterStacking(
        string raceId,
        string raceName,
        BlueprintFeatureSelection primarySelection,
        BlueprintFeature standard) {
        var race = BlueprintTool.Get<BlueprintRace>(raceId);
        var options = (primarySelection.m_AllFeatures ??
                Array.Empty<BlueprintFeatureReference>())
            .Select(reference => reference?.Get())
            .Where(option => option != null && option != standard)
            .Distinct()
            .Where(option => GetReplacementKeys(option).Count > 0)
            .ToArray();
        if (options.Length == 0) {
            return;
        }

        // Eligibility belongs to the stage-specific wrapper selected by the
        // player. The original feature is granted by that wrapper's progression;
        // leaving this prerequisite on the original would make it conflict with
        // its own wrapper while the progression is being applied.
        foreach (var option in options) {
            FeatureConfigurator.For(option.AssetGuid.ToString())
                .RemoveComponents(component =>
                    component is AlternateHeritageReplacementPrerequisite)
                .Configure();
        }

        var maximumHeritages = Main.Settings.AlternateHeritageStacking
            ? GetMaximumCompatibleHeritageCount(options)
            : 1;
        var followUps = Enumerable.Range(1, Math.Max(0, maximumHeritages - 1))
            .Select(stage => CreateFollowUpSelection(
                raceName,
                stage,
                standard))
            .ToArray();

        if (followUps.Length > 0) {
            for (var stage = 1; stage < maximumHeritages; stage++) {
                var nextSelection = stage < followUps.Length
                    ? followUps[stage]
                    : null;
                var stageOptions = options
                    .Select((option, index) => CreateStageOptionWrapper(
                        raceName,
                        stage,
                        index,
                        option,
                        followUps[stage - 1],
                        nextSelection))
                    .ToArray();
                SetFollowUpOptions(
                    followUps[stage - 1],
                    CreateNoAdditionalHeritage(raceName, stage, standard),
                    stageOptions);
            }

            var primaryWrappers = options
                .Select((option, index) => CreateStageOptionWrapper(
                    raceName,
                    stage: 0,
                    index,
                    option,
                    primarySelection,
                    followUps[0]))
                .ToArray();
            ReplaceSelectionOptions(primarySelection, options, primaryWrappers);
        }

        var definition = new StackingDefinition {
            Race = race,
            PrimarySelection = primarySelection,
            FollowUpSelections = followUps,
            Standard = standard,
            Options = options,
        };
        StackingBySelection[primarySelection.AssetGuid.ToString()] = definition;
        foreach (var followUp in followUps) {
            StackingBySelection[followUp.AssetGuid.ToString()] = definition;
        }

        // Follow-up selections are granted by the selected wrapper progression.
        // They must not also be permanent race features, or all possible stages
        // would be visible before the player chooses an alternate heritage.
        race.m_Features = (race.m_Features ??
                Array.Empty<BlueprintFeatureBaseReference>())
            .Where(reference => followUps.All(followUp =>
                reference?.deserializedGuid != followUp.AssetGuid))
            .ToArray();
    }

    private static BlueprintFeatureSelection CreateFollowUpSelection(
        string raceName,
        int stage,
        BlueprintFeature standard) {
        var suffix = stage == 1 ? string.Empty : $"{stage}";
        var followUp = FeatureSelectionConfigurator.New(
                $"ClassesReborn{raceName.Replace("-", string.Empty)}StackedAlternateRacialHeritageSelection{suffix}",
                StackingSelectionId(raceName, stage))
            .SetDisplayName("ClassesReborn.AdditionalAlternateRacialHeritage.Name")
            .SetDescription("ClassesReborn.AdditionalAlternateRacialHeritage.Description")
            .SetIcon(standard.Icon)
            .SetGroups(FeatureGroup.Racial)
            .SetRanks(1)
            .SetIsClassFeature(true)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .Configure();
        return followUp;
    }

    private static BlueprintFeature CreateNoAdditionalHeritage(
        string raceName,
        int stage,
        BlueprintFeature standard) =>
        FeatureConfigurator.New(
                $"ClassesReborn{raceName.Replace("-", string.Empty)}NoAdditionalAlternateRacialHeritage{stage}",
                FutureContentIds.Get($"Heritage.NoAdditional.{raceName}.{stage}"))
            .SetDisplayName("ClassesReborn.NoAdditionalAlternateRacialHeritage.Name")
            .SetDescription("ClassesReborn.NoAdditionalAlternateRacialHeritage.Description")
            .SetIcon(standard.Icon)
            .SetRanks(1)
            .SetIsClassFeature(false)
            .Configure();

    private static BlueprintProgression CreateStageOptionWrapper(
        string raceName,
        int stage,
        int optionIndex,
        BlueprintFeature original,
        BlueprintFeatureSelection sourceSelection,
        BlueprintFeatureSelection nextSelection) {
        var wrapper = ProgressionConfigurator.New(
                $"ClassesReborn{raceName.Replace("-", string.Empty)}AlternateHeritageStage{stage}Option{optionIndex}",
                FutureContentIds.Get(
                    $"Heritage.StackedOption.{raceName}.{stage}.{original.AssetGuid}"))
            .SetDisplayName(original.m_DisplayName)
            .SetDescription(original.m_Description)
            .SetIcon(original.Icon)
            .SetGroups(FeatureGroup.Racial)
            .SetRanks(1)
            .SetGiveFeaturesForPreviousLevels(true)
            .SetReapplyOnLevelUp(true)
            .SetIsClassFeature(false)
            .Configure();

        var grantedFeatures = new List<BlueprintFeatureBaseReference> {
            original.ToReference<BlueprintFeatureBaseReference>(),
        };
        if (nextSelection != null) {
            grantedFeatures.Add(
                nextSelection.ToReference<BlueprintFeatureBaseReference>());
        }
        wrapper.LevelEntries = new[] {
            new LevelEntry {
                Level = 1,
                m_Features = grantedFeatures,
            },
        };

        var wrapperId = wrapper.AssetGuid.ToString();
        var originalId = original.AssetGuid.ToString();
        OriginalOptionIdByWrapper[wrapperId] = originalId;
        SourceSelectionIdByWrapper[wrapperId] = sourceSelection.AssetGuid.ToString();
        GeneratedSourceIdsByWrapper[wrapperId] = new HashSet<string>(
            new[] { wrapperId, originalId },
            StringComparer.OrdinalIgnoreCase);
        if (!WrapperIdsByGeneratedSource.TryGetValue(originalId, out var wrappers)) {
            wrappers = new List<string>();
            WrapperIdsByGeneratedSource[originalId] = wrappers;
        }
        wrappers.Add(wrapperId);
        DeclareReplacement(wrapper, GetReplacementKeys(original).ToArray());
        return ProgressionConfigurator.For(wrapper)
            .RemoveComponents(component =>
                component is AlternateHeritageReplacementPrerequisite)
            .AddComponent(new AlternateHeritageReplacementPrerequisite {
                m_Option = wrapper.ToReference<BlueprintFeatureReference>(),
            })
            .Configure();
    }

    private static void SetFollowUpOptions(
        BlueprintFeatureSelection selection,
        BlueprintFeature noAdditionalHeritage,
        IReadOnlyList<BlueprintProgression> options) {
        selection.m_AllFeatures = new[] { noAdditionalHeritage }
            .Concat<BlueprintFeature>(options)
            .Select(feature => feature.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
    }

    private static void ReplaceSelectionOptions(
        BlueprintFeatureSelection selection,
        IReadOnlyList<BlueprintFeature> originals,
        IReadOnlyList<BlueprintProgression> replacements) {
        var replacementById = originals
            .Select((original, index) => new {
                Id = original.AssetGuid.ToString(),
                Replacement = replacements[index],
            })
            .ToDictionary(
                entry => entry.Id,
                entry => entry.Replacement,
                StringComparer.OrdinalIgnoreCase);

        BlueprintFeatureReference[] Replace(
            BlueprintFeatureReference[] features) =>
            (features ?? Array.Empty<BlueprintFeatureReference>())
                .Select(reference =>
                    reference?.Get() is BlueprintFeature original &&
                    replacementById.TryGetValue(
                        original.AssetGuid.ToString(),
                        out var replacement)
                        ? replacement.ToReference<BlueprintFeatureReference>()
                        : reference)
                .ToArray();

        selection.m_AllFeatures = Replace(selection.m_AllFeatures);
        selection.m_Features = Replace(selection.m_Features);
    }

    internal static void PlaceGeneratedSelectionNextToHeritage(
        LevelUpState state,
        FeatureSelectionState addedSelection) {
        var sourceId = addedSelection?.Source.Blueprint?.AssetGuid.ToString();
        if (state?.Selections == null || string.IsNullOrEmpty(sourceId) ||
            !TryResolveGeneratingWrapper(state, sourceId, out var wrapperId) ||
            !SourceSelectionIdByWrapper.TryGetValue(
                wrapperId,
                out var sourceSelectionId)) {
            return;
        }

        var sourceState = state.Selections.FirstOrDefault(candidate =>
            candidate.Selection is BlueprintFeatureSelection selection &&
            selection.AssetGuid.ToString().Equals(
                sourceSelectionId,
                StringComparison.OrdinalIgnoreCase) &&
            candidate.SelectedItem?.Feature?.AssetGuid.ToString().Equals(
                wrapperId,
                StringComparison.OrdinalIgnoreCase) == true);
        if (sourceState == null || ReferenceEquals(sourceState, addedSelection) ||
            !state.Selections.Remove(addedSelection)) {
            return;
        }

        var insertionIndex = state.Selections.IndexOf(sourceState) + 1;
        while (insertionIndex < state.Selections.Count &&
               IsGeneratedByWrapper(state.Selections[insertionIndex], wrapperId)) {
            insertionIndex++;
        }
        state.Selections.Insert(insertionIndex, addedSelection);
    }

    private static bool TryResolveGeneratingWrapper(
        LevelUpState state,
        string sourceId,
        out string wrapperId) {
        if (SourceSelectionIdByWrapper.ContainsKey(sourceId)) {
            wrapperId = sourceId;
            return true;
        }

        if (WrapperIdsByGeneratedSource.TryGetValue(sourceId, out var candidates)) {
            foreach (var candidateId in candidates) {
                if (!SourceSelectionIdByWrapper.TryGetValue(
                        candidateId,
                        out var sourceSelectionId)) {
                    continue;
                }
                if (state.Selections.Any(selectionState =>
                        selectionState.Selection is BlueprintFeatureSelection selection &&
                        selection.AssetGuid.ToString().Equals(
                            sourceSelectionId,
                            StringComparison.OrdinalIgnoreCase) &&
                        selectionState.SelectedItem?.Feature?.AssetGuid.ToString().Equals(
                            candidateId,
                            StringComparison.OrdinalIgnoreCase) == true)) {
                    wrapperId = candidateId;
                    return true;
                }
            }
        }

        wrapperId = null;
        return false;
    }

    private static bool IsGeneratedByWrapper(
        FeatureSelectionState selectionState,
        string wrapperId) {
        var sourceId = selectionState?.Source.Blueprint?.AssetGuid.ToString();
        return sourceId != null &&
            GeneratedSourceIdsByWrapper.TryGetValue(wrapperId, out var sourceIds) &&
            sourceIds.Contains(sourceId);
    }

    private static int GetMaximumCompatibleHeritageCount(
        IReadOnlyList<BlueprintFeature> options) {
        var maximum = 0;
        SearchMaximumCompatibleSet(
            options,
            index: 0,
            usedKeys: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            selected: 0,
            ref maximum);
        return maximum;
    }

    private static void SearchMaximumCompatibleSet(
        IReadOnlyList<BlueprintFeature> options,
        int index,
        HashSet<string> usedKeys,
        int selected,
        ref int maximum) {
        if (index >= options.Count) {
            maximum = Math.Max(maximum, selected);
            return;
        }

        if (selected + options.Count - index <= maximum) {
            return;
        }

        SearchMaximumCompatibleSet(
            options,
            index + 1,
            usedKeys,
            selected,
            ref maximum);

        var keys = GetReplacementKeys(options[index]);
        if (keys.Count == 0 || usedKeys.Overlaps(keys)) {
            return;
        }

        var nextKeys = new HashSet<string>(usedKeys, StringComparer.OrdinalIgnoreCase);
        nextKeys.UnionWith(keys);
        SearchMaximumCompatibleSet(
            options,
            index + 1,
            nextKeys,
            selected + 1,
            ref maximum);
    }

    private static HashSet<string> GetReplacementKeys(BlueprintFeature option) {
        var id = option.AssetGuid.ToString();
        if (ReplacementKeysByOption.TryGetValue(id, out var existing)) {
            return existing;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (DeclaredReplacementKeys.TryGetValue(id, out var declared)) {
            keys.UnionWith(declared);
        }
        foreach (var component in option.GetComponents<RemoveFeatureOnApply>()) {
            var removed = component.m_Feature?.deserializedGuid.ToString();
            if (!string.IsNullOrEmpty(removed)) {
                keys.Add($"Feature.{removed}");
            }
        }
        ReplacementKeysByOption[id] = keys;
        return keys;
    }

    internal static bool IsRegisteredSelection(
        BlueprintFeatureSelection selection,
        out object definition) {
        if (selection != null &&
            StackingBySelection.TryGetValue(selection.AssetGuid.ToString(), out var found)) {
            definition = found;
            return true;
        }
        definition = null;
        return false;
    }

    internal static bool IsHumanSelection(BlueprintFeatureSelection selection) =>
        selection != null &&
        StackingBySelection.TryGetValue(selection.AssetGuid.ToString(), out var definition) &&
        definition.Race.AssetGuid.ToString().Equals(
            BlueprintIds.HumanRace,
            StringComparison.OrdinalIgnoreCase);

    internal static bool HasEyeForTalent(LevelUpState state) =>
        state?.Selections.Any(candidate =>
            candidate.Selection is BlueprintFeatureSelection selection &&
            IsHumanSelection(selection) &&
            ResolveOriginalOptionId(candidate.SelectedItem?.Feature) == EyeForTalentId) == true;

    internal static bool HasHumanBonusFeatReplacement(LevelUpState state) =>
        state?.Selections.Any(candidate =>
            candidate.Selection is BlueprintFeatureSelection selection &&
            IsHumanSelection(selection) &&
            candidate.SelectedItem?.Feature is BlueprintFeature selectedHeritage &&
            GetReplacementKeys(selectedHeritage).Contains("Human.BonusFeat")) == true;

    private static string ResolveOriginalOptionId(BlueprintFeature option) {
        var id = option?.AssetGuid.ToString();
        return id != null && OriginalOptionIdByWrapper.TryGetValue(id, out var originalId)
            ? originalId
            : id;
    }

    internal static bool CanSelectWithCurrentHeritages(
        BlueprintFeature option,
        FeatureSelectionState selectionState,
        LevelUpState state) {
        if (!Main.Settings.AlternateHeritageStacking || option == null || state == null) {
            return true;
        }
        var candidateKeys = GetReplacementKeys(option);
        if (candidateKeys.Count == 0) {
            return true;
        }

        foreach (var otherState in state.Selections) {
            if (otherState == selectionState ||
                otherState.SelectedItem?.Feature is not BlueprintFeature other ||
                !ReplacementKeysByOption.TryGetValue(
                    other.AssetGuid.ToString(),
                    out var otherKeys)) {
                continue;
            }
            if (candidateKeys.Overlaps(otherKeys)) {
                return false;
            }
        }
        return true;
    }

    private static string SelectionId(string raceName) =>
        FutureContentIds.Get($"Heritage.Selection.{raceName}");

    private static string StackingSelectionId(string raceName, int stage) =>
        FutureContentIds.Get(stage == 1
            ? $"Heritage.StackingSelection.{raceName}"
            : $"Heritage.StackingSelection.{raceName}.{stage}");
}

[AllowedOn(typeof(BlueprintFeatureBase))]
[TypeId("3113028d-214c-4afd-a86d-92cae24285c0")]
public sealed class AlternateHeritageReplacementPrerequisite : Prerequisite {
    public BlueprintFeatureReference m_Option;

    public override bool CheckInternal(
        FeatureSelectionState selectionState,
        UnitDescriptor unit,
        LevelUpState state) =>
        AlternateRacialHeritageRebalance.CanSelectWithCurrentHeritages(
            m_Option?.Get(),
            selectionState,
            state);

    public override string GetUITextInternal(UnitDescriptor unit) =>
        "Does not replace a racial feature already exchanged by another heritage";
}

[HarmonyPatch(
    typeof(LevelUpState),
    nameof(LevelUpState.AddSelection),
    new[] {
        typeof(FeatureSelectionState),
        typeof(FeatureSource),
        typeof(IFeatureSelection),
        typeof(int),
    })]
internal static class ProgressiveHeritageSelectionOrderPatch {
    [HarmonyPostfix]
    private static void Postfix(
        LevelUpState __instance,
        FeatureSelectionState __result) =>
        AlternateRacialHeritageRebalance.PlaceGeneratedSelectionNextToHeritage(
            __instance,
            __result);
}

[HarmonyPatch(
    typeof(ApplyClassMechanics),
    nameof(ApplyClassMechanics.Apply),
    new[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
internal static class FocusedStudyLevelUpPatch {
    [HarmonyPostfix]
    private static void Postfix(LevelUpState state, UnitDescriptor unit) {
        if (!Main.Settings.HumanAlternateHeritages || state == null || unit == null ||
            state.NextCharacterLevel is not (8 or 16)) {
            return;
        }

        var focusedStudy = BlueprintTool.Get<BlueprintProgression>(
            FutureContentIds.Get("Trait.Human.FocusedStudy"));
        var skillFocus = FeatureSelectionRefs.SkillFocusSelection.Reference.Get();
        if (focusedStudy == null || skillFocus == null || !unit.HasFact(focusedStudy) ||
            state.Selections.Any(existing =>
                existing.Selection == skillFocus &&
                existing.Source.Blueprint == focusedStudy)) {
            return;
        }

        state.AddSelection(
            null,
            new FeatureSource(focusedStudy),
            skillFocus,
            state.NextCharacterLevel);
    }
}

[HarmonyPatch]
internal static class HumanEyeForTalentBonusFeatPatch {
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(LevelUpController),
        nameof(LevelUpController.SelectFeature),
        new[] { typeof(FeatureSelectionState), typeof(IFeatureSelectionItem) })]
    private static bool PreventReplacedBonusFeatSelection(
        LevelUpController __instance,
        FeatureSelectionState selection,
        ref bool __result) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !Main.Settings.HumanAlternateHeritages ||
            selection?.Selection is not BlueprintFeatureSelection selectedBlueprint ||
            selectedBlueprint.AssetGuid.ToString() != BlueprintIds.HumanBonusFeatSelection ||
            !AlternateRacialHeritageRebalance.HasHumanBonusFeatReplacement(
                __instance?.State)) {
            return true;
        }

        __result = false;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(LevelUpController),
        nameof(LevelUpController.SelectFeature),
        new[] { typeof(FeatureSelectionState), typeof(IFeatureSelectionItem) })]
    private static void SelectFeaturePostfix(
        LevelUpController __instance,
        FeatureSelectionState selection,
        IFeatureSelectionItem item,
        bool __result) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !__result || !Main.Settings.HumanAlternateHeritages ||
            selection?.Selection is not BlueprintFeatureSelection selectedBlueprint ||
            !AlternateRacialHeritageRebalance.IsHumanSelection(selectedBlueprint)) {
            return;
        }

        SyncHumanBonusFeat(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(LevelUpController),
        nameof(LevelUpController.UnselectFeature),
        new[] { typeof(FeatureSelectionState) })]
    private static void UnselectFeaturePostfix(
        LevelUpController __instance,
        FeatureSelectionState selectionState) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !Main.Settings.HumanAlternateHeritages ||
            selectionState?.Selection is not BlueprintFeatureSelection selection ||
            !AlternateRacialHeritageRebalance.IsHumanSelection(selection)) {
            return;
        }
        SyncHumanBonusFeat(__instance);
    }

    internal static void SyncHumanBonusFeat(LevelUpController controller) {
        var state = controller?.State;
        var humanRace = BlueprintTool.Get<BlueprintRace>(BlueprintIds.HumanRace);
        var bonusFeat = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.HumanBonusFeatSelection);
        if (state?.SelectedRace != humanRace || bonusFeat == null) {
            return;
        }

        if (AlternateRacialHeritageRebalance.HasHumanBonusFeatReplacement(state)) {
            foreach (var raceSelection in state.Selections
                         .Where(candidate => candidate.Selection == bonusFeat &&
                             candidate.Source.Blueprint == humanRace &&
                             candidate.SelectedItem != null)
                         .ToArray()) {
                controller.UnselectFeature(raceSelection);
            }
        }

        controller.m_RecalculatePreview = true;
        controller.m_NeedUpdateView = true;
    }
}

[HarmonyPatch(
    typeof(FeatureSelectionState),
    nameof(FeatureSelectionState.Selected),
    MethodType.Getter)]
internal static class HumanEyeForTalentBonusFeatCompletionPatch {
    [HarmonyPostfix]
    private static void Postfix(FeatureSelectionState __instance, ref bool __result) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            __result || !Main.Settings.HumanAlternateHeritages ||
            __instance?.Selection is not BlueprintFeatureSelection selection ||
            selection.AssetGuid.ToString() != BlueprintIds.HumanBonusFeatSelection) {
            return;
        }

        var state = Kingmaker.Game.Instance?.LevelUpController?.State;
        if (state?.SelectedRace?.AssetGuid.ToString() == BlueprintIds.HumanRace &&
            AlternateRacialHeritageRebalance.HasHumanBonusFeatReplacement(state)) {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.RemainingSelections))]
internal static class HumanBonusFeatRemainingSelectionsPatch {
    [HarmonyPostfix]
    private static void Postfix(LevelUpState __instance, ref int __result) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            __result <= 0 ||
            !Main.Settings.HumanAlternateHeritages ||
            __instance?.SelectedRace?.AssetGuid.ToString() != BlueprintIds.HumanRace ||
            !AlternateRacialHeritageRebalance.HasHumanBonusFeatReplacement(__instance)) {
            return;
        }

        var unresolvedReplacedSelections = __instance.Selections.Count(candidate =>
            candidate.Selection is BlueprintFeatureSelection selection &&
            selection.AssetGuid.ToString() == BlueprintIds.HumanBonusFeatSelection &&
            candidate.Source.Blueprint?.AssetGuid.ToString() == BlueprintIds.HumanRace &&
            !candidate.Selected);
        __result = Math.Max(0, __result - unresolvedReplacedSelections);
    }
}

[HarmonyPatch]
internal static class AlternateHeritageSelectionRollbackPatch {
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(LevelUpController),
        nameof(LevelUpController.SelectFeature),
        new[] { typeof(FeatureSelectionState), typeof(IFeatureSelectionItem) })]
    private static void SelectFeaturePrefix(
        LevelUpController __instance,
        FeatureSelectionState selection,
        IFeatureSelectionItem item) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !IsChangingRegisteredHeritage(selection, item)) {
            return;
        }

        // Wrath checks whether the target selection is empty before it asks the
        // selection whether the replacement itself is legal.  Our rollback must
        // therefore validate first: otherwise an already-selected or conflicting
        // heritage clears the current choice and is then rejected, leaving the
        // roadmap with an unresolved slot.
        var state = __instance?.State;
        var unit = __instance?.GetUnit(selection.Selection)?.Descriptor;
        if (state == null || unit == null ||
            !selection.Selection.CanSelect(unit, state, selection, item)) {
            return;
        }

        // Selecting another entry does not call UnselectFeature first. Remove the
        // complete old selection tree before the new SelectFeature action is added.
        // AddAction will rebuild the preview once with the replacement in place.
        RemoveSelectionTreeActions(__instance, selection, updatePreview: false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(LevelUpController),
        nameof(LevelUpController.UnselectFeature),
        new[] { typeof(FeatureSelectionState) })]
    private static bool UnselectFeaturePrefix(
        LevelUpController __instance,
        FeatureSelectionState selectionState) {
        if (AddClassLevelsTraitSelectionGuardPatch.IsActive ||
            !IsRegisteredHeritage(selectionState)) {
            return true;
        }

        RemoveSelectionTreeActions(__instance, selectionState, updatePreview: true);
        return false;
    }

    private static bool IsChangingRegisteredHeritage(
        FeatureSelectionState selectionState,
        IFeatureSelectionItem replacement) =>
        IsRegisteredHeritage(selectionState) &&
        selectionState.SelectedItem?.Feature != null &&
        selectionState.SelectedItem.Feature != replacement?.Feature;

    private static bool IsRegisteredHeritage(FeatureSelectionState selectionState) =>
        Main.Settings.AlternateHeritageStacking &&
        selectionState?.Selection is BlueprintFeatureSelection selection &&
        selectionState.SelectedItem?.Feature is BlueprintFeature &&
        AlternateRacialHeritageRebalance.IsRegisteredSelection(selection, out _);

    private static void RemoveSelectionTreeActions(
        LevelUpController controller,
        FeatureSelectionState root,
        bool updatePreview) {
        var state = controller?.State;
        if (state == null || root == null) {
            return;
        }

        var selectionTree = CollectSelectionTree(state, root);
        var actions = controller.LevelUpActions
            .OfType<SelectFeature>()
            .Where(action => {
                var actionState = action.GetSelectionState(state);
                return actionState != null && selectionTree.Contains(actionState);
            })
            .Reverse()
            .ToArray();

        // Keep unrelated later roadmap choices intact. Every selected feature
        // belonging to the old heritage is removed as one atomic operation.
        using (controller.EnterIgnoreDropPlanScope()) {
            foreach (var action in actions) {
                controller.RemoveAction<SelectFeature>(candidate =>
                    ReferenceEquals(candidate, action));
            }

            // A transient selection index can leave an older root action unable
            // to resolve back to its current state. Remove that exact heritage as
            // a fallback without touching either of the other heritage slots.
            var rootSelection = root.Selection;
            var rootFeature = root.SelectedItem?.Feature;
            while (controller.RemoveAction<SelectFeature>(action =>
                       action.Selection == rootSelection &&
                       action.Item?.Feature == rootFeature)) {
            }
        }

        controller.m_RecalculatePreview = true;
        controller.m_NeedUpdateView = true;
        if (updatePreview) {
            controller.UpdatePreview();
        }
    }

    private static HashSet<FeatureSelectionState> CollectSelectionTree(
        LevelUpState state,
        FeatureSelectionState root) {
        var result = new HashSet<FeatureSelectionState> { root };
        var sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSelectedFeatureId(root, sourceIds);

        var changed = true;
        while (changed) {
            changed = false;
            foreach (var candidate in state.Selections) {
                if (candidate == null || result.Contains(candidate)) {
                    continue;
                }

                var sourceId = candidate.Source.Blueprint?.AssetGuid.ToString();
                if ((candidate.Parent != null && result.Contains(candidate.Parent)) ||
                    (!string.IsNullOrEmpty(sourceId) && sourceIds.Contains(sourceId))) {
                    result.Add(candidate);
                    AddSelectedFeatureId(candidate, sourceIds);
                    changed = true;
                }
            }
        }
        return result;
    }

    private static void AddSelectedFeatureId(
        FeatureSelectionState selection,
        ISet<string> ids) {
        var id = selection?.SelectedItem?.Feature?.AssetGuid.ToString();
        if (!string.IsNullOrEmpty(id)) {
            ids.Add(id);
        }
    }
}
