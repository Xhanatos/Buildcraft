using HarmonyLib;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.AbilityScores;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.FactLogic;

namespace ClassesReborn;

internal static class RaceRebalance {
    internal static void Configure() {
        var halfOrc = BlueprintTool.Get<BlueprintRace>(BlueprintIds.HalfOrcRace);
        var iconSource = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.HalfOrcFerocity);
        var strengthBonus = FeatureConfigurator.New(
                "ClassesRebornHalfOrcStrengthBonusFeature",
                BlueprintIds.HalfOrcStrengthBonusFeature)
            .SetDisplayName("ClassesReborn.HalfOrcStrengthBonus.Name")
            .SetDescription("ClassesReborn.HalfOrcStrengthBonus.Description")
            .SetIcon(iconSource.Icon)
            .AddStatBonus(
                ModifierDescriptor.Racial,
                false,
                StatType.Strength,
                2)
            .AddStatBonus(
                ModifierDescriptor.Racial,
                false,
                StatType.Intelligence,
                -2)
            .Configure();

        var racialFeatures = (halfOrc.m_Features ??
                Array.Empty<BlueprintFeatureBaseReference>())
            .Where(reference =>
                reference?.deserializedGuid != strengthBonus.AssetGuid)
            .ToList();
        racialFeatures.Add(
            BlueprintTool.GetRef<BlueprintFeatureBaseReference>(
                strengthBonus.AssetGuid.ToString()));
        halfOrc.m_Features = racialFeatures.ToArray();

        var bonuses = strengthBonus.GetComponents<AddStatBonus>().ToArray();
        var grantCount = halfOrc.m_Features.Count(reference =>
            reference?.deserializedGuid == strengthBonus.AssetGuid);
        if (!halfOrc.SelectableRaceStat ||
            grantCount != 1 ||
            bonuses.Length != 2 ||
            bonuses.Count(component =>
                component.Stat == StatType.Strength &&
                component.Value == 2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1 ||
            bonuses.Count(component =>
                component.Stat == StatType.Intelligence &&
                component.Value == -2 &&
                component.Descriptor == ModifierDescriptor.Racial) != 1) {
            throw new InvalidOperationException(
                "Half-Orcs must retain their selectable ability bonus and gain exactly one racial feature providing +2 Strength and -2 Intelligence.");
        }
    }
}

[HarmonyPatch(typeof(TooltipTemplateLevelUpRace), "Prepare")]
internal static class HalfOrcRaceStatDisplayPatch {
    [HarmonyPostfix]
    private static void Postfix(
        BlueprintRace ___m_Race,
        List<UIStatBonus> ___m_StatBonusList) {
        if (!Main.Settings.HalfOrc ||
            ___m_Race?.AssetGuid.ToString() != BlueprintIds.HalfOrcRace ||
            ___m_StatBonusList == null) {
            return;
        }

        var racialStatFeature = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.HalfOrcStrengthBonusFeature);
        foreach (var bonus in racialStatFeature.GetComponents<AddStatBonus>()) {
            if (___m_StatBonusList.Any(existing =>
                existing.StatType == bonus.Stat &&
                existing.Value == bonus.Value &&
                existing.Descriptor == bonus.Descriptor)) {
                continue;
            }

            ___m_StatBonusList.Add(new UIStatBonus {
                StatType = bonus.Stat,
                Descriptor = bonus.Descriptor,
                Value = bonus.Value,
            });
        }
    }
}
