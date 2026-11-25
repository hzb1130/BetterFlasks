#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    [HarmonyPatch(typeof(Panel_InsulatedFlask), "OnInventoryToContainer")]
    internal static class FlaskAddWaterPatch
    {
        private static bool Prefix(Panel_InsulatedFlask __instance)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod)
                return true;

            GearItem selected = FlaskWaterHelper.GetSelectedItem(__instance);
            if (selected == null)
                return true;

            InsulatedFlask flask = FlaskWaterHelper.GetFlask(__instance);

            if (FlaskLiquidRules.IsPotable(selected))
            {
                if (!Settings.options.enableHotWaterEffect)
                    return false;

                if (!FlaskLiquidRules.CanAddPotableWater(flask))
                    return false;

                FlaskWaterHelper.TransferWater(__instance, selected);
                return false;
            }

            if (FlaskLiquidRules.FlaskContainsWater(flask) &&
                selected.m_FoodItem != null &&
                selected.m_FoodItem.m_IsDrink)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Panel_InsulatedFlask), "OnContainerToInventory")]
    internal static class FlaskRemoveWaterPatch
    {
        private static bool Prefix(Panel_InsulatedFlask __instance)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod)
                return true;

            GearItem selected = FlaskWaterHelper.GetSelectedItem(__instance);
            if (!FlaskWaterHelper.IsPotableWater(selected))
                return true;

            if (!Settings.options.enableHotWaterEffect)
                return false;

            FlaskWaterHelper.RemoveWater(__instance, selected);
            return false;
        }
    }

    [HarmonyPatch(
        typeof(InsulatedFlask),
        "AddLiquid",
        new System.Type[]
        {
            typeof(LiquidType),
            typeof(ItemLiquidVolume),
            typeof(float)
        })]
    internal static class FlaskAddLiquidPatch
    {
        private static bool Prefix(
            InsulatedFlask __instance,
            LiquidType type,
            ItemLiquidVolume volumeLitres,
            float heatPercent,
            ref ItemLiquidVolume __result)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod)
                return true;

            LiquidType potable = FlaskWaterHelper.PotableWater;

            if (__instance == null ||
                type == null ||
                potable == null ||
                (type != potable && !type.Equals(potable)))
            {
                return true;
            }

            if (!Settings.options.enableHotWaterEffect)
            {
                __result = ItemLiquidVolume.Zero;
                return false;
            }

            ItemLiquidVolume current = __instance.GetVolumeLitres();
            ItemLiquidVolume capacity = __instance.GetCapacityLitres();
            ItemLiquidVolume remaining = capacity - current;
            ItemLiquidVolume actual = volumeLitres <= remaining
                ? volumeLitres
                : remaining;

            if (actual <= ItemLiquidVolume.Zero)
            {
                __result = ItemLiquidVolume.Zero;
                return false;
            }

            float mixedHeatPercent =
                FlaskWaterHelper.CalculateMixedHeatPercent(
                    current,
                    __instance.GetHeatPercent(),
                    actual);
            ItemLiquidVolume newVolume = current + actual;
            GearItem content = FlaskWaterHelper.EnsureWaterContent(
                __instance,
                null,
                newVolume);

            __instance.m_LiquidType = potable;
            __instance.m_VolumeLitres = newVolume;
            __instance.m_HeatPercent = mixedHeatPercent;

            if (content != null && content.m_WaterSupply != null)
            {
                FlaskWaterHelper.SetWaterVolume(
                    content.m_WaterSupply,
                    newVolume);
            }

            FlaskWaterHelper.SyncFlask(__instance);
            __result = actual;
            return false;
        }
    }

    [HarmonyPatch(typeof(Panel_InsulatedFlask), "IsCompatibleDrink")]
    internal static class FlaskCompatibilityPatch
    {
        private static void Postfix(
            Panel_InsulatedFlask __instance,
            GearItem __0,
            ref bool __result)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod)
                return;

            bool isWater = FlaskWaterHelper.IsPotableWater(__0);
            InsulatedFlask flask = FlaskWaterHelper.GetFlask(__instance);
            bool flaskHasWater =
                FlaskLiquidRules.FlaskContainsWater(flask);

            if (isWater)
            {
                __result = Settings.options.enableHotWaterEffect &&
                           FlaskLiquidRules.CanAddPotableWater(flask);
            }
            else if (flaskHasWater)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(
        typeof(PlayerManager),
        nameof(PlayerManager.OnDrinkWaterComplete))]
    internal static class FlaskDrinkWaterCompletePatch
    {
        private static void Postfix()
        {
            FlaskHelper.SyncPendingWaterDrink();
        }
    }
}
