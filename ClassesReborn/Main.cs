using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;
using Kingmaker.Blueprints.JsonSystem;

namespace ClassesReborn;

public static class Main {
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger Log;
    internal static ClassesRebornSettings Settings = new();

    public static bool Load(UnityModManager.ModEntry modEntry) {
        Log = modEntry.Logger;
        Settings = UnityModManager.ModSettings.Load<ClassesRebornSettings>(modEntry)
            ?? new ClassesRebornSettings();
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        try {
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        } catch (Exception exception) {
            Log.Error($"Buildcraft failed while installing Harmony patches:\n{exception}");
            HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
            throw;
        }
        return true;
    }

    public static void OnGUI(UnityModManager.ModEntry modEntry) {
        Settings.Draw(modEntry);
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) =>
        Settings.Save(modEntry);

    [HarmonyPatch(typeof(BlueprintsCache))]
    public static class BlueprintsCaches_Patch {
        private static bool Initialized = false;

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(nameof(BlueprintsCache.Init)), HarmonyPostfix]
        public static void Init_Postfix() {
            try {
                if (Initialized) {
                    Log.Log("Already initialized blueprints cache.");
                    return;
                }
                Initialized = true;

                Log.Log("Patching blueprints.");
                ConfigureSection(
                    "Rime Spell UI support",
                    Settings.RimeSpell,
                    RimeMetamagicUiPatches.Install);
                ConfigureSection(
                    "Race changes",
                    Settings.HalfOrc,
                    RaceRebalance.Configure);
                ConfigureSection(
                    "Playable Goblin race",
                    Settings.GoblinRace || Settings.GoblinAlternateHeritages,
                    GoblinRaceRebalance.Configure);
                ConfigureSection(
                    "Playable Mongrel race",
                    Settings.MongrelRace || Settings.MongrelAlternateHeritages,
                    MongrelRaceRebalance.Configure);
                ConfigureSection(
                    "Background bonus changes",
                    Settings.BackgroundTraitBonuses,
                    BackgroundRebalance.Configure);
                ConfigureSection(
                    "Added backgrounds",
                    Settings.SarkorianExile || Settings.WardstoneVeteran ||
                    Settings.RedeemedCultist || Settings.KenabresWatchman ||
                    Settings.NumerianSalvager || Settings.TempleWeaponmaster ||
                    Settings.CrusadeQuartermaster || Settings.WorldwoundCartographer ||
                    Settings.WildRaised || Settings.Knight,
                    BackgroundRebalance.ConfigureAddedBackgrounds);
                ConfigureSection(
                    "Character traits",
                    Settings.CharacterTraits || Settings.AdditionalTraits ||
                    Settings.AnyAlternateRacialHeritages,
                    TraitRebalance.Configure);
                ConfigureSection(
                    "Alternate racial heritages",
                    Settings.AnyAlternateRacialHeritages,
                    AlternateRacialHeritageRebalance.Configure);
                ConfigureSection(
                    "Trapfinding changes",
                    Settings.ImprovedTrapfinding,
                    TrapfindingRebalance.Configure);
                ConfigureSection(
                    "Danger Sense changes",
                    Settings.Barbarian || Settings.Rogue || Settings.Bard,
                    BarbarianRebalance.ConfigureDangerSenseChanges);
                ConfigureSection(
                    "Unlimited Abyssal and Draconic bloodline claws",
                    Settings.UnlimitedBloodlineClaws,
                    BloodlineClawRebalance.Configure);
                ConfigureSection(
                    "Long Arm",
                    Settings.LongArm,
                    ArcaneSpellRebalance.Configure);
                ConfigureSection(
                    "Tabletop spell additions",
                    TabletopSpellRebalance.Configure);
                ConfigureSection(
                    "Bard spell-list changes",
                    BardRebalance.ConfigureSpellListChanges);
                ConfigureSection(
                    "Dance of a Hundred Cuts",
                    Settings.DanceOfAHundredCuts,
                    BardRebalance.ConfigureDanceOfAHundredCuts);
                ConfigureSection(
                    "Paladin spell-list changes",
                    PaladinRebalance.ConfigureSpellListChanges);
                ConfigureSection(
                    "Gifted Adept spell choices",
                    Settings.CharacterTraits || Settings.AdditionalTraits ||
                    Settings.AnyAlternateRacialHeritages,
                    TraitRebalance.RefreshGiftedAdeptSpellVariants);
                ConfigureSection(
                    "Feat additions",
                    FeatRebalance.Configure);
                ConfigureSection(
                    "Rage power additions",
                    Settings.RagePowerSuperstition ||
                    Settings.RagePowerWitchHunter ||
                    Settings.RagePowerEaterOfMagic ||
                    Settings.RagePowerStrengthSurge ||
                    Settings.RagePowerElementalRage ||
                    Settings.RagePowerGhostRager ||
                    Settings.RagePowerFerociousMount,
                    RagePowerRebalance.Configure);
                ConfigureSection(
                    "Dread Archer archetype",
                    Settings.DreadArcher,
                    DreadArcherArchetype.Configure);
                ConfigureSection(
                    "Alchemist class changes",
                    Settings.Alchemist,
                    AlchemistRebalance.Configure);
                ConfigureSection(
                    "Fighter class changes",
                    Settings.Fighter,
                    FighterRebalance.Configure);
                ConfigureSection(
                    "Barbarian class changes",
                    Settings.Barbarian,
                    BarbarianRebalance.Configure);
                ConfigureSection(
                    "Bard class changes",
                    Settings.Bard,
                    BardRebalance.Configure);
                ConfigureSection(
                    "Bloodrager class changes",
                    Settings.Bloodrager,
                    BloodragerRebalance.Configure);
                ConfigureSection(
                    "Cavalier class changes",
                    Settings.Cavalier,
                    CavalierRebalance.Configure);
                ConfigureSection(
                    "Cleric class changes",
                    Settings.Cleric,
                    ClericRebalance.Configure);
                ConfigureSection(
                    "Druid class changes",
                    Settings.Druid,
                    DruidRebalance.Configure);
                ConfigureSection(
                    "Hunter class changes",
                    Settings.Hunter,
                    HunterRebalance.Configure);
                ConfigureSection(
                    "Inquisitor class changes",
                    Settings.Inquisitor,
                    InquisitorRebalance.Configure);
                ConfigureSection(
                    "Magus class changes",
                    Settings.Magus,
                    MagusRebalance.Configure);
                ConfigureSection(
                    "Monk class changes",
                    Settings.Monk,
                    MonkRebalance.Configure);
                ConfigureSection(
                    "Paladin class changes",
                    Settings.Paladin,
                    PaladinRebalance.Configure);
                ConfigureSection(
                    "Ranger class changes",
                    Settings.Ranger,
                    RangerRebalance.Configure);
                ConfigureSection(
                    "Sorcerer class changes",
                    Settings.Sorcerer,
                    SorcererRebalance.Configure);
                ConfigureSection(
                    "Warpriest class changes",
                    Settings.Warpriest,
                    WarpriestRebalance.Configure);
                ConfigureSection(
                    "Wizard class changes",
                    Settings.Wizard &&
                    (Settings.WizardSupremeIntellect ||
                     Settings.WizardArcaneBondTwoUses ||
                     Settings.ArcaneBomberBombFeats ||
                     Settings.AnyArcaneDiscoveries),
                    WizardRebalance.Configure);
                ConfigureSection(
                    "Witch class changes",
                    Settings.Witch,
                    WitchRebalance.Configure);
                ConfigureSection(
                    "Shaman class changes",
                    Settings.Shaman,
                    ShamanRebalance.Configure);
                ConfigureSection(
                    "Shifter class changes",
                    Settings.Shifter,
                    ShifterRebalance.Configure);
                ConfigureSection(
                    "Slayer class changes",
                    Settings.Slayer,
                    SlayerRebalance.Configure);
                ConfigureSection(
                    "Skald class changes",
                    Settings.Skald,
                    SkaldRebalance.Configure);
                ConfigureSection(
                    "Rogue class changes",
                    Settings.Rogue,
                    RogueRebalance.Configure);
            } catch (Exception e) {
                Log.LogException(e);
            }
        }

        private static void ConfigureSection(string name, Action configure) {
            try {
                configure();
                Log.Log($"{name} configured.");
            } catch (Exception e) {
                Log.Error($"{name} failed to configure; continuing with later sections.");
                Log.LogException(e);
            }
        }

        private static void ConfigureSection(
            string name,
            bool enabled,
            Action configure) {
            if (!enabled) {
                Log.Log($"{name} disabled in Unity Mod Manager settings.");
                return;
            }
            ConfigureSection(name, configure);
        }
    }
}
