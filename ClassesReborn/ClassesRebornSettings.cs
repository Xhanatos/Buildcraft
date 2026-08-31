using System.Xml.Serialization;
using UnityEngine;
using UnityModManagerNet;

namespace ClassesReborn;

public sealed class ClassesRebornSettings : UnityModManager.ModSettings {
    // Global changes
    public bool BackgroundTraitBonuses = true;
    public bool ImprovedTrapfinding = true;
    public bool CharacterTraits = true;
    public bool ArchetypeStacking = true;
    public bool AlternateHeritageStacking = true;
    public bool UnlimitedBloodlineClaws = true;

    // Added backgrounds
    public bool SarkorianExile = true;
    public bool WardstoneVeteran = true;
    public bool RedeemedCultist = true;
    public bool KenabresWatchman = true;
    public bool NumerianSalvager = true;
    public bool TempleWeaponmaster = true;
    public bool CrusadeQuartermaster = true;
    public bool WorldwoundCartographer = true;

    // Race changes
    public bool HalfOrc = true;
    public bool GoblinRace = true;
    public bool MongrelRace = true;
    public bool HumanAlternateHeritages = true;
    public bool ElfAlternateHeritages = true;
    public bool DwarfAlternateHeritages = true;
    public bool GnomeAlternateHeritages = true;
    public bool HalflingAlternateHeritages = true;
    public bool HalfElfAlternateHeritages = true;
    public bool HalfOrcAlternateHeritages = true;
    public bool KitsuneAlternateHeritages = true;
    public bool AasimarAlternateHeritages = true;
    public bool TieflingAlternateHeritages = true;
    public bool OreadAlternateHeritages = true;
    public bool DhampirAlternateHeritages = true;
    public bool GoblinAlternateHeritages = true;
    public bool MongrelAlternateHeritages = true;

    [XmlIgnore]
    internal bool AnyAlternateRacialHeritages =>
        HumanAlternateHeritages || ElfAlternateHeritages ||
        DwarfAlternateHeritages || GnomeAlternateHeritages ||
        HalflingAlternateHeritages || HalfElfAlternateHeritages ||
        HalfOrcAlternateHeritages || KitsuneAlternateHeritages ||
        AasimarAlternateHeritages || TieflingAlternateHeritages ||
        OreadAlternateHeritages || DhampirAlternateHeritages ||
        GoblinAlternateHeritages || MongrelAlternateHeritages;

    // Class and archetype changes
    public bool Alchemist = true;
    public bool Barbarian = true;
    public bool Bard = true;
    public bool Bloodrager = true;
    public bool Cavalier = true;
    public bool Cleric = true;
    public bool Druid = true;
    public bool Fighter = true;
    public bool DreadArcher = true;
    public bool Hunter = true;
    public bool Inquisitor = true;
    public bool Magus = true;
    public bool Monk = true;
    public bool Paladin = true;
    public bool Ranger = true;
    public bool Rogue = true;
    public bool Shaman = true;
    public bool Shifter = true;
    public bool Skald = true;
    public bool Slayer = true;
    public bool Sorcerer = true;
    public bool Warpriest = true;
    public bool Wizard = true;
    public bool WizardSupremeIntellect = true;
    public bool WizardArcaneBondTwoUses = true;
    public bool ArcaneBomberBombFeats = true;
    public bool Witch = true;

    // Added feats and exploits
    public bool ExtraHexWitch = true;
    public bool ExtraHexShaman = true;
    public bool ExtraRevelation = true;
    public bool HorseMaster = true;
    public bool ErastilsBlessing = true;
    public bool GuidedHand = true;
    public bool Hurtful = true;
    public bool DirtyFighting = true;
    public bool SplitHex = true;
    public bool CursingGaze = true;
    public bool Ricochet = true;
    public bool BashingBulwark = true;
    public bool ShieldedCasting = true;
    public bool HexStrike = true;
    public bool ShieldBrace = true;
    public bool MightyHurling = true;
    public bool CrushingThrow = true;
    public bool BalancedGrip = true;
    public bool TwoWeaponDefense = true;
    public bool ArmorOfThePit = true;
    public bool GreaterUnarmedStrike = true;
    public bool AdditionalTraits = true;
    public bool DervishDance = true;
    public bool QuickStudy = true;
    public bool ArcanistExploitFamiliar = true;
    public bool ArcanistExploitSchoolUnderstanding = true;
    public bool ArcanistExploitSpellThief = true;
    public bool MadMagic = true;
    public bool CrusadersFlurry = true;
    public bool RimeSpell = true;
    public bool DesnasShootingStar = true;
    public bool BladedBrush = true;
    public bool AsceticStyle = true;
    public bool AsceticForm = true;
    public bool AsceticStrike = true;
    public bool FeyFoundling = true;
    public bool ViciousStomp = true;
    public bool UnsanctionedKnowledge = true;
    public bool EldritchHeritage = true;
    public bool FeralCombatTraining = true;
    public bool RacialHeritage = true;
    public bool ArtfulDodge = true;
    public bool CutFromTheAir = true;
    public bool Multiattack = true;
    public bool ImprovedNaturalAttack = true;
    public bool ClawPounce = true;
    public bool CloseQuartersThrower = true;
    public bool JabbingStyle = true;
    public bool JabbingMaster = true;
    public bool TandemTrip = true;
    public bool VolleyFire = true;
    public bool FocusedShot = true;
    public bool TacticalReflexes = true;
    public bool RakingClaws = true;
    public bool ArcaneDiscoveryKnowledgeIsPower = true;
    public bool ArcaneDiscoveryOppositionResearch = true;
    public bool ArcaneDiscoveryCreativeDestruction = true;
    public bool ArcaneDiscoveryAlchemicalAffinity = true;
    public bool ArcaneDiscoveryIdealize = true;

    [XmlIgnore]
    internal bool AnyArcaneDiscoveries =>
        ArcaneDiscoveryKnowledgeIsPower ||
        ArcaneDiscoveryOppositionResearch ||
        ArcaneDiscoveryCreativeDestruction ||
        ArcaneDiscoveryAlchemicalAffinity ||
        ArcaneDiscoveryIdealize;

    // Added rage powers
    public bool RagePowerSuperstition = true;
    public bool RagePowerWitchHunter = true;
    public bool RagePowerEaterOfMagic = true;
    public bool RagePowerStrengthSurge = true;
    public bool RagePowerElementalRage = true;
    public bool RagePowerGhostRager = true;
    public bool RagePowerFerociousMount = true;

    // Added spells and spell-list changes
    public bool LongArm = true;
    public bool DanceOfAHundredCuts = true;
    public bool BlisteringInvective = true;
    public bool ArcaneConcordance = true;
    public bool BurstOfRadiance = true;
    public bool BladeTutorsSpirit = true;
    public bool DeadlyJuggernaut = true;
    public bool Shillelagh = true;
    public bool WeaponOfAwe = true;
    public bool SanctifyArmor = true;
    public bool ForcefulStrike = true;
    public bool WrathfulWeapon = true;
    public bool BardTrueStrike = true;
    public bool BardMagicWeapon = true;
    public bool BardGreaterMagicWeapon = true;
    public bool PaladinAid = true;
    public bool PaladinFreedomOfMovement = true;

    [XmlIgnore] public bool ShowGlobal = true;
    [XmlIgnore] public bool ShowRaces = true;
    [XmlIgnore] public bool ShowBackgrounds;
    [XmlIgnore] public bool ShowClasses = true;
    [XmlIgnore] public bool ShowFeats;
    [XmlIgnore] public bool ShowRagePowers;
    [XmlIgnore] public bool ShowSpells;

    public override void Save(UnityModManager.ModEntry modEntry) =>
        Save(this, modEntry);

    internal void Draw(UnityModManager.ModEntry modEntry) {
        GUILayout.Label("All settings are applied while Wrath builds its blueprint cache.");
        GUILayout.Label("Save your choices, close Wrath completely, and restart the game. Respec characters after changing progression options.");
        GUILayout.Space(6f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save settings", GUILayout.Width(180f))) {
            Save(modEntry);
            Main.Log.Log("Buildcraft settings saved. Restart Wrath to apply them.");
        }
        if (GUILayout.Button("Enable everything", GUILayout.Width(180f))) {
            SetEverything(true);
        }
        if (GUILayout.Button("Disable everything", GUILayout.Width(180f))) {
            SetEverything(false);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        DrawGlobalSection();
        DrawBackgroundSection();
        DrawRaceSection();
        DrawClassSection();
        DrawFeatSection();
        DrawRagePowerSection();
        DrawSpellSection();
    }

    private void DrawGlobalSection() {
        if (!DrawSectionHeader(ref ShowGlobal, "Global changes", SetGlobal)) {
            return;
        }
        DrawToggleRow(
            ("Background bonuses use the Trait descriptor", () => BackgroundTraitBonuses, value => BackgroundTraitBonuses = value),
            ("Improved Trapfinding for every class", () => ImprovedTrapfinding, value => ImprovedTrapfinding = value));
        DrawToggleRow(
            ("Two character traits at character creation", () => CharacterTraits, value => CharacterTraits = value),
            ("Tabletop-compatible archetype stacking", () => ArchetypeStacking, value => ArchetypeStacking = value));
        DrawToggleRow(
            ("Unlimited Abyssal and Draconic bloodline claws", () => UnlimitedBloodlineClaws, value => UnlimitedBloodlineClaws = value));
        if (ArchetypeStacking &&
            ClassesReborn.ArchetypeStacking.TryGetExternalProvider(out var provider)) {
            GUILayout.Label(
                $"Archetype stacking is currently supplied by {provider}; " +
                "Buildcraft suspends its overlapping patches to prevent conflicts.");
        }
        GUILayout.EndVertical();
    }

    private void DrawRaceSection() {
        if (!DrawSectionHeader(ref ShowRaces, "Race changes", SetRaces)) {
            return;
        }
        DrawToggleRow(
            ("Half-Orc: +2 Strength, -2 Intelligence", () => HalfOrc, value => HalfOrc = value),
            ("Playable Goblin race", () => GoblinRace, value => GoblinRace = value));
        DrawToggleRow(
            ("Playable Mongrel race", () => MongrelRace, value => MongrelRace = value));
        DrawToggleRow(
            ("Compatible alternate-heritage stacking", () => AlternateHeritageStacking, value => AlternateHeritageStacking = value));
        DrawToggleRow(
            ("Human alternate heritages", () => HumanAlternateHeritages, value => HumanAlternateHeritages = value),
            ("Elf alternate heritages", () => ElfAlternateHeritages, value => ElfAlternateHeritages = value));
        DrawToggleRow(
            ("Dwarf alternate heritages", () => DwarfAlternateHeritages, value => DwarfAlternateHeritages = value),
            ("Gnome alternate heritages", () => GnomeAlternateHeritages, value => GnomeAlternateHeritages = value));
        DrawToggleRow(
            ("Halfling alternate heritages", () => HalflingAlternateHeritages, value => HalflingAlternateHeritages = value),
            ("Half-Elf alternate heritages", () => HalfElfAlternateHeritages, value => HalfElfAlternateHeritages = value));
        DrawToggleRow(
            ("Half-Orc alternate heritages", () => HalfOrcAlternateHeritages, value => HalfOrcAlternateHeritages = value),
            ("Kitsune alternate heritages", () => KitsuneAlternateHeritages, value => KitsuneAlternateHeritages = value));
        DrawToggleRow(
            ("Aasimar alternate heritages", () => AasimarAlternateHeritages, value => AasimarAlternateHeritages = value),
            ("Tiefling alternate heritages", () => TieflingAlternateHeritages, value => TieflingAlternateHeritages = value));
        DrawToggleRow(
            ("Oread alternate heritages", () => OreadAlternateHeritages, value => OreadAlternateHeritages = value),
            ("Dhampir alternate heritages", () => DhampirAlternateHeritages, value => DhampirAlternateHeritages = value));
        DrawToggleRow(
            ("Goblin alternate heritages", () => GoblinAlternateHeritages, value => GoblinAlternateHeritages = value),
            ("Mongrel alternate heritages", () => MongrelAlternateHeritages, value => MongrelAlternateHeritages = value));
        GUILayout.EndVertical();
    }

    private void DrawBackgroundSection() {
        if (!DrawSectionHeader(ref ShowBackgrounds, "Added backgrounds", SetBackgrounds)) {
            return;
        }
        DrawToggleRow(
            ("Sarkorian Exile", () => SarkorianExile, value => SarkorianExile = value),
            ("Wardstone Veteran", () => WardstoneVeteran, value => WardstoneVeteran = value));
        DrawToggleRow(
            ("Redeemed Cultist", () => RedeemedCultist, value => RedeemedCultist = value),
            ("Kenabres Watchman", () => KenabresWatchman, value => KenabresWatchman = value));
        DrawToggleRow(
            ("Numerian Salvager", () => NumerianSalvager, value => NumerianSalvager = value),
            ("Temple Weaponmaster", () => TempleWeaponmaster, value => TempleWeaponmaster = value));
        DrawToggleRow(
            ("Crusade Quartermaster", () => CrusadeQuartermaster, value => CrusadeQuartermaster = value),
            ("Worldwound Cartographer", () => WorldwoundCartographer, value => WorldwoundCartographer = value));
        GUILayout.EndVertical();
    }

    private void DrawClassSection() {
        if (!DrawSectionHeader(ref ShowClasses, "Class and archetype changes", SetClasses)) {
            return;
        }
        DrawToggleRow(
            ("Alchemist", () => Alchemist, value => Alchemist = value),
            ("Barbarian", () => Barbarian, value => Barbarian = value));
        DrawToggleRow(
            ("Bard", () => Bard, value => Bard = value),
            ("Bloodrager", () => Bloodrager, value => Bloodrager = value));
        DrawToggleRow(
            ("Cavalier", () => Cavalier, value => Cavalier = value),
            ("Cleric", () => Cleric, value => Cleric = value));
        DrawToggleRow(
            ("Druid", () => Druid, value => Druid = value),
            ("Fighter", () => Fighter, value => Fighter = value));
        DrawToggleRow(
            ("Dread Archer archetype", () => DreadArcher, value => DreadArcher = value));
        DrawToggleRow(
            ("Hunter", () => Hunter, value => Hunter = value),
            ("Inquisitor", () => Inquisitor, value => Inquisitor = value));
        DrawToggleRow(
            ("Magus", () => Magus, value => Magus = value),
            ("Monk", () => Monk, value => Monk = value));
        DrawToggleRow(
            ("Paladin", () => Paladin, value => Paladin = value),
            ("Ranger", () => Ranger, value => Ranger = value));
        DrawToggleRow(
            ("Rogue", () => Rogue, value => Rogue = value),
            ("Shaman", () => Shaman, value => Shaman = value));
        DrawToggleRow(
            ("Shifter", () => Shifter, value => Shifter = value),
            ("Skald", () => Skald, value => Skald = value));
        DrawToggleRow(
            ("Slayer", () => Slayer, value => Slayer = value),
            ("Sorcerer", () => Sorcerer, value => Sorcerer = value));
        DrawToggleRow(
            ("Warpriest", () => Warpriest, value => Warpriest = value),
            ("Wizard", () => Wizard, value => Wizard = value));
        DrawToggleRow(
            ("Witch", () => Witch, value => Witch = value));
        DrawToggleRow(
            ("Wizard: Supreme Intellect capstone", () => WizardSupremeIntellect, value => WizardSupremeIntellect = value),
            ("Wizard: Arcane Bond — Object twice per day", () => WizardArcaneBondTwoUses, value => WizardArcaneBondTwoUses = value));
        DrawToggleRow(
            ("Arcane Bomber: Fast Bombs and Precise Bombs", () => ArcaneBomberBombFeats, value => ArcaneBomberBombFeats = value));
        GUILayout.EndVertical();
    }

    private void DrawFeatSection() {
        if (!DrawSectionHeader(ref ShowFeats, "Added feats and exploits", SetFeats)) {
            return;
        }
        DrawToggleRow(
            ("Extra Hex (Witch)", () => ExtraHexWitch, value => ExtraHexWitch = value),
            ("Extra Hex (Shaman)", () => ExtraHexShaman, value => ExtraHexShaman = value));
        DrawToggleRow(
            ("Extra Revelation", () => ExtraRevelation, value => ExtraRevelation = value),
            ("Horse Master", () => HorseMaster, value => HorseMaster = value));
        DrawToggleRow(
            ("Erastil's Blessing", () => ErastilsBlessing, value => ErastilsBlessing = value),
            ("Guided Hand", () => GuidedHand, value => GuidedHand = value));
        DrawToggleRow(
            ("Hurtful", () => Hurtful, value => Hurtful = value),
            ("Dirty Fighting", () => DirtyFighting, value => DirtyFighting = value));
        DrawToggleRow(
            ("Split Hex", () => SplitHex, value => SplitHex = value),
            ("Shield Brace", () => ShieldBrace, value => ShieldBrace = value));
        DrawToggleRow(
            ("Cursing Gaze (mythic ability)", () => CursingGaze, value => CursingGaze = value),
            ("Hex Strike", () => HexStrike, value => HexStrike = value));
        DrawToggleRow(
            ("Ricochet (mythic ability)", () => Ricochet, value => Ricochet = value),
            ("Bashing Bulwark (mythic ability)", () => BashingBulwark, value => BashingBulwark = value));
        DrawToggleRow(
            ("Shielded Casting (mythic ability)", () => ShieldedCasting, value => ShieldedCasting = value));
        DrawToggleRow(
            ("Mighty Hurling", () => MightyHurling, value => MightyHurling = value),
            ("Crushing Throw", () => CrushingThrow, value => CrushingThrow = value));
        DrawToggleRow(
            ("Balanced Grip", () => BalancedGrip, value => BalancedGrip = value),
            ("Dervish Dance", () => DervishDance, value => DervishDance = value));
        DrawToggleRow(
            ("Two-Weapon Defense", () => TwoWeaponDefense, value => TwoWeaponDefense = value),
            ("Armor of the Pit", () => ArmorOfThePit, value => ArmorOfThePit = value));
        DrawToggleRow(
            ("Greater Unarmed Strike", () => GreaterUnarmedStrike, value => GreaterUnarmedStrike = value),
            ("Additional Traits", () => AdditionalTraits, value => AdditionalTraits = value));
        DrawToggleRow(
            ("Quick Study (Arcanist exploit)", () => QuickStudy, value => QuickStudy = value),
            ("Familiar (Arcanist exploit)", () => ArcanistExploitFamiliar, value => ArcanistExploitFamiliar = value));
        DrawToggleRow(
            ("School Understanding (Arcanist exploit)", () => ArcanistExploitSchoolUnderstanding, value => ArcanistExploitSchoolUnderstanding = value),
            ("Spell Thief (greater Arcanist exploit)", () => ArcanistExploitSpellThief, value => ArcanistExploitSpellThief = value));
        DrawToggleRow(
            ("Mad Magic", () => MadMagic, value => MadMagic = value));
        DrawToggleRow(
            ("Crusader's Flurry", () => CrusadersFlurry, value => CrusadersFlurry = value),
            ("Rime Spell", () => RimeSpell, value => RimeSpell = value));
        DrawToggleRow(
            ("Desna's Shooting Star", () => DesnasShootingStar, value => DesnasShootingStar = value),
            ("Bladed Brush", () => BladedBrush, value => BladedBrush = value));
        DrawToggleRow(
            ("Ascetic Style", () => AsceticStyle, value => AsceticStyle = value),
            ("Ascetic Form", () => AsceticForm, value => AsceticForm = value));
        DrawToggleRow(
            ("Ascetic Strike", () => AsceticStrike, value => AsceticStrike = value),
            ("Fey Foundling", () => FeyFoundling, value => FeyFoundling = value));
        DrawToggleRow(
            ("Vicious Stomp", () => ViciousStomp, value => ViciousStomp = value),
            ("Unsanctioned Knowledge", () => UnsanctionedKnowledge, value => UnsanctionedKnowledge = value));
        DrawToggleRow(
            ("Eldritch Heritage chain", () => EldritchHeritage, value => EldritchHeritage = value),
            ("Feral Combat Training", () => FeralCombatTraining, value => FeralCombatTraining = value));
        DrawToggleRow(
            ("Racial Heritage", () => RacialHeritage, value => RacialHeritage = value),
            ("Artful Dodge", () => ArtfulDodge, value => ArtfulDodge = value));
        DrawToggleRow(
            ("Cut from the Air", () => CutFromTheAir, value => CutFromTheAir = value),
            ("Multiattack", () => Multiattack, value => Multiattack = value));
        DrawToggleRow(
            ("Improved Natural Attack", () => ImprovedNaturalAttack, value => ImprovedNaturalAttack = value),
            ("Claw Pounce", () => ClawPounce, value => ClawPounce = value));
        DrawToggleRow(
            ("Close-Quarters Thrower", () => CloseQuartersThrower, value => CloseQuartersThrower = value),
            ("Jabbing Style", () => JabbingStyle, value => JabbingStyle = value));
        DrawToggleRow(
            ("Jabbing Master", () => JabbingMaster, value => JabbingMaster = value),
            ("Tandem Trip", () => TandemTrip, value => TandemTrip = value));
        DrawToggleRow(
            ("Volley Fire", () => VolleyFire, value => VolleyFire = value),
            ("Focused Shot", () => FocusedShot, value => FocusedShot = value));
        DrawToggleRow(
            ("Tactical Reflexes", () => TacticalReflexes, value => TacticalReflexes = value),
            ("Raking Claws prerequisites", () => RakingClaws, value => RakingClaws = value));
        DrawToggleRow(
            ("Arcane Discovery: Knowledge Is Power", () => ArcaneDiscoveryKnowledgeIsPower, value => ArcaneDiscoveryKnowledgeIsPower = value),
            ("Arcane Discovery: Opposition Research", () => ArcaneDiscoveryOppositionResearch, value => ArcaneDiscoveryOppositionResearch = value));
        DrawToggleRow(
            ("Arcane Discovery: Creative Destruction", () => ArcaneDiscoveryCreativeDestruction, value => ArcaneDiscoveryCreativeDestruction = value),
            ("Arcane Discovery: Alchemical Affinity", () => ArcaneDiscoveryAlchemicalAffinity, value => ArcaneDiscoveryAlchemicalAffinity = value));
        DrawToggleRow(
            ("Arcane Discovery: Idealize", () => ArcaneDiscoveryIdealize, value => ArcaneDiscoveryIdealize = value));
        GUILayout.EndVertical();
    }

    private void DrawSpellSection() {
        if (!DrawSectionHeader(ref ShowSpells, "Added spells and spell-list changes", SetSpells)) {
            return;
        }
        DrawToggleRow(
            ("Long Arm", () => LongArm, value => LongArm = value),
            ("Dance of a Hundred Cuts", () => DanceOfAHundredCuts, value => DanceOfAHundredCuts = value));
        DrawToggleRow(
            ("Blistering Invective", () => BlisteringInvective, value => BlisteringInvective = value),
            ("Arcane Concordance", () => ArcaneConcordance, value => ArcaneConcordance = value));
        DrawToggleRow(
            ("Burst of Radiance", () => BurstOfRadiance, value => BurstOfRadiance = value),
            ("Blade Tutor's Spirit", () => BladeTutorsSpirit, value => BladeTutorsSpirit = value));
        DrawToggleRow(
            ("Deadly Juggernaut", () => DeadlyJuggernaut, value => DeadlyJuggernaut = value),
            ("Shillelagh", () => Shillelagh, value => Shillelagh = value));
        DrawToggleRow(
            ("Weapon of Awe", () => WeaponOfAwe, value => WeaponOfAwe = value),
            ("Sanctify Armor", () => SanctifyArmor, value => SanctifyArmor = value));
        DrawToggleRow(
            ("Forceful Strike", () => ForcefulStrike, value => ForcefulStrike = value),
            ("Wrathful Weapon", () => WrathfulWeapon, value => WrathfulWeapon = value));
        DrawToggleRow(
            ("Bard: True Strike", () => BardTrueStrike, value => BardTrueStrike = value),
            ("Bard: Magic Weapon", () => BardMagicWeapon, value => BardMagicWeapon = value));
        DrawToggleRow(
            ("Bard: Greater Magic Weapon", () => BardGreaterMagicWeapon, value => BardGreaterMagicWeapon = value),
            ("Paladin: Aid", () => PaladinAid, value => PaladinAid = value));
        DrawToggleRow(
            ("Paladin: Freedom of Movement", () => PaladinFreedomOfMovement, value => PaladinFreedomOfMovement = value));
        GUILayout.EndVertical();
    }

    private void DrawRagePowerSection() {
        if (!DrawSectionHeader(ref ShowRagePowers, "Added rage powers", SetRagePowers)) {
            return;
        }
        DrawToggleRow(
            ("Superstition", () => RagePowerSuperstition, value => RagePowerSuperstition = value),
            ("Witch Hunter", () => RagePowerWitchHunter, value => RagePowerWitchHunter = value));
        DrawToggleRow(
            ("Eater of Magic", () => RagePowerEaterOfMagic, value => RagePowerEaterOfMagic = value),
            ("Strength Surge", () => RagePowerStrengthSurge, value => RagePowerStrengthSurge = value));
        DrawToggleRow(
            ("Elemental Rage chain", () => RagePowerElementalRage, value => RagePowerElementalRage = value),
            ("Ghost Rager", () => RagePowerGhostRager, value => RagePowerGhostRager = value));
        DrawToggleRow(
            ("Ferocious Mount chain", () => RagePowerFerociousMount, value => RagePowerFerociousMount = value));
        GUILayout.EndVertical();
    }

    private static bool DrawSectionHeader(
        ref bool expanded,
        string title,
        Action<bool> setGroup) {
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"{(expanded ? "▼" : "▶")} {title}")) {
            expanded = !expanded;
        }
        if (GUILayout.Button("All", GUILayout.Width(60f))) {
            setGroup(true);
        }
        if (GUILayout.Button("None", GUILayout.Width(60f))) {
            setGroup(false);
        }
        GUILayout.EndHorizontal();
        if (!expanded) {
            GUILayout.EndVertical();
        }
        return expanded;
    }

    private static void DrawToggleRow(
        params (string Label, Func<bool> Get, Action<bool> Set)[] entries) {
        GUILayout.BeginHorizontal();
        foreach (var entry in entries) {
            var current = entry.Get();
            var updated = GUILayout.Toggle(
                current,
                entry.Label,
                GUILayout.Width(360f));
            if (updated != current) {
                entry.Set(updated);
            }
        }
        GUILayout.EndHorizontal();
    }

    private void SetEverything(bool value) {
        SetGlobal(value);
        SetBackgrounds(value);
        SetRaces(value);
        SetClasses(value);
        SetFeats(value);
        SetRagePowers(value);
        SetSpells(value);
    }

    private void SetGlobal(bool value) {
        BackgroundTraitBonuses = value;
        ImprovedTrapfinding = value;
        CharacterTraits = value;
        ArchetypeStacking = value;
        UnlimitedBloodlineClaws = value;
    }

    private void SetRaces(bool value) {
        HalfOrc = value;
        GoblinRace = value;
        MongrelRace = value;
        AlternateHeritageStacking = value;
        HumanAlternateHeritages = value;
        ElfAlternateHeritages = value;
        DwarfAlternateHeritages = value;
        GnomeAlternateHeritages = value;
        HalflingAlternateHeritages = value;
        HalfElfAlternateHeritages = value;
        HalfOrcAlternateHeritages = value;
        KitsuneAlternateHeritages = value;
        AasimarAlternateHeritages = value;
        TieflingAlternateHeritages = value;
        OreadAlternateHeritages = value;
        DhampirAlternateHeritages = value;
        GoblinAlternateHeritages = value;
        MongrelAlternateHeritages = value;
    }

    private void SetBackgrounds(bool value) {
        SarkorianExile = value;
        WardstoneVeteran = value;
        RedeemedCultist = value;
        KenabresWatchman = value;
        NumerianSalvager = value;
        TempleWeaponmaster = value;
        CrusadeQuartermaster = value;
        WorldwoundCartographer = value;
    }

    private void SetClasses(bool value) {
        Alchemist = value;
        Barbarian = value;
        Bard = value;
        Bloodrager = value;
        Cavalier = value;
        Cleric = value;
        Druid = value;
        Fighter = value;
        DreadArcher = value;
        Hunter = value;
        Inquisitor = value;
        Magus = value;
        Monk = value;
        Paladin = value;
        Ranger = value;
        Rogue = value;
        Shaman = value;
        Shifter = value;
        Skald = value;
        Slayer = value;
        Sorcerer = value;
        Warpriest = value;
        Wizard = value;
        WizardSupremeIntellect = value;
        WizardArcaneBondTwoUses = value;
        ArcaneBomberBombFeats = value;
        Witch = value;
    }

    private void SetFeats(bool value) {
        ExtraHexWitch = value;
        ExtraHexShaman = value;
        ExtraRevelation = value;
        HorseMaster = value;
        ErastilsBlessing = value;
        GuidedHand = value;
        Hurtful = value;
        DirtyFighting = value;
        SplitHex = value;
        CursingGaze = value;
        Ricochet = value;
        BashingBulwark = value;
        ShieldedCasting = value;
        HexStrike = value;
        ShieldBrace = value;
        MightyHurling = value;
        CrushingThrow = value;
        BalancedGrip = value;
        TwoWeaponDefense = value;
        ArmorOfThePit = value;
        GreaterUnarmedStrike = value;
        AdditionalTraits = value;
        DervishDance = value;
        QuickStudy = value;
        ArcanistExploitFamiliar = value;
        ArcanistExploitSchoolUnderstanding = value;
        ArcanistExploitSpellThief = value;
        MadMagic = value;
        CrusadersFlurry = value;
        RimeSpell = value;
        DesnasShootingStar = value;
        BladedBrush = value;
        AsceticStyle = value;
        AsceticForm = value;
        AsceticStrike = value;
        FeyFoundling = value;
        ViciousStomp = value;
        UnsanctionedKnowledge = value;
        EldritchHeritage = value;
        FeralCombatTraining = value;
        RacialHeritage = value;
        ArtfulDodge = value;
        CutFromTheAir = value;
        Multiattack = value;
        ImprovedNaturalAttack = value;
        ClawPounce = value;
        CloseQuartersThrower = value;
        JabbingStyle = value;
        JabbingMaster = value;
        TandemTrip = value;
        VolleyFire = value;
        FocusedShot = value;
        TacticalReflexes = value;
        RakingClaws = value;
        ArcaneDiscoveryKnowledgeIsPower = value;
        ArcaneDiscoveryOppositionResearch = value;
        ArcaneDiscoveryCreativeDestruction = value;
        ArcaneDiscoveryAlchemicalAffinity = value;
        ArcaneDiscoveryIdealize = value;
    }

    private void SetSpells(bool value) {
        LongArm = value;
        DanceOfAHundredCuts = value;
        BlisteringInvective = value;
        ArcaneConcordance = value;
        BurstOfRadiance = value;
        BladeTutorsSpirit = value;
        DeadlyJuggernaut = value;
        Shillelagh = value;
        WeaponOfAwe = value;
        SanctifyArmor = value;
        ForcefulStrike = value;
        WrathfulWeapon = value;
        BardTrueStrike = value;
        BardMagicWeapon = value;
        BardGreaterMagicWeapon = value;
        PaladinAid = value;
        PaladinFreedomOfMovement = value;
    }

    private void SetRagePowers(bool value) {
        RagePowerSuperstition = value;
        RagePowerWitchHunter = value;
        RagePowerEaterOfMagic = value;
        RagePowerStrengthSurge = value;
        RagePowerElementalRage = value;
        RagePowerGhostRager = value;
        RagePowerFerociousMount = value;
    }
}
