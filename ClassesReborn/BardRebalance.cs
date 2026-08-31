using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

namespace ClassesReborn;

internal static class BardRebalance {
    private static readonly int[] BardTalentLevels = { 2, 5, 8, 11, 14, 17, 20 };
    private static readonly StatType[] SkillStats = {
        StatType.SkillAthletics,
        StatType.SkillMobility,
        StatType.SkillThievery,
        StatType.SkillStealth,
        StatType.SkillKnowledgeArcana,
        StatType.SkillKnowledgeWorld,
        StatType.SkillLoreNature,
        StatType.SkillLoreReligion,
        StatType.SkillPerception,
        StatType.SkillPersuasion,
        StatType.SkillUseMagicDevice,
    };

    internal static void Configure() {
        ConfigureRepeatableCombatTrick();
        ConfigureProgression();
        ConfigureTrueArtist();
        ConfigureArchaeologist();
        ConfigureFlameDancer();
    }

    internal static void ConfigureSpellListChanges() {
        if (Main.Settings.BardTrueStrike) {
            AddExistingSpellToBardList(BlueprintIds.TrueStrikeAbility, 1);
        }
        if (Main.Settings.BardMagicWeapon) {
            AddExistingSpellToBardList(BlueprintIds.MagicWeaponAbility, 1);
        }
        if (Main.Settings.BardGreaterMagicWeapon) {
            AddExistingSpellToBardList(BlueprintIds.GreaterMagicWeaponAbility, 3);
        }
    }

    private static void ConfigureArchaeologist() {
        var performanceResource = BlueprintTool.Get<BlueprintAbilityResource>(
            BlueprintIds.BardicPerformanceResource);
        var bardClass = BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.BardClass);
        var luckFeatureBlueprint = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.ArchaeologistLuckFeature);
        luckFeatureBlueprint.ComponentsArray =
            (luckFeatureBlueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            .Where(component =>
                component is not IncreaseResourcesByClass resourceBonus ||
                resourceBonus.Resource != performanceResource ||
                resourceBonus.CharacterClass != bardClass)
            .ToArray();

        var luckFeature = FeatureConfigurator.For(BlueprintIds.ArchaeologistLuckFeature)
            .SetDescription("ClassesReborn.ArchaeologistLuck.Description")
            .AddComponent(new ArchaeologistLuckResourceBonus {
                m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                    BlueprintIds.BardicPerformanceResource),
                m_Class = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.BardClass),
            })
            .Configure();

        var trueLuck = FeatureConfigurator.New(
                "ClassesRebornArchaeologistTrueLuckFeature",
                BlueprintIds.ArchaeologistTrueLuckFeature)
            .SetDisplayName("ClassesReborn.TrueLuck.Name")
            .SetDescription("ClassesReborn.TrueLuck.Description")
            .SetIcon(luckFeature.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new TrueLuckReroll())
            .Configure();

        var archaeologist = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ArchaeologistArchetype);
        var additions = archaeologist.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(additions, 20, trueLuck);
        archaeologist.AddFeatures = additions.OrderBy(entry => entry.Level).ToArray();

        var matchingResourceBonuses = luckFeature
            .GetComponents<ArchaeologistLuckResourceBonus>()
            .Where(component =>
                component.m_Resource?.Get() == performanceResource &&
                component.m_Class?.Get() == bardClass)
            .ToArray();
        var staleResourceBonuses = luckFeature
            .GetComponents<IncreaseResourcesByClass>()
            .Count(component =>
                component.Resource == performanceResource &&
                component.CharacterClass == bardClass);
        if (matchingResourceBonuses.Length != 1 ||
            staleResourceBonuses != 0 ||
            CountFeatureAtLevel(archaeologist.AddFeatures, trueLuck, 20) != 1) {
            throw new InvalidOperationException(
                "Archaeologist Luck scaling or the level-20 True Luck grant is invalid.");
        }
    }

    private static void ConfigureTrueArtist() {
        var deadlyPerformance = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.DeadlyPerformanceFeature);
        var dexterityBuff = BuffConfigurator.New(
                "ClassesRebornTrueArtistDexterityBuff",
                FutureContentIds.Get("Bard.TrueArtist.DexterityBuff"))
            .SetDisplayName("ClassesReborn.TrueArtist.Name")
            .SetDescription("ClassesReborn.TrueArtist.Description")
            .SetIcon(deadlyPerformance.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.Dexterity,
                value: 4)
            .Configure();

        var configurator = FeatureConfigurator.New(
                "ClassesRebornTrueArtistFeature",
                FutureContentIds.Get("Bard.TrueArtist"))
            .SetDisplayName("ClassesReborn.TrueArtist.Name")
            .SetDescription("ClassesReborn.TrueArtist.Description")
            .SetIcon(deadlyPerformance.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new TrueArtistPerformanceDcBonus { Bonus = 2 })
            .AddComponent(new ApplyBuffWhileAnyFactActive {
                m_BonusBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    dexterityBuff.AssetGuid.ToString()),
                m_RequiredFacts = BlueprintIds.BardPerformanceBuffs
                    .Select(BlueprintTool.GetRef<BlueprintUnitFactReference>)
                    .ToArray(),
            });
        foreach (var skill in SkillStats) {
            configurator.AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: skill,
                value: 2);
        }
        var trueArtist = configurator.Configure();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.BardProgression);
        var entries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(entries, 20, trueArtist);
        progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();

        var bardClass = BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.BardClass);
        foreach (var archetype in bardClass.Archetypes.Where(archetype =>
                     CountFeature(archetype.RemoveFeatures, deadlyPerformance) > 0)) {
            var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
            AddFeature(removals, 20, trueArtist);
            archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
        }

        var skillBonuses = trueArtist.GetComponents<AddStatBonus>().ToArray();
        var performanceDc = trueArtist.GetComponents<TrueArtistPerformanceDcBonus>().ToArray();
        var dexterityController = trueArtist
            .GetComponents<ApplyBuffWhileAnyFactActive>()
            .SingleOrDefault();
        var dexterityBonuses = dexterityBuff.GetComponents<AddStatBonus>().ToArray();
        if (CountFeatureAtLevel(progression.LevelEntries, trueArtist, 20) != 1 ||
            skillBonuses.Length != SkillStats.Length ||
            SkillStats.Any(skill => skillBonuses.Count(component =>
                component.Stat == skill && component.Value == 2) != 1) ||
            performanceDc.Length != 1 || performanceDc[0].Bonus != 2 ||
            dexterityController?.m_RequiredFacts?.Length !=
                BlueprintIds.BardPerformanceBuffs.Length ||
            dexterityBonuses.Length != 1 ||
            dexterityBonuses[0].Stat != StatType.Dexterity ||
            dexterityBonuses[0].Value != 4 ||
            bardClass.Archetypes.Any(archetype =>
                CountFeature(archetype.RemoveFeatures, deadlyPerformance) > 0 &&
                CountFeatureAtLevel(archetype.RemoveFeatures, trueArtist, 20) != 1)) {
            throw new InvalidOperationException(
                "True Artist must grant +2 to every skill and Bard-performance DC, +4 Dexterity during a performance, and be removed by every archetype that replaces Deadly Performance.");
        }
    }

    private static void ConfigureFlameDancer() {
        FeatureConfigurator.For(BlueprintIds.FlameDanceFeature)
            .SetDescription("ClassesReborn.FlameDance.Description")
            .Configure();

        ActivatableAbilityConfigurator.For(BlueprintIds.FlameDanceAbility)
            .SetDescription("ClassesReborn.FlameDance.Description")
            .Configure();

        var effectBuff = BuffConfigurator.For(BlueprintIds.FlameDanceEffectBuff)
            .SetDescription("ClassesReborn.FlameDance.EffectDescription")
            .AddComponent(new FlameDanceFireDamage {
                m_FlameDanceFeature = BlueprintTool.GetRef<BlueprintFeatureReference>(
                    BlueprintIds.FlameDanceFeature),
            })
            .Configure();

        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.FlameDancerArchetype);
        var feature = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.FlameDanceFeature);
        var components = effectBuff.GetComponents<FlameDanceFireDamage>().ToArray();
        if (feature.Ranks != 3 ||
            CountFeatureAtLevel(archetype.AddFeatures, feature, 3) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, feature, 6) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, feature, 11) != 1 ||
            components.Length != 1 ||
            components[0].m_FlameDanceFeature?.Get() != feature) {
            throw new InvalidOperationException(
                "Fire Dance damage scaling or archetype progression is invalid.");
        }
    }

    private static void AddExistingSpellToBardList(string abilityId, int spellLevel) {
        AbilityConfigurator.For(abilityId)
            .AddToSpellList(spellLevel, BlueprintIds.BardSpellList)
            .Configure();

        var spellList = BlueprintTool.Get<BlueprintSpellList>(BlueprintIds.BardSpellList);
        var ability = BlueprintTool.Get<BlueprintAbility>(abilityId);
        var matchingEntries = spellList.SpellsByLevel[spellLevel].Spells.Count(spell =>
            spell == ability);
        if (matchingEntries != 1) {
            throw new InvalidOperationException(
                $"{ability.name} must appear exactly once on the level-{spellLevel} Bard spell list.");
        }
    }

    private static void ConfigureRepeatableCombatTrick() {
        var original = BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.CombatTrick);
        var repeatable = FeatureSelectionConfigurator.New(
                "ClassesRebornRepeatableBardCombatTrick",
                BlueprintIds.RepeatableBardCombatTrick)
            .CopyFrom(BlueprintIds.CombatTrick)
            .SetRanks(20)
            .Configure();

        var bardTalents = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.BardTalentSelection);
        bardTalents.m_AllFeatures = ReplaceFeatureReference(
            bardTalents.m_AllFeatures,
            original,
            repeatable);
        bardTalents.m_Features = ReplaceFeatureReference(
            bardTalents.m_Features,
            original,
            repeatable);

        var repeatableCount = bardTalents.m_AllFeatures.Count(reference =>
            reference?.Get() == repeatable);
        var originalCount = bardTalents.m_AllFeatures.Count(reference =>
            reference?.Get() == original);
        if (repeatableCount != 1 || originalCount != 0 || repeatable.Ranks < 20) {
            throw new InvalidOperationException(
                "Bard Talent must contain exactly one repeatable Combat Trick selection.");
        }
    }

    private static void ConfigureProgression() {
        var bardClass = BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.BardClass);
        var progression = BlueprintTool.Get<BlueprintProgression>(BlueprintIds.BardProgression);
        var bardTalent = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.BardTalentSelection);
        var jackOfAllTrades = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.BardJackOfAllTrades);

        FeatureConfigurator.For(BlueprintIds.BardJackOfAllTrades)
            .SetDescription("ClassesReborn.BardJackOfAllTrades.Description")
            .Configure();

        bardClass.SkillPoints = 5;
        RemoveFeature(progression.LevelEntries, bardTalent);
        RemoveFeature(progression.LevelEntries, jackOfAllTrades);

        var entries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        foreach (var level in BardTalentLevels) {
            AddFeature(entries, level, bardTalent);
        }
        AddFeature(entries, 1, jackOfAllTrades);
        progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();

        PreserveArchetypeTradeoffs(bardClass, bardTalent, jackOfAllTrades);

        if (bardClass.SkillPoints != 5 ||
            CountFeature(progression.LevelEntries, bardTalent) != BardTalentLevels.Length ||
            BardTalentLevels.Any(level =>
                CountFeatureAtLevel(progression.LevelEntries, bardTalent, level) != 1) ||
            CountFeature(progression.LevelEntries, jackOfAllTrades) != 1 ||
            CountFeatureAtLevel(progression.LevelEntries, jackOfAllTrades, 1) != 1) {
            throw new InvalidOperationException(
                "Bard progression validation failed for talents or Jack of All Trades.");
        }
    }

    private static void PreserveArchetypeTradeoffs(
        BlueprintCharacterClass bardClass,
        BlueprintFeatureSelection bardTalent,
        BlueprintFeature jackOfAllTrades) {
        var beastTamer = bardClass.Archetypes.FirstOrDefault(archetype =>
            archetype.AssetGuid.ToString() == BlueprintIds.BeastTamerArchetype);
        if (beastTamer != null) {
            RemoveFeature(beastTamer.RemoveFeatures, bardTalent);
            var removals = beastTamer.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
            AddFeature(removals, 2, bardTalent);
            AddFeature(removals, 8, bardTalent);
            beastTamer.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
        }

        foreach (var archetype in bardClass.Archetypes.Where(archetype =>
                     archetype.AssetGuid.ToString() == BlueprintIds.ChelishDivaArchetype ||
                     archetype.AssetGuid.ToString() == BlueprintIds.DirgeBardArchetype)) {
            RemoveFeature(archetype.RemoveFeatures, jackOfAllTrades);
            var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
            AddFeature(removals, 1, jackOfAllTrades);
            archetype.RemoveFeatures = removals.OrderBy(entry => entry.Level).ToArray();
            archetype.AddSkillPoints -= 1;
        }

        if (beastTamer != null &&
            (CountFeature(beastTamer.RemoveFeatures, bardTalent) != 2 ||
             CountFeatureAtLevel(beastTamer.RemoveFeatures, bardTalent, 2) != 1 ||
             CountFeatureAtLevel(beastTamer.RemoveFeatures, bardTalent, 8) != 1)) {
            throw new InvalidOperationException(
                "Beast Tamer must replace the first and third Bard Talent grants.");
        }
    }

    internal static void ConfigureDanceOfAHundredCuts() {
        var icon = BlueprintTool.Get<BlueprintAbility>(BlueprintIds.HasteAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornDanceOfAHundredCutsEffectBuff",
                BlueprintIds.DanceOfAHundredCutsEffectBuff)
            .SetDisplayName("ClassesReborn.DanceOfAHundredCuts.Name")
            .SetDescription("ClassesReborn.DanceOfAHundredCuts.EffectDescription")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddComponent(new AttackTypeAttackBonus {
                Type = WeaponRangeType.Melee,
                AllTypesExcept = false,
                AttackBonus = 0,
                Descriptor = ModifierDescriptor.UntypedStackable,
                Value = new ContextValue {
                    ValueType = ContextValueType.Simple,
                    Value = 3,
                },
                CheckFact = false,
            })
            .AddStatBonus(
                descriptor: ModifierDescriptor.Dodge,
                stat: StatType.AC,
                value: 3)
            .Configure();

        BuffConfigurator.New(
                "ClassesRebornDanceOfAHundredCutsBuff",
                BlueprintIds.DanceOfAHundredCutsBuff)
            .SetDisplayName("ClassesReborn.DanceOfAHundredCuts.Name")
            .SetDescription("ClassesReborn.DanceOfAHundredCuts.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BuffExtraEffects {
                m_CheckedBuffList = BlueprintIds.BardPerformanceBuffs
                    .Concat(BlueprintIds.SkaldRagingSongBuffs)
                    .Distinct()
                    .Select(BlueprintTool.GetRef<BlueprintBuffReference>)
                    .ToArray(),
                m_ExtraEffectBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.DanceOfAHundredCutsEffectBuff),
            })
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.DanceOfAHundredCutsBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Rounds,
                isExtendable: true),
            isFromSpell: true);

        AbilityConfigurator.NewSpell(
                "ClassesRebornDanceOfAHundredCutsAbility",
                BlueprintIds.DanceOfAHundredCutsAbility,
                SpellSchool.Transmutation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.DanceOfAHundredCuts.Name")
            .SetDescription("ClassesReborn.DanceOfAHundredCuts.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Heighten,
                Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.DanceOfAHundredCuts.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.DanceOfAHundredCuts.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddContextRankConfig(ContextRankConfigs.ClassLevel(
                new[] { BlueprintIds.BardClass, BlueprintIds.SkaldClass },
                false))
            .AddAbilityEffectRunAction(applyBuff)
            .AddToSpellList(5, BlueprintIds.BardSpellList)
            .Configure();

        var spellList = BlueprintTool.Get<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>(
            BlueprintIds.BardSpellList);
        var ability = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.DanceOfAHundredCutsAbility);
        var levelFiveCount = spellList.SpellsByLevel[5].Spells.Count(spell => spell == ability);
        if (levelFiveCount != 1 ||
            ability.GetComponent<SpellComponent>()?.School != SpellSchool.Transmutation) {
            throw new InvalidOperationException(
                "Dance of a Hundred Cuts must be a level-5 Bard transmutation spell.");
        }
    }

    private static BlueprintFeatureReference[] ReplaceFeatureReference(
        BlueprintFeatureReference[] references,
        BlueprintFeature original,
        BlueprintFeature replacement) =>
        references?.Select(reference => reference?.Get() == original
                ? BlueprintTool.GetRef<BlueprintFeatureReference>(replacement.AssetGuid.ToString())
                : reference)
            .ToArray() ?? Array.Empty<BlueprintFeatureReference>();

    private static void AddFeature(
        List<LevelEntry> entries,
        int level,
        BlueprintFeature feature) {
        var entry = entries.FirstOrDefault(candidate => candidate.Level == level);
        if (entry == null) {
            entry = new LevelEntry { Level = level, m_Features = new() };
            entries.Add(entry);
        }

        entry.m_Features ??= new();
        if (!entry.m_Features.Any(reference => reference?.Get() == feature)) {
            entry.m_Features.Add(BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                feature.AssetGuid.ToString()));
        }
    }

    private static void RemoveFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) {
        if (entries == null) {
            return;
        }

        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
    }

    private static int CountFeature(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountFeatureAtLevel(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature feature,
        int level) =>
        entries?.Where(entry => entry.Level == level).Sum(entry =>
            entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;
}
