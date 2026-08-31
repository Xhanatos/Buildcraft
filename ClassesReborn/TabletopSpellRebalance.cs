using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

namespace ClassesReborn;

internal static partial class TabletopSpellRebalance {
    private static readonly (string SpellList, int Level)[]
        BlisteringInvectiveSpellLists = {
            (BlueprintIds.AlchemistSpellList, 2),
            (BlueprintIds.BardSpellList, 2),
            (BlueprintIds.InquisitorSpellList, 2),
        };

    private static readonly (string SpellList, int Level)[]
        BurstOfRadianceSpellLists = {
            (BlueprintIds.ClericSpellList, 2),
            (BlueprintIds.DruidSpellList, 2),
            (BlueprintIds.WizardSpellList, 2),
        };

    private static readonly (string SpellList, int Level)[]
        BladeTutorsSpiritSpellLists = {
            (BlueprintIds.MagusSpellList, 1),
            (BlueprintIds.PaladinSpellList, 2),
            (BlueprintIds.WizardSpellList, 2),
        };

    private static readonly (string SpellList, int Level)[]
        DeadlyJuggernautSpellLists = {
            (BlueprintIds.ClericSpellList, 3),
            (BlueprintIds.InquisitorSpellList, 3),
            (BlueprintIds.PaladinSpellList, 3),
        };

    private static readonly (string SpellList, int Level)[]
        ShillelaghSpellLists = {
            (BlueprintIds.DruidSpellList, 1),
            (BlueprintIds.HunterSpellList, 1),
            (BlueprintIds.RangerSpellList, 1),
            (BlueprintIds.ShamanSpellList, 1),
        };

    internal static void Configure() {
        if (Main.Settings.BlisteringInvective) {
            ConfigureBlisteringInvective();
        }
        if (Main.Settings.ArcaneConcordance) {
            ConfigureArcaneConcordance();
        }
        if (Main.Settings.BurstOfRadiance) {
            ConfigureBurstOfRadiance();
        }
        if (Main.Settings.BladeTutorsSpirit) {
            ConfigureBladeTutorsSpirit();
        }
        if (Main.Settings.DeadlyJuggernaut) {
            ConfigureDeadlyJuggernaut();
        }
        if (Main.Settings.Shillelagh) {
            ConfigureShillelagh();
        }
        if (Main.Settings.WeaponOfAwe) {
            ConfigureWeaponOfAwe();
        }
        if (Main.Settings.SanctifyArmor) {
            ConfigureSanctifyArmor();
        }
        if (Main.Settings.ForcefulStrike) {
            ConfigureForcefulStrike();
        }
        if (Main.Settings.WrathfulWeapon) {
            ConfigureWrathfulWeapon();
        }
    }

    private static void ConfigureBlisteringInvective() {
        var icon = BlueprintTool.Get<BlueprintFeature>(
            BlueprintIds.FlameDanceFeature).Icon;

        BuffConfigurator.New(
                "ClassesRebornBlisteringInvectiveCatchFireBuff",
                BlueprintIds.BlisteringInvectiveCatchFireBuff)
            .SetDisplayName("ClassesReborn.BlisteringInvective.CatchFire.Name")
            .SetDescription("ClassesReborn.BlisteringInvective.CatchFire.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BlisteringInvectiveCatchFire())
            .Configure();

        BuffConfigurator.New(
                "ClassesRebornBlisteringInvectiveHandlerBuff",
                BlueprintIds.BlisteringInvectiveHandlerBuff)
            .SetDisplayName("ClassesReborn.BlisteringInvective.Name")
            .SetDescription("ClassesReborn.BlisteringInvective.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BlisteringInvectiveDemoralizeHandler {
                m_Ability = BlueprintTool.GetRef<BlueprintAbilityReference>(
                    BlueprintIds.BlisteringInvectiveAbility),
                m_CatchFireBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                    BlueprintIds.BlisteringInvectiveCatchFireBuff),
            })
            .Configure();

        var armHandler = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.BlisteringInvectiveHandlerBuff,
            ContextDuration.Fixed(1, DurationRate.Rounds),
            isFromSpell: true,
            toCaster: true);
        var persuasion = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.PersuasionUseAbility);
        var templateAction = persuasion
            .GetComponent<AbilityEffectRunAction>()?
            .Actions.Actions
            .OfType<Demoralize>()
            .SingleOrDefault() ?? throw new InvalidOperationException(
                "The native Persuasion ability must contain one Demoralize action.");
        var demoralize = ActionsBuilder.New().Add<Demoralize>(action => {
            action.m_Buff = templateAction.m_Buff;
            action.m_GreaterBuff = templateAction.m_GreaterBuff;
            action.DazzlingDisplay = templateAction.DazzlingDisplay;
            action.m_SwordlordProwessFeature =
                templateAction.m_SwordlordProwessFeature;
            action.m_ShatterConfidenceFeature =
                templateAction.m_ShatterConfidenceFeature;
            action.m_ShatterConfidenceBuff =
                templateAction.m_ShatterConfidenceBuff;
            action.Bonus = templateAction.Bonus;
            action.TricksterRank3Actions = templateAction.TricksterRank3Actions;
        });

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornBlisteringInvectiveAbility",
                BlueprintIds.BlisteringInvectiveAbility,
                SpellSchool.Evocation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.BlisteringInvective.Name")
            .SetDescription("ClassesReborn.BlisteringInvective.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Heighten, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.BlisteringInvective.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.BlisteringInvective.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Fire)
            .AddAbilityAoERadius(
                radius: new Feet(30),
                targetType: TargetType.Enemy)
            .AddAbilityTargetsAround(
                radius: new Feet(30),
                targetType: TargetType.Enemy)
            .AddAbilityExecuteActionOnCast(armHandler)
            .AddAbilityEffectRunAction(demoralize);

        foreach (var entry in BlisteringInvectiveSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Evocation,
            BlisteringInvectiveSpellLists,
            "Blistering Invective");
    }

    private static void ConfigureArcaneConcordance() {
        var icon = BlueprintTool.Get<BlueprintAbility>(BlueprintIds.HasteAbility).Icon;

        ConfigureArcaneConcordanceVariant(
            "Extend",
            BlueprintIds.ArcaneConcordanceExtendAbility,
            BlueprintIds.ArcaneConcordanceExtendSourceBuff,
            BlueprintIds.ArcaneConcordanceExtendArea,
            BlueprintIds.ArcaneConcordanceExtendEffectBuff,
            Metamagic.Extend,
            icon);
        ConfigureArcaneConcordanceVariant(
            "Reach",
            BlueprintIds.ArcaneConcordanceReachAbility,
            BlueprintIds.ArcaneConcordanceReachSourceBuff,
            BlueprintIds.ArcaneConcordanceReachArea,
            BlueprintIds.ArcaneConcordanceReachEffectBuff,
            Metamagic.Reach,
            icon);

        var ability = AbilityConfigurator.NewSpell(
                "ClassesRebornArcaneConcordanceAbility",
                BlueprintIds.ArcaneConcordanceAbility,
                SpellSchool.Evocation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.ArcaneConcordance.Name")
            .SetDescription("ClassesReborn.ArcaneConcordance.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.ArcaneConcordance.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.ArcaneConcordance.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddAbilityVariants(new() {
                BlueprintIds.ArcaneConcordanceExtendAbility,
                BlueprintIds.ArcaneConcordanceReachAbility,
            })
            .AddToSpellList(3, BlueprintIds.BardSpellList)
            .Configure();

        ValidateSpell(
            ability,
            SpellSchool.Evocation,
            new[] { (BlueprintIds.BardSpellList, 3) },
            "Arcane Concordance");
    }

    private static void ConfigureArcaneConcordanceVariant(
        string variant,
        string abilityId,
        string sourceBuffId,
        string areaId,
        string effectBuffId,
        Metamagic metamagic,
        UnityEngine.Sprite icon) {
        BuffConfigurator.New(
                $"ClassesRebornArcaneConcordance{variant}EffectBuff",
                effectBuffId)
            .SetDisplayName($"ClassesReborn.ArcaneConcordance.{variant}.Name")
            .SetDescription("ClassesReborn.ArcaneConcordance.EffectDescription")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.HiddenInUi)
            .SetStacking(StackingType.Replace)
            .AddComponent(new ArcaneConcordanceMetamagic {
                GrantedMetamagic = metamagic,
            })
            .Configure();

        AbilityAreaEffectConfigurator.New(
                $"ClassesRebornArcaneConcordance{variant}Area",
                areaId)
            .SetTargetType(BlueprintAbilityAreaEffect.TargetType.Any)
            .SetShape(AreaEffectShape.Cylinder)
            .SetSize(new Feet(10))
            .SetAffectEnemies(true)
            .SetAggroEnemies(false)
            .AddAbilityAreaEffectBuff(effectBuffId)
            .Configure();

        BuffConfigurator.New(
                $"ClassesRebornArcaneConcordance{variant}SourceBuff",
                sourceBuffId)
            .SetDisplayName($"ClassesReborn.ArcaneConcordance.{variant}.Name")
            .SetDescription("ClassesReborn.ArcaneConcordance.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddAreaEffect(areaId)
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            sourceBuffId,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Rounds,
                isExtendable: true),
            isFromSpell: true);

        AbilityConfigurator.NewSpell(
                $"ClassesRebornArcaneConcordance{variant}Ability",
                abilityId,
                SpellSchool.Evocation,
                canSpecialize: true)
            .SetDisplayName($"ClassesReborn.ArcaneConcordance.{variant}.Name")
            .SetDescription("ClassesReborn.ArcaneConcordance.Description")
            .SetIcon(icon)
            .SetParent(BlueprintIds.ArcaneConcordanceAbility)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.ArcaneConcordance.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.ArcaneConcordance.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff)
            .Configure();
    }

    private static void ConfigureBurstOfRadiance() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.HasteAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornBurstOfRadianceBlindBuff",
                BlueprintIds.BurstOfRadianceBlindBuff)
            .SetDisplayName("ClassesReborn.BurstOfRadiance.Blind.Name")
            .SetDescription("ClassesReborn.BurstOfRadiance.Blind.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddCondition(UnitCondition.Blindness)
            .Configure();

        BuffConfigurator.New(
                "ClassesRebornBurstOfRadianceDazzledBuff",
                BlueprintIds.BurstOfRadianceDazzledBuff)
            .SetDisplayName("ClassesReborn.BurstOfRadiance.Dazzled.Name")
            .SetDescription("ClassesReborn.BurstOfRadiance.Dazzled.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddCondition(UnitCondition.Dazzled)
            .Configure();

        var actions = ActionsBuilder.New().Add<ContextActionBurstOfRadiance>(action => {
            action.m_BlindBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                BlueprintIds.BurstOfRadianceBlindBuff);
            action.m_DazzledBuff = BlueprintTool.GetRef<BlueprintBuffReference>(
                BlueprintIds.BurstOfRadianceDazzledBuff);
        });

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornBurstOfRadianceAbility",
                BlueprintIds.BurstOfRadianceAbility,
                SpellSchool.Evocation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.BurstOfRadiance.Name")
            .SetDescription("ClassesReborn.BurstOfRadiance.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Long)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(true)
            .SetAvailableMetamagic(
                Metamagic.Empower,
                Metamagic.Maximize,
                Metamagic.Heighten,
                Metamagic.Reach,
                Metamagic.Quicken,
                Metamagic.Selective)
            .SetLocalizedDuration("ClassesReborn.BurstOfRadiance.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.BurstOfRadiance.SavingThrow")
            .AllowTargeting(point: true, enemies: true, friends: true, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Good)
            .AddAbilityAoERadius(
                radius: new Feet(10),
                targetType: TargetType.Any)
            .AddAbilityTargetsAround(
                radius: new Feet(10),
                targetType: TargetType.Any)
            .AddAbilityEffectRunAction(actions);

        foreach (var entry in BurstOfRadianceSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Evocation,
            BurstOfRadianceSpellLists,
            "Burst of Radiance");
    }

    private static void ConfigureBladeTutorsSpirit() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.TrueStrikeAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornBladeTutorsSpiritBuff",
                BlueprintIds.BladeTutorsSpiritBuff)
            .SetDisplayName("ClassesReborn.BladeTutorsSpirit.Name")
            .SetDescription("ClassesReborn.BladeTutorsSpirit.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new BladeTutorsSpiritReduction())
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.BladeTutorsSpiritBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornBladeTutorsSpiritAbility",
                BlueprintIds.BladeTutorsSpiritAbility,
                SpellSchool.Conjuration,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.BladeTutorsSpirit.Name")
            .SetDescription("ClassesReborn.BladeTutorsSpirit.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.BladeTutorsSpirit.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.BladeTutorsSpirit.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff);

        foreach (var entry in BladeTutorsSpiritSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Conjuration,
            BladeTutorsSpiritSpellLists,
            "Blade Tutor's Spirit");
    }

    private static void ConfigureDeadlyJuggernaut() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.DivinePowerAbility).Icon;
        var tierIds = new[] {
            BlueprintIds.DeadlyJuggernautTier1Buff,
            BlueprintIds.DeadlyJuggernautTier2Buff,
            BlueprintIds.DeadlyJuggernautTier3Buff,
            BlueprintIds.DeadlyJuggernautTier4Buff,
            BlueprintIds.DeadlyJuggernautTier5Buff,
        };
        var tierReferences = tierIds
            .Select(id => BlueprintTool.GetRef<BlueprintBuffReference>(id))
            .ToArray();

        for (var tier = 1; tier <= tierIds.Length; tier++) {
            BuffConfigurator.New(
                    $"ClassesRebornDeadlyJuggernautTier{tier}Buff",
                    tierIds[tier - 1])
                .SetDisplayName("ClassesReborn.DeadlyJuggernaut.Name")
                .SetDescription("ClassesReborn.DeadlyJuggernaut.Description")
                .SetIcon(icon)
                .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                .SetStacking(StackingType.Replace)
                .AddComponent(new DeadlyJuggernautBonuses { Bonus = tier })
                .AddComponent(new AddDamageResistancePhysical {
                    Value = ContextValues.Constant(2 * tier),
                })
                .Configure();
        }

        BuffConfigurator.New(
                "ClassesRebornDeadlyJuggernautSourceBuff",
                BlueprintIds.DeadlyJuggernautSourceBuff)
            .SetDisplayName("ClassesReborn.DeadlyJuggernaut.Name")
            .SetDescription("ClassesReborn.DeadlyJuggernaut.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new DeadlyJuggernautKillTrigger {
                m_TierBuffs = tierReferences,
            })
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.DeadlyJuggernautSourceBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornDeadlyJuggernautAbility",
                BlueprintIds.DeadlyJuggernautAbility,
                SpellSchool.Necromancy,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.DeadlyJuggernaut.Name")
            .SetDescription("ClassesReborn.DeadlyJuggernaut.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.DeadlyJuggernaut.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.DeadlyJuggernaut.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddSpellDescriptorComponent(SpellDescriptor.Death)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff);

        foreach (var entry in DeadlyJuggernautSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Necromancy,
            DeadlyJuggernautSpellLists,
            "Deadly Juggernaut");

        var source = BlueprintTool.Get<BlueprintBuff>(
            BlueprintIds.DeadlyJuggernautSourceBuff);
        if (source.GetComponent<DeadlyJuggernautKillTrigger>() == null ||
            tierIds.Any(id =>
                BlueprintTool.Get<BlueprintBuff>(id)
                    .GetComponent<DeadlyJuggernautBonuses>() == null)) {
            throw new InvalidOperationException(
                "Deadly Juggernaut must configure its kill trigger and all five bonus tiers.");
        }
    }

    private static void ConfigureShillelagh() {
        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.LeadBladesAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornShillelaghBuff",
                BlueprintIds.ShillelaghBuff)
            .SetDisplayName("ClassesReborn.Shillelagh.Name")
            .SetDescription("ClassesReborn.Shillelagh.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddComponent(new ShillelaghWeaponBonuses())
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.ShillelaghBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornShillelaghAbility",
                BlueprintIds.ShillelaghAbility,
                SpellSchool.Transmutation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.Shillelagh.Name")
            .SetDescription("ClassesReborn.Shillelagh.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Omni)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.Shillelagh.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.Shillelagh.SavingThrow")
            .AllowTargeting(point: false, enemies: false, friends: false, self: true)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff);

        foreach (var entry in ShillelaghSpellLists) {
            configurator.AddToSpellList(entry.Level, entry.SpellList);
        }

        var ability = configurator.Configure();
        ValidateSpell(
            ability,
            SpellSchool.Transmutation,
            ShillelaghSpellLists,
            "Shillelagh");

        if (BlueprintTool.Get<BlueprintBuff>(BlueprintIds.ShillelaghBuff)
                .GetComponents<ShillelaghWeaponBonuses>().Count() != 1) {
            throw new InvalidOperationException(
                "Shillelagh must configure exactly one conditional weapon bonus component.");
        }
    }

    private static void ValidateSpell(
        BlueprintAbility ability,
        SpellSchool school,
        IEnumerable<(string SpellList, int Level)> entries,
        string name) {
        if (ability.GetComponent<SpellComponent>()?.School != school) {
            throw new InvalidOperationException(
                $"{name} must be a {school} spell.");
        }

        foreach (var entry in entries) {
            var spellList = BlueprintTool.Get<BlueprintSpellList>(entry.SpellList);
            var atLevel = spellList.SpellsByLevel[entry.Level].Spells.Count(
                spell => spell == ability);
            var total = spellList.SpellsByLevel.Sum(level =>
                level.Spells.Count(spell => spell == ability));
            if (atLevel != 1 || total != 1) {
                throw new InvalidOperationException(
                    $"{name} must appear exactly once and only at level {entry.Level} on {spellList.name}.");
            }
        }
    }
}
