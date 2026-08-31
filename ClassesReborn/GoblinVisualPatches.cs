using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Visual.CharacterSystem;

namespace ClassesReborn;

internal static class GoblinVisualGenderOverride {
    private static bool Logged;
    private static bool EquipmentLogged;

    internal static bool Begin(DollState dollState, out Gender originalGender) {
        originalGender = dollState?.Gender ?? Gender.Male;
        if (!Main.Settings.GoblinRace || dollState == null ||
            originalGender != Gender.Female ||
            dollState.Race?.RaceId != Race.Goblin) {
            return false;
        }

        dollState.Gender = Gender.Male;
        LogOnce();
        return true;
    }

    internal static bool Begin(DollData dollData, out Gender originalGender) {
        originalGender = dollData?.Gender ?? Gender.Male;
        if (!Main.Settings.GoblinRace || dollData == null ||
            originalGender != Gender.Female ||
            dollData.RacePreset?.RaceId != Race.Goblin) {
            return false;
        }

        dollData.Gender = Gender.Male;
        LogOnce();
        return true;
    }

    internal static void Restore(
        DollState dollState,
        Gender originalGender,
        bool changed) {
        if (changed && dollState != null) {
            dollState.Gender = originalGender;
        }
    }

    internal static void Restore(
        DollData dollData,
        Gender originalGender,
        bool changed) {
        if (changed && dollData != null) {
            dollData.Gender = originalGender;
        }
    }

    internal static bool MustUseMaleEquipment(Gender gender, Race race) {
        if (!Main.Settings.GoblinRace ||
            gender != Gender.Female ||
            race != Race.Goblin) {
            return false;
        }

        if (!EquipmentLogged) {
            EquipmentLogged = true;
            Main.Log.Log(
                "Female Goblin body and outfit equipment is using the " +
                "compatible male Goblin variants.");
        }
        return true;
    }

    private static void LogOnce() {
        if (Logged) {
            return;
        }
        Logged = true;
        Main.Log.Log(
            "Female Goblin visual construction is using the compatible " +
            "male Goblin doll while preserving the character's female gender.");
    }
}

[HarmonyPatch(
    typeof(KingmakerEquipmentEntity),
    nameof(KingmakerEquipmentEntity.GetLinks))]
internal static class GoblinEquipmentGenderPatch {
    [HarmonyPrefix]
    private static void Prefix(ref Gender gender, Race race) {
        if (GoblinVisualGenderOverride.MustUseMaleEquipment(gender, race)) {
            gender = Gender.Male;
        }
    }
}

[HarmonyPatch(
    typeof(BlueprintCharacterClass),
    nameof(BlueprintCharacterClass.GetClothesLinks))]
internal static class GoblinClassClothesGenderPatch {
    [HarmonyPrefix]
    private static void Prefix(ref Gender gender, Race race) {
        if (GoblinVisualGenderOverride.MustUseMaleEquipment(gender, race)) {
            gender = Gender.Male;
        }
    }
}

[HarmonyPatch(typeof(DollRoom), nameof(DollRoom.UpdateDoll))]
internal static class GoblinDollRoomGenderPatch {
    [HarmonyPrefix]
    private static void Prefix(
        DollState dollState,
        out (Gender OriginalGender, bool Changed) __state) {
        var changed = GoblinVisualGenderOverride.Begin(
            dollState,
            out var originalGender);
        __state = (originalGender, changed);
    }

    [HarmonyPostfix]
    private static void Postfix(
        DollState dollState,
        (Gender OriginalGender, bool Changed) __state) =>
        GoblinVisualGenderOverride.Restore(
            dollState,
            __state.OriginalGender,
            __state.Changed);

    [HarmonyFinalizer]
    private static Exception Finalizer(
        DollState dollState,
        (Gender OriginalGender, bool Changed) __state,
        Exception __exception) {
        GoblinVisualGenderOverride.Restore(
            dollState,
            __state.OriginalGender,
            __state.Changed);
        return __exception;
    }
}

[HarmonyPatch(typeof(DollData), nameof(DollData.CreateUnitView))]
internal static class GoblinCreatedUnitGenderPatch {
    [HarmonyPrefix]
    private static void Prefix(
        DollData __instance,
        out (Gender OriginalGender, bool Changed) __state) {
        var changed = GoblinVisualGenderOverride.Begin(
            __instance,
            out var originalGender);
        __state = (originalGender, changed);
    }

    [HarmonyPostfix]
    private static void Postfix(
        DollData __instance,
        (Gender OriginalGender, bool Changed) __state) =>
        GoblinVisualGenderOverride.Restore(
            __instance,
            __state.OriginalGender,
            __state.Changed);

    [HarmonyFinalizer]
    private static Exception Finalizer(
        DollData __instance,
        (Gender OriginalGender, bool Changed) __state,
        Exception __exception) {
        GoblinVisualGenderOverride.Restore(
            __instance,
            __state.OriginalGender,
            __state.Changed);
        return __exception;
    }
}
