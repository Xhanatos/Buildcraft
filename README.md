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
improve action economy, and add level-20 capstones.

### Alchemist

- Every Alchemist gains **Awakened Intellect** at level 20, permanently
  increasing Intelligence by 4. It is no longer offered as a Grand Discovery.
- **True Cognatogen** takes its place in the Grand Discovery list. It grants
  +8 natural armor and +8 Intelligence, Wisdom, and Charisma, with -2
  Strength, Dexterity, and Constitution while active.
- **Bone-Spike Mutagen** adds +2 natural armor and a 1d6 piercing spike attack
  while a Mutagen is active. **Collective Memory** adds half the Alchemist's
  level to all Knowledge and Lore skills during a Cognatogen. **Pheromones**
  grants a permanent +4 competence bonus to Persuasion.
- Vivisectionists can select Combat Trick at multiple Medical Discovery
  opportunities. Grenadiers trade Discoveries at levels 2, 8, and 14 and gain
  Fighter combat-feat selections at levels 8 and 14.
- Incense Synthesizer's Incense Fog lasts for `3 + Alchemist level +
  Intelligence modifier` rounds per day, becomes a swift action at level 10,
  and no longer removes the normal Bomb ranks at levels 3, 7, 11, 15, and 19.

### Barbarian

- Standard Barbarian damage reduction begins at level 1 and improves at
  levels 4, 7, 10, 13, 16, and 19, reaching DR 7/-.
- **Battleborn** is gained at level 8. Every enemy personally killed restores
  one round of Rage, up to the normal maximum; Rage does not need to be active.
  Instinctual Warrior's Focused Rage resource is supported as well.
- Danger Sense retains its trap bonuses and also grants its current rank as
  dodge AC and a Reflex bonus against attacks and effects from enemies that
  are invisible to the Barbarian.
- Beastkin Berserker gains +10 movement speed while Feral Transformation is
  active. Invulnerable Rager reaches DR 10/-, gains another DR 2/- below half
  health, and adds electricity resistance to Extreme Endurance.
- Mad Dog and its companion share the full DR 7/- progression. Pack Rager
  receives the same progression, while Armored Hulk and Flesheater regain
  their intended Uncanny Dodge features.

### Bard

- Bard Talents are gained at levels 2, 5, 8, 11, 14, 17, and 20, and Combat
  Trick can be selected repeatedly through them.
- Jack of All Trades moves to level 1. In addition to its +1 bonus on every
  skill check, it grants one extra skill point for every Bard level gained.
- Bards that retain Deadly Performance gain **True Artist** at level 20:
  +2 on all skill checks, +2 to the saving throw DC of Bardic Performances,
  and +4 Dexterity while a Bardic Performance is active.
- Archaeologist's Luck has twice its normal rounds per day. At level 20,
  **True Luck** rerolls the first failed attack, save, skill check, or spell
  resistance check each round, takes the better result, and adds half the
  Archaeologist's Charisma modifier as a luck bonus.
- Flame Dancer's Fire Dance adds fire damage to affected allies' weapon and
  natural attacks: 1d4 at level 3, 1d8 at level 6, and 1d10 at level 11.

### Bloodrager

- **Consuming Rage** is gained at level 8. As a swift action, a Bloodrager can
  sacrifice a level 1-4 spell slot to restore the same number of Bloodrage
  rounds, without exceeding the normal maximum.
- Steelblood regains the complete DR 1/- through DR 5/- progression at levels
  7, 10, 13, 16, and 19.
- Spell Eater and Hag-Riven regain Uncanny Dodge at level 2 while still losing
  Improved Uncanny Dodge at level 5.
- Hag-Riven's Claws of the Hag improve their critical threat range to 17-20 at
  level 17.

### Cavalier

- Cavalier Bonus Feats are gained at levels 2, 6, 10, 14, and 18. Gendarme
  continues to replace those grants with its own feat selection.
- Cavaliers that retain Expert Trainer now receive it at level 4.
- Order of the Cockatrice's Braggart attack bonus becomes profane; Order of
  the Shroud's challenge attack bonus and Order of the Star's challenge saving
  throw bonuses become sacred; Order of the Sword's By My Honor saves become
  luck bonuses. These types allow the abilities to work alongside common
  morale effects.
- Fearsome Leader's Fearmonger grants +2 Intimidate at level 3 and another +2
  every three Cavalier levels. Gendarme's level-9 **Glorious Charge** grants
  all saving throws a morale bonus equal to Charisma for two rounds after a
  charge.
- Ghost Rider uses Frightful Gaze as a swift action. Knight of the Wall counts
  shield enhancement bonuses for Deflective Shield and Soul Shield. Standard
  Bearer uses Banner of Solace as a swift action, and Awesome Pennon's attack
  and mind-affecting save bonuses become sacred.

### Cleric

- **Faithful Determination** is gained at level 10 and adds the positive part
  of the Cleric's Charisma modifier as a morale bonus on Will saves.
- At level 20, **Divine Conduit** maximizes every healing or damage die of
  single-target and mass Cure and Inflict spells. It also grants fear immunity
  and +4 Charisma. Scrolls, potions, Channel Energy, Heal, and Harm are not
  affected.
- Crusader replaces Divine Conduit with **Sacred War**, adding one-third of
  the positive Charisma modifier to martial attack and damage rolls as a
  sacred or profane bonus determined by the deity.
- Demonbane Priest gains Teamwork Bonus Feats at levels 4, 8, 12, and 16.

### Druid

- **Mighty Transformation** is gained at level 11. While shapeshifted, the
  Druid's base attack bonus becomes equal to total character level, potentially
  granting additional iterative attacks.
- **Beasts of Legends** is gained at level 20. The Druid's animal companion
  receives +4 sacred Strength, Dexterity, and Constitution; the Druid receives
  the same bonuses while shapeshifted; and summoned creatures receive +2.
  Winter Child keeps its own capstone instead.
- Defender of the True World's Enemy of the Fey, Fey Stalker, and Feybane
  bonuses apply against evil outsiders as well as fey.

### Fighter

- Fighters gain free Weapon Focus selections at levels 1, 4, and 7 and free
  Greater Weapon Focus selections at levels 8, 11, and 14. Each Armor Training
  rank reduces armor check penalty by 2 and raises maximum Dexterity by 2.
- Tower Shield Specialist gains **I Am Your Shield**, a limited swift-action
  stance that grants adjacent allies the specialist's complete tower-shield AC
  bonus. It lasts for 2 rounds per day plus the positive Constitution modifier,
  with 2 more rounds at levels 5, 10, 15, and 20. Tower Shield Defense also
  counts the shield's enhancement bonus against touch attacks.
- Two-Handed Fighter's Strong Grip grants +1 inherent Strength per rank at
  levels 2, 6, 10, 14, and 18, reaching +5. Dragonheir Scion's Fearful Might
  reaches +10 Intimidate and the archetype retains Armor and Weapon Mastery.
- **Dread Archer** trades Bravery for Merciless Reputation, granting +2 on
  demoralize checks per rank at levels 2, 6, 10, 14, and 18. It gains Deadly
  Aim at level 1; while Deadly Aim is active, Painful Shots makes a free
  demoralize attempt on the first ranged hit each round.
- Dread Archer's Ranged Weapon Training applies to every ranged weapon and
  counts as Weapon Training. It deals 1d6 additional damage against frightened
  or shaken targets from level 8, increasing to 2d6 at level 16, gains
  Dreadful Carnage without prerequisites at level 12, and increases its range
  to 50 feet at level 18. It lacks heavy-armor and shield proficiency.

### Hunter

- At level 5, Precise Companion grants whichever of Precise Shot or Outflank
  was not selected at level 2.
- **Nurtured Growth** is gained at level 13 and gives the animal companion +2
  natural armor and +2 Strength, Dexterity, and Constitution.
- Master Hunter keeps unlimited Animal Focus and adds +2 dodge AC and +2 base
  attack bonus to the Hunter or companion while that creature has an Animal
  Focus active.
- Colluding Scoundrel gains Sneak Attack at levels 5, 10, and 15. Divine Hound
  gains unlimited Judgment at level 20 and +2 dodge AC and base attack bonus
  while Judgment is active.
- Forester gains Uncanny Dodge at level 2 and combat feats at levels 2, 6, 10,
  14, and 18. Tandem Executioner gains seven Techniques and, from level 10,
  both partners deal +1d6 Sneak Attack damage after attacking the same target.

### Inquisitor

- **Experienced Judgement** is gained at level 10, providing two additional
  Judgment uses per day and +4 Wisdom. Only archetypes that retain a form of
  Judgment receive it.
- True Judgment at level 20 allows four different Judgment effects to remain
  active simultaneously; its existing death-judgment ability is unchanged.

### Magus

- Every non-Sword-Saint Magus gains **Canny Defense** at level 1, adding the
  Intelligence modifier to dodge AC while in light or no armor and without a
  shield, capped by Magus level. Sword Saint keeps its chosen-weapon version.
- Magus Bonus Feats are gained at levels 2, 6, 10, 14, and 18, and every Magus
  archetype inherits the expanded schedule.

### Monk

- Uncanny Dodge becomes selectable from every Monk Bonus Feat pool available
  at level 6 or later and upgrades safely to Improved Uncanny Dodge when the
  character already has it.
- Perfect Self adds 1d10 force damage to every unarmed hit while retaining DR
  10/chaotic. Drunken Master receives the same improvement.
- Quarterstaff Master gains quarterstaff Weapon Training at levels 7, 10, 13,
  and 16. Sensei regains Evasion and Fast Movement.
- Student of Stone adds 1d6 bludgeoning damage to unarmed hits at level 9 and
  gains +4 inherent Strength and Constitution at level 15.
- Traditional Monk improves unarmed damage at level 7, gains +2 dodge AC at
  level 11, and gains another highest-bonus Flurry attack at level 15.
- Zen Archer regains Evasion and Improved Evasion, receives Deflect Arrows
  automatically at level 7, gains larger Ki Arrow damage dice at level 15, and
  gains an additional Flurry attack plus a wider bow critical range at level
  20.

### Paladin

- Every Paladin gains Weapon Focus with the selected deity's favored weapon at
  level 1. The real parametrized feat is granted, so it satisfies prerequisites.
- At level 3, Paladins choose between the complete Mercy progression or Fighter
  combat feats at levels 3, 9, and 15. Archetypes that already replace Mercy
  retain their intended exchanges.
- Lay on Hands uses equal Paladin level plus Charisma modifier instead of half
  Paladin level plus Charisma. Tortured Crusader uses the same formula.
- Martyr and Stonelord regain Divine Grace. Warrior of the Holy Light activates
  Power of Faith and Shining Light as swift actions, and Power of Faith's AC,
  attack, damage, and fear-save bonuses become sacred.
- Aid is added as a level-2 Paladin spell and Freedom of Movement as a level-4
  spell.

### Ranger

- Combat Style feats are gained at every even level from 2 through 18. Favored
  Terrain is selected at levels 3, 6, 9, 13, 16, and 19.
- Master Hunter supplies Instant Enemy five times per day and grants +2 AC and
  all saving throws against favored enemies, including marked targets.
- Espionage Expert gains +1d6 Sneak Attack at levels 7 and 14. Calculated
  Assault grants half the positive Charisma modifier to attack at level 12 and
  the full modifier at level 20.
- Flamewarden regains Evasion and Improved Evasion. Nomad's horse gains natural
  armor and Constitution at level 7, speed and Dexterity at level 12, and
  Strength plus an additional full-attack strike at level 17.
- Stormwalker uses Wind Treader as a swift action, gains electricity resistance
  10 at level 7, and regains Quarry, Improved Evasion, and Improved Quarry.

### Rogue

- Finesse Training weapon choices are gained at levels 3, 7, 11, 15, and 19,
  and Combat Trick can be selected at every Rogue Talent opportunity.
- **Slippery** is gained at levels 7, 12, and 17. Each rank adds +1 dodge AC,
  +1 Reflex, +1 Stealth, and +1 Mobility, reaching +3 to each benefit.
- At level 20, **Professional Craft** adds +2 to every skill check and +2 on
  attacks against enemies that are unaware of the Rogue and therefore
  flat-footed against that attack.
- Danger Sense also protects against attacks and Reflex effects from invisible
  enemies. Knife Master's Blade Sense works against both light and heavy blades.
- Underground Chemist trades Finesse Training for five Bomb ranks, gains an
  expanded Rogue Talent list containing bomb discoveries, and uses Rogue levels
  for bomb uses and scaling. Sylvan Trickster can select multiclass-safe
  Uncanny Dodge from level 4.

### Shaman

- Additional Hexes at levels 6 and 14 expand the normal schedule to every even
  level from 2 through 20. Unsworn Shaman is excluded.
- **Charming Spirits** grants +2 Charisma at levels 7 and 15, reaching +4.
- Battle Ward and Bone Ward grant dodge AC instead of deflection AC.
- Flame, Frost, Stone, Waves, and Wind Spirit Manifestations add 1d6 matching
  elemental damage to weapon, natural, and unarmed attacks. Flame, Stone,
  Waves, and Wind also upgrade resistance 30 to immunity.
- Nature Spirit gains its fully scaling animal companion at level 8 and moves
  its greater fast healing to level 16.

### Shifter

- Bonus Combat Talents are gained at levels 8 and 16, offering the Fighter
  combat-feat list with normal prerequisites. Every Shifter archetype inherits
  them.
- Shifters and archetypes that retain Final Aspect gain **Primal Shifting** at
  level 20. Major forms can be assumed or changed as a swift action and grant
  +10 movement speed and +2 on all saving throws while active.

### Skald

- Skald Talents are gained at levels 2, 5, 8, 11, 14, and 17. Combat Trick can
  be selected repeatedly, and Weapon Focus is available through the level-1
  Skald Bonus Feat.
- Dance of a Hundred Cuts counts both Bard and Skald levels and functions with
  every native or archetype Raging Song.
- Hunt Caller regains Rage Power selections at levels 6 and 18, restoring the
  full level 3, 6, 9, 12, 15, and 18 progression.

### Slayer

- Evasion, multiclass-safe Uncanny Dodge, and repeatable Combat Trick appear in
  every Slayer Talent tier. Armored Marauder and Armored Swiftness are normal
  Slayer Talents without a level-10 requirement; Reaping Stalker remains an
  Advanced Talent.
- Imitator regains all six Sneak Attack ranks and all ten Slayer Talent choices
  while retaining its other archetype exchanges.
- Master Slayer keeps its death attack and adds half the positive Intelligence
  modifier as an insight attack bonus against the Slayer's own studied targets.
- Arcane Enforcer uses Intelligence instead of Charisma for Arcane Reservoir,
  exploit saving throw DCs, damage, temporary hit points, durations, and damage
  reduction.

### Sorcerer

- Bloodline bonus feats are selected at levels 3, 7, 11, 15, and 19 while
  retaining each bloodline's own permitted feat list.
- Celestial Body and Infernal Body retain their resistances and also grant +2
  Charisma at level 3, increasing to +4 at level 9.
- Fey and Serpentine bloodlines gain inherent Dexterity bonuses of +2 at level
  9, +4 at level 13, and +6 at level 17, including compatible alternate,
  Seeker, and Crossblooded-secondary progressions.
- Draconic Breath Weapon uses increase to 3 at level 9, 6 at level 17, and 9
  at level 20.
- Geomancer gains Favored Terrain at levels 3, 7, 11, 15, and 19, replacing
  each bloodline bonus feat it loses with a terrain choice or rank increase.

### Warpriest

- Fervor uses per day equal full Warpriest level plus Wisdom modifier instead
  of half Warpriest level plus Wisdom.
- Cult Leader's Enthrall becomes a move action.
- Champion of the Faith's Smite uses Wisdom for its attack bonus and
  target-specific deflection AC; damage continues to scale with Warpriest level.
- Shieldbearer's Sacred Armor enhancement also applies to shield-bash attack
  and damage rolls, following its active +1 through +5 rank without stacking
  with a higher weapon enhancement bonus.

### Wizard

- Arcane Bond — Object can restore a prepared spell twice per day.
- At level 20, every Wizard except Shadowcaster gains **Supreme Intellect**:
  +4 inherent Intelligence and +2 on Wizard-spell caster-level checks to
  overcome spell resistance and on dispel checks.
- Repeatable **Arcane Discovery** choices are available as general feats and
  Wizard Bonus Feats. Knowledge Is Power adds Intelligence to CMB and CMD;
  Opposition Research removes the extra preparation-slot cost of one opposition
  school; and Creative Destruction grants non-stacking temporary hit points
  equal to the damage dice of a damaging evocation spell.
- Alchemical Affinity grants +1 caster level and DC to spells shared by the
  Wizard and Alchemist lists. Idealize increases ability-score enhancement
  bonuses from the Wizard's transmutation spells by +2, or +4 at level 20.
- Arcane Bomber can select Precise Bombs from level 1 and Fast Bombs from level
  8 as Wizard Bonus Feats. Both work with its Spellblast-enhanced Arcane Bombs,
  including on multiclass characters.

### Witch

- At level 20, **Witchcraft** adds +2 to Hex DCs and spells with the Curse
  descriptor and +2 on saves against Hex and Curse effects. If a target saves
  against a once-per-day Hex, the Witch may attempt that Hex against the target
  one additional time.
- Hagbound retains the level-1 Hex and gains Hag's Claw Mastery at levels 3, 7,
  and 11. Each rank adds +1 claw attack and widens the claw critical range by
  1, reaching +3 attack and a 17-20 threat range.
- Keen-Eyed Adventurer gains Cantrip Mastery at level 10 in place of that
  level's Hex and retains the level-20 Hex.
- Ley Line Guardian retains its level-8 Hex alongside the Conduit Surge
  improvement gained at that level.

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
