using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace ClassesReborn;

internal static class RagePowerRebalance {
    private static readonly string[] RageBuffIds = {
        BlueprintIds.StandardRageBuff,
        BlueprintIds.FocusedRageBuff,
        BlueprintIds.BloodragerStandardRageBuff,
        BlueprintIds.BloodragerGreaterRageBuff,
        BlueprintIds.BloodragerMightyRageBuff,
        BlueprintIds.FleshEaterUnboundRageBuff,
        BlueprintIds.ArmyStandardRageBuff,
        BlueprintIds.ReformedFiendBloodrageBuff,
        BlueprintIds.RageshaperDevastatingFormBuff,
        BlueprintIds.RageshaperGreaterDevastatingFormBuff,
        BlueprintIds.RageshaperMightyDevastatingFormBuff,
        BlueprintIds.InspiredRageEffectBuff,
        BlueprintIds.InspiredRageBeforeMasterSkaldBuff,
        BlueprintIds.InspiredRageMythicBuff,
        BlueprintIds.InspiredRageNoCasterBuff,
    };

    private static BlueprintBuffReference[] RageBuffs() =>
        RageBuffIds
            .Select(BlueprintTool.GetRef<BlueprintBuffReference>)
            .ToArray();

    internal static void Configure() {
        var rageIcon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.BarbarianRage).Icon;
        var ragePowers = new List<BlueprintFeatureBase>();
        var sharedByInspiredRage = new List<BlueprintUnitFactReference>();

        BlueprintFeature superstition = null;
        BlueprintFeature witchHunter = null;
        if (Main.Settings.RagePowerSuperstition) {
            superstition = ConfigureSuperstition(rageIcon);
            ragePowers.Add(superstition);
            sharedByInspiredRage.Add(Ref(superstition));
        }
        if (Main.Settings.RagePowerWitchHunter && superstition != null) {
            witchHunter = ConfigureWitchHunter(rageIcon, superstition);
            ragePowers.Add(witchHunter);
            sharedByInspiredRage.Add(Ref(witchHunter));
        }
        if (Main.Settings.RagePowerEaterOfMagic && superstition != null) {
            ragePowers.Add(ConfigureEaterOfMagic(rageIcon, superstition));
        }
        if (Main.Settings.RagePowerStrengthSurge) {
            ragePowers.Add(ConfigureStrengthSurge(rageIcon));
        }
        if (Main.Settings.RagePowerElementalRage) {
            var elemental = ConfigureElementalRage(rageIcon);
            ragePowers.Add(elemental.BaseSelection);
            ragePowers.Add(elemental.GreaterSelection);
            sharedByInspiredRage.AddRange(elemental.ConcreteFeatures.Select(Ref));
        }
        if (Main.Settings.RagePowerGhostRager) {
            var ghostRager = ConfigureGhostRager(rageIcon);
            ragePowers.Add(ghostRager);
            sharedByInspiredRage.Add(Ref(ghostRager));
        }
        if (Main.Settings.RagePowerFerociousMount) {
            ragePowers.AddRange(ConfigureFerociousMount(rageIcon, ragePowers));
        }

        if (ragePowers.Count == 0) {
            return;
        }

        foreach (var selectionId in new[] {
                     BlueprintIds.RagePowerSelection,
                     BlueprintIds.ExtraRagePowerSelection,
                     BlueprintIds.InstinctualWarriorRagePowerSelection,
                     BlueprintIds.SkaldRagePowerSelection,
                 }) {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
            var additions = ragePowers
                .Select(feature => BlueprintTool.GetRef<BlueprintFeatureReference>(
                    feature.AssetGuid.ToString()))
                .Where(reference => selection.m_AllFeatures.All(existing =>
                    existing?.Get() != reference.Get()))
                .ToArray();
            selection.m_AllFeatures = selection.m_AllFeatures.Concat(additions).ToArray();
        }

        ExtendInspiredRage(sharedByInspiredRage);
        Validate(ragePowers);
    }

    private static BlueprintFeature ConfigureSuperstition(UnityEngine.Sprite icon) =>
        AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornSuperstitionRagePower",
                        FutureContentIds.Get("RagePower.Superstition"))
                    .SetDisplayName("ClassesReborn.RagePower.Superstition.Name")
                    .SetDescription("ClassesReborn.RagePower.Superstition.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddComponent(new SuperstitionEnemyMagicSaveBonus {
                        m_RageBuffs = RageBuffs(),
                    }),
                1)
            .Configure();

    private static BlueprintFeature ConfigureWitchHunter(
        UnityEngine.Sprite icon,
        BlueprintFeature superstition) =>
        AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornWitchHunterRagePower",
                        FutureContentIds.Get("RagePower.WitchHunter"))
                    .SetDisplayName("ClassesReborn.RagePower.WitchHunter.Name")
                    .SetDescription("ClassesReborn.RagePower.WitchHunter.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddPrerequisiteFeature(superstition)
                    .AddComponent(new WitchHunterRageDamage {
                        m_RageBuffs = RageBuffs(),
                    }),
                1)
            .Configure();

    private static BlueprintFeature ConfigureEaterOfMagic(
        UnityEngine.Sprite icon,
        BlueprintFeature superstition) {
        var tempHpBuff = BuffConfigurator.New(
                "ClassesRebornEaterOfMagicTemporaryHitPointsBuff",
                FutureContentIds.Get("RagePower.EaterOfMagic.TempHpBuff"))
            .SetDisplayName("ClassesReborn.RagePower.EaterOfMagic.Name")
            .SetDescription("ClassesReborn.RagePower.EaterOfMagic.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddTemporaryHitPointsFromAbilityValue(
                descriptor: ModifierDescriptor.UntypedStackable,
                value: ContextValues.Rank())
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .Configure();

        return AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornEaterOfMagicRagePower",
                        FutureContentIds.Get("RagePower.EaterOfMagic"))
                    .SetDisplayName("ClassesReborn.RagePower.EaterOfMagic.Name")
                    .SetDescription("ClassesReborn.RagePower.EaterOfMagic.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddPrerequisiteFeature(superstition)
                    .AddComponent(new EaterOfMagicReroll {
                        m_RageBuffs = RageBuffs(),
                        m_TemporaryHitPointsBuff =
                            BlueprintTool.GetRef<BlueprintBuffReference>(tempHpBuff.AssetGuid.ToString()),
                    }),
                10)
            .Configure();
    }

    private static BlueprintFeature ConfigureStrengthSurge(UnityEngine.Sprite icon) {
        var resource = AbilityResourceConfigurator.New(
                "ClassesRebornStrengthSurgeResource",
                FutureContentIds.Get("RagePower.StrengthSurge.Resource"))
            .SetLocalizedName("ClassesReborn.RagePower.StrengthSurge.Name")
            .SetLocalizedDescription("ClassesReborn.RagePower.StrengthSurge.Description")
            .SetIcon(icon)
            .SetMax(3)
            .Configure();

        var buff = BuffConfigurator.New(
                "ClassesRebornStrengthSurgeBuff",
                FutureContentIds.Get("RagePower.StrengthSurge.Buff"))
            .SetDisplayName("ClassesReborn.RagePower.StrengthSurge.Name")
            .SetDescription("ClassesReborn.RagePower.StrengthSurge.BuffDescription")
            .SetIcon(icon)
            .SetStacking(StackingType.Replace)
            .AddComponent(new StrengthSurgeNextManeuver())
            .Configure();

        var ability = AbilityConfigurator.New(
                "ClassesRebornStrengthSurgeAbility",
                FutureContentIds.Get("RagePower.StrengthSurge.Ability"))
            .SetDisplayName("ClassesReborn.RagePower.StrengthSurge.Name")
            .SetDescription("ClassesReborn.RagePower.StrengthSurge.Description")
            .SetIcon(icon)
            .SetType(AbilityType.Supernatural)
            .SetRange(AbilityRange.Personal)
            .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddAbilityResourceLogic(
                amount: 1,
                isSpendResource: true,
                requiredResource: resource)
            .AddComponent(new StrengthSurgeRestriction {
                m_RageBuffs = RageBuffs(),
                m_SurgeBuff = BlueprintTool.GetRef<BlueprintBuffReference>(buff.AssetGuid.ToString()),
            })
            .AddAbilityEffectRunAction(
                ActionsBuilder.New().ApplyBuff(buff, ContextDuration.Fixed(10), isNotDispelable: true))
            .Configure();

        return AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornStrengthSurgeRagePower",
                        FutureContentIds.Get("RagePower.StrengthSurge"))
                    .SetDisplayName("ClassesReborn.RagePower.StrengthSurge.Name")
                    .SetDescription("ClassesReborn.RagePower.StrengthSurge.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddFacts(new() { ability })
                    .AddAbilityResources(
                        amount: 0,
                        resource: resource,
                        restoreAmount: true,
                        restoreOnLevelUp: true)
                    .AddComponent(new StrengthSurgeResourceController {
                        m_RageBuffs = RageBuffs(),
                        m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(
                            resource.AssetGuid.ToString()),
                        m_SurgeBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                            buff.AssetGuid.ToString()),
                    }),
                8)
            .Configure();
    }

    private static (
        BlueprintFeatureSelection BaseSelection,
        BlueprintFeatureSelection GreaterSelection,
        BlueprintFeature[] ConcreteFeatures) ConfigureElementalRage(UnityEngine.Sprite icon) {
        var baseFeatures = new List<BlueprintFeature>();
        var greaterFeatures = new List<BlueprintFeature>();
        foreach (var (name, energy) in new[] {
                     ("Acid", DamageEnergyType.Acid),
                     ("Cold", DamageEnergyType.Cold),
                     ("Electricity", DamageEnergyType.Electricity),
                     ("Fire", DamageEnergyType.Fire),
                 }) {
            var baseFeature = FeatureConfigurator.New(
                    $"ClassesRebornElementalRage{name}RagePower",
                    FutureContentIds.Get($"RagePower.ElementalRage.{name}"))
                .SetDisplayName($"ClassesReborn.RagePower.ElementalRage.{name}.Name")
                .SetDescription("ClassesReborn.RagePower.ElementalRage.Description")
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .AddComponent(new ElementalRageDamage {
                    EnergyType = energy,
                    m_RageBuffs = RageBuffs(),
                })
                .Configure();
            baseFeatures.Add(baseFeature);

            var greater = FeatureConfigurator.New(
                    $"ClassesRebornGreaterElementalRage{name}RagePower",
                    FutureContentIds.Get($"RagePower.GreaterElementalRage.{name}"))
                .SetDisplayName($"ClassesReborn.RagePower.GreaterElementalRage.{name}.Name")
                .SetDescription("ClassesReborn.RagePower.GreaterElementalRage.Description")
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .AddPrerequisiteFeature(baseFeature)
                .AddComponent(new GreaterElementalRageCriticalDamage {
                    EnergyType = energy,
                    m_RageBuffs = RageBuffs(),
                })
                .Configure();
            greaterFeatures.Add(greater);
        }

        var baseSelection = AddRageLevelPrerequisites(
                FeatureSelectionConfigurator.New(
                        "ClassesRebornElementalRageSelection",
                        FutureContentIds.Get("RagePower.ElementalRage.Selection"))
                    .SetDisplayName("ClassesReborn.RagePower.ElementalRage.Name")
                    .SetDescription("ClassesReborn.RagePower.ElementalRage.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower),
                4)
            .Configure();
        baseSelection.m_AllFeatures = baseFeatures
            .Select(feature => BlueprintTool.GetRef<BlueprintFeatureReference>(
                feature.AssetGuid.ToString()))
            .ToArray();

        var greaterSelection = AddRageLevelPrerequisites(
                FeatureSelectionConfigurator.New(
                        "ClassesRebornGreaterElementalRageSelection",
                        FutureContentIds.Get("RagePower.GreaterElementalRage.Selection"))
                    .SetDisplayName("ClassesReborn.RagePower.GreaterElementalRage.Name")
                    .SetDescription("ClassesReborn.RagePower.GreaterElementalRage.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower),
                8)
            .Configure();
        greaterSelection.m_AllFeatures = greaterFeatures
            .Select(feature => BlueprintTool.GetRef<BlueprintFeatureReference>(
                feature.AssetGuid.ToString()))
            .ToArray();

        return (baseSelection, greaterSelection,
            baseFeatures.Concat(greaterFeatures).ToArray());
    }

    private static BlueprintFeature ConfigureGhostRager(UnityEngine.Sprite icon) {
        var effectBuff = BuffConfigurator.New(
                "ClassesRebornGhostRagerEffectBuff",
                FutureContentIds.Get("RagePower.GhostRager.Buff"))
            .SetDisplayName("ClassesReborn.RagePower.GhostRager.Name")
            .SetDescription("ClassesReborn.RagePower.GhostRager.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .CopyFrom(
                BlueprintIds.BloodragerUndeadGhostStrikeBuff,
                typeof(AddOutgoingPhysicalDamageProperty))
            .AddComponent(new GhostRagerTouchDefense())
            .Configure();

        return AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornGhostRagerRagePower",
                        FutureContentIds.Get("RagePower.GhostRager"))
                    .SetDisplayName("ClassesReborn.RagePower.GhostRager.Name")
                    .SetDescription("ClassesReborn.RagePower.GhostRager.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddComponent(new BuffExtraEffects {
                        m_CheckedBuffList = RageBuffs(),
                        m_ExtraEffectBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                            effectBuff.AssetGuid.ToString()),
                    }),
                6)
            .Configure();
    }

    private static BlueprintFeatureBase[] ConfigureFerociousMount(
        UnityEngine.Sprite icon,
        IEnumerable<BlueprintFeatureBase> customRagePowers) {
        var shareableRagePowers = CollectShareableRagePowers(customRagePowers);

        var ferociousPet = FeatureConfigurator.New(
                "ClassesRebornFerociousMountPetFeature",
                FutureContentIds.Get("RagePower.FerociousMount.Pet"))
            .SetDisplayName("ClassesReborn.RagePower.FerociousMount.Name")
            .SetDescription("ClassesReborn.RagePower.FerociousMount.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .AddComponent(new FerociousMountPetRageBonuses())
            .Configure();

        var ferocious = AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornFerociousMountRagePower",
                        FutureContentIds.Get("RagePower.FerociousMount"))
                    .SetDisplayName("ClassesReborn.RagePower.FerociousMount.Name")
                    .SetDescription("ClassesReborn.RagePower.FerociousMount.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddFeatureToPet(ferociousPet, PetType.AnimalCompanion),
                1)
            .Configure();

        var greaterPet = FeatureConfigurator.New(
                "ClassesRebornGreaterFerociousMountPetFeature",
                FutureContentIds.Get("RagePower.GreaterFerociousMount.Pet"))
            .SetDisplayName("ClassesReborn.RagePower.GreaterFerociousMount.Name")
            .SetDescription("ClassesReborn.RagePower.GreaterFerociousMount.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .Configure();

        var greater = AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornGreaterFerociousMountRagePower",
                        FutureContentIds.Get("RagePower.GreaterFerociousMount"))
                    .SetDisplayName("ClassesReborn.RagePower.GreaterFerociousMount.Name")
                    .SetDescription("ClassesReborn.RagePower.GreaterFerociousMount.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddPrerequisiteFeature(ferocious)
                    .AddFeatureToPet(greaterPet, PetType.AnimalCompanion)
                    .AddComponent(new GreaterFerociousMountRagePowerSharing {
                        m_RageBuffs = RageBuffs(),
                        m_RagePowerFeatures = shareableRagePowers,
                    }),
                8)
            .Configure();

        var spiritPet = FeatureConfigurator.New(
                "ClassesRebornSpiritSteedPetFeature",
                FutureContentIds.Get("RagePower.SpiritSteed.Pet"))
            .SetDisplayName("ClassesReborn.RagePower.SpiritSteed.Name")
            .SetDescription("ClassesReborn.RagePower.SpiritSteed.Description")
            .SetIcon(icon)
            .SetIsClassFeature(true)
            .CopyFrom(
                BlueprintIds.GhostRiderMagicAttacksPet,
                typeof(AddOutgoingPhysicalDamageProperty))
            .AddComponent(new SpiritSteedDamageReduction())
            .Configure();

        var spirit = AddRageLevelPrerequisites(
                FeatureConfigurator.New(
                        "ClassesRebornSpiritSteedRagePower",
                        FutureContentIds.Get("RagePower.SpiritSteed"))
                    .SetDisplayName("ClassesReborn.RagePower.SpiritSteed.Name")
                    .SetDescription("ClassesReborn.RagePower.SpiritSteed.Description")
                    .SetIcon(icon)
                    .SetIsClassFeature(true)
                    .SetGroups(FeatureGroup.RagePower)
                    .AddPrerequisiteFeature(ferocious)
                    .AddFeatureToPet(spiritPet, PetType.AnimalCompanion),
                6)
            .Configure();

        return new BlueprintFeatureBase[] { ferocious, greater, spirit };
    }

    private static BlueprintFeatureReference[] CollectShareableRagePowers(
        IEnumerable<BlueprintFeatureBase> customRagePowers) {
        var features = new HashSet<BlueprintFeature>();
        var visited = new HashSet<BlueprintFeatureBase>();

        void Collect(BlueprintFeatureBase candidate) {
            if (candidate == null || !visited.Add(candidate)) {
                return;
            }
            if (candidate is BlueprintFeatureSelection selection) {
                foreach (var reference in selection.m_AllFeatures) {
                    Collect(reference?.Get());
                }
                return;
            }
            if (candidate is BlueprintFeature feature) {
                features.Add(feature);
            }
        }

        foreach (var selectionId in new[] {
                     BlueprintIds.RagePowerSelection,
                     BlueprintIds.ExtraRagePowerSelection,
                     BlueprintIds.InstinctualWarriorRagePowerSelection,
                     BlueprintIds.SkaldRagePowerSelection,
                 }) {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
            foreach (var reference in selection.m_AllFeatures) {
                Collect(reference?.Get());
            }
        }
        foreach (var customRagePower in customRagePowers) {
            Collect(customRagePower);
        }

        return features
            .OrderBy(feature => feature.AssetGuid.ToString())
            .Select(feature => BlueprintTool.GetRef<BlueprintFeatureReference>(
                feature.AssetGuid.ToString()))
            .ToArray();
    }

    private static FeatureConfigurator AddRageLevelPrerequisites(
        FeatureConfigurator configurator,
        int level) {
        configurator
            .AddPrerequisiteClassLevel(
                BlueprintIds.BarbarianClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteClassLevel(
                BlueprintIds.SkaldClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteClassLevel(
                BlueprintIds.ShifterClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteArchetypeLevel(
                archetype: BlueprintIds.PrimalistArchetype,
                characterClass: BlueprintIds.BloodragerClass,
                level: level,
                group: Prerequisite.GroupType.Any);
        return configurator;
    }

    private static FeatureSelectionConfigurator AddRageLevelPrerequisites(
        FeatureSelectionConfigurator configurator,
        int level) {
        configurator
            .AddPrerequisiteClassLevel(
                BlueprintIds.BarbarianClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteClassLevel(
                BlueprintIds.SkaldClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteClassLevel(
                BlueprintIds.ShifterClass, level, group: Prerequisite.GroupType.Any)
            .AddPrerequisiteArchetypeLevel(
                archetype: BlueprintIds.PrimalistArchetype,
                characterClass: BlueprintIds.BloodragerClass,
                level: level,
                group: Prerequisite.GroupType.Any);
        return configurator;
    }

    private static void ExtendInspiredRage(
        IEnumerable<BlueprintUnitFactReference> additions) {
        var buff = BlueprintTool.Get<BlueprintBuff>(BlueprintIds.InspiredRageEffectBuff);
        var component = buff.GetComponent<AddFactsFromCaster>();
        if (component == null) {
            throw new InvalidOperationException(
                "Inspired Rage Effect Buff is missing AddFactsFromCaster.");
        }

        component.m_Facts = component.m_Facts
            .Concat(additions)
            .GroupBy(reference => reference?.Get())
            .Select(group => group.First())
            .ToArray();
    }

    private static void Validate(IReadOnlyCollection<BlueprintFeatureBase> powers) {
        foreach (var selectionId in new[] {
                     BlueprintIds.RagePowerSelection,
                     BlueprintIds.ExtraRagePowerSelection,
                     BlueprintIds.InstinctualWarriorRagePowerSelection,
                     BlueprintIds.SkaldRagePowerSelection,
                 }) {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionId);
            foreach (var power in powers) {
                if (selection.m_AllFeatures.Count(reference => reference?.Get() == power) != 1) {
                    throw new InvalidOperationException(
                        $"{power.name} must appear exactly once in {selection.name}.");
                }
            }
        }
    }

    private static BlueprintUnitFactReference Ref(BlueprintUnitFact fact) =>
        BlueprintTool.GetRef<BlueprintUnitFactReference>(fact.AssetGuid.ToString());

    private static BlueprintUnitFactReference Ref(string id) =>
        BlueprintTool.GetRef<BlueprintUnitFactReference>(id);
}
