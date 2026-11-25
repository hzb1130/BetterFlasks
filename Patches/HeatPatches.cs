#nullable disable
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    [HarmonyPatch(typeof(InsulatedFlask), nameof(InsulatedFlask.CalculateHeatLoss))]
    internal class FlaskHeatLossPatch
    {
        private static void Prefix(ref float deltaTODHours)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod ||
                !Settings.options.modifyFlaskHeatLoss)
            {
                return;
            }

            deltaTODHours *= Settings.options.flaskHeatLossMultiplier;
        }
    }

    [HarmonyPatch(typeof(GearItem), "get_HoverText")]
    internal class FlaskHoverHeatTextPatch
    {
        private static void Postfix(GearItem __instance, ref string __result)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod ||
                !Settings.options.showHeatText)
            {
                return;
            }

            if (__instance == null || __instance.m_InsulatedFlask == null)
                return;

            if (__instance.m_InPlayerInventory || __instance.IsInsideContainer())
                return;

            float heat = __instance.m_InsulatedFlask.GetHeatPercent();
            int heatPercent = Mathf.RoundToInt(Mathf.Clamp(heat, 0f, 100f));
            string heatColor = heatPercent <= 0 ? "3399ff" : "ff9900";
            string trendText =
                FlaskHeatStateHelper.GetHeatTrendText(__instance.m_InsulatedFlask);

            __result += $" [{heatColor}]{heatPercent}%[-]{trendText}";
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), nameof(InsulatedFlask.ManualUpdate))]
    internal class FlaskHeatGainNearFirePatch
    {
        private static void Postfix(InsulatedFlask __instance, float __0)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod ||
                !Settings.options.enableFireHeating)
            {
                return;
            }

            if (__instance == null || __instance.GearItem == null)
                return;

            GearItem gear = __instance.GearItem;
            if (gear.m_InPlayerInventory || gear.IsInsideContainer())
                return;

            if (!FlaskHeatStateHelper.IsNearFire(__instance))
                return;

            if (__instance.GetVolumeLitres() <= ItemLiquidVolume.Zero)
                return;

            float heatBefore = __instance.GetHeatPercent();
            if (heatBefore >= 100f)
                return;

            float deltaTODHours = __0;
            float deltaMinutes = deltaTODHours * 60f;
            bool isIndoor = FlaskHeatStateHelper.IsIndoor();

            float heatGainPerMinute = isIndoor
                ? __instance.m_PercentHeatLossPerMinuteOutdoors
                : __instance.m_PercentHeatLossPerMinuteIndoors;

            float heatGain =
                heatGainPerMinute *
                deltaMinutes *
                Settings.options.fireHeatingMultiplier;

            float heatAfter = Mathf.Clamp(heatBefore + heatGain, 0f, 100f);
            __instance.m_HeatPercent = heatAfter;
        }
    }
}
