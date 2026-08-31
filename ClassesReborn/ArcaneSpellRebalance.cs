using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

namespace ClassesReborn;

internal static class ArcaneSpellRebalance {
    private static readonly string[] LongArmSpellLists = {
        BlueprintIds.AlchemistSpellList,
        BlueprintIds.BardSpellList,
        BlueprintIds.BloodragerSpellList,
        BlueprintIds.MagusSpellList,
        BlueprintIds.WitchSpellList,
        BlueprintIds.WizardSpellList,
        BlueprintIds.MagicDeceiverSpellList,
        BlueprintIds.NatureMageLongArmSpellList,
        BlueprintIds.WizardTransmutationSpellList,
        BlueprintIds.ThassilonianTransmutationSpellList,
    };

    internal static void Configure() {
        ConfigureNatureMageSpellList();

        var icon = BlueprintTool.Get<BlueprintAbility>(
            BlueprintIds.EnlargePersonAbility).Icon;

        BuffConfigurator.New(
                "ClassesRebornLongArmBuff",
                BlueprintIds.LongArmBuff)
            .SetDisplayName("ClassesReborn.LongArm.Name")
            .SetDescription("ClassesReborn.LongArm.Description")
            .SetIcon(icon)
            .SetFlags(BlueprintBuff.Flags.IsFromSpell)
            .SetStacking(StackingType.Replace)
            .AddReachMultiplicator(
                descriptor: ModifierDescriptor.UntypedStackable,
                multiplicator: 2)
            .Configure();

        var applyBuff = ActionsBuilder.New().ApplyBuff(
            BlueprintIds.LongArmBuff,
            ContextDuration.Variable(
                ContextValues.Rank(),
                DurationRate.Minutes,
                isExtendable: true),
            isFromSpell: true);

        var configurator = AbilityConfigurator.NewSpell(
                "ClassesRebornLongArmAbility",
                BlueprintIds.LongArmAbility,
                SpellSchool.Transmutation,
                canSpecialize: true)
            .SetDisplayName("ClassesReborn.LongArm.Name")
            .SetDescription("ClassesReborn.LongArm.Description")
            .SetIcon(icon)
            .SetRange(AbilityRange.Personal)
            .SetActionType(UnitCommand.CommandType.Standard)
            .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Touch)
            .SetSpellResistance(false)
            .SetAvailableMetamagic(Metamagic.Extend, Metamagic.Quicken)
            .SetLocalizedDuration("ClassesReborn.LongArm.Duration")
            .SetLocalizedSavingThrow("ClassesReborn.LongArm.SavingThrow")
            .AllowTargeting(
                point: false,
                enemies: false,
                friends: false,
                self: true)
            .AddContextRankConfig(ContextRankConfigs.CasterLevel())
            .AddAbilityEffectRunAction(applyBuff);

        foreach (var spellListId in LongArmSpellLists) {
            configurator.AddToSpellList(1, spellListId);
        }

        var ability = configurator.Configure();
        Validate(ability);
    }

    private static void ConfigureNatureMageSpellList() {
        var druidSpellList = BlueprintTool.Get<BlueprintSpellList>(
            BlueprintIds.DruidSpellList);
        var copiedLevels = druidSpellList.SpellsByLevel
            .Select(level => new SpellLevelList(level.SpellLevel) {
                m_Spells = level.SpellsRefs.ToList(),
            })
            .ToArray();

        var natureMageSpellList = SpellListConfigurator.New(
                "ClassesRebornNatureMageLongArmSpellList",
                BlueprintIds.NatureMageLongArmSpellList)
            .Configure();
        natureMageSpellList.IsMythic = druidSpellList.IsMythic;
        natureMageSpellList.SpellsByLevel = copiedLevels;
        natureMageSpellList.m_FilteredList = druidSpellList.m_FilteredList;
        natureMageSpellList.FilterByMaxLevel = druidSpellList.FilterByMaxLevel;
        natureMageSpellList.FilterByDescriptor = druidSpellList.FilterByDescriptor;
        natureMageSpellList.Descriptor = druidSpellList.Descriptor;
        natureMageSpellList.FilterBySchool = druidSpellList.FilterBySchool;
        natureMageSpellList.ExcludeFilterSchool = druidSpellList.ExcludeFilterSchool;
        natureMageSpellList.FilterSchool = druidSpellList.FilterSchool;
        natureMageSpellList.FilterSchool2 = druidSpellList.FilterSchool2;
        natureMageSpellList.m_MaxLevel = druidSpellList.m_MaxLevel;
        natureMageSpellList.Comment = druidSpellList.Comment;

        SpellbookConfigurator.For(BlueprintIds.NatureMageSpellbook)
            .SetSpellList(BlueprintIds.NatureMageLongArmSpellList)
            .Configure();
    }

    private static void Validate(BlueprintAbility ability) {
        if (ability.GetComponent<SpellComponent>()?.School !=
                SpellSchool.Transmutation) {
            throw new InvalidOperationException(
                "Long Arm must be a Transmutation spell.");
        }

        foreach (var spellListId in LongArmSpellLists) {
            var spellList = BlueprintTool.Get<BlueprintSpellList>(spellListId);
            var levelOneCount = spellList.SpellsByLevel[1].Spells.Count(
                spell => spell == ability);
            var totalCount = spellList.SpellsByLevel.Sum(level =>
                level.Spells.Count(spell => spell == ability));
            if (levelOneCount != 1 || totalCount != 1) {
                throw new InvalidOperationException(
                    $"Long Arm must appear exactly once and only at level 1 on {spellList.name}.");
            }
        }

        var natureMageSpellbook = BlueprintTool.Get<BlueprintSpellbook>(
            BlueprintIds.NatureMageSpellbook);
        var natureMageSpellList = BlueprintTool.Get<BlueprintSpellList>(
            BlueprintIds.NatureMageLongArmSpellList);
        if (natureMageSpellbook.SpellList != natureMageSpellList) {
            throw new InvalidOperationException(
                "Nature Mage must use its isolated Druid-derived spell list containing Long Arm.");
        }
    }
}
