# Buildcraft

Buildcraft is a standalone Unity Mod Manager mod for **Pathfinder: Wrath of
the Righteous**. It expands character creation and build variety with new
playable races, archetypes, backgrounds, traits, feats, rage powers, spells,
class options, capstones, and progression reworks.

The mod does not add a new base class. Instead, it broadens the choices and
improves the progression of the game's existing classes and archetypes.

## Compatibility notice

> **Buildcraft has currently only been tested together with Toy Box.** It is
> intended to be used as a standalone content mod. Using Buildcraft alongside
> other mods that add or alter classes, archetypes, races, feats, spells,
> character-creation options, or other gameplay content will most likely cause
> conflicts or unexpected problems.

Please report any bugs you encounter through
[GitHub Issues](https://github.com/Xhanatos/Buildcraft/issues) so they can be
investigated and fixed. Include the Buildcraft version, your other installed
mods, and any relevant screenshots or log files when possible.

## Requirements

- Pathfinder: Wrath of the Righteous 2.7.0x
- Unity Mod Manager 0.27.11 or newer

BlueprintCore is merged into the compiled mod. TabletopTweaks and other
gameplay mods are not required.

## Installation

1. Download the latest `Buildcraft-<version>.zip` from
   [GitHub Releases](https://github.com/Xhanatos/Buildcraft/releases).
2. Install the ZIP through Unity Mod Manager, or extract its contents to
   `<Wrath installation>/Mods/ClassesReborn/`.
3. Start the game and configure Buildcraft from the Unity Mod Manager menu.

When changing an option, save the settings, close Wrath completely, and
restart it. Respec characters affected by progression changes. Disabling
custom content already selected by a character is not recommended until that
character has been respecced without it.

## Content overview

Buildcraft 3.44.5 contains:

| Content category | Count |
| --- | ---: |
| Playable races | 2 |
| New Fighter archetypes | 1 |
| New alternate racial heritages | 47 |
| Worldwound backgrounds | 8 |
| Character traits | 87 |
| Added feat entries | 48 |
| Mythic abilities | 4 |
| Selectable rage powers | 16 |
| Arcanist exploits | 4 |
| Alchemist discoveries | 4 |
| Wizard Arcane Discoveries | 5 |
| New spells | 12 |
| Existing spells added to new class lists | 5 |
| Reworked class families | 23 |

The counts above describe player-facing selections. Variants such as the four
energy choices of Elemental Rage are counted separately when they are
separate selectable options.

## Playable races — 2

### Goblin

Goblin uses Owlcat's native Goblin model, animations, equipment support, and
three body presets. Both genders have working character-creation models and
support skin color, eye color, scars, warpaint, tattoos, and outfit colors.

- Ability modifiers: +4 Dexterity, -2 Strength, -2 Charisma
- Base racial abilities: Stealthy, Keen Senses, and 30-foot movement
- Six alternate heritages
- Six Goblin racial traits

### Mongrel

Mongrel uses a distinct playable race identity with Human-compatible
character-doll customization and eight optional mutation visuals: two
mismatched-ear variants, five horn variants, and unusual glossy eyes.

- Ability modifiers: +2 Strength, +2 Dexterity, -2 Charisma
- Base racial abilities: Mixed Ancestry, Mongrel Resilience, Underground
  Survivor, and Sound Mimicry
- Eight alternate heritages
- Five Mongrel racial traits

## New Fighter archetype — 1

- **Dread Archer** — a ranged fear specialist with Merciless Reputation,
  Deadly Aim, Painful Shots, Ranged Weapon Training, Merciless, Dreadful
  Carnage, and an increased carnage radius at level 18.

## Alternate racial heritages — 47 new options

Buildcraft adds compatible heritage stacking and 47 new alternate heritages
across 14 races:

- **Human (10):** Eye for Talent, Heart of the Fey, Focused Study, Awareness,
  Military Tradition, Unstoppable Magic, Dimdweller, Giant Ancestry, Heart of
  the Slums, Practiced Hunter
- **Elf (2):** Fleet-Footed, Dreamspeaker
- **Dwarf (2):** Magic Resistant, Relentless
- **Gnome (2):** Eternal Hope, Fell Magic
- **Halfling (2):** Jinxed, Low Blow
- **Half-Elf (1):** Ancestral Arms
- **Half-Orc (3):** Sacred Tattoo, Toothy, Shaman's Apprentice
- **Kitsune (2):** Skilled Kitsune, Fast Shifter
- **Aasimar (3):** Heavenborn, Deathless Spirit, Celestial Crusader
- **Tiefling (2):** Maw or Claw, Fiendish Sprinter
- **Oread (2):** Stone in the Blood, Earth Insight
- **Dhampir (2):** Dayborn, Vampiric Fangs
- **Goblin (6):** Cave Crawler, City Scavenger, Eat Anything, Hard Head, Big
  Teeth, Over-Sized Ears, Tree Runner
- **Mongrel (8):** Chitin-Plated, Keen-Eared, Hooved Runner, First-Crusade
  Scion, Adaptive Lineage, Cliffborn, Fanged Offshoot, Crushing Limbs

The system also integrates the native **Keen Kitsune**, **Pyromaniac**, and
**Dual-Minded** options into its compatibility rules. These three native
heritages are not included in the count of 47 new options.

## Worldwound backgrounds — 8

- Sarkorian Exile
- Wardstone Veteran
- Redeemed Cultist
- Kenabres Watchman
- Numerian Salvager
- Temple Weaponmaster
- Crusade Quartermaster
- Worldwound Cartographer

## Character traits — 87

Every new or fully respecced character can select up to two different traits
at level 1. The one-time **Additional Traits** feat grants two more choices.
Traits are divided into Combat, Faith, Magic, Social, and Racial categories;
each slot can also be skipped with **None**.

### Combat traits — 12

Anatomist, Armor Expert, Reactionary, Resilient, Bullied, Courageous, Deft
Dodger, Dirty Fighter, Fencer, Killer, Sharp Nails, Shield Fighter

### Faith traits — 8

Birthmark, Devotee of the Green, Ease of Faith, History of Heresy, Indomitable
Faith, Sacred Conduit, Scholar of the Great Beyond, Fate's Favored

### Magic traits — 7

Dangerously Curious, Focused Mind, Skeptic, Mathematical Prodigy, Pragmatic
Activator, Two-World Magic, Gifted Adept

### Social traits — 5

Bully, Child of the Streets, Fast-Talker, Bruising Intellect, Adopted

### Racial traits — 55

- **Human (4):** Bred for War, Superstitious (Kellid), Weapon Training
  (Ulfen), Warrior of Old
- **Elf (3):** Forlorn, Insular, Ruthless
- **Dwarf (5):** Grounded, Deep Marker, Tunnel Fighter, Warsmith, Adrenaline
  Rush
- **Gnome (4):** Illusion Obsession, Rapscallion, Animal Friend,
  Well-Informed
- **Halfling (2):** Intrepid Volunteer, Successful Shirker
- **Half-Elf (3):** Elven Reflexes, Failed Apprentice, Experimental Rebel
- **Half-Orc (5):** Cruel Rager, Finish the Fight, Tusked, Scrapper, Brute
  (Orc)
- **Aasimar (3):** Celestial Contact, Martyr's Blood, Toxophilite
- **Tiefling (5):** Adrift, Selective Health, Dark Magic Affinity, Hard to Pin
  Down, Shadow Stabber
- **Oread (3):** Ever Wary, Born Damned, Stoic Dignity
- **Dhampir (3):** Acknowledged Scion, Undead Slayer, Half-Forgotten Secrets
- **Kitsune (4):** Clever Predator, Two Faces, One Mind, Hidden Tail, Vulpine
  Ambusher
- **Goblin (6):** Color Thief, Foul Belch, Goblin Foolhardiness, Bouncy,
  Underfoot Menace, Vile Chemist
- **Mongrel (5):** Crusader's Descendant, Unsettling Appearance, Twisted
  Balance, Many-Blooded, Hardened Mutation

Racial traits normally require their associated race. **Adopted** allows a
character to select a racial trait belonging to another race.

## Added feats — 48

Buildcraft adds the repeatable **Arcane Discovery** selection plus 47 named
general, combat, racial, style, teamwork, metamagic, and class-support feats:

- Extra Hex (Witch)
- Extra Hex (Shaman)
- Extra Revelation
- Horse Master
- Erastil's Blessing
- Guided Hand
- Hurtful
- Dirty Fighting
- Split Hex
- Hex Strike
- Shield Brace
- Mighty Hurling
- Crushing Throw
- Balanced Grip
- Two-Weapon Defense
- Armor of the Pit
- Greater Unarmed Strike
- Additional Traits
- Dervish Dance
- Mad Magic
- Crusader's Flurry
- Rime Spell
- Desna's Shooting Star
- Bladed Brush
- Ascetic Style
- Ascetic Form
- Ascetic Strike
- Fey Foundling
- Vicious Stomp
- Unsanctioned Knowledge
- Eldritch Heritage
- Improved Eldritch Heritage
- Greater Eldritch Heritage
- Feral Combat Training
- Racial Heritage
- Artful Dodge
- Cut from the Air
- Multiattack
- Improved Natural Attack
- Claw Pounce
- Close-Quarters Thrower
- Jabbing Style
- Jabbing Master
- Tandem Trip
- Volley Fire
- Focused Shot
- Tactical Reflexes

Buildcraft also changes the native **Raking Claws** prerequisite to base
attack bonus +4 and removes its Wild Shape and Major Form requirements.

## Mythic abilities — 4

- Cursing Gaze
- Ricochet
- Bashing Bulwark
- Shielded Casting

## Rage powers — 16 selectable options

- Superstition
- Witch Hunter
- Eater of Magic
- Strength Surge
- Elemental Rage — Acid
- Elemental Rage — Cold
- Elemental Rage — Electricity
- Elemental Rage — Fire
- Greater Elemental Rage — Acid
- Greater Elemental Rage — Cold
- Greater Elemental Rage — Electricity
- Greater Elemental Rage — Fire
- Ghost Rager
- Ferocious Mount
- Greater Ferocious Mount
- Spirit Steed

These powers are registered for the appropriate Barbarian, Bloodrager, Skald,
and rage-power-sharing selections while retaining their prerequisites.

## Arcanist exploits — 4

- Quick Study
- Familiar
- School Understanding
- Spell Thief (greater exploit)

School Understanding supports every Wizard school and includes a Bolster
ability that spends arcane reservoir to temporarily use the character's full
effective exploit level. Exploiter Wizards and Arcane Enforcers use
Intelligence for Buildcraft's exploits; ordinary Arcanists use Charisma.

## Alchemist discoveries — 4

- Bone-Spike Mutagen
- Collective Memory
- Pheromones
- True Cognatogen

All Alchemists also gain **Awakened Intellect** at level 20. True Cognatogen
takes its former place in the Grand Discovery selection.

## Wizard Arcane Discoveries — 5

- Knowledge Is Power
- Opposition Research
- Creative Destruction
- Alchemical Affinity
- Idealize

The repeatable **Arcane Discovery** feat is available from both the general
feat list and Wizard bonus-feat selections. Each individual discovery can be
selected only once.

## New spells — 12

- Long Arm
- Dance of a Hundred Cuts
- Blistering Invective
- Arcane Concordance
- Burst of Radiance
- Blade Tutor's Spirit
- Deadly Juggernaut
- Shillelagh
- Weapon of Awe
- Sanctify Armor
- Forceful Strike
- Wrathful Weapon

Buildcraft also adds five existing spells to new class lists:

- **Bard:** True Strike, Magic Weapon, Greater Magic Weapon
- **Paladin:** Aid, Freedom of Movement

Spell levels and class lists are documented in the in-game tooltips and the
[changelog](CHANGELOG.txt).

## Class and archetype reworks — 23 class families

Buildcraft changes the following class families:

Alchemist, Barbarian, Bard, Bloodrager, Cavalier, Cleric, Druid, Fighter,
Hunter, Inquisitor, Magus, Monk, Paladin, Ranger, Rogue, Shaman, Shifter,
Skald, Slayer, Sorcerer, Warpriest, Wizard, and Witch.

The reworks restore missing base features, improve weak archetype exchanges,
expand talent and bonus-feat schedules, correct bonus types and scaling,
improve action economy, and add level-20 capstones. Major named additions
include:

- **Alchemist:** Awakened Intellect and True Cognatogen
- **Barbarian:** Battleborn and revised damage-reduction progressions
- **Bard:** Bard Talents, True Luck, and True Artist
- **Bloodrager:** Consuming Rage
- **Cleric:** Faithful Determination, Divine Conduit, and Sacred War
- **Druid:** Beasts of Legends; Defender of the True World also applies its
  specialization to evil outsiders
- **Fighter:** expanded Weapon Focus and Armor Training progressions, I Am
  Your Shield, and Dread Archer
- **Hunter:** Nurtured Growth and Tandem Execution
- **Inquisitor:** Experienced Judgement
- **Magus:** Canny Defense and expanded bonus feats
- **Monk:** archetype feature restorations and new high-level martial benefits
- **Paladin:** expanded Lay on Hands, favored-weapon focus, and a Mercy or
  combat-feat progression choice
- **Ranger:** expanded Combat Style progression and archetype restorations
- **Rogue:** Slippery, Professional Craft, and expanded Finesse Training
- **Shaman:** Charming Spirits, expanded Hex progression, and stronger Spirit
  Manifestations
- **Shifter:** Bonus Combat Talents and Primal Shifting
- **Skald:** Skald Talents and restored Hunt Caller Rage Powers
- **Slayer:** expanded Slayer Talents and an improved Master Slayer
- **Sorcerer:** revised bloodline powers and bloodline ability scaling
- **Warpriest:** expanded Fervor, archetype restorations, Wisdom-based Champion
  of the Faith Smite, and improved Shieldbearer shield enhancement
- **Wizard:** Supreme Intellect, Arcane Bond twice per day, Arcane Discoveries,
  and Arcane Bomber bomb-feat support
- **Witch:** Witchcraft, Hag's Claw Mastery, and corrected archetype Hex
  progressions

The [changelog](CHANGELOG.txt) contains the exhaustive progression details and
version history.

## Global systems

- **Tabletop-compatible archetype stacking:** compatible archetypes of the
  same class may be selected together during character creation or a full
  respec. Native feature conflicts remain authoritative.
- **Compatible alternate-heritage stacking:** alternate heritages may be
  combined when they replace different racial abilities.
- **Character trait system:** two level-1 trait choices organized into five
  thematic categories, with Additional Traits providing two more.
- **Improved Trapfinding:** every Trapfinding source adds half the granting
  class level to trap Perception and Trickery checks.
- **Unlimited bloodline claws:** Abyssal and Draconic bloodline claws can be
  used at will.

If ToyBox or the standalone Multiple Archetypes mod supplies an overlapping
archetype-stacking implementation, Buildcraft suspends its own runtime patches
to prevent double-patching.

## Configuration

Buildcraft content is organized into seven Unity Mod Manager sections:

1. Global changes
2. Added backgrounds
3. Race changes
4. Class and archetype changes
5. Added feats and exploits
6. Added rage powers
7. Added spells and spell-list changes

The major content families and nearly every individual addition have their
own toggle. All options are enabled by default.

## Building from source

The project uses the current `Owlcat.Templates` BlueprintCore/UMM layout and
targets .NET Framework 4.8.1.

```powershell
dotnet build .\ClassesReborn.slnx -c Release -p:SkipDeploy=true
```

Omit `-p:SkipDeploy=true` to copy the mod into the detected game's
`Mods\ClassesReborn` directory and create the installable ZIP.

Local game paths belong in `GamePath.props`, which is excluded from Git.

## Source and third-party notices

Buildcraft's original source is not currently distributed under an
open-source license. Third-party components retain their respective licenses;
see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for attribution and terms.
