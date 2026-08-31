using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.Configurators.Items.Weapons;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class AlchemistRebalance {
    private static readonly int[] GrenadierRemovedDiscoveryLevels = { 2, 8, 14 };
    private static readonly int[] GrenadierBonusCombatFeatLevels = { 8, 14 };
    private static readonly string[] MutagenBuffs = {
        "bd48322a4e258b8418106dcc6459e024",
        "f2be3d538b5d75c409289d35399723c4",
        "b84abc3531ed5674284ef0ba4aafcd3b",
        "a8e7ca242395c3b49af5a3dbc9dee683",
        "84c42fea967a2a8499ceeaef3a6416b8",
        "84ae955af09809b4ea31a2c719c68377",
        "d0a5cedfd497f3b4f9581b6066d9043b",
        "83ed8d5c1e4ed9045874494c0fe2b682",
        "a42c49fcb081bd1469679e4f515732c8",
        "0d51a2ff0a6ce85458309affbc00b933",
        "9c3761b9f48f69849ad78873c5a12147",
        "8d4357118c75a5746802a3582a937376",
        "bf73a2b70b6fac54e891431cf6c7d8eb",
        "204a74affae72d54984fb533704caf72",
        "3b7cf6307d3e61545a977c9f4156e12e",
        "3fb9e9a6408589343bc8bfc3fd1610e5",
    };
    private static readonly string[] CognatogenBuffs = {
        "32f2bc843effd9b45a0952a3cffbbe9f",
        "20e740104092b5e49bfb167f1670a9de",
        "6871149a90e278f479aa171ee8bb563e",
        "b60f8b93d3d1d26439c1bb48fd461a3a",
        "61271a59038390c488c313f7a0aee6ea",
        "1c2fdba3b33dacd41afd5b74d84c7332",
        "34fde71198d30094aa133546e8cf8733",
        "60eb20b9d1077ed4f8f8a9df5490a208",
        "bc0890817bb28fe4a86094fe57cd40fb",
        "a5a6f915d13fd994fb109473032d7440",
        "608dd115b3b0fba4ab511f448bc798f8",
        "8de52f7aa6052a0498875e0d834330af",
        "ac7753d72b0b7264982c2b6670fa2a2e",
        "232fe914c22744c4ea3e050901bda424",
        "98a46e8da1dca9f47b41b9d71d579628",
        FutureContentIds.Get("Alchemist.TrueCognatogen.Buff"),
    };

    internal static void Configure() {
        ConfigureAwakenedIntellectAndTrueCognatogen();
        ConfigureTabletopDiscoveries();
        ConfigureGrenadier();
        ConfigureIncenseSynthesizer();
        ConfigureRepeatableVivisectionistCombatTrick();
    }

    private static void ConfigureAwakenedIntellectAndTrueCognatogen() {
        var grandCognatogen = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.GrandCognatogenFeature);
        var trueMutagenBuff = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.TrueMutagenBuff);
        var abilityStats = new HashSet<StatType> {
            StatType.Strength,
            StatType.Dexterity,
            StatType.Constitution,
            StatType.Intelligence,
            StatType.Wisdom,
            StatType.Charisma,
        };
        var nativeNonAbilityBonuses = trueMutagenBuff
            .GetComponents<AddStatBonus>()
            .Where(component => !abilityStats.Contains(component.Stat))
            .Select(component => (component.Stat, component.Value, component.Descriptor))
            .OrderBy(component => component.Stat)
            .ThenBy(component => component.Value)
            .ThenBy(component => component.Descriptor)
            .ToArray();
        var trueCognatogenBuff = BuffConfigurator.New(
                "ClassesRebornTrueCognatogenBuff",
                FutureContentIds.Get("Alchemist.TrueCognatogen.Buff"))
            .CopyFrom(BlueprintIds.TrueMutagenBuff, component => true)
            .SetDisplayName("ClassesReborn.TrueCognatogen.Name")
            .SetDescription("ClassesReborn.TrueCognatogen.Description")
            .SetIcon(grandCognatogen.Icon)
            .Configure();

        foreach (var bonus in trueCognatogenBuff.GetComponents<AddStatBonus>()) {
            bonus.Stat = bonus.Stat switch {
                StatType.Strength => StatType.Intelligence,
                StatType.Dexterity => StatType.Wisdom,
                StatType.Constitution => StatType.Charisma,
                StatType.Intelligence => StatType.Strength,
                StatType.Wisdom => StatType.Dexterity,
                StatType.Charisma => StatType.Constitution,
                _ => bonus.Stat,
            };
        }

        var trueCognatogenAbility = AbilityConfigurator.New(
                "ClassesRebornTrueCognatogenAbility",
                FutureContentIds.Get("Alchemist.TrueCognatogen.Ability"))
            .CopyFrom(BlueprintIds.TrueMutagenAbility, component => true)
            .SetDisplayName("ClassesReborn.TrueCognatogen.Name")
            .SetDescription("ClassesReborn.TrueCognatogen.Description")
            .SetIcon(grandCognatogen.Icon)
            .Configure();
        var runActions = trueCognatogenAbility.GetComponent<AbilityEffectRunAction>();
        var applyBuffs = runActions == null
            ? Array.Empty<ContextActionApplyBuff>()
            : EnumerateActions(runActions.Actions)
                .OfType<ContextActionApplyBuff>()
                .ToArray();
        foreach (var applyBuff in applyBuffs) {
            applyBuff.m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(
                trueCognatogenBuff.AssetGuid.ToString());
        }

        var trueCognatogen = FeatureConfigurator.New(
                "ClassesRebornTrueCognatogenFeature",
                FutureContentIds.Get("Alchemist.TrueCognatogen"))
            .SetDisplayName("ClassesReborn.TrueCognatogen.Name")
            .SetDescription("ClassesReborn.TrueCognatogen.Description")
            .SetIcon(grandCognatogen.Icon)
            .SetIsClassFeature(true)
            .SetGroups(FeatureGroup.Discovery)
            .AddPrerequisiteFeature(BlueprintIds.GrandCognatogenFeature)
            .AddFacts(new() { trueCognatogenAbility })
            .Configure();

        var awakenedIntellect = FeatureConfigurator.For(
                BlueprintIds.AwakenedIntellectFeature)
            .SetDescription("ClassesReborn.AwakenedIntellect.Description")
            .Configure();
        var intelligenceBonuses = awakenedIntellect.GetComponents<AddStatBonus>()
            .Where(component => component.Stat == StatType.Intelligence)
            .ToArray();
        foreach (var bonus in intelligenceBonuses) {
            bonus.Value = 4;
        }

        var grandDiscovery = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.GrandDiscoverySelection);
        grandDiscovery.m_AllFeatures = RemoveSelectionFeature(
            grandDiscovery.m_AllFeatures,
            awakenedIntellect);
        grandDiscovery.m_Features = RemoveSelectionFeature(
            grandDiscovery.m_Features,
            awakenedIntellect);
        AddDiscoveryOptions(
            BlueprintIds.GrandDiscoverySelection,
            new[] { trueCognatogen });

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.AlchemistProgression);
        var entries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(entries, 20, awakenedIntellect);
        progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();

        var buffBonuses = trueCognatogenBuff.GetComponents<AddStatBonus>().ToArray();
        var copiedNonAbilityBonuses = buffBonuses
            .Where(component => !abilityStats.Contains(component.Stat))
            .Select(component => (component.Stat, component.Value, component.Descriptor))
            .OrderBy(component => component.Stat)
            .ThenBy(component => component.Value)
            .ThenBy(component => component.Descriptor)
            .ToArray();
        var validationFailures = new List<string>();
        if (intelligenceBonuses.Length != 1 || intelligenceBonuses.SingleOrDefault()?.Value != 4) {
            validationFailures.Add($"Awakened Intellect bonuses={intelligenceBonuses.Length}, value={intelligenceBonuses.SingleOrDefault()?.Value}");
        }
        if (CountSelectionFeature(grandDiscovery.m_AllFeatures, awakenedIntellect) != 0 ||
            CountSelectionFeature(grandDiscovery.m_Features, awakenedIntellect) != 0) {
            validationFailures.Add("Awakened Intellect remains in Grand Discovery");
        }
        if (CountSelectionFeature(grandDiscovery.m_AllFeatures, trueCognatogen) != 1 ||
            (grandDiscovery.m_Features?.Length > 0 &&
             CountSelectionFeature(grandDiscovery.m_Features, trueCognatogen) != 1)) {
            validationFailures.Add("True Cognatogen is not present exactly once in Grand Discovery");
        }
        if (CountFeatureAtLevel(progression.LevelEntries, awakenedIntellect, 20) != 1) {
            validationFailures.Add($"Awakened Intellect level-20 grants={CountFeatureAtLevel(progression.LevelEntries, awakenedIntellect, 20)}");
        }
        if (applyBuffs.Length == 0 || applyBuffs.Any(action => action.m_Buff?.Get() != trueCognatogenBuff)) {
            validationFailures.Add($"True Cognatogen apply-buff actions={applyBuffs.Length}, redirected={applyBuffs.Count(action => action.m_Buff?.Get() == trueCognatogenBuff)}");
        }
        foreach (var expected in new[] {
                     (StatType.Intelligence, 8),
                     (StatType.Wisdom, 8),
                     (StatType.Charisma, 8),
                     (StatType.Strength, -2),
                     (StatType.Dexterity, -2),
                     (StatType.Constitution, -2),
                 }) {
            if (!HasStatBonus(buffBonuses, expected.Item1, expected.Item2)) {
                var actual = string.Join(",", buffBonuses
                    .Where(component => component.Stat == expected.Item1)
                    .Select(component => component.Value));
                validationFailures.Add($"{expected.Item1} expected {expected.Item2}, actual [{actual}]");
            }
        }
        if (!nativeNonAbilityBonuses.SequenceEqual(copiedNonAbilityBonuses)) {
            validationFailures.Add("native non-ability bonuses were not preserved");
        }
        if (trueMutagenBuff.ComponentsArray.Length != trueCognatogenBuff.ComponentsArray.Length) {
            validationFailures.Add($"component count native={trueMutagenBuff.ComponentsArray.Length}, copy={trueCognatogenBuff.ComponentsArray.Length}");
        }
        if (validationFailures.Count > 0) {
            throw new InvalidOperationException(
                "Alchemist capstone validation failed: " + string.Join("; ", validationFailures));
        }
    }

    private static bool HasStatBonus(
        IEnumerable<AddStatBonus> bonuses,
        StatType stat,
        int value) =>
        bonuses.Count(component => component.Stat == stat && component.Value == value) == 1;

    private static IEnumerable<GameAction> EnumerateActions(ActionList actions) {
        var pending = new Stack<GameAction>(
            (actions.Actions ?? Array.Empty<GameAction>()).Reverse());
        var visited = new HashSet<GameAction>();

        while (pending.Count > 0) {
            var action = pending.Pop();
            if (action == null || !visited.Add(action)) {
                continue;
            }

            yield return action;

            foreach (var field in action.GetType().GetFields(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.NonPublic)) {
                switch (field.GetValue(action)) {
                    case ActionList nestedList:
                        PushActions(pending, nestedList.Actions);
                        break;
                    case GameAction nestedAction:
                        pending.Push(nestedAction);
                        break;
                    case GameAction[] nestedActions:
                        PushActions(pending, nestedActions);
                        break;
                }
            }
        }
    }

    private static void PushActions(
        Stack<GameAction> pending,
        IEnumerable<GameAction> actions) {
        if (actions == null) {
            return;
        }

        foreach (var action in actions.Reverse()) {
            pending.Push(action);
        }
    }

    private static void ConfigureTabletopDiscoveries() {
        var boneSpike = ConfigureBoneSpikeMutagen();
        var collectiveMemory = ConfigureCollectiveMemory();
        var pheromones = ConfigurePheromones();

        var standardDiscoveries = new[] { boneSpike, collectiveMemory, pheromones };
        foreach (var selectionId in new[] {
            BlueprintIds.AlchemistDiscoverySelection,
            BlueprintIds.ExtraDiscoverySelection,
            BlueprintIds.VivisectionistDiscoverySelection,
            BlueprintIds.ExtraVivisectionistDiscoverySelection,
        }) {
            AddDiscoveryOptions(selectionId, standardDiscoveries);
        }

        AddDiscoveryOptions(
            BlueprintIds.MutationWarriorDiscoverySelection,
            new[] { boneSpike, collectiveMemory });

        ValidateDiscoverySelections(standardDiscoveries, boneSpike, collectiveMemory);
    }

    private static BlueprintFeature ConfigureBoneSpikeMutagen() {
        var weapon = ItemWeaponConfigurator.New(
                "ClassesRebornBoneSpikeWeapon",
                BlueprintIds.BoneSpikeWeapon)
            .CopyFrom(BlueprintIds.ArmorFistWeapon)
            .SetDisplayNameText("ClassesReborn.BoneSpikeMutagen.Spike.Name")
            .SetDescriptionText("ClassesReborn.BoneSpikeMutagen.Spike.Description")
            .SetType(BlueprintIds.SpikeWeaponType)
            .SetOverrideDamageDice(true)
            .SetDamageDice(new DiceFormula(1, DiceType.D6))
            .SetOverrideDamageType(true)
            .SetDamageType(DamageTypes.Physical(form: PhysicalDamageForm.Piercing))
            .SetEnchantments(BlueprintIds.MasterworkWeaponEnchantment)
            .Configure();

        var mutagen = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.AlchemistMutagenFeature);
        var effectBuff = BuffConfigurator.New(
                "ClassesRebornBoneSpikeMutagenBuff",
                BlueprintIds.BoneSpikeMutagenBuff)
            .SetDisplayName("ClassesReborn.BoneSpikeMutagen.Name")
            .SetDescription("ClassesReborn.BoneSpikeMutagen.Description")
            .SetIcon(mutagen.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddStatBonus(
                descriptor: ModifierDescriptor.NaturalArmor,
                stat: StatType.AC,
                value: 2)
            .AddComponent(new AddAdditionalLimb {
                m_Weapon = BlueprintTool.GetRef<BlueprintItemWeaponReference>(
                    BlueprintIds.BoneSpikeWeapon),
            })
            .Configure();

        var feature = FeatureConfigurator.New(
                "ClassesRebornBoneSpikeMutagenFeature",
                BlueprintIds.BoneSpikeMutagenFeature)
            .SetDisplayName("ClassesReborn.BoneSpikeMutagen.Name")
            .SetDescription("ClassesReborn.BoneSpikeMutagen.Description")
            .SetIcon(mutagen.Icon)
            .SetIsClassFeature(true)
            .SetGroups(FeatureGroup.Discovery)
            .AddPrerequisiteClassLevel(
                BlueprintIds.AlchemistClass,
                6,
                group: Prerequisite.GroupType.Any)
            .AddPrerequisiteArchetypeLevel(
                BlueprintIds.MutationWarriorArchetype,
                BlueprintIds.FighterClass,
                level: 7,
                group: Prerequisite.GroupType.Any)
            .AddComponent(new PrerequisiteFeaturesFromList {
                m_Features = new[] {
                    BlueprintTool.GetRef<BlueprintFeatureReference>(
                        BlueprintIds.AlchemistMutagenFeature),
                    BlueprintTool.GetRef<BlueprintFeatureReference>(
                        BlueprintIds.MutationWarriorMutagenFeature),
                },
                Amount = 1,
                Group = Prerequisite.GroupType.All,
                CheckInProgression = true,
            })
            .AddComponent(new ApplyBuffWhileAnyFactActive {
                m_BonusBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.BoneSpikeMutagenBuff),
                m_RequiredFacts = MutagenBuffs
                    .Select(BlueprintTool.GetRef<BlueprintUnitFactReference>)
                    .ToArray(),
            })
            .Configure();

        if (weapon.Type?.Category != WeaponCategory.Spike ||
            weapon.m_DamageDice != new DiceFormula(1, DiceType.D6) ||
            effectBuff.GetComponents<AddAdditionalLimb>().Count() != 1 ||
            effectBuff.GetComponents<AddStatBonus>().Count(component =>
                component.Stat == StatType.AC &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.NaturalArmor) != 1 ||
            feature.GetComponents<ApplyBuffWhileAnyFactActive>().SingleOrDefault()
                ?.m_RequiredFacts.Length != MutagenBuffs.Length) {
            throw new InvalidOperationException(
                "Bone-Spike Mutagen must grant +2 natural armor and one masterwork 1d6 piercing spike attack while any mutagen is active.");
        }

        return feature;
    }

    private static BlueprintFeature ConfigureCollectiveMemory() {
        var cognatogen = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.CognatogenFeature);
        var halfAlchemistLevel = ContextRankConfigs
            .ClassLevel(
                new[] { BlueprintIds.AlchemistClass },
                type: AbilityRankType.StatBonus)
            .WithDivStepProgression(2);
        var configurator = BuffConfigurator.New(
                "ClassesRebornCollectiveMemoryBuff",
                BlueprintIds.CollectiveMemoryBuff)
            .SetDisplayName("ClassesReborn.CollectiveMemory.Name")
            .SetDescription("ClassesReborn.CollectiveMemory.Description")
            .SetIcon(cognatogen.Icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddContextRankConfig(halfAlchemistLevel);
        foreach (var skill in new[] {
            StatType.SkillKnowledgeArcana,
            StatType.SkillKnowledgeWorld,
            StatType.SkillLoreNature,
            StatType.SkillLoreReligion,
        }) {
            configurator.AddContextStatBonus(
                skill,
                ContextValues.Rank(AbilityRankType.StatBonus),
                ModifierDescriptor.UntypedStackable);
        }
        var effectBuff = configurator.Configure();

        var feature = FeatureConfigurator.New(
                "ClassesRebornCollectiveMemoryFeature",
                BlueprintIds.CollectiveMemoryFeature)
            .SetDisplayName("ClassesReborn.CollectiveMemory.Name")
            .SetDescription("ClassesReborn.CollectiveMemory.Description")
            .SetIcon(cognatogen.Icon)
            .SetIsClassFeature(true)
            .SetGroups(FeatureGroup.Discovery)
            .AddPrerequisiteFeature(BlueprintIds.CognatogenFeature)
            .AddComponent(new ApplyBuffWhileAnyFactActive {
                m_BonusBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.CollectiveMemoryBuff),
                m_RequiredFacts = CognatogenBuffs
                    .Select(BlueprintTool.GetRef<BlueprintUnitFactReference>)
                    .ToArray(),
            })
            .Configure();

        var skillBonuses = effectBuff.GetComponents<AddContextStatBonus>().ToArray();
        if (skillBonuses.Length != 4 ||
            skillBonuses.Any(component =>
                component.Descriptor != ModifierDescriptor.UntypedStackable) ||
            feature.GetComponents<ApplyBuffWhileAnyFactActive>().SingleOrDefault()
                ?.m_RequiredFacts.Length != CognatogenBuffs.Length) {
            throw new InvalidOperationException(
                "Collective Memory must grant half Alchemist level to all four Knowledge and Lore skills while any cognatogen is active.");
        }

        return feature;
    }

    private static BlueprintFeature ConfigurePheromones() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.PersuasiveFeat).Icon;
        var feature = FeatureConfigurator.New(
                "ClassesRebornPheromonesFeature",
                BlueprintIds.PheromonesFeature)
            .SetDisplayName("ClassesReborn.Pheromones.Name")
            .SetDescription("ClassesReborn.Pheromones.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetGroups(FeatureGroup.Discovery)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Competence,
                stat: StatType.SkillPersuasion,
                value: 4)
            .Configure();

        var bonuses = feature.GetComponents<AddStatBonus>().ToArray();
        if (bonuses.Length != 1 ||
            bonuses[0].Stat != StatType.SkillPersuasion ||
            bonuses[0].Value != 4 ||
            bonuses[0].Descriptor != ModifierDescriptor.Competence) {
            throw new InvalidOperationException(
                "Pheromones must grant a permanent +4 competence bonus to Persuasion.");
        }

        return feature;
    }

    private static void AddDiscoveryOptions(
        string selectionId,
        IEnumerable<BlueprintFeature> discoveries) {
        var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
        foreach (var discovery in discoveries) {
            selection.m_AllFeatures = AppendUniqueFeature(
                selection.m_AllFeatures,
                discovery);
            if (selection.m_Features?.Length > 0) {
                selection.m_Features = AppendUniqueFeature(
                    selection.m_Features,
                    discovery);
            }
        }
    }

    private static BlueprintFeatureReference[] AppendUniqueFeature(
        BlueprintFeatureReference[] references,
        BlueprintFeature feature) {
        var result = references?.ToList() ?? new List<BlueprintFeatureReference>();
        if (!result.Any(reference => reference?.Get() == feature)) {
            result.Add(BlueprintTool.GetRef<BlueprintFeatureReference>(
                feature.AssetGuid.ToString()));
        }
        return result.ToArray();
    }

    private static BlueprintFeatureReference[] RemoveSelectionFeature(
        BlueprintFeatureReference[] references,
        BlueprintFeature feature) =>
        references?
            .Where(reference => reference?.Get() != feature)
            .ToArray()
        ?? Array.Empty<BlueprintFeatureReference>();

    private static void ValidateDiscoverySelections(
        IReadOnlyCollection<BlueprintFeature> standardDiscoveries,
        BlueprintFeature boneSpike,
        BlueprintFeature collectiveMemory) {
        foreach (var selectionId in new[] {
            BlueprintIds.AlchemistDiscoverySelection,
            BlueprintIds.ExtraDiscoverySelection,
            BlueprintIds.VivisectionistDiscoverySelection,
            BlueprintIds.ExtraVivisectionistDiscoverySelection,
        }) {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
            foreach (var discovery in standardDiscoveries) {
                if (CountSelectionFeature(selection.m_AllFeatures, discovery) != 1 ||
                    (selection.m_Features?.Length > 0 &&
                     CountSelectionFeature(selection.m_Features, discovery) != 1)) {
                    throw new InvalidOperationException(
                        $"Discovery {discovery.name} must appear exactly once in selection {selection.name}.");
                }
            }
        }

        var mutationWarrior = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.MutationWarriorDiscoverySelection);
        foreach (var discovery in new[] { boneSpike, collectiveMemory }) {
            if (CountSelectionFeature(mutationWarrior.m_AllFeatures, discovery) != 1 ||
                (mutationWarrior.m_Features?.Length > 0 &&
                 CountSelectionFeature(mutationWarrior.m_Features, discovery) != 1)) {
                throw new InvalidOperationException(
                    $"Mutagen discovery {discovery.name} must appear exactly once for Mutation Warrior.");
            }
        }
    }

    private static void ConfigureGrenadier() {
        var discovery = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.AlchemistDiscoverySelection);
        var bonusCombatFeat = FeatureSelectionConfigurator.New(
                "ClassesRebornGrenadierBonusCombatFeatSelection",
                BlueprintIds.GrenadierBonusCombatFeatSelection)
            .CopyFrom(BlueprintIds.FighterBonusFeatSelection)
            .SetDisplayName("ClassesReborn.GrenadierBonusCombatFeat.Name")
            .SetDescription("ClassesReborn.GrenadierBonusCombatFeat.Description")
            .SetIsClassFeature(true)
            .Configure();
        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.GrenadierArchetype);

        var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(removals, discovery);
        foreach (var level in GrenadierRemovedDiscoveryLevels) {
            AddFeature(removals, level, discovery);
        }
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var additions = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        RemoveFeature(additions, bonusCombatFeat);
        foreach (var level in GrenadierBonusCombatFeatLevels) {
            AddFeature(additions, level, bonusCombatFeat);
        }
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        var fighterBonusFeats = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.FighterBonusFeatSelection);
        var expectedChoices = fighterBonusFeats.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        var actualChoices = bonusCombatFeat.m_AllFeatures
            .Select(reference => reference.deserializedGuid)
            .ToHashSet();
        if (CountFeature(archetype.RemoveFeatures, discovery) !=
                GrenadierRemovedDiscoveryLevels.Length ||
            GrenadierRemovedDiscoveryLevels.Any(level =>
                CountFeatureAtLevel(archetype.RemoveFeatures, discovery, level) != 1) ||
            CountFeature(archetype.AddFeatures, bonusCombatFeat) !=
                GrenadierBonusCombatFeatLevels.Length ||
            GrenadierBonusCombatFeatLevels.Any(level =>
                CountFeatureAtLevel(archetype.AddFeatures, bonusCombatFeat, level) != 1) ||
            !expectedChoices.SetEquals(actualChoices) ||
            bonusCombatFeat.m_AllFeatures.Length != expectedChoices.Count) {
            throw new InvalidOperationException(
                "Grenadier must lose Discovery at levels 2/8/14 and gain full combat-feat selections at levels 8/14.");
        }
    }

    private static void ConfigureRepeatableVivisectionistCombatTrick() {
        var original = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.CombatTrick);
        var repeatable = FeatureSelectionConfigurator.New(
                "ClassesRebornRepeatableVivisectionistCombatTrick",
                BlueprintIds.RepeatableVivisectionistCombatTrick)
            .CopyFrom(BlueprintIds.CombatTrick)
            .SetRanks(20)
            .Configure();
        var medicalDiscoveries = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.VivisectionistDiscoverySelection);

        var originalAllCount = CountSelectionFeature(
            medicalDiscoveries.m_AllFeatures,
            original);
        var originalFeatureCount = CountSelectionFeature(
            medicalDiscoveries.m_Features,
            original);
        if (originalAllCount + originalFeatureCount == 0 ||
            originalAllCount > 1 ||
            originalFeatureCount > 1) {
            throw new InvalidOperationException(
                $"Vivisectionist Medical Discovery must contain one native Combat Trick choice in each populated option array before it is made repeatable (AllFeatures: {originalAllCount}, Features: {originalFeatureCount}).");
        }

        medicalDiscoveries.m_AllFeatures = ReplaceFeatureReference(
            medicalDiscoveries.m_AllFeatures,
            original,
            repeatable);
        medicalDiscoveries.m_Features = ReplaceFeatureReference(
            medicalDiscoveries.m_Features,
            original,
            repeatable);

        var repeatableAllCount = CountSelectionFeature(
            medicalDiscoveries.m_AllFeatures,
            repeatable);
        var repeatableFeatureCount = CountSelectionFeature(
            medicalDiscoveries.m_Features,
            repeatable);
        if (CountSelectionFeature(medicalDiscoveries.m_AllFeatures, original) != 0 ||
            CountSelectionFeature(medicalDiscoveries.m_Features, original) != 0 ||
            repeatableAllCount != originalAllCount ||
            repeatableFeatureCount != originalFeatureCount ||
            repeatable.Ranks < 20) {
            throw new InvalidOperationException(
                "Vivisectionist Medical Discovery must offer exactly one repeatable Combat Trick choice.");
        }
    }

    private static void ConfigureIncenseSynthesizer() {
        ConfigureIncenseResource();

        var incense = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.IncenseFogFeature);
        var swiftIncense = FeatureConfigurator.New(
                "ClassesRebornSwiftIncenseFeature",
                BlueprintIds.SwiftIncenseFeature)
            .SetDisplayName("ClassesReborn.SwiftIncense.Name")
            .SetDescription("ClassesReborn.SwiftIncense.Description")
            .SetIcon(incense.Icon)
            .SetIsClassFeature(true)
            .AddComponent(new ChangeActivatableAbilitiesCommandType {
                m_ActivatableAbilities = new[] {
                    BlueprintTool.GetRef<BlueprintActivatableAbilityReference>(
                        BlueprintIds.IncenseFogToggleAbility),
                    BlueprintTool.GetRef<BlueprintActivatableAbilityReference>(
                        BlueprintIds.IncenseFog30ToggleAbility),
                },
                m_NewCommandType = UnitCommand.CommandType.Swift,
            })
            .Configure();

        var archetype = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.IncenseSynthesizerArchetype);
        var bombs = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.AlchemistBombsFeature);
        var removals = archetype.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        var otherRemovalsBefore = CountOtherFeatures(removals, bombs);
        var removedBombRanks = CountFeature(removals, bombs);

        if (removedBombRanks == 0) {
            throw new InvalidOperationException(
                "Incense Synthesizer did not contain any native Alchemist Bombs Feature removal entries to restore.");
        }

        RemoveFeature(removals, bombs);
        archetype.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var additions = archetype.AddFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(additions, 10, swiftIncense);
        archetype.AddFeatures = additions
            .OrderBy(entry => entry.Level)
            .ToArray();

        ValidateIncenseSynthesizer(
            archetype,
            bombs,
            swiftIncense,
            otherRemovalsBefore);

        Main.Log.Log(
            $"Restored the Incense Synthesizer's full Bomb progression by clearing {removedBombRanks} native Bomb removal entries.");
    }

    private static void ConfigureIncenseResource() {
        var amount = new ResourceAmountBuilder()
            .IncreaseByLevel(new[] { BlueprintIds.AlchemistClass }, 1)
            .IncreaseByStat(StatType.Intelligence);
        var resource = AbilityResourceConfigurator.For(BlueprintIds.IncenseFogResource)
            .SetMaxAmount(amount)
            .Configure();
        resource.m_MaxAmount.BaseValue = 3;

        var alchemist = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.AlchemistClass);
        if (resource.m_MaxAmount.BaseValue != 3 ||
            !resource.m_MaxAmount.IncreasedByLevel ||
            resource.m_MaxAmount.LevelIncrease != 1 ||
            resource.m_MaxAmount.m_Class?.Length != 1 ||
            resource.m_MaxAmount.m_Class[0]?.Get() != alchemist ||
            resource.m_MaxAmount.IncreasedByLevelStartPlusDivStep ||
            !resource.m_MaxAmount.IncreasedByStat ||
            resource.m_MaxAmount.ResourceBonusStat != StatType.Intelligence) {
            throw new InvalidOperationException(
                "Incense Fog must grant 3 + Alchemist level + Intelligence modifier rounds per day.");
        }
    }

    private static void ValidateIncenseSynthesizer(
        BlueprintArchetype archetype,
        BlueprintFeature bombs,
        BlueprintFeature swiftIncense,
        int otherRemovalsBefore) {
        var actionOverrides = swiftIncense
            .GetComponents<ChangeActivatableAbilitiesCommandType>()
            .ToArray();
        var expectedAbilities = new HashSet<string> {
            BlueprintIds.IncenseFogToggleAbility,
            BlueprintIds.IncenseFog30ToggleAbility,
        };
        var configuredAbilities = actionOverrides
            .SelectMany(component =>
                component.m_ActivatableAbilities ??
                Array.Empty<BlueprintActivatableAbilityReference>())
            .Select(reference => reference?.Get()?.AssetGuid.ToString())
            .Where(id => id != null)
            .ToHashSet();

        if (CountFeature(archetype.RemoveFeatures, bombs) != 0 ||
            CountOtherFeatures(archetype.RemoveFeatures, bombs) !=
                otherRemovalsBefore ||
            CountFeature(archetype.AddFeatures, swiftIncense) != 1 ||
            CountFeatureAtLevel(archetype.AddFeatures, swiftIncense, 10) != 1 ||
            actionOverrides.Length != 1 ||
            actionOverrides[0].m_NewCommandType != UnitCommand.CommandType.Swift ||
            !configuredAbilities.SetEquals(expectedAbilities)) {
            throw new InvalidOperationException(
                "Incense Synthesizer must retain every Bomb rank and gain Swift Incense at level 10 for both Incense Fog toggles.");
        }
    }

    private static BlueprintFeatureReference[] ReplaceFeatureReference(
        BlueprintFeatureReference[] references,
        BlueprintFeature original,
        BlueprintFeature replacement) =>
        references?
            .Select(reference => reference?.Get() == original
                ? BlueprintTool.GetRef<BlueprintFeatureReference>(
                    replacement.AssetGuid.ToString())
                : reference)
            .ToArray()
        ?? Array.Empty<BlueprintFeatureReference>();

    private static int CountSelectionFeature(
        IEnumerable<BlueprintFeatureReference> references,
        BlueprintFeature feature) =>
        references?.Count(reference => reference?.Get() == feature) ?? 0;

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
        List<LevelEntry> entries,
        BlueprintFeature feature) {
        foreach (var entry in entries) {
            entry.m_Features?.RemoveAll(reference => reference?.Get() == feature);
        }
        entries.RemoveAll(entry =>
            entry.m_Features == null || entry.m_Features.Count == 0);
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
        entries?
            .Where(entry => entry.Level == level)
            .Sum(entry =>
                entry.m_Features?.Count(reference => reference?.Get() == feature) ?? 0) ?? 0;

    private static int CountOtherFeatures(
        IEnumerable<LevelEntry> entries,
        BlueprintFeature excludedFeature) =>
        entries?.Sum(entry =>
            entry.m_Features?.Count(reference =>
                reference?.Get() != excludedFeature) ?? 0) ?? 0;
}
