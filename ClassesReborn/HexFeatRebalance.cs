using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static partial class FeatRebalance {
    private sealed class HexFeatEntry {
        internal BlueprintAbility RootAbility;
        internal List<BlueprintAbility> Abilities = new();
        internal List<BlueprintFeature> GrantingFeatures = new();
    }

    private static void ConfigureCursingGaze() {
        var entries = CollectHexFeatEntries(excludeGrandHexes: true);
        var abilities = entries
            .SelectMany(entry => entry.Abilities)
            .Distinct()
            .ToArray();
        var grantingFeatures = entries
            .SelectMany(entry => entry.GrantingFeatures)
            .Distinct()
            .ToArray();
        if (abilities.Length == 0 || grantingFeatures.Length == 0) {
            throw new InvalidOperationException(
                "Cursing Gaze could not find any non-grand hexes.");
        }

        var feature = FeatureConfigurator.New(
                "ClassesRebornCursingGaze",
                FutureContentIds.Get("MythicAbility.CursingGaze"))
            .SetDisplayName("ClassesReborn.CursingGaze.Name")
            .SetDescription("ClassesReborn.CursingGaze.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeatureSelection>(
                BlueprintIds.WitchHexSelection).Icon)
            .SetGroups(FeatureGroup.MythicAbility)
            .SetRanks(1)
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific)
            .AddComponent(CreateAnyHexPrerequisite(grantingFeatures))
            .AddComponent(new CursingGazeActionType {
                m_Abilities = abilities.Select(ability =>
                        ability.ToReference<BlueprintAbilityReference>())
                    .ToArray(),
            })
            .Configure();
        AddToSelection(BlueprintIds.MythicAbilitySelection, feature);
    }

    private static void ConfigureHexStrike() {
        var entries = CollectHexFeatEntries(excludeGrandHexes: false)
            .Select(entry => {
                entry.Abilities = entry.Abilities
                    .Where(IsEligibleHexStrikeAbility)
                    .Distinct()
                    .ToList();
                return entry;
            })
            .Where(entry => entry.Abilities.Count > 0)
            .ToArray();
        if (entries.Length == 0) {
            throw new InvalidOperationException(
                "Hex Strike could not find any eligible single-target hexes.");
        }

        var icon = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.WitchHexSelection).Icon;
        BuffConfigurator.New(
                "ClassesRebornHexStrikeToggleBuff",
                FutureContentIds.Get("Feat.HexStrike.ToggleBuff"))
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddComponent(new HexStrikeToggle())
            .Configure();
        ActivatableAbilityConfigurator.New(
                "ClassesRebornHexStrikeToggleAbility",
                FutureContentIds.Get("Feat.HexStrike.ToggleAbility"))
            .SetDisplayName("ClassesReborn.HexStrike.Name")
            .SetDescription("ClassesReborn.HexStrike.Description")
            .SetIcon(icon)
            .SetBuff(FutureContentIds.Get("Feat.HexStrike.ToggleBuff"))
            .SetIsOnByDefault(true)
            .SetDoNotTurnOffOnRest(true)
            .SetDeactivateImmediately(true)
            .SetActivationType(AbilityActivationType.Immediately)
            .Configure();

        var options = entries.Select(entry => CreateHexStrikeOption(entry, icon))
            .ToArray();
        var grantingFeatures = entries
            .SelectMany(entry => entry.GrantingFeatures)
            .Distinct()
            .ToArray();
        var selection = FeatureSelectionConfigurator.New(
                "ClassesRebornHexStrike",
                FutureContentIds.Get("Feat.HexStrike"))
            .SetDisplayName("ClassesReborn.HexStrike.Name")
            .SetDescription("ClassesReborn.HexStrike.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.Feat)
            .SetRanks(1)
            .SetIgnorePrerequisites(false)
            .SetObligatory(true)
            .AddFeatureTagsComponent(FeatureTag.ClassSpecific | FeatureTag.Melee)
            .AddComponent(CreateAnyHexPrerequisite(grantingFeatures))
            .Configure();
        selection.m_AllFeatures = options.Select(option =>
                option.ToReference<BlueprintFeatureReference>())
            .ToArray();
        selection.m_Features = selection.m_AllFeatures.ToArray();
        AddAsFeat(selection);

        foreach (var ability in entries.SelectMany(entry => entry.Abilities).Distinct()) {
            if (ability.GetComponent<AbilityTargetHexStrike>() == null) {
                AbilityConfigurator.For(ability)
                    .AddComponent(new AbilityTargetHexStrike())
                    .Configure();
            }
        }

        if (options.Any(option => option.Groups?.Any() == true)) {
            throw new InvalidOperationException(
                "Hex Strike's nested hex choices must not be standalone feats.");
        }
    }

    private static BlueprintFeature CreateHexStrikeOption(
        HexFeatEntry entry,
        UnityEngine.Sprite fallbackIcon) {
        var displayFeature = entry.GrantingFeatures.First();
        var configurator = FeatureConfigurator.New(
                $"ClassesRebornHexStrike{entry.RootAbility.AssetGuid}",
                FutureContentIds.Get(
                    $"Feat.HexStrike.Option.{entry.RootAbility.AssetGuid}"))
            .SetDisplayName(displayFeature.m_DisplayName)
            .SetDescription("ClassesReborn.HexStrike.Description")
            .SetIcon(entry.RootAbility.Icon ?? displayFeature.Icon ?? fallbackIcon)
            .SetRanks(1)
            .AddFacts(new() { FutureContentIds.Get("Feat.HexStrike.ToggleAbility") })
            .AddComponent(new HexStrikeTrigger {
                m_Hexes = entry.Abilities.Select(ability =>
                        ability.ToReference<BlueprintAbilityReference>())
                    .ToArray(),
                m_ToggleBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    FutureContentIds.Get("Feat.HexStrike.ToggleBuff")),
            });
        foreach (var feature in entry.GrantingFeatures) {
            configurator.AddComponent(new PrerequisiteFeature {
                Group = Prerequisite.GroupType.Any,
                m_Feature = feature.ToReference<BlueprintFeatureReference>(),
            });
        }
        return configurator.Configure();
    }

    private static PrerequisiteFeaturesFromList CreateAnyHexPrerequisite(
        IEnumerable<BlueprintFeature> features) => new() {
        m_Features = features.Distinct().Select(feature =>
                feature.ToReference<BlueprintFeatureReference>())
            .ToArray(),
        Amount = 1,
        Group = Prerequisite.GroupType.All,
    };

    private static HexFeatEntry[] CollectHexFeatEntries(bool excludeGrandHexes) {
        var primarySelections = new[] {
            BlueprintIds.WitchHexSelection,
            BlueprintIds.ShamanHexSelection,
            BlueprintIds.HexcrafterHexSelection,
        }.Select(BlueprintTool.Get<BlueprintFeatureSelection>).ToArray();
        var sylvanSelection = BlueprintTool.Get<BlueprintFeatureSelection>(
            BlueprintIds.SylvanTricksterTalentSelection);
        var primaryFeatures = primarySelections
            .SelectMany(selection => selection.m_AllFeatures)
            .Select(reference => reference?.Get() as BlueprintFeature)
            .Where(feature => feature != null)
            .ToHashSet();
        var features = primaryFeatures
            .Concat(sylvanSelection.m_AllFeatures
                .Select(reference => reference?.Get() as BlueprintFeature)
                .Where(feature => feature != null)
                .Where(feature =>
                    primaryFeatures.Contains(feature) ||
                    feature.Groups?.Contains(FeatureGroup.WitchHex) == true ||
                    feature.Groups?.Contains(FeatureGroup.ShamanHex) == true ||
                    feature.GetComponents<AddFacts>()
                        .SelectMany(component => component.Facts)
                        .OfType<BlueprintAbility>()
                        .SelectMany(ExpandVariants)
                        .Any(ability => ability.SpellDescriptor.HasFlag(
                            Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Hex))))
            .Distinct()
            .ToArray();
        var grandFeatures = excludeGrandHexes
            ? BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WitchGrandHex)
                .IsPrerequisiteFor
                .Select(reference => reference?.Get())
                .Where(feature => feature != null)
                .ToHashSet()
            : new HashSet<BlueprintFeature>();
        var entries = new Dictionary<string, HexFeatEntry>();

        foreach (var feature in features) {
            if (grandFeatures.Contains(feature)) {
                continue;
            }
            foreach (var rootAbility in feature.GetComponents<AddFacts>()
                         .SelectMany(component => component.Facts)
                         .OfType<BlueprintAbility>()) {
                var key = rootAbility.AssetGuid.ToString();
                if (!entries.TryGetValue(key, out var entry)) {
                    entry = new HexFeatEntry { RootAbility = rootAbility };
                    entries.Add(key, entry);
                }
                if (!entry.GrantingFeatures.Contains(feature)) {
                    entry.GrantingFeatures.Add(feature);
                }
                foreach (var ability in ExpandVariants(rootAbility)) {
                    if (!entry.Abilities.Contains(ability)) {
                        entry.Abilities.Add(ability);
                    }
                }
            }
        }

        return entries.Values.ToArray();
    }

    private static bool IsEligibleHexStrikeAbility(BlueprintAbility ability) =>
        ability.CanTargetEnemies && !ability.CanTargetPoint &&
        ability.GetComponent<AbilityAoERadius>() == null;
}
