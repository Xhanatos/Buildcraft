// The selection and preview strategy in this file is derived from the
// MIT-licensed ToyBox Archetypes implementation, which was itself based on
// Vek17's MIT-licensed Multiple Archetypes mod. See THIRD_PARTY_NOTICES.md.
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class.Mechanic;
using Kingmaker.UI.MVVM._VM.Other.NestedSelectionGroup;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.Classes;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.ChupaChupses;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Level;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Main;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Spellbook;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Stats;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using Owlcat.Runtime.UI.Tooltips;
using System.Reflection;
using System.Reflection.Emit;
using UniRx;

namespace ClassesReborn;

internal static class ArchetypeStacking {
    private static string BlockingProvider;
    private static bool ExternalProvidersResolved;
    private static object ToyBoxSettings;
    private static FieldInfo ToyBoxToggleField;
    private static PropertyInfo ToyBoxToggleProperty;
    private static FieldInfo ToyBoxGestaltToggleField;
    private static PropertyInfo ToyBoxGestaltToggleProperty;
    private static bool StandaloneProviderLoaded;
    private static bool ToyBoxSelectorCorrectionLogged;
    private static bool ToyBoxPreviewGuardLogged;

    internal static bool Enabled {
        get {
            if (!Main.Settings.ArchetypeStacking) {
                return false;
            }

            if (TryGetExternalProvider(out var provider)) {
                if (!string.Equals(BlockingProvider, provider, StringComparison.Ordinal)) {
                    BlockingProvider = provider;
                    Main.Log.Log(
                        $"Archetype stacking is enabled in {provider}; " +
                        "Buildcraft's overlapping runtime patches are suspended.");
                }
                return false;
            }

            if (BlockingProvider != null) {
                Main.Log.Log(
                    "No external archetype-stacking provider is active; " +
                    "Buildcraft's implementation is active again.");
                BlockingProvider = null;
            }
            return true;
        }
    }

    internal static bool TryGetExternalProvider(out string provider) {
        ResolveExternalProviders();

        if (ReadToyBoxToggle()) {
            provider = "ToyBox";
            return true;
        }

        // The standalone mod leaves several display/calculation patches active
        // even when its own UI toggle is off, so its loaded assembly alone is
        // enough to require conflict avoidance.
        if (StandaloneProviderLoaded) {
            provider = "Multiple Archetypes";
            return true;
        }

        provider = null;
        return false;
    }

    private static void ResolveExternalProviders() {
        if (ExternalProvidersResolved) {
            return;
        }

        ExternalProvidersResolved = true;
        try {
            var toyBoxMain = AccessTools.TypeByName("ToyBox.Main");
            if (toyBoxMain != null) {
                ToyBoxSettings = AccessTools.Field(toyBoxMain, "Settings")
                    ?.GetValue(null)
                    ?? AccessTools.Property(toyBoxMain, "Settings")
                        ?.GetValue(null);
                if (ToyBoxSettings != null) {
                    var settingsType = ToyBoxSettings.GetType();
                    ToyBoxToggleField = AccessTools.Field(
                        settingsType,
                        "toggleMultiArchetype");
                    ToyBoxToggleProperty = AccessTools.Property(
                        settingsType,
                        "toggleMultiArchetype");
                    ToyBoxGestaltToggleField = AccessTools.Field(
                        settingsType,
                        "toggleMulticlass");
                    ToyBoxGestaltToggleProperty = AccessTools.Property(
                        settingsType,
                        "toggleMulticlass");
                }
            }

            StandaloneProviderLoaded =
                AccessTools.TypeByName("MultipleArchetypes.Main") != null;
        } catch (Exception exception) {
            Main.Log.Log(
                "Could not resolve an external archetype-stacking provider; " +
                "assuming it is disabled. " +
                exception.Message);
        }
    }

    private static bool ReadToyBoxToggle() {
        try {
            if (ToyBoxToggleField?.GetValue(ToyBoxSettings) is bool fieldValue) {
                return fieldValue;
            }

            return ToyBoxToggleProperty?.GetValue(ToyBoxSettings) is
                bool propertyValue && propertyValue;
        } catch (Exception exception) {
            Main.Log.Log(
                "Could not read ToyBox's archetype-stacking toggle; " +
                "assuming it is disabled. " +
                exception.Message);
            return false;
        }
    }

    private static bool ReadToyBoxGestaltToggle() {
        ResolveExternalProviders();
        try {
            if (ToyBoxGestaltToggleField?.GetValue(ToyBoxSettings) is
                bool fieldValue) {
                return fieldValue;
            }

            return ToyBoxGestaltToggleProperty?.GetValue(ToyBoxSettings) is
                bool propertyValue && propertyValue;
        } catch (Exception exception) {
            Main.Log.Log(
                "Could not read ToyBox's gestalt toggle; assuming it is " +
                "disabled. " + exception.Message);
            return false;
        }
    }

    private static void SynchronizeToyBoxPrimaryClass(
        CharGenClassPhaseVM phase,
        CharGenClassSelectorItemVM expectedSelection = null) {
        if (!ReadToyBoxGestaltToggle() || phase == null) {
            return;
        }

        var selectedViewModel = expectedSelection ?? phase.SelectedClassVM.Value;
        var controller = phase.LevelUpController;
        if (selectedViewModel?.Class == null || controller?.State == null) {
            return;
        }

        if (!ReferenceEquals(phase.SelectedClassVM.Value, selectedViewModel)) {
            phase.SelectedClassVM.Value = selectedViewModel;
        }

        if (controller.State.SelectedClass == selectedViewModel.Class) {
            return;
        }

        phase.OnSelectorClassChanged(selectedViewModel);
        if (!ToyBoxSelectorCorrectionLogged &&
            controller.State.SelectedClass == selectedViewModel.Class) {
            ToyBoxSelectorCorrectionLogged = true;
            Main.Log.Log(
                "Corrected a desynchronized ToyBox gestalt class selection.");
        }
    }

    private static bool HasValidToyBoxPrimaryClass(
        LevelUpState state,
        BlueprintCharacterClass[] appliedClasses) {
        if (appliedClasses == null) {
            return false;
        }

        if (appliedClasses.Length == 0) {
            return true;
        }

        return state?.SelectedClass != null &&
            appliedClasses.All(characterClass => characterClass != null) &&
            appliedClasses.Contains(state.SelectedClass);
    }

    private static void LogToyBoxPreviewGuard() {
        if (ToyBoxPreviewGuardLogged) {
            return;
        }

        ToyBoxPreviewGuardLogged = true;
        Main.Log.Log(
            "Skipped a transient ToyBox gestalt BAB/save/HP preview update " +
            "that had no valid primary class.");
    }

    private static string ArchetypeNames(ClassData classData) =>
        string.Join("/", classData.Archetypes.Select(archetype => archetype.Name));

    private static CharGenClassPhaseVM FindClassPhase(
        INestedListSource source) {
        for (var depth = 0; source != null && depth < 8; depth++) {
            if (source is CharGenClassPhaseVM phase) {
                return phase;
            }

            var parent = source.Source;
            if (ReferenceEquals(parent, source)) {
                break;
            }
            source = parent;
        }
        return null;
    }

    private static void RefreshArchetypeChoices(
        CharGenClassSelectorItemVM changedItem,
        LevelUpController controller) {
        var progression = controller.Preview?.Progression;
        var selectedClass = controller.State?.SelectedClass;
        var classData = selectedClass == null
            ? null
            : progression?.GetClassData(selectedClass);
        if (progression == null || classData == null) {
            return;
        }

        var siblings = changedItem.Source?.ExtractNestedEntities();
        if (siblings == null) {
            return;
        }

        foreach (var item in siblings.OfType<CharGenClassSelectorItemVM>()) {
            if (item.Class != classData.CharacterClass || item.Archetype == null) {
                continue;
            }

            var selected = classData.Archetypes.HasItem(item.Archetype);
            var available = selected ||
                (item.PrerequisitesDone &&
                 progression.CanAddArchetype(
                     classData.CharacterClass,
                     item.Archetype));
            item.SetAvailableState(available);
            item.IsSelected.Value = selected;
            item.RefreshView.Execute();
        }
    }

    [HarmonyPatch(typeof(ClassData), nameof(ClassData.CalcSkillPoints))]
    private static class ClassDataCalcSkillPointsPatch {
        private static bool Prefix(ClassData __instance, ref int __result) {
            if (!Enabled || !__instance.Archetypes.Any()) {
                return true;
            }

            __result = __instance.CharacterClass.SkillPoints +
                __instance.Archetypes.Max(archetype => archetype.AddSkillPoints);
            return false;
        }
    }

    [HarmonyPatch(
        typeof(TooltipTemplateClass),
        MethodType.Constructor,
        typeof(ClassData))]
    private static class TooltipTemplateClassConstructorPatch {
        private static void Postfix(
            TooltipTemplateClass __instance,
            ClassData classData) {
            if (!Enabled) {
                return;
            }

            var name = ArchetypeNames(classData);
            if (string.IsNullOrEmpty(name)) {
                return;
            }

            var description = string.Join(
                "\n\n",
                classData.Archetypes.Select(archetype => archetype.Description));
            AccessTools.Field(typeof(TooltipTemplateClass), "m_Name")
                ?.SetValue(__instance, name);
            AccessTools.Field(typeof(TooltipTemplateClass), "m_Desc")
                ?.SetValue(__instance, description);
        }
    }

    [HarmonyPatch(
        typeof(CharInfoClassEntryVM),
        MethodType.Constructor,
        typeof(ClassData))]
    private static class CharInfoClassEntryVmConstructorPatch {
        private static void Postfix(
            CharInfoClassEntryVM __instance,
            ClassData classData) {
            if (!Enabled) {
                return;
            }

            var name = ArchetypeNames(classData);
            if (!string.IsNullOrEmpty(name)) {
                AccessTools.Field(
                        typeof(CharInfoClassEntryVM),
                        "<ClassName>k__BackingField")
                    ?.SetValue(__instance, name);
            }
        }
    }

    [HarmonyPatch(typeof(ClassProgressionVM))]
    private static class ClassProgressionVmPatches {
        [HarmonyPatch(
            MethodType.Constructor,
            typeof(UnitDescriptor),
            typeof(ClassData))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MakeEmptyProgressionSafe(
            IEnumerable<CodeInstruction> instructions) {
            var first = typeof(Enumerable)
                .GetMethods()
                .Single(method =>
                    method.Name == nameof(Enumerable.First) &&
                    method.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(ProgressionVM));
            var firstOrDefault = typeof(Enumerable)
                .GetMethods()
                .Single(method =>
                    method.Name == nameof(Enumerable.FirstOrDefault) &&
                    method.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(ProgressionVM));

            foreach (var instruction in instructions) {
                if (instruction.opcode == OpCodes.Call &&
                    Equals(instruction.operand as MethodInfo, first)) {
                    instruction.operand = firstOrDefault;
                }
                yield return instruction;
            }
        }

        [HarmonyPatch(
            MethodType.Constructor,
            typeof(UnitDescriptor),
            typeof(ClassData))]
        [HarmonyPostfix]
        private static void ExistingClassPostfix(
            ClassProgressionVM __instance,
            ClassData unitClass) {
            if (!Enabled) {
                return;
            }

            var name = ArchetypeNames(unitClass);
            if (!string.IsNullOrEmpty(name)) {
                __instance.Name = $"{unitClass.CharacterClass.Name} ({name})";
            }

            var castingArchetype = unitClass.Archetypes
                .FirstOrDefault(archetype => archetype.ReplaceSpellbook != null);
            if (castingArchetype != null) {
                __instance.AddDisposable(
                    __instance.SpellbookProgressionVM = new SpellbookProgressionVM(
                        __instance.m_UnitClass,
                        castingArchetype,
                        __instance.m_Unit,
                        __instance.m_LevelProgressionVM));
            }
        }

        [HarmonyPatch(
            MethodType.Constructor,
            typeof(UnitDescriptor),
            typeof(BlueprintCharacterClass),
            typeof(BlueprintArchetype),
            typeof(bool),
            typeof(int))]
        [HarmonyPostfix]
        private static void LevelUpPreviewPostfix(
            ClassProgressionVM __instance,
            BlueprintCharacterClass classBlueprint,
            bool buildDifference,
            int level) {
            if (!Enabled) {
                return;
            }

            var data = __instance.ProgressionVms
                .Select(viewModel => viewModel.ProgressionData)
                .OfType<AdvancedProgressionData>()
                .FirstOrDefault();
            if (data == null || Game.Instance?.LevelUpController == null) {
                return;
            }

            var addArchetypes = Game.Instance.LevelUpController.LevelUpActions
                .OfType<AddArchetype>()
                .Where(action => action.Archetype.GetParentClass() == classBlueprint)
                .ToArray();
            if (addArchetypes.Length == 0) {
                return;
            }

            __instance.ProgressionVms.Clear();
            foreach (var action in addArchetypes) {
                data.AddArchetype(action.Archetype);
            }

            var progression = new ProgressionVM(
                data,
                __instance.m_Unit,
                level,
                buildDifference);
            __instance.ProgressionVms.Add(progression);
            __instance.AddProgressions(
                __instance.m_Unit.Progression
                    .GetClassProgressions(__instance.m_UnitClass)
                    .EmptyIfNull<ProgressionData>());
            __instance.AddProgressionSources(progression.ProgressionSourceFeatures);

            var archetypeNames = string.Join(
                "/",
                addArchetypes.Select(action => action.Archetype.Name));
            __instance.Name = $"{classBlueprint.Name} ({archetypeNames})";

            var castingArchetype = addArchetypes
                .Select(action => action.Archetype)
                .FirstOrDefault(archetype => archetype.ReplaceSpellbook != null);
            if (castingArchetype != null) {
                __instance.AddDisposable(
                    __instance.SpellbookProgressionVM = new SpellbookProgressionVM(
                        __instance.m_UnitClass,
                        castingArchetype,
                        __instance.m_Unit,
                        __instance.m_LevelProgressionVM));
            }
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassSelectorItemVM),
        MethodType.Constructor,
        typeof(BlueprintCharacterClass),
        typeof(BlueprintArchetype),
        typeof(LevelUpController),
        typeof(INestedListSource),
        typeof(ReactiveProperty<CharGenClassSelectorItemVM>),
        typeof(ReactiveProperty<TooltipBaseTemplate>),
        typeof(bool),
        typeof(bool),
        typeof(bool))]
    private static class CharGenClassSelectorItemVmConstructorPatch {
        private static void Postfix(
            CharGenClassSelectorItemVM __instance,
            BlueprintCharacterClass cls,
            LevelUpController levelUpController) {
            if (!Enabled || !__instance.HasClassLevel) {
                return;
            }

            var classData = levelUpController.Unit.Progression.GetClassData(cls);
            if (classData == null || !classData.Archetypes.Any()) {
                return;
            }

            AccessTools.Field(typeof(CharGenClassSelectorItemVM), "DisplayName")
                ?.SetValue(__instance, $"{cls.Name} — {ArchetypeNames(classData)}");
        }
    }

    [HarmonyPatch(
        typeof(NestedSelectionGroupEntityVM),
        nameof(NestedSelectionGroupEntityVM.SetSelected),
        typeof(bool))]
    private static class NestedSelectionSetSelectedPatch {
        private static bool Prefix(
            NestedSelectionGroupEntityVM __instance,
            ref bool state) {
            if (__instance is not CharGenClassSelectorItemVM viewModel ||
                viewModel.Archetype == null ||
                !Enabled) {
                return true;
            }

            var controller = Game.Instance?.LevelUpController;
            var progression = controller?.Preview?.Progression;
            var selectedClass = controller?.State?.SelectedClass;
            var classData = selectedClass == null
                ? null
                : progression?.GetClassData(selectedClass);
            if (controller == null || classData == null) {
                return true;
            }

            if (controller.Unit.Progression.GetClassLevel(viewModel.Class) >= 1) {
                return true;
            }

            state |= classData.Archetypes.HasItem(viewModel.Archetype);
            if (!state && progression != null) {
                viewModel.SetAvailableState(
                    progression.CanAddArchetype(
                        classData.CharacterClass,
                        viewModel.Archetype) &&
                    viewModel.PrerequisitesDone);
            }
            return true;
        }
    }

    [HarmonyPatch(
        typeof(NestedSelectionGroupEntityVM),
        nameof(NestedSelectionGroupEntityVM.SetSelectedFromView),
        typeof(bool))]
    private static class NestedSelectionSetSelectedFromViewPatch {
        private static bool Prefix(
            NestedSelectionGroupEntityVM __instance,
            bool state) {
            if (__instance is not CharGenClassSelectorItemVM viewModel ||
                !Enabled) {
                return true;
            }

            var controller = Game.Instance?.LevelUpController;
            if (controller == null ||
                controller.Unit.Progression.GetClassLevel(viewModel.Class) >= 1) {
                return true;
            }

            if (!state &&
                !__instance.AllowSwitchOff &&
                viewModel.Archetype == null) {
                return false;
            }

            if (!state && viewModel.Archetype != null) {
                controller.RemoveArchetype(viewModel.Archetype);
                __instance.IsSelected.Value = false;
                __instance.RefreshView.Execute();
                FindClassPhase(viewModel.Source)?.UpdateClassInformation();
                RefreshArchetypeChoices(viewModel, controller);
                return false;
            }

            __instance.IsSelected.Value = state;
            __instance.RefreshView.Execute();
            if (state) {
                __instance.DoSelectMe();
            }
            return false;
        }

        private static void Postfix(
            NestedSelectionGroupEntityVM __instance,
            bool state) {
            if (state &&
                __instance is CharGenClassSelectorItemVM viewModel &&
                viewModel.Archetype == null) {
                SynchronizeToyBoxPrimaryClass(
                    FindClassPhase(viewModel.Source),
                    viewModel);
            }
        }
    }

    [HarmonyPatch]
    private static class ToyBoxMulticlassCheckboxChangedPatch {
        private static Type TargetType() =>
            AccessTools.TypeByName(
                "ToyBox.Multiclass.MultipleClasses+MulticlassCheckBoxHelper");

        private static bool Prepare() => TargetType() != null;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(TargetType(), "MulticlassCheckBoxChanged");

        private static void Postfix(object[] __args) {
            if (__args == null ||
                __args.Length < 2 ||
                __args[1] is not CharGenClassSelectorItemPCView itemView) {
                return;
            }

            var phase = FindClassPhase(itemView.ViewModel?.Source);
            SynchronizeToyBoxPrimaryClass(phase);
        }
    }

    [HarmonyPatch]
    private static class ToyBoxSavesBabPreviewGuardPatch {
        private static Type TargetType() =>
            AccessTools.TypeByName("ToyBox.Multiclass.SavesBAB");

        private static bool Prepare() => TargetType() != null;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(TargetType(), "ApplySingleStat");

        private static bool Prefix(
            LevelUpState state,
            BlueprintCharacterClass[] appliedClasses,
            BlueprintStatProgression[] statProgs) {
            if (!ReadToyBoxGestaltToggle()) {
                return true;
            }

            var valid = HasValidToyBoxPrimaryClass(state, appliedClasses) &&
                statProgs != null &&
                statProgs.Length >= (appliedClasses?.Length ?? 0) &&
                statProgs.All(progression => progression != null);
            if (valid) {
                return true;
            }

            LogToyBoxPreviewGuard();
            return false;
        }
    }

    [HarmonyPatch]
    private static class ToyBoxHpPreviewGuardPatch {
        private static Type TargetType() =>
            AccessTools.TypeByName("ToyBox.Multiclass.HPDice");

        private static bool Prepare() => TargetType() != null;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(TargetType(), "ApplyHPDice");

        private static bool Prefix(
            LevelUpState state,
            BlueprintCharacterClass[] appliedClasses) {
            if (!ReadToyBoxGestaltToggle() ||
                HasValidToyBoxPrimaryClass(state, appliedClasses)) {
                return true;
            }

            LogToyBoxPreviewGuard();
            return false;
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassPhaseVM),
        nameof(CharGenClassPhaseVM.OnSelectorArchetypeChanged),
        typeof(BlueprintArchetype))]
    private static class CharGenClassPhaseArchetypeChangedPatch {
        private static bool Prefix(
            CharGenClassPhaseVM __instance,
            BlueprintArchetype archetype) {
            if (!Enabled) {
                return true;
            }

            var controller = __instance.LevelUpController;
            __instance.UpdateTooltipTemplate(false);

            if (controller.State.SelectedClass == null && archetype != null) {
                controller.SelectClass(archetype.GetParentClass(), ignoreAlignment: true);
            }

            if (controller.State.SelectedClass != null && archetype == null) {
                var selectedClass = controller.State.SelectedClass;
                var removalClassData = controller.Preview.Progression
                    .GetClassData(selectedClass);
                foreach (var existing in
                    removalClassData?.Archetypes.ToArray() ??
                    Array.Empty<BlueprintArchetype>()) {
                    controller.RemoveArchetype(existing);
                }
                __instance.UpdateClassInformation();
                return false;
            }

            var progression = controller.Preview.Progression;
            var classData = progression.GetClassData(controller.State.SelectedClass);
            if (classData == null || archetype == null) {
                __instance.UpdateClassInformation();
                return false;
            }

            if (classData.Archetypes.HasItem(archetype)) {
                __instance.UpdateClassInformation();
                return false;
            }

            if (!progression.CanAddArchetype(
                    classData.CharacterClass,
                    archetype)) {
                foreach (var existing in classData.Archetypes.ToArray()) {
                    controller.RemoveArchetype(existing);
                }
            }

            controller.RemoveArchetype(archetype);
            controller.AddArchetype(archetype);
            __instance.UpdateClassInformation();
            return false;
        }
    }

    [HarmonyPatch(
        typeof(ProgressionVM),
        nameof(ProgressionVM.SetClassArchetypeDifType),
        typeof(ProgressionVM.FeatureEntry))]
    private static class ProgressionVmDifferenceTypePatch {
        private static void Postfix(
            ProgressionVM __instance,
            ref ProgressionVM.FeatureEntry featureEntry) {
            if (!Enabled) {
                return;
            }

            var level = featureEntry.Level;
            var feature = featureEntry.Feature;
            foreach (var archetype in __instance.ProgressionData.Archetypes) {
                foreach (var entry in archetype.RemoveFeatures
                             .Where(entry => entry.Level == level)) {
                    if (entry.Features.Contains(feature)) {
                        featureEntry.DifType = ClassArchetypeDifType.Removed;
                    }
                }

                foreach (var entry in archetype.AddFeatures
                             .Where(entry => entry.Level == level)) {
                    if (entry.Features.Contains(feature)) {
                        featureEntry.DifType = ClassArchetypeDifType.Added;
                    }
                }
            }
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassCasterStatsVM),
        MethodType.Constructor,
        typeof(BlueprintCharacterClass),
        typeof(BlueprintArchetype))]
    private static class CharGenClassCasterStatsPatch {
        private static void Postfix(
            CharGenClassCasterStatsVM __instance,
            BlueprintCharacterClass valueClass) {
            if (!Enabled) {
                return;
            }

            var classData = Game.Instance?.LevelUpController?.Preview?.Progression
                ?.GetClassData(valueClass);
            if (classData == null) {
                return;
            }

            __instance.CanCast.Value = classData.Spellbook != null;
            if (classData.Spellbook == null) {
                return;
            }

            var casterTypeArchetype = classData.Archetypes
                .FirstOrDefault(archetype => archetype.ChangeCasterType);
            __instance.MaxSpellsLevel.Value =
                classData.Spellbook.MaxSpellLevel.ToString();
            __instance.CasterAbilityScore.Value =
                LocalizedTexts.Instance.Stats.GetText(
                    classData.Spellbook.CastingAttribute);
            __instance.CasterMindType.Value = casterTypeArchetype == null
                ? UIUtilityUnit.GetCasterMindType(valueClass) ?? "—"
                : UIUtilityUnit.GetCasterMindType(casterTypeArchetype) ?? "—";
            __instance.SpellbookUseType.Value =
                UIUtilityUnit.GetCasterSpellbookUseType(classData.Spellbook);
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassMartialStatsVM),
        MethodType.Constructor,
        typeof(BlueprintCharacterClass),
        typeof(BlueprintArchetype),
        typeof(UnitDescriptor))]
    private static class CharGenClassMartialStatsPatch {
        private static void Postfix(
            CharGenClassMartialStatsVM __instance,
            BlueprintCharacterClass valueClass) {
            if (!Enabled) {
                return;
            }

            var classData = Game.Instance?.LevelUpController?.Preview?.Progression
                ?.GetClassData(valueClass);
            if (classData == null) {
                return;
            }

            __instance.Fortitude.Value =
                UIUtilityUnit.GetStatProgressionGrade(classData.FortitudeSave);
            __instance.Will.Value =
                UIUtilityUnit.GetStatProgressionGrade(classData.WillSave);
            __instance.Reflex.Value =
                UIUtilityUnit.GetStatProgressionGrade(classData.ReflexSave);
            __instance.BAB.Value =
                UIUtilityUnit.GetStatProgressionGrade(classData.BaseAttackBonus);
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassSkillsVM),
        MethodType.Constructor,
        typeof(BlueprintCharacterClass),
        typeof(BlueprintArchetype))]
    private static class CharGenClassSkillsPatch {
        private static void Postfix(
            CharGenClassSkillsVM __instance,
            BlueprintCharacterClass valueClass) {
            if (!Enabled) {
                return;
            }

            var classData = Game.Instance?.LevelUpController?.Preview?.Progression
                ?.GetClassData(valueClass);
            if (classData == null) {
                return;
            }

            var classSkills = classData.Archetypes
                .SelectMany(archetype => archetype.ClassSkills)
                .Concat(classData.CharacterClass.ClassSkills)
                .Distinct()
                .ToArray();
            __instance.ClassSkills.Clear();
            foreach (var skill in classSkills) {
                var entry = new CharGenClassStatEntryVM(skill);
                __instance.AddDisposable(entry);
                __instance.ClassSkills.Add(entry);
            }
        }
    }

    [HarmonyPatch(
        typeof(CharGenClassPhaseVM),
        nameof(CharGenClassPhaseVM.UpdateClassInformation))]
    private static class CharGenClassPhaseUpdateInformationPatch {
        private static void Postfix(CharGenClassPhaseVM __instance) {
            if (!Enabled) {
                return;
            }

            var selectedClass = __instance.SelectedClassVM.Value?.Class;
            var classData = selectedClass == null
                ? null
                : Game.Instance?.LevelUpController?.Preview?.Progression
                    ?.GetClassData(selectedClass);
            if (classData == null) {
                return;
            }

            var names = ArchetypeNames(classData);
            if (string.IsNullOrEmpty(names)) {
                return;
            }

            __instance.ClassDisplayName.Value =
                $"{classData.CharacterClass.Name} ({names})";
            __instance.ClassDescription.Value = string.Join(
                "\n\n",
                classData.Archetypes.Select(archetype => archetype.Description));
        }
    }
}
