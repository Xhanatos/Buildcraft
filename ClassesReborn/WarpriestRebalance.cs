using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace ClassesReborn;

internal static class WarpriestRebalance {
    private const string SacredArmorDescription =
        "ClassesReborn.Shieldbearer.SacredArmor.Description";
    private const string FervorDescription =
        "ClassesReborn.Warpriest.Fervor.Description";
    private const string EnthrallDescription =
        "ClassesReborn.Warpriest.CultLeader.Enthrall.Description";
    private const string ChampionOfTheFaithSmiteDescription =
        "ClassesReborn.Warpriest.ChampionOfTheFaith.Smite.Description";

    internal static void Configure() {
        ConfigureFervorUses();
        ConfigureCultLeaderEnthrall();
        ConfigureChampionOfTheFaithSmite();
        ConfigureShieldbearerSacredArmor();
    }

    private static void ConfigureFervorUses() {
        var amount = new ResourceAmountBuilder()
            .IncreaseByLevel(new[] { BlueprintIds.WarpriestClass }, 1)
            .IncreaseByStat(StatType.Wisdom);
        var resource = AbilityResourceConfigurator.For(
                BlueprintIds.WarpriestFervorResource)
            .SetMaxAmount(amount)
            .Configure();
        resource.m_MaxAmount.BaseValue = 0;

        foreach (var featureId in
                 BlueprintIds.WarpriestFervorDescriptionFeatures) {
            FeatureConfigurator.For(featureId)
                .SetDescription(FervorDescription)
                .Configure();
        }

        foreach (var abilityId in
                 BlueprintIds.WarpriestFervorDescriptionAbilities) {
            AbilityConfigurator.For(abilityId)
                .SetDescription(FervorDescription)
                .Configure();
        }

        var warpriest = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.WarpriestClass);
        if (resource.m_MaxAmount.BaseValue != 0 ||
            !resource.m_MaxAmount.IncreasedByLevel ||
            resource.m_MaxAmount.LevelIncrease != 1 ||
            resource.m_MaxAmount.m_Class?.Length != 1 ||
            resource.m_MaxAmount.m_Class[0]?.Get() != warpriest ||
            resource.m_MaxAmount.IncreasedByLevelStartPlusDivStep ||
            !resource.m_MaxAmount.IncreasedByStat ||
            resource.m_MaxAmount.ResourceBonusStat != StatType.Wisdom) {
            throw new InvalidOperationException(
                "Fervor must grant uses equal to Warpriest level plus Wisdom modifier.");
        }
    }

    private static void ConfigureShieldbearerSacredArmor() {
        FeatureConfigurator.For(BlueprintIds.ShieldbearerSacredArmorFeature)
            .SetDescription(SacredArmorDescription)
            .Configure();

        foreach (var featureId in
                 BlueprintIds.ShieldbearerSacredArmorUpgradeFeatures) {
            FeatureConfigurator.For(featureId)
                .SetDescription(SacredArmorDescription)
                .Configure();
        }

        ActivatableAbilityConfigurator
            .For(BlueprintIds.ShieldbearerSacredArmorOnAbility)
            .SetDescription(SacredArmorDescription)
            .Configure();
        AbilityConfigurator.For(
                BlueprintIds.ShieldbearerSacredArmorSwitchAbility)
            .SetDescription(SacredArmorDescription)
            .Configure();

        var onBuff = BuffConfigurator.For(
                BlueprintIds.ShieldbearerSacredArmorOnBuff)
            .SetDescription(SacredArmorDescription)
            .AddComponent(new SacredArmorShieldBashEnhancement {
                m_Enhancements = BlueprintIds
                    .ShieldbearerSacredArmorEnhancements
                    .Select(BlueprintTool.GetRef<
                        BlueprintItemEnchantmentReference>)
                    .ToArray(),
            })
            .Configure();

        var component = onBuff
            .GetComponents<SacredArmorShieldBashEnhancement>()
            .SingleOrDefault();
        if (component?.m_Enhancements.Length != 5 ||
            component.m_Enhancements
                .Select(reference => reference?.Get())
                .Where(enchantment => enchantment != null)
                .Distinct()
                .Count() != 5) {
            throw new InvalidOperationException(
                "Shieldbearer Sacred Armor must map its five numeric enhancement ranks to shield-bash attack and damage bonuses.");
        }
    }

    private static void ConfigureCultLeaderEnthrall() {
        FeatureConfigurator.For(BlueprintIds.CultLeaderEnthrallFeature)
            .SetDescription(EnthrallDescription)
            .Configure();

        var ability = AbilityConfigurator.For(
                BlueprintIds.CultLeaderEnthrallAbility)
            .SetDescription(EnthrallDescription)
            .SetActionType(UnitCommand.CommandType.Move)
            .Configure();

        if (ability.ActionType != UnitCommand.CommandType.Move) {
            throw new InvalidOperationException(
                "Cult Leader Enthrall must use a move action.");
        }
    }

    private static void ConfigureChampionOfTheFaithSmite() {
        FeatureConfigurator.For(BlueprintIds.ChampionOfTheFaithSmiteFeature)
            .SetDescription(ChampionOfTheFaithSmiteDescription)
            .Configure();

        var ability = AbilityConfigurator.For(
                BlueprintIds.ChampionOfTheFaithSmiteAbility)
            .SetDescription(ChampionOfTheFaithSmiteDescription)
            .Configure();
        var rankConfigs = ability.GetComponents<ContextRankConfig>().ToArray();
        var statRanks = rankConfigs
            .Where(component =>
                component.m_Type == AbilityRankType.Default &&
                component.m_BaseValueType ==
                    ContextRankBaseValueType.StatBonus)
            .ToArray();
        var damageRanks = rankConfigs
            .Where(component => component.m_Type == AbilityRankType.DamageBonus)
            .ToArray();

        if (statRanks.Length != 1 || damageRanks.Length != 1) {
            throw new InvalidOperationException(
                "Champion of the Faith Smite must have one ability-score rank and one damage rank.");
        }

        statRanks[0].m_Stat = StatType.Wisdom;

        var warpriest = BlueprintTool.Get<BlueprintCharacterClass>(
            BlueprintIds.WarpriestClass);
        if (!statRanks[0].m_UseMin ||
            statRanks[0].m_Min != 0 ||
            damageRanks[0].m_BaseValueType !=
                ContextRankBaseValueType.ClassLevel ||
            damageRanks[0].m_Class?.Length != 1 ||
            damageRanks[0].m_Class[0]?.Get() != warpriest) {
            throw new InvalidOperationException(
                "Champion of the Faith Smite must use nonnegative Wisdom for attack and AC while retaining Warpriest-level damage.");
        }
    }
}

[AllowedOn(typeof(BlueprintBuff))]
[TypeId("70fd120a-2c20-4977-81aa-12f70b3a9e24")]
public sealed class SacredArmorShieldBashEnhancement :
    UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
    IInitiatorRulebookHandler<RuleCalculateWeaponStats> {
    public BlueprintItemEnchantmentReference[] m_Enhancements =
        Array.Empty<BlueprintItemEnchantmentReference>();

    public void OnEventAboutToTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) {
        var bonus = GetSacredArmorEnhancement(evt.Weapon);
        if (bonus > 0) {
            evt.AddModifier(
                bonus,
                Fact,
                ModifierDescriptor.Enhancement);
        }
    }

    public void OnEventDidTrigger(
        RuleCalculateAttackBonusWithoutTarget evt) { }

    public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
        var bonus = GetSacredArmorEnhancement(evt.Weapon);
        if (bonus > 0) {
            evt.AddDamageModifier(
                bonus,
                Fact,
                ModifierDescriptor.Enhancement);
        }
    }

    public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

    private int GetSacredArmorEnhancement(ItemEntityWeapon weapon) {
        if (weapon?.IsShield != true || weapon.Shield?.ArmorComponent == null) {
            return 0;
        }

        var armor = weapon.Shield.ArmorComponent;
        for (var index = m_Enhancements.Length - 1; index >= 0; index--) {
            var enchantment = m_Enhancements[index]?.Get();
            if (enchantment != null && armor.HasEnchantment(enchantment)) {
                return index + 1;
            }
        }

        return 0;
    }
}
