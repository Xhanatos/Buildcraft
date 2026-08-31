using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;

namespace ClassesReborn;

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("6181e0f8-54b0-4ef9-854b-deb1d77f85a1")]
public sealed class ArchaeologistLuckResourceBonus : UnitFactComponentDelegate,
    IResourceAmountBonusHandler {
    public BlueprintAbilityResourceReference m_Resource;
    public BlueprintCharacterClassReference m_Class;

    public void CalculateMaxResourceAmount(
        BlueprintAbilityResource resource,
        ref int bonus) {
        if (resource != m_Resource?.Get()) {
            return;
        }

        var characterClass = m_Class?.Get();
        if (characterClass == null) {
            return;
        }

        bonus += 2 + Owner.Stats.Charisma.Bonus +
            (2 * Owner.Progression.GetClassLevel(characterClass));
    }
}

[AllowedOn(typeof(BlueprintUnitFact))]
[TypeId("d4169b39-fc52-4dcc-abfa-311955f17fb9")]
public sealed class TrueArtistPerformanceDcBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
    private static readonly string[] PerformanceNameFragments = {
        "BardPerformance",
        "DeadlyPerformance",
        "Fascinate",
        "FrighteningTune",
        "DirgeOfDoom",
        "Thundercaller",
        "VindictiveSoliloquy",
        "BlazingRondo",
        "DeadlyVibrato",
        "BansheesRequiem",
        "PrimaDonna",
        "TragedyOfFalseHope",
        "InciteRage",
    };

    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) {
        var blueprint = evt?.Spell ?? evt?.Blueprint;
        var name = blueprint?.name;
        if (!string.IsNullOrWhiteSpace(name) &&
            PerformanceNameFragments.Any(fragment =>
                name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)) {
            evt.AddBonusDC(Bonus, ModifierDescriptor.UntypedStackable);
        }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
}
