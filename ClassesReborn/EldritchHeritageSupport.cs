using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Newtonsoft.Json;

namespace ClassesReborn;

// Small standalone compatibility layer for the MIT-licensed Character Options+
// Eldritch Heritage implementation adapted in EldritchHeritageRebalance.cs.
internal static class Logging {
    internal static Logger GetLogger(string name) => new(name);

    internal sealed class Logger {
        private readonly string Name;

        internal Logger(string name) => Name = name;

        internal void Log(string message) => Main.Log.Log($"[{Name}] {message}");

        internal void Verbose(Func<string> message) { }

        internal void LogException(string context, Exception exception) {
            Main.Log.Error($"[{Name}] {context}: {exception.Message}");
            Main.Log.LogException(exception);
        }
    }
}

internal static class Common {
    internal static void AddIsPrequisiteFor(
        Blueprint<BlueprintReference<BlueprintFeature>> prerequisite,
        params Blueprint<BlueprintFeatureReference>[] features) {
        FeatureConfigurator.For(prerequisite)
            .SkipAddToSelections()
            .AddToIsPrerequisiteFor(features)
            .Configure();
    }
}

[AllowMultipleComponents]
[TypeId("a4510255-5b48-4d8b-8d82-54c97e06bb9f")]
internal sealed class ApplyFeatureOnCharacterLevel :
    UnitFactComponentDelegate<ApplyFeatureOnCharacterLevel.ComponentData>,
    IOwnerGainLevelHandler {
    private readonly List<(BlueprintFeatureReference feature, int level)> FeatureLevels;
    private readonly BlueprintFeatureReference GreaterFeature;
    private readonly List<(BlueprintFeatureReference feature, int level)> GreaterFeatureLevels;

    public ApplyFeatureOnCharacterLevel(
        List<(BlueprintFeatureReference feature, int level)> featureLevels,
        BlueprintFeatureReference greaterFeature = null,
        List<(BlueprintFeatureReference feature, int level)> greaterFeatureLevels = null) {
        FeatureLevels = featureLevels.OrderByDescending(entry => entry.level).ToList();
        GreaterFeature = greaterFeature;
        GreaterFeatureLevels = greaterFeatureLevels?
            .OrderByDescending(entry => entry.level)
            .ToList();
    }

    public void HandleUnitGainLevel() => Apply();

    public override void OnActivate() => Apply();

    public override void OnDeactivate() => Remove();

    private void Apply() {
        var levels = GreaterFeature != null && Owner.HasFact(GreaterFeature)
            ? GreaterFeatureLevels
            : FeatureLevels;
        if (levels == null) {
            return;
        }

        var characterLevel = Owner.Descriptor.Progression.CharacterLevel;
        foreach (var (feature, level) in levels) {
            if (characterLevel < level) {
                continue;
            }
            if (Data.AppliedLevel != level) {
                Remove();
                Data.AppliedFact = Owner.AddFact(feature);
                Data.AppliedLevel = level;
            }
            return;
        }
    }

    private void Remove() {
        if (Data.AppliedFact == null) {
            return;
        }
        Owner.RemoveFact(Data.AppliedFact);
        Data.AppliedFact = null;
        Data.AppliedLevel = -1;
    }

    internal sealed class ComponentData {
        [JsonProperty]
        public EntityFact AppliedFact;

        [JsonProperty]
        public int AppliedLevel = -1;
    }
}

[AllowMultipleComponents]
[TypeId("4be7d7c4-b349-4cab-a32a-e360e6fcc7bc")]
internal sealed class AddFeatureOnCharacterLevel :
    UnitFactComponentDelegate<AddFeatureOnCharacterLevel.ComponentData>,
    IOwnerGainLevelHandler {
    private readonly List<(BlueprintFeatureReference feature, int level)> FeatureLevels;
    private readonly BlueprintFeatureReference GreaterFeature;
    private readonly List<(BlueprintFeatureReference feature, int level)> GreaterFeatureLevels;

    public AddFeatureOnCharacterLevel(
        List<(BlueprintFeatureReference feature, int level)> featureLevels,
        BlueprintFeatureReference greaterFeature = null,
        List<(BlueprintFeatureReference feature, int level)> greaterFeatureLevels = null) {
        FeatureLevels = featureLevels.ToList();
        GreaterFeature = greaterFeature;
        GreaterFeatureLevels = greaterFeatureLevels;
    }

    public void HandleUnitGainLevel() => Apply();

    public override void OnActivate() => Apply();

    public override void OnDeactivate() {
        for (var index = 0; index < Data.AppliedFacts.Length; index++) {
            if (Data.AppliedFacts[index] == null) {
                continue;
            }
            Owner.RemoveFact(Data.AppliedFacts[index]);
            Data.AppliedFacts[index] = null;
        }
    }

    private void Apply() {
        var levels = GreaterFeature != null && Owner.HasFact(GreaterFeature)
            ? GreaterFeatureLevels
            : FeatureLevels;
        if (levels == null) {
            return;
        }

        var characterLevel = Owner.Descriptor.Progression.CharacterLevel;
        foreach (var (feature, level) in levels) {
            if (level < 0 || level >= Data.AppliedFacts.Length ||
                characterLevel < level || Data.AppliedFacts[level] != null) {
                continue;
            }
            Data.AppliedFacts[level] = Owner.AddFact(feature);
        }
    }

    internal sealed class ComponentData {
        [JsonProperty]
        public EntityFact[] AppliedFacts = new EntityFact[21];
    }
}

[TypeId("1f44bcca-7207-4f5d-8ce2-c9861c71e44e")]
internal sealed class SetResourceMax : UnitFactComponentDelegate,
    IResourceAmountBonusHandler {
    private readonly ContextValue Max;
    private readonly BlueprintAbilityResourceReference Resource;

    public SetResourceMax(
        ContextValue max,
        BlueprintAbilityResourceReference resource) {
        Max = max;
        Resource = resource;
    }

    public void CalculateMaxResourceAmount(
        BlueprintAbilityResource resource,
        ref int bonus) {
        if (Resource?.Get() == resource) {
            bonus = Max.Calculate(Context);
        }
    }
}
