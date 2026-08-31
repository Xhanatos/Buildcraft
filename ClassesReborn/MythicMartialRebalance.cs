using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace ClassesReborn;

internal static partial class FeatRebalance {
    private static void ConfigureMythicMartialAbilities() {
        if (Main.Settings.Ricochet) {
            ConfigureRicochet();
        }
        if (Main.Settings.BashingBulwark) {
            ConfigureBashingBulwark();
        }
        if (Main.Settings.ShieldedCasting) {
            ConfigureShieldedCasting();
        }
    }

    private static void ConfigureRicochet() {
        var feature = FeatureConfigurator.New(
                "ClassesRebornRicochetMythicAbility",
                FutureContentIds.Get("MythicAbility.Ricochet"))
            .SetDisplayName("ClassesReborn.Ricochet.Name")
            .SetDescription("ClassesReborn.Ricochet.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(BlueprintIds.WeaponFocus).Icon)
            .SetGroups(FeatureGroup.MythicAbility)
            .SetRanks(1)
            .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
            .AddComponent(new RicochetComponent())
            .Configure();
        AddToSelection(BlueprintIds.MythicAbilitySelection, feature);
    }

    private static void ConfigureBashingBulwark() {
        var icon = BlueprintTool.Get<BlueprintFeature>(BlueprintIds.ShieldFocus).Icon;
        var acBuff = BuffConfigurator.New(
                "ClassesRebornBashingBulwarkAcBuff",
                FutureContentIds.Get("Buff.BashingBulwark.AC"))
            .SetDisplayName("ClassesReborn.BashingBulwark.Name")
            .SetDescription("ClassesReborn.BashingBulwark.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .AddStatBonus(
                descriptor: ModifierDescriptor.UntypedStackable,
                stat: StatType.AC,
                value: 2)
            .Configure();

        var feature = FeatureConfigurator.New(
                "ClassesRebornBashingBulwarkMythicAbility",
                FutureContentIds.Get("MythicAbility.BashingBulwark"))
            .SetDisplayName("ClassesReborn.BashingBulwark.Name")
            .SetDescription("ClassesReborn.BashingBulwark.Description")
            .SetIcon(icon)
            .SetGroups(FeatureGroup.MythicAbility)
            .SetRanks(1)
            .AddFeatureTagsComponent(
                FeatureTag.Attack | FeatureTag.Damage | FeatureTag.Defense)
            .AddComponent(new BashingBulwarkComponent {
                m_AcBuff = acBuff.ToReference<BlueprintBuffReference>(),
            })
            .Configure();
        AddToSelection(BlueprintIds.MythicAbilitySelection, feature);
    }

    private static void ConfigureShieldedCasting() {
        var feature = FeatureConfigurator.New(
                "ClassesRebornShieldedCastingMythicAbility",
                FutureContentIds.Get("MythicAbility.ShieldedCasting"))
            .SetDisplayName("ClassesReborn.ShieldedCasting.Name")
            .SetDescription("ClassesReborn.ShieldedCasting.Description")
            .SetIcon(BlueprintTool.Get<BlueprintFeature>(
                BlueprintIds.ArcaneArmorTraining).Icon)
            .SetGroups(FeatureGroup.MythicAbility)
            .SetRanks(1)
            .AddFeatureTagsComponent(FeatureTag.Magic | FeatureTag.Defense)
            .AddComponent(new ArcaneSpellFailureModify {
                ArmorMultiplier = 1f,
                ArmorAddition = 0f,
                ShieldMultiplier = 0f,
                ShieldAddition = 0f,
            })
            .AddComponent(new ShieldedCastingComponent())
            .Configure();
        AddToSelection(BlueprintIds.MythicAbilitySelection, feature);
    }
}
