#nullable disable
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    [HarmonyPatch(
        typeof(Panel_ActionsRadial),
        nameof(Panel_ActionsRadial.GetDrinkItemsInInventory))]
    internal class AddFlaskToRadialPatch
    {
        private static void Postfix(
            Panel_ActionsRadial __instance,
            ref Il2CppSystem.Collections.Generic.List<GearItem> __result)
        {
            FlaskHelper.drinkToFlaskMap.Clear();

            if (Settings.options == null ||
                !Settings.options.enableFlaskMod ||
                __result == null)
            {
                return;
            }

            var inventory = GameManager.GetInventoryComponent();
            if (inventory == null || inventory.m_Items == null)
                return;

            for (int i = 0; i < inventory.m_Items.Count; i++)
            {
                GearItem flaskGear = inventory.m_Items[i];
                if (flaskGear == null ||
                    flaskGear.m_InsulatedFlask == null ||
                    flaskGear.m_InsulatedFlask.m_VolumeLitres <= ItemLiquidVolume.Zero)
                {
                    continue;
                }

                InsulatedFlask flask = flaskGear.m_InsulatedFlask;
                GearItem radialItem = FlaskHelper.GetPreferredDrink(flask);

                if (radialItem == null)
                {
                    var flaskItems = FlaskHelper.GetItems(flask);
                    for (int j = 0; j < flaskItems.Count; j++)
                    {
                        GearItem item = flaskItems[j];
                        if (item != null &&
                            item.m_WaterSupply != null &&
                            FlaskWaterHelper.IsPotableWater(item))
                        {
                            radialItem = item;
                            break;
                        }
                    }
                }

                if (radialItem == null || __result.Contains(radialItem))
                    continue;

                FlaskHelper.drinkToFlaskMap[radialItem.GetInstanceID()] = flaskGear;

                int insertIndex = 0;
                for (int j = 0; j < __result.Count; j++)
                {
                    if (__result[j] != null && __result[j].m_WaterSupply != null)
                    {
                        insertIndex = j + 1;
                        break;
                    }
                }

                __result.Insert(insertIndex, radialItem);
            }
        }
    }

    [HarmonyPatch(typeof(RadialMenuArm), nameof(RadialMenuArm.SetRadialInfoGear))]
    internal class ReplaceFlaskDrinkIconPatch
    {
        private static void Postfix(RadialMenuArm __instance)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod)
            {
                return;
            }

            if (__instance == null || __instance.m_GearItem == null)
                return;

            int drinkId = __instance.m_GearItem.GetInstanceID();
            GearItem flaskGear;
            if (!FlaskHelper.drinkToFlaskMap.TryGetValue(drinkId, out flaskGear))
                return;

            if (flaskGear == null || flaskGear.m_InsulatedFlask == null)
            {
                FlaskHelper.drinkToFlaskMap.Remove(drinkId);
                return;
            }

            Texture2D flaskTexture = flaskGear.GetInventoryIconTexture();
            if (flaskTexture != null)
            {
                UITexture uiTexture =
                    __instance.GetComponentInChildren<UITexture>(true);
                if (uiTexture != null)
                    uiTexture.mainTexture = flaskTexture;
            }
        }
    }

    [HarmonyPatch(typeof(Panel_ActionsRadial), "UpdateDisplayText")]
    internal static class FlaskRadialConditionHeatTextPatch
    {
        private static string lastConditionBaseText;
        private static string lastConditionRenderedText;
        private static string lastCenteredBaseText;
        private static string lastCenteredRenderedText;
        private static UISprite overriddenConditionSprite;
        private static string originalConditionSpriteName;
        private static Color originalConditionSpriteColor;
        private static UISprite overriddenCenteredSprite;
        private static string originalCenteredSpriteName;
        private static Color originalCenteredSpriteColor;

        private static void Postfix(Panel_ActionsRadial __instance)
        {
            if (__instance == null)
                return;

            if (Settings.options == null ||
                !Settings.options.enableFlaskMod)
            {
                RestoreConditionDisplay(__instance);
                return;
            }

            GearItem radialItem = GetHoveredGearItem(__instance);
            GearItem flaskGear;
            if (radialItem == null ||
                !FlaskWaterHelper.IsPotableWater(radialItem) ||
                !FlaskHelper.drinkToFlaskMap.TryGetValue(
                    radialItem.GetInstanceID(),
                    out flaskGear) ||
                flaskGear == null ||
                flaskGear.m_InsulatedFlask == null)
            {
                RestoreConditionDisplay(__instance);
                return;
            }

            float heat = flaskGear.m_InsulatedFlask.GetHeatPercent();
            if (heat <= 0f)
            {
                RestoreConditionDisplay(__instance);
                return;
            }

            int heatPercent = Mathf.Max(
                1,
                Mathf.RoundToInt(Mathf.Clamp(heat, 0f, 100f)));
            ApplyConditionLabel(
                __instance.m_GearConditionLabel,
                heatPercent,
                ref lastConditionBaseText,
                ref lastConditionRenderedText);
            ApplyConditionLabel(
                __instance.m_GearConditionCenteredLabel,
                heatPercent,
                ref lastCenteredBaseText,
                ref lastCenteredRenderedText);

            string temperatureSpriteName =
                string.IsNullOrEmpty(__instance.m_FoodHotStatusIcon)
                    ? "ico_units_temperature"
                    : __instance.m_FoodHotStatusIcon;
            Color temperatureColor = new Color(1f, 0.6f, 0f, 1f);
            ApplyConditionSprite(
                __instance.m_GearConditionSprite,
                temperatureSpriteName,
                temperatureColor,
                ref overriddenConditionSprite,
                ref originalConditionSpriteName,
                ref originalConditionSpriteColor);
            ApplyConditionSprite(
                __instance.m_GearConditionCenteredSprite,
                temperatureSpriteName,
                temperatureColor,
                ref overriddenCenteredSprite,
                ref originalCenteredSpriteName,
                ref originalCenteredSpriteColor);
        }

        private static GearItem GetHoveredGearItem(
            Panel_ActionsRadial panel)
        {
            if (panel.m_RadialArms != null)
            {
                for (int i = 0; i < panel.m_RadialArms.Length; i++)
                {
                    RadialMenuArm arm = panel.m_RadialArms[i];
                    if (arm != null &&
                        arm.m_IsHoveredOver &&
                        arm.m_GearItem != null)
                    {
                        return arm.m_GearItem;
                    }
                }
            }

            return panel.m_GearItemBeingInspected;
        }

        private static void ApplyConditionLabel(
            UILabel label,
            int heatPercent,
            ref string lastBaseText,
            ref string lastRenderedText)
        {
            if (label == null)
                return;

            string currentText = label.text ?? string.Empty;
            string baseText = currentText == lastRenderedText
                ? lastBaseText
                : currentText;
            string renderedText = $"[ff9900]{heatPercent}%[-]";
            label.text = renderedText;
            lastBaseText = baseText;
            lastRenderedText = renderedText;
        }

        private static void RestoreConditionDisplay(
            Panel_ActionsRadial panel)
        {
            RestoreConditionLabel(
                panel.m_GearConditionLabel,
                ref lastConditionBaseText,
                ref lastConditionRenderedText);
            RestoreConditionLabel(
                panel.m_GearConditionCenteredLabel,
                ref lastCenteredBaseText,
                ref lastCenteredRenderedText);
            RestoreConditionSprite(
                ref overriddenConditionSprite,
                ref originalConditionSpriteName,
                ref originalConditionSpriteColor);
            RestoreConditionSprite(
                ref overriddenCenteredSprite,
                ref originalCenteredSpriteName,
                ref originalCenteredSpriteColor);
        }

        private static void RestoreConditionLabel(
            UILabel label,
            ref string lastBaseText,
            ref string lastRenderedText)
        {
            if (label != null && label.text == lastRenderedText)
                label.text = lastBaseText;

            lastRenderedText = null;
        }

        private static void ApplyConditionSprite(
            UISprite sprite,
            string spriteName,
            Color color,
            ref UISprite overriddenSprite,
            ref string originalSpriteName,
            ref Color originalSpriteColor)
        {
            if (sprite == null)
                return;

            if (overriddenSprite != sprite)
            {
                RestoreConditionSprite(
                    ref overriddenSprite,
                    ref originalSpriteName,
                    ref originalSpriteColor);
                overriddenSprite = sprite;
                originalSpriteName = sprite.spriteName;
                originalSpriteColor = sprite.color;
            }

            sprite.spriteName = spriteName;
            sprite.color = color;
        }

        private static void RestoreConditionSprite(
            ref UISprite overriddenSprite,
            ref string originalSpriteName,
            ref Color originalSpriteColor)
        {
            if (overriddenSprite != null)
            {
                overriddenSprite.spriteName = originalSpriteName;
                overriddenSprite.color = originalSpriteColor;
            }

            overriddenSprite = null;
            originalSpriteName = null;
        }
    }

    [HarmonyPatch(typeof(Panel_ActionsRadial), nameof(Panel_ActionsRadial.UseItem))]
    internal class FlaskUseItemPatch
    {
        private static int lastDrinkId = -1;
        private static int lastFrame = -1;

        private static bool Prefix(ref GearItem gi)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod)
            {
                return true;
            }

            if (gi == null)
                return true;

            int drinkId = gi.GetInstanceID();
            GearItem flaskGear;
            if (!FlaskHelper.drinkToFlaskMap.TryGetValue(drinkId, out flaskGear))
                return true;

            if (flaskGear == null || flaskGear.m_InsulatedFlask == null)
            {
                FlaskHelper.drinkToFlaskMap.Remove(drinkId);
                return true;
            }

            if (lastDrinkId == drinkId && lastFrame == Time.frameCount)
                return false;

            lastDrinkId = drinkId;
            lastFrame = Time.frameCount;

            FlaskHelper.ConsumeFromFlask(
                flaskGear.m_InsulatedFlask,
                gi);

            var inventory = InterfaceManager.GetPanel<Panel_Inventory>();
            if (inventory != null)
                inventory.UpdateFilteredInventoryList();

            return false;
        }
    }
}
