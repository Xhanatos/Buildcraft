using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace ClassesReborn;

internal static class BloodlineClawRebalance {
    private const string AbyssalDescription =
        "ClassesReborn.BloodlineClaws.Abyssal.Unlimited.Description";
    private const string DraconicDescription =
        "ClassesReborn.BloodlineClaws.Draconic.Unlimited.Description";

    internal static void Configure() {
        ConfigureFamily(
            AbyssalDescription,
            new[] {
                ActivatableAbilityRefs.BloodlineAbyssalClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineAbyssalClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineAbyssalClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineAbyssalClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineAbyssalClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineAbyssalClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineAbyssalClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineAbyssalClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineAbyssalClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineAbyssalClawsBuffLevel4.ToString(),
            });

        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicBlackClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlackClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlackClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlackClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicBlackClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBlackClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicBlackClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicBlackClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicBlackClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicBlackClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicBlueClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlueClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlueClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBlueClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicBlueClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBlueClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicBlueClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicBlueClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicBlueClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicBlueClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicBrassClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBrassClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBrassClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBrassClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicBrassClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBrassClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicBrassClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicBrassClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicBrassClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicBrassClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicBronzeClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBronzeClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBronzeClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicBronzeClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicBronzeClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicBronzeClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicBronzeClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicBronzeClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicBronzeClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicCopperClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicCopperClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicCopperClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicCopperClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicCopperClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicCopperClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicCopperClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicCopperClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicCopperClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicCopperClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicGoldClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGoldClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGoldClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGoldClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicGoldClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicGoldClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicGoldClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicGoldClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicGoldClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicGoldClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicGreenClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGreenClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGreenClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicGreenClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicGreenClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicGreenClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicGreenClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicGreenClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicGreenClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicGreenClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicRedClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicRedClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicRedClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicRedClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicRedClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicRedClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicRedClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicRedClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicRedClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicRedClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicSilverClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicSilverClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicSilverClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicSilverClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicSilverClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicSilverClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicSilverClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicSilverClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicSilverClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicSilverClawsBuffLevel4.ToString(),
            });
        ConfigureDraconicFamily(
            new[] {
                ActivatableAbilityRefs.BloodlineDraconicWhiteClawsAbililyLevel1.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicWhiteClawsAbililyLevel2.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicWhiteClawsAbililyLevel3.ToString(),
                ActivatableAbilityRefs.BloodlineDraconicWhiteClawsAbililyLevel4.ToString(),
            },
            new[] {
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureLevel1.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureLevel2.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureLevel3.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureLevel4.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureAddLevel1.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureAddLevel2.ToString(),
                FeatureRefs.BloodlineDraconicWhiteClawsFeatureAddLevel3.ToString(),
            },
            new[] {
                BuffRefs.BloodlineDraconicWhiteClawsBuffLevel1.ToString(),
                BuffRefs.BloodlineDraconicWhiteClawsBuffLevel2.ToString(),
                BuffRefs.BloodlineDraconicWhiteClawsBuffLevel3.ToString(),
                BuffRefs.BloodlineDraconicWhiteClawsBuffLevel4.ToString(),
            });
    }

    private static void ConfigureDraconicFamily(
        string[] abilities,
        string[] features,
        string[] buffs) =>
        ConfigureFamily(DraconicDescription, abilities, features, buffs);

    private static void ConfigureFamily(
        string description,
        IEnumerable<string> abilityIds,
        IEnumerable<string> featureIds,
        IEnumerable<string> buffIds) {
        var configuredAbilities = 0;
        foreach (var abilityId in abilityIds) {
            var ability = ActivatableAbilityConfigurator.For(abilityId)
                .SetDescription(description)
                .SetDeactivateIfCombatEnded(false)
                .SetDeactivateAfterFirstRound(false)
                .SetDeactivateImmediately(false)
                .SetOnlyInCombat(false)
                .RemoveComponents(component =>
                    component is ActivatableAbilityResourceLogic)
                .Configure();

            if (ability.ComponentsArray?.OfType<ActivatableAbilityResourceLogic>().Any() == true ||
                ability.DeactivateIfCombatEnded ||
                ability.DeactivateAfterFirstRound ||
                ability.DeactivateImmediately ||
                ability.OnlyInCombat) {
                throw new InvalidOperationException(
                    $"{ability.name} still has a duration or resource restriction.");
            }
            configuredAbilities++;
        }

        foreach (var featureId in featureIds) {
            FeatureConfigurator.For(featureId)
                .SetDescription(description)
                .Configure();
        }
        foreach (var buffId in buffIds) {
            BuffConfigurator.For(buffId)
                .SetDescription(description)
                .Configure();
        }

        if (configuredAbilities != 4) {
            throw new InvalidOperationException(
                "Each bloodline claw family must configure all four scaling toggles.");
        }
    }
}
