using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UI.GenericSlot;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

namespace ClassesReborn;

internal static partial class TabletopSpellRebalance {
    private static readonly (string SpellList, int Level)[]
        WeaponOfAweSpellLists = {
            (BlueprintIds.ClericSpellList, 2),
            (BlueprintIds.InquisitorSpellList, 2),
            (BlueprintIds.PaladinSpellList, 2),
            (BlueprintIds.WarpriestSpellList, 2),
        };

    private static readonly (string SpellList, int Level)[]
        SanctifyArmorSpellLists = {
            (BlueprintIds.PaladinSpellList, 3),
            (BlueprintIds.InquisitorSpellList, 4),
        };

    private static readonly (string SpellList, int Level)[]
        ForcefulStrikeSpellLists = {
            (BlueprintIds.ClericSpellList, 4),
            (BlueprintIds.InquisitorSpellList, 4),
            (BlueprintIds.PaladinSpellList, 4),
            (BlueprintIds.WarpriestSpellList, 4),
        };

    private static readonly (string SpellList, int Level)[]
        WrathfulWeaponSpellLists = {
            (BlueprintIds.ClericSpellList, 4),
            (BlueprintIds.WarpriestSpellList, 4),
        };

    private static void ConfigureWeaponOfAwe() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.BlessWeaponCastAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornWeaponOfAweShakenBuff",
                BlueprintIds.WeaponOfAweShakenBuff)
            .SetDisplayName("ClassesReborn.WeaponOfAwe.Shaken.Name")
            .SetDescription("ClassesReborn.WeaponOfAwe.Shaken.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddCondition(Kingmaker.UnitLogic.UnitCondition.Shaken)
            .AddSpellDescriptorComponent(
                SpellDescriptor.Fear |
                SpellDescriptor.MindAffecting |
                SpellDescriptor.Shaken)
            .Configure();

        ConfigureWeaponOfAweVariant(
            "MainHand",
            BlueprintIds.WeaponOfAweMainHandAbility,
            BlueprintIds.WeaponOfAweMainHandBuff,
            secondaryHand: false,
            icon);
        ConfigureWeaponOfAweVariant(
            "OffHand",
            BlueprintIds.WeaponOfAweOffHandAbility,
            BlueprintIds.WeaponOfAweOffHandBuff,
            secondaryHand: true,
            icon);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornWeaponOfAweAbility",
                BlueprintIds.WeaponOfAweAbility,
                SpellSchool.Transmutation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.WeaponOfAwe.Name")
            .SetDescription("ClassesReborn.WeaponOfAwe.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Quicken,
                Metamagic.Reach)
            .SetLocalizedDuration("ClassesReborn.WeaponOfAwe.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.WeaponOfAwe.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: true, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Emotion)
            .AddAbilityVariants(new() {
                BlueprintIds.WeaponOfAweMainHandAbility,
                BlueprintIds.WeaponOfAweOffHandAbility,
            });

        foreach (var entry in WeaponOfAweSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Transmutation,
            WeaponOfAweSpellLists,
            "Weapon of Awe");
    }

    private static void ConfigureWeaponOfAweVariant(
        string variant,
        string abilityId,
        string buffId,
        bool secondaryHand,
        UnityEngine.Sprite icon) {
        BuffConfigurator.New(
                $"ClassesRebornWeaponOfAwe{variant}Buff",
                buffId)
            .SetDisplayName("ClassesReborn.WeaponOfAwe.Name")
            .SetDescription("ClassesReborn.WeaponOfAwe.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new WeaponOfAweBonuses {
                SecondaryHand = secondaryHand,
                m_ShakenBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.WeaponOfAweShakenBuff),
            })
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            buffId,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        AbilityConfigurator.NewSpell(
                $"ClassesRebornWeaponOfAwe{variant}Ability",
                abilityId,
                SpellSchool.Transmutation,
                canSpecialize: false)
            .SetDisplayName($"ClassesReborn.WeaponOfAwe.{variant}.Name")
            .SetDescription("ClassesReborn.WeaponOfAwe.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Quicken,
                Metamagic.Reach)
            .SetLocalizedDuration("ClassesReborn.WeaponOfAwe.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.WeaponOfAwe.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: true, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Emotion)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddComponent(new WeaponSpellTargetRestriction {
                SecondaryHand = secondaryHand,
                AllowUnarmed = true,
            })
            .AddAbilityEffectRunAction(applyBuff)
            .Configure();
    }

    private static void ConfigureSanctifyArmor() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.MagicalVestmentArmorAbility).Icon;
        var enhancementEnchantments = new[] {
            BlueprintIds.ArmorEnhancement1,
            BlueprintIds.ArmorEnhancement2,
            BlueprintIds.ArmorEnhancement3,
            BlueprintIds.ArmorEnhancement4,
            BlueprintIds.ArmorEnhancement5,
        }.Select(BlueprintTool.GetRef<BlueprintItemEnchantmentReference>)
            .ToArray();

        BuffConfigurator.New(
                "ClassesRebornSanctifyArmorBuff",
                BlueprintIds.SanctifyArmorBuff)
            .SetDisplayName("ClassesReborn.SanctifyArmor.Name")
            .SetDescription("ClassesReborn.SanctifyArmor.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BuffEnchantArmor {
                m_Enchantments = enhancementEnchantments,
                m_Scaling = new BuffScaling {
                    TypeOfScaling = BuffScaling.ScalingType.ByCasterLevel,
                    Modifier = 4,
                    StartingMod = 0,
                    Minimum = 1,
                },
                m_ItemType = BuffEnchantArmor.ItemType.Armor,
            })
            .AddComponent(new SanctifyArmorBonuses {
                m_JudgmentWatcher = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.JudgmentWatcherBuff),
                m_SmiteBuffs = new[] {
                    BlueprintIds.SmiteEvilBuff,
                    BlueprintIds.SmiteEvilNoScabbardBuff,
                    BlueprintIds.SmiteEvilScabbardBuff,
                    BlueprintIds.AuraOfJusticeSmiteBuff,
                    BlueprintIds.AllIsDarknessSmiteBuff,
                }.Select(BlueprintTool.GetRef<BlueprintBuffReference>).ToArray(),
            })
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.SanctifyArmorBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornSanctifyArmorAbility",
                BlueprintIds.SanctifyArmorAbility,
                SpellSchool.Abjuration,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.SanctifyArmor.Name")
            .SetDescription("ClassesReborn.SanctifyArmor.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Quicken,
                Metamagic.Reach)
            .SetLocalizedDuration("ClassesReborn.SanctifyArmor.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.SanctifyArmor.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: true, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Good)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff);

        foreach (var entry in SanctifyArmorSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Abjuration,
            SanctifyArmorSpellLists,
            "Sanctify Armor");
    }

    private static void ConfigureForcefulStrike() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.ForcePunchCastAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornForcefulStrikeHandlerBuff",
                BlueprintIds.ForcefulStrikeHandlerBuff)
            .SetDisplayName("ClassesReborn.ForcefulStrike.Name")
            .SetDescription("ClassesReborn.ForcefulStrike.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddComponent(new ForcefulStrikeHandler {
                m_Ability = BlueprintTool.GetRef<BlueprintAbilityReference>(
                    BlueprintIds.ForcefulStrikeAbility),
            })
            .Configure();

        var attack = ActionsBuilder.New()
            .ApplyBuff(
                BlueprintIds.ForcefulStrikeHandlerBuff,
                ContextDuration.Fixed(1, DurationRate.Rounds),
                isFromSpell: true,
                toCaster: true)
            .Add<ContextActionMeleeAttack>(action => {
                action.ExtraAttack = true;
                action.ForceStartAnimation = true;
            });

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornForcefulStrikeAbility",
                BlueprintIds.ForcefulStrikeAbility,
                SpellSchool.Evocation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.ForcefulStrike.Name")
            .SetDescription("ClassesReborn.ForcefulStrike.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Weapon)
            .SetActionType(UnitCommand.CommandType.Swift)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(true)
            .SetAvailableMetamagic(
                Metamagic.Empower,
                Metamagic.Maximize,
                Metamagic.Heighten)
            .SetLocalizedDuration("ClassesReborn.ForcefulStrike.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.ForcefulStrike.SavingThrow")
            .AllowTargeting(point: false, enemies: true, friends: false, self: false)
            .AddSpellDescriptorComponent(SpellDescriptor.Force)
            .AddAbilityCasterMainWeaponIsMelee()
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(attack);

        foreach (var entry in ForcefulStrikeSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Evocation,
            ForcefulStrikeSpellLists,
            "Forceful Strike");
    }

    private static void ConfigureWrathfulWeapon() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.BlessWeaponCastAbility).Icon;
        var variants = new List<Blueprint<BlueprintAbilityReference>>();

        ConfigureWrathfulWeaponPair(
            "Anarchic",
            BlueprintIds.AnarchicWeaponEnchantment,
            SpellDescriptor.Chaos,
            BlueprintIds.WrathfulWeaponAnarchicMainAbility,
            BlueprintIds.WrathfulWeaponAnarchicOffAbility,
            BlueprintIds.WrathfulWeaponAnarchicMainBuff,
            BlueprintIds.WrathfulWeaponAnarchicOffBuff,
            icon,
            variants);
        ConfigureWrathfulWeaponPair(
            "Axiomatic",
            BlueprintIds.AxiomaticWeaponEnchantment,
            SpellDescriptor.Law,
            BlueprintIds.WrathfulWeaponAxiomaticMainAbility,
            BlueprintIds.WrathfulWeaponAxiomaticOffAbility,
            BlueprintIds.WrathfulWeaponAxiomaticMainBuff,
            BlueprintIds.WrathfulWeaponAxiomaticOffBuff,
            icon,
            variants);
        ConfigureWrathfulWeaponPair(
            "Holy",
            BlueprintIds.HolyWeaponEnchantment,
            SpellDescriptor.Good,
            BlueprintIds.WrathfulWeaponHolyMainAbility,
            BlueprintIds.WrathfulWeaponHolyOffAbility,
            BlueprintIds.WrathfulWeaponHolyMainBuff,
            BlueprintIds.WrathfulWeaponHolyOffBuff,
            icon,
            variants);
        ConfigureWrathfulWeaponPair(
            "Unholy",
            BlueprintIds.UnholyWeaponEnchantment,
            SpellDescriptor.Evil,
            BlueprintIds.WrathfulWeaponUnholyMainAbility,
            BlueprintIds.WrathfulWeaponUnholyOffAbility,
            BlueprintIds.WrathfulWeaponUnholyMainBuff,
            BlueprintIds.WrathfulWeaponUnholyOffBuff,
            icon,
            variants);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornWrathfulWeaponAbility",
                BlueprintIds.WrathfulWeaponAbility,
                SpellSchool.Transmutation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.WrathfulWeapon.Name")
            .SetDescription("ClassesReborn.WrathfulWeapon.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Quicken,
                Metamagic.Reach)
            .SetLocalizedDuration("ClassesReborn.WrathfulWeapon.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.WrathfulWeapon.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: true, self: true)
            .AddAbilityVariants(variants);

        foreach (var entry in WrathfulWeaponSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Transmutation,
            WrathfulWeaponSpellLists,
            "Wrathful Weapon");
    }

    private static void ConfigureWrathfulWeaponPair(
        string property,
        string enchantmentId,
        SpellDescriptor descriptor,
        string mainAbilityId,
        string offAbilityId,
        string mainBuffId,
        string offBuffId,
        UnityEngine.Sprite icon,
        ICollection<Blueprint<BlueprintAbilityReference>> variants) {
        ConfigureWrathfulWeaponVariant(
            property,
            "MainHand",
            enchantmentId,
            descriptor,
            mainAbilityId,
            mainBuffId,
            secondaryHand: false,
            icon);
        ConfigureWrathfulWeaponVariant(
            property,
            "OffHand",
            enchantmentId,
            descriptor,
            offAbilityId,
            offBuffId,
            secondaryHand: true,
            icon);
        variants.Add(mainAbilityId);
        variants.Add(offAbilityId);
    }

    private static void ConfigureWrathfulWeaponVariant(
        string property,
        string hand,
        string enchantmentId,
        SpellDescriptor descriptor,
        string abilityId,
        string buffId,
        bool secondaryHand,
        UnityEngine.Sprite icon) {
        var slot = secondaryHand
            ? EquipSlotBase.SlotType.SecondaryHand
            : EquipSlotBase.SlotType.PrimaryHand;
        var enchantment = BlueprintTool.GetRef<BlueprintItemEnchantmentReference>(
            enchantmentId);

        BuffConfigurator.New(
                $"ClassesRebornWrathfulWeapon{property}{hand}Buff",
                buffId)
            .SetDisplayName($"ClassesReborn.WrathfulWeapon.{property}.Name")
            .SetDescription("ClassesReborn.WrathfulWeapon.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BuffEnchantAnyWeapon {
                m_EnchantmentBlueprint = enchantment,
                Slot = slot,
            })
            .Configure();

        var applyBuff = ActionsBuilder.New()
            .Add<ContextActionApplyWrathfulWeaponBuff>(action => {
                action.m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(buffId);
                action.m_WarpriestClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.WarpriestClass);
            });

        AbilityConfigurator.NewSpell(
                $"ClassesRebornWrathfulWeapon{property}{hand}Ability",
                abilityId,
                SpellSchool.Transmutation,
                canSpecialize: false)
            .SetDisplayName($"ClassesReborn.WrathfulWeapon.{property}.{hand}.Name")
            .SetDescription("ClassesReborn.WrathfulWeapon.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Touch)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(
                Metamagic.Extend,
                Metamagic.Quicken,
                Metamagic.Reach)
            .SetLocalizedDuration("ClassesReborn.WrathfulWeapon.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.WrathfulWeapon.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: true, self: true)
            .AddSpellDescriptorComponent(descriptor)
            .AddComponent(new WeaponSpellTargetRestriction {
                SecondaryHand = secondaryHand,
                MeleeOnly = true,
                AllowUnarmed = true,
                UnarmedRequiresWarpriest = true,
                m_WarpriestClass = BlueprintTool.GetRef<BlueprintCharacterClassReference>(
                    BlueprintIds.WarpriestClass),
                m_ExcludedEnchantment = enchantment,
            })
            .AddAbilityEffectRunAction(applyBuff)
            .Configure();
    }
}
