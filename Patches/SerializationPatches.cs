#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    [HarmonyPatch(typeof(Panel_InsulatedFlask), "RefreshTables")]
    internal static class FlaskRefreshPatch
    {
        private static void Prefix(Panel_InsulatedFlask __instance)
        {
            InsulatedFlask flask = FlaskWaterHelper.GetFlask(__instance);

            if (flask == null ||
                !FlaskLiquidRules.FlaskContainsWater(flask))
            {
                return;
            }

            FlaskWaterHelper.EnsureWaterContent(
                flask,
                null,
                flask.GetVolumeLitres());
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), "Deserialize")]
    internal static class FlaskDeserializePatch
    {
        private static void Prefix(InsulatedFlask.SaveData __0)
        {
            FlaskWaterHelper.NormalizeWaterSaveData(__0);
        }

        private static void Postfix(
            InsulatedFlask __instance,
            InsulatedFlask.SaveData __0)
        {
            if (__instance == null)
                return;

            if (FlaskWaterHelper.RestoreWaterContentFromSaveData(
                    __instance,
                    __0))
            {
                return;
            }

            LiquidType potable = FlaskWaterHelper.PotableWater;
            if (__instance.m_LiquidType == null ||
                potable == null ||
                (__instance.m_LiquidType != potable &&
                 !__instance.m_LiquidType.Equals(potable)))
            {
                return;
            }

            ItemLiquidVolume volume = __instance.GetVolumeLitres();
            if (volume <= ItemLiquidVolume.Zero)
                return;

            FlaskWaterHelper.EnsureWaterContent(__instance, null, volume);
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), "Serialize")]
    internal static class FlaskSerializePatch
    {
        private static void Prefix(InsulatedFlask __instance)
        {
            if (__instance == null)
                return;

            LiquidType potable = FlaskWaterHelper.PotableWater;
            if (__instance.m_LiquidType == null ||
                potable == null ||
                (__instance.m_LiquidType != potable &&
                 !__instance.m_LiquidType.Equals(potable)))
            {
                return;
            }

            ItemLiquidVolume volume = __instance.GetVolumeLitres();
            if (volume <= ItemLiquidVolume.Zero)
                return;

            FlaskWaterHelper.EnsureWaterContent(__instance, null, volume);
        }

        private static void Postfix(
            InsulatedFlask __instance,
            ref InsulatedFlask.SaveData __result)
        {
            FlaskWaterHelper.EnsureWaterSaveData(__instance, __result);
        }
    }

    [HarmonyPatch(
        typeof(GearItem),
        "Deserialize",
        new System.Type[]
        {
            typeof(GearItemSaveDataProxy),
            typeof(bool)
        })]
    internal static class FlaskGearItemDeserializePatch
    {
        private static void Postfix(
            GearItem __instance,
            GearItemSaveDataProxy __0)
        {
            if (__instance == null ||
                __instance.m_InsulatedFlask == null ||
                __0 == null ||
                __0.m_InsulatedFlaskSerialized == null)
            {
                return;
            }

            FlaskWaterHelper.RestoreWaterAfterGearDeserialize(
                __instance,
                __0.m_InsulatedFlaskSerialized);
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), "GetFullnessLabelWithContents")]
    internal static class FlaskFullnessLabelPatch
    {
        private static void Postfix(
            InsulatedFlask __instance,
            ref string __result)
        {
            if (__instance == null)
                return;

            if (!FlaskLiquidRules.FlaskContainsWater(__instance))
                return;

            if (string.IsNullOrEmpty(__result))
                __result = "净水";
        }
    }
}
