using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class WizardRebalance {
    private static readonly string[] ArcaneBombAbilityIds = {
        "03b305962d8a9c2478deb76e1015fc9a",
        "9e305d3c312fa0f4296e3174f0a7cfd8",
        "feff1b48642214c45a6cb13f683f3552",
        "cd65a240f32aa7441a0f10b1a86ed520",
    };

    private static readonly (SpellSchool School, string FeatureId)[] OppositionSchools = {
        (SpellSchool.Abjuration, "7f8c1b838ff2d2e4f971b42ccdfa0bfd"),
        (SpellSchool.Conjuration, "ca4a0d68c0408d74bb83ade784ebeb0d"),
        (SpellSchool.Divination, "09595544116fe5349953f939aeba7611"),
        (SpellSchool.Enchantment, "875fff6feb84f5240bf4375cb497e395"),
        (SpellSchool.Evocation, "c3724cfbe98875f4a9f6d1aabd4011a6"),
        (SpellSchool.Illusion, "6750ead44c0c034428c6509c68110375"),
        (SpellSchool.Necromancy, "a9bb3dcb2e8d44a49ac36c393c114bd9"),
        (SpellSchool.Transmutation, "fc519612a3c604446888bb345bca5234"),
    };

    private static string Id(string name) =>
        FutureContentIds.Get($"Wizard.ArcaneDiscovery.{name}");

    internal static void Configure() {
        if (Main.Settings.WizardSupremeIntellect) {
            ConfigureSupremeIntellect();
        }
        if (Main.Settings.ArcaneBomberBombFeats) {
            ConfigureArcaneBomberBombFeats();
        }
        if (Main.Settings.WizardArcaneBondTwoUses) {
            ConfigureArcaneBondObject();
        }

        var options = new List<BlueprintFeature>();
        if (Main.Settings.ArcaneDiscoveryKnowledgeIsPower) {
            options.Add(ConfigureKnowledgeIsPower());
        }
        if (Main.Settings.ArcaneDiscoveryOppositionResearch) {
            options.Add(ConfigureOppositionResearch());
        }
        if (Main.Settings.ArcaneDiscoveryCreativeDestruction) {
            options.Add(ConfigureCreativeDestruction());
        }
        if (Main.Settings.ArcaneDiscoveryAlchemicalAffinity) {
            options.Add(ConfigureAlchemicalAffinity());
        }
        if (Main.Settings.ArcaneDiscoveryIdealize) {
            options.Add(ConfigureIdealize());
        }
        if (options.Count == 0) {
            return;
        }

        var icon = FeatureRefs.WizardSchools.Reference.Get().Icon;
        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornArcaneDiscoverySelection",
                Id("Selection"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat, FeatureGroup.WizardFeat)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .SetRanks(20)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 1)
            .Configure();
        selection.m_AllFeatures = options
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();

        AddToSelection(
            BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.BasicFeatSelection),
            selection);
        AddToSelection(FeatureSelectionRefs.WizardFeatSelection.Reference.Get(), selection);
        Validate(selection, options);
    }

    private static void ConfigureSupremeIntellect() {
        var feature = FeatureConfigurator.New(
                "ClassesRebornSupremeIntellectFeature",
                FutureContentIds.Get("Wizard.SupremeIntellect"))
            .SetDisplayName("ClassesReborn.Wizard.SupremeIntellect.Name")
            .SetDescription("ClassesReborn.Wizard.SupremeIntellect.Description")
            .SetIcon(AbilityRefs.FoxsCunning.Reference.Get().Icon)
            .SetIsClassFeature(true)
            .SetRanks(1)
            .AddStatBonus(
                descriptor: ModifierDescriptor.Inherent,
                stat: StatType.Intelligence,
                value: 4)
            .AddComponent(new SupremeIntellectSpellCheckBonus {
                m_WizardClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.WizardClass),
                Bonus = 2,
            })
            .Configure();

        var progression = BlueprintTool.Get<BlueprintProgression>(
            BlueprintIds.WizardProgression);
        var entries = progression.LevelEntries?.ToList() ?? new List<LevelEntry>();
        AddFeature(entries, 20, feature);
        progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();

        var shadowCaster = BlueprintTool.Get<BlueprintArchetype>(
            BlueprintIds.ShadowCasterArchetype);
        var removals = shadowCaster.RemoveFeatures?.ToList() ?? new List<LevelEntry>();
        AddFeature(removals, 20, feature);
        shadowCaster.RemoveFeatures = removals
            .OrderBy(entry => entry.Level)
            .ToArray();

        var intelligenceBonus = feature.GetComponents<AddStatBonus>().ToArray();
        var spellCheckBonus = feature
            .GetComponents<SupremeIntellectSpellCheckBonus>()
            .SingleOrDefault();
        var wizard = BlueprintTool.Get<BlueprintCharacterClass>(BlueprintIds.WizardClass);
        if (CountFeatureAtLevel(progression.LevelEntries, feature, 20) != 1 ||
            CountFeature(progression.LevelEntries, feature) != 1 ||
            intelligenceBonus.Length != 1 ||
            intelligenceBonus[0].Stat != StatType.Intelligence ||
            intelligenceBonus[0].Descriptor != ModifierDescriptor.Inherent ||
            intelligenceBonus[0].Value != 4 ||
            spellCheckBonus?.m_WizardClass?.Get() != wizard ||
            spellCheckBonus.Bonus != 2 ||
            CountFeatureAtLevel(shadowCaster.RemoveFeatures, feature, 20) != 1 ||
            CountFeature(shadowCaster.RemoveFeatures, feature) != 1 ||
            wizard.Archetypes.Any(archetype =>
                archetype != shadowCaster &&
                CountFeature(archetype.RemoveFeatures, feature) != 0)) {
            throw new InvalidOperationException(
                "Supreme Intellect must be a Wizard level-20 capstone with +4 inherent Intelligence and +2 Wizard-spell penetration and dispel checks, removed only by Shadowcaster.");
        }
    }

    private static void ConfigureArcaneBomberBombFeats() {
        var fastBombs = ConfigureArcaneBomberBombFeat(
            BlueprintIds.FastBombsFeature,
            "FastBombs",
            minimumLevel: 8);
        var preciseBombs = ConfigureArcaneBomberBombFeat(
            BlueprintIds.PreciseBombsFeature,
            "PreciseBombs",
            minimumLevel: 1);

        var wizardBonusFeats = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.WizardFeatSelection);
        AddToSelection(wizardBonusFeats, fastBombs);
        AddToSelection(wizardBonusFeats, preciseBombs);

        var arcaneBombs = ArcaneBombAbilityIds
            .Select(BlueprintTool.Get<BlueprintAbility>)
            .ToArray();
        foreach (var ability in arcaneBombs) {
            if (ability.GetComponent<AbilityIsBomb>() == null) {
                AbilityConfigurator.For(ability.AssetGuid.ToString())
                    .AddComponent(new AbilityIsBomb())
                    .Configure();
            }
        }

        var fastBombComponents = new[] {
                BuffRefs.FastBombsBuff.Reference.Get(),
                BuffRefs.FastBombsTwoHandBuff.Reference.Get(),
            }
            .SelectMany(buff => buff.GetComponents<FastBombs>())
            .ToArray();
        foreach (var component in fastBombComponents) {
            component.m_Abilities = (component.m_Abilities ??
                    Array.Empty<BlueprintAbilityReference>())
                .Concat(arcaneBombs.Select(ability =>
                    ability.ToReference<BlueprintAbilityReference>()))
                .GroupBy(reference => reference.deserializedGuid)
                .Select(group => group.First())
                .ToArray();
        }

        ValidateArcaneBomberBombFeats(
            wizardBonusFeats,
            fastBombs,
            preciseBombs,
            arcaneBombs,
            fastBombComponents);
    }

    private static BlueprintFeature ConfigureArcaneBomberBombFeat(
        string nativeFeatureId,
        string name,
        int minimumLevel) {
        var nativeFeature = BlueprintTool.Get<BlueprintFeature>(nativeFeatureId);
        var configurator = FeatureConfigurator.New(
                $"ClassesRebornArcaneBomber{name}Feature",
                FutureContentIds.Get($"Wizard.ArcaneBomber.{name}"))
            .SetDisplayName($"ClassesReborn.Wizard.ArcaneBomber.{name}.Name")
            .SetDescription($"ClassesReborn.Wizard.ArcaneBomber.{name}.Description")
            .SetIcon(nativeFeature.Icon)
            .SetIsClassFeature(true)
            .SetRanks(1)
            .AddPrerequisiteNoFeature(nativeFeatureId)
            .AddFacts(new() { nativeFeature });
        configurator.AddComponent(new PrerequisiteArchetypeLevel {
            m_CharacterClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                BlueprintIds.WizardClass),
            m_Archetype = BlueprintTool.GetRef<BlueprintArchetypeReference>(
                BlueprintIds.ArcaneBomberArchetype),
            Level = minimumLevel,
        });
        return configurator.Configure();
    }

    private static void ValidateArcaneBomberBombFeats(
        BlueprintFeatureSelection wizardBonusFeats,
        BlueprintFeature fastBombs,
        BlueprintFeature preciseBombs,
        IReadOnlyCollection<BlueprintAbility> arcaneBombs,
        IReadOnlyCollection<FastBombs> fastBombComponents) {
        foreach (var feature in new[] { fastBombs, preciseBombs }) {
            if (wizardBonusFeats.m_AllFeatures.Count(reference =>
                    reference?.Get() == feature) != 1 ||
                (wizardBonusFeats.m_Features?.Length > 0 &&
                 wizardBonusFeats.m_Features.Count(reference =>
                     reference?.Get() == feature) != 1) ||
                feature.GetComponents<AddFacts>().Count(component =>
                    component.Facts.Any(fact =>
                        fact.AssetGuid == (feature == fastBombs
                            ? BlueprintTool.Get<BlueprintFeature>(
                                BlueprintIds.FastBombsFeature).AssetGuid
                            : BlueprintTool.Get<BlueprintFeature>(
                                BlueprintIds.PreciseBombsFeature).AssetGuid))) != 1) {
                throw new InvalidOperationException(
                    $"{feature.name} must appear exactly once in Wizard Bonus Feats and grant its native bomb feat.");
            }
        }

        if (arcaneBombs.Any(ability =>
                ability.GetComponent<AbilityIsBomb>() == null) ||
            fastBombComponents.Count == 0 ||
            fastBombComponents.Any(component =>
                arcaneBombs.Any(ability =>
                    component.m_Abilities.Count(reference =>
                        reference?.Get() == ability) != 1))) {
            throw new InvalidOperationException(
                "Fast Bombs and Precise Bombs must recognize every Arcane Bomber bomb ability.");
        }
    }

    private static void ConfigureArcaneBondObject() {
        var resource = AbilityResourceRefs.ItemBondResource.Reference.Get();
        resource.m_MaxAmount.BaseValue = 2;

        FeatureConfigurator.For(BlueprintIds.WizardArcaneBond)
            .SetDescription("ClassesReborn.Wizard.ArcaneBondObject.Description")
            .Configure();

        if (resource.m_MaxAmount.BaseValue != 2) {
            throw new InvalidOperationException(
                "Arcane Bond — Object must have two daily uses.");
        }
    }

    private static BlueprintFeature ConfigureKnowledgeIsPower() =>
        FeatureConfigurator.New(
                "ClassesRebornKnowledgeIsPowerDiscovery",
                Id("KnowledgeIsPower"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.KnowledgeIsPower.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.KnowledgeIsPower.Description")
            .SetIcon(FeatureRefs.CombatExpertiseFeature.Reference.Get().Icon)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 1)
            .AddComponent(new KnowledgeIsPowerComponent())
            .Configure();

    private static BlueprintFeature ConfigureOppositionResearch() {
        var options = OppositionSchools.Select(entry => {
            var opposition = BlueprintTool.Get<BlueprintFeature>(entry.FeatureId);
            return FeatureConfigurator.New(
                    $"ClassesRebornOppositionResearch{entry.School}",
                    Id($"OppositionResearch.{entry.School}"))
                .SetDisplayName(
                    $"ClassesReborn.ArcaneDiscovery.OppositionResearch.{entry.School}.Name")
                .SetDescription(
                    $"ClassesReborn.ArcaneDiscovery.OppositionResearch.{entry.School}.Description")
                .SetIcon(opposition.Icon)
                .SetIsClassFeature(true)
                .SetReapplyOnLevelUp(true)
                .AddPrerequisiteFeature(opposition)
                .AddComponent(new OppositionResearchComponent { School = entry.School })
                .Configure();
        }).ToArray();

        var configurator = FeatureSelectionConfigurator.New(
                "ClassesRebornOppositionResearchDiscovery",
                Id("OppositionResearch"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.OppositionResearch.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.OppositionResearch.Description")
            .SetIcon(FeatureRefs.SpellFocusAbjuration.Reference.Get().Icon)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .SetRanks(1)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 9);
        configurator.AddComponent(new PrerequisiteFeaturesFromList {
            m_Features = OppositionSchools.Select(entry =>
                    BlueprintTool.GetRef<BlueprintFeatureReference>(entry.FeatureId))
                .ToArray(),
            Amount = 1,
            Group = Prerequisite.GroupType.All,
        });
        var selection = configurator.Configure();
        selection.m_AllFeatures = options
            .Select(option => option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        return selection;
    }

    private static BlueprintFeature ConfigureCreativeDestruction() {
        var icon = AbilityRefs.Fireball.Reference.Get().Icon;
        var tempHpBuff = BuffConfigurator.New(
                "ClassesRebornCreativeDestructionTemporaryHitPointsBuff",
                Id("CreativeDestruction.Buff"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.CreativeDestruction.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.CreativeDestruction.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddTemporaryHitPointsFromAbilityValue(
                descriptor: ModifierDescriptor.UntypedStackable,
                value: ContextValues.Rank())
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .Configure();

        return FeatureConfigurator.New(
                "ClassesRebornCreativeDestructionDiscovery",
                Id("CreativeDestruction"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.CreativeDestruction.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.CreativeDestruction.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 1)
            .AddComponent(new CreativeDestructionComponent {
                m_TemporaryHitPointsBuff =
                    tempHpBuff.ToReference<BlueprintBuffReference>(),
            })
            .Configure();
    }

    private static BlueprintFeature ConfigureAlchemicalAffinity() =>
        FeatureConfigurator.New(
                "ClassesRebornAlchemicalAffinityDiscovery",
                Id("AlchemicalAffinity"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.AlchemicalAffinity.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.AlchemicalAffinity.Description")
            .SetIcon(FeatureSelectionRefs.DiscoverySelection.Reference.Get().Icon)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 5)
            .AddComponent(new SharedSpellListAffinityComponent {
                m_SpellLists = new[] {
                    BlueprintTool.GetRef<BlueprintSpellListReference>(
                        BlueprintIds.WizardSpellList),
                    BlueprintTool.GetRef<BlueprintSpellListReference>(
                        BlueprintIds.AlchemistSpellList),
                },
            })
            .Configure();

    private static BlueprintFeature ConfigureIdealize() =>
        FeatureConfigurator.New(
                "ClassesRebornIdealizeDiscovery",
                Id("Idealize"))
            .SetDisplayName("ClassesReborn.ArcaneDiscovery.Idealize.Name")
            .SetDescription("ClassesReborn.ArcaneDiscovery.Idealize.Description")
            .SetIcon(AbilityRefs.FoxsCunning.Reference.Get().Icon)
            .SetIsClassFeature(true)
            .SetReapplyOnLevelUp(true)
            .AddPrerequisiteClassLevel(BlueprintIds.WizardClass, 10)
            .Configure();

    private static void AddToSelection(
        BlueprintFeatureSelection selection,
        BlueprintFeature feature) {
        selection.m_AllFeatures = Append(selection.m_AllFeatures, feature);
        if (selection.m_Features?.Length > 0) {
            selection.m_Features = Append(selection.m_Features, feature);
        }
    }

    private static BlueprintFeatureReference[] Append(
        BlueprintFeatureReference[] existing,
        BlueprintFeature feature) {
        var result = existing?.ToList() ?? new List<BlueprintFeatureReference>();
        if (!result.Any(reference => reference?.Get() == feature)) {
            result.Add(feature.ToReference<BlueprintFeatureReference>());
        }
        return result.ToArray();
    }

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

    private static void Validate(
        BlueprintFeatureSelection selection,
        IReadOnlyCollection<BlueprintFeature> options) {
        if (selection.m_AllFeatures.Length != options.Count ||
            options.Any(option => selection.m_AllFeatures.Count(reference =>
                reference?.Get() == option) != 1)) {
            throw new InvalidOperationException(
                "Arcane Discovery must contain every enabled discovery exactly once.");
        }

        foreach (var parent in new[] {
                     BlueprintTool.Get<BlueprintFeatureSelection>(BlueprintIds.BasicFeatSelection),
                     FeatureSelectionRefs.WizardFeatSelection.Reference.Get(),
                 }) {
            if (parent.m_AllFeatures.Count(reference =>
                    reference?.Get() == selection) != 1) {
                throw new InvalidOperationException(
                    $"Arcane Discovery must appear exactly once in {parent.name}.");
            }
        }

        if (Main.Settings.ArcaneDiscoveryOppositionResearch &&
            BlueprintTool.Get<BlueprintFeatureSelection>(
                    Id("OppositionResearch"))
                .m_AllFeatures.Length != OppositionSchools.Length) {
            throw new InvalidOperationException(
                "Opposition Research must offer all eight opposition schools.");
        }
    }
}
