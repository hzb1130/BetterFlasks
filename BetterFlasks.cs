#nullable disable
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(BetterFlasks.BetterFlasksMain), "BetterFlasks", "1.0.0", "hzb1130")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace BetterFlasks
{
    public class BetterFlasksMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Settings.OnLoad();
            Settings.OnSettingsChanged += () =>
            {
                var radial = InterfaceManager.GetPanel<Panel_ActionsRadial>();
                if (radial != null && radial.isActiveAndEnabled)
                    radial.Initialize();
            };
        }
    }

    internal static class FlaskHelper
    {
        internal static Dictionary<int, GearItem> drinkToFlaskMap = new Dictionary<int, GearItem>();

        public static bool HasContent(InsulatedFlask flask)
        {
            if (flask == null) return false;
            try
            {
                var items = new Il2CppSystem.Collections.Generic.List<GearItem>();
                flask.GetAllItems(items);
                return items.Count > 0;
            }
            catch { }
            return false;
        }

        public static Il2CppSystem.Collections.Generic.List<GearItem> GetItems(InsulatedFlask flask)
        {
            var result = new Il2CppSystem.Collections.Generic.List<GearItem>();
            if (flask == null) return result;
            try { flask.GetAllItems(result); } catch { }
            return result;
        }

        public static GearItem GetPreferredDrink(InsulatedFlask flask)
        {
            var items = GetItems(flask);
            if (items.Count == 0) return null;

            GearItem best = null;
            float bestCal = -1f;
            bool useHighest = Settings.options != null && Settings.options.flaskSelectionMode == 0;

            for (int i = 0; i < items.Count; i++)
            {
                GearItem gear = items[i];
                if (gear == null || gear.m_FoodItem == null) continue;
                float cal = gear.m_FoodItem.m_CaloriesRemaining;

                if (best == null)
                {
                    best = gear;
                    bestCal = cal;
                }
                else if (useHighest && cal > bestCal)
                {
                    best = gear;
                    bestCal = cal;
                }
                else if (!useHighest && cal < bestCal)
                {
                    best = gear;
                    bestCal = cal;
                }
            }

            return best;
        }

        public static void ConsumeFromFlask(InsulatedFlask flask)
        {
            GearItem preferred = GetPreferredDrink(flask);
            if (preferred == null)
            {
                GameAudioManager.PlayGUIError();
                HUDMessage.AddMessage("Flask is empty.");
                return;
            }

            FoodItem consumable = preferred.m_FoodItem;
            if (consumable == null) return;

            if (consumable.m_IsDrink && GameManager.GetThirstComponent().m_CurrentThirst / GameManager.GetThirstComponent().m_MaxThirst < 0.01f)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Youarenotthirsty"), false, true);
                GameAudioManager.PlayGUIError();
                return;
            }
            else if (!consumable.m_IsDrink && GameManager.GetHungerComponent().m_MaxReserveCalories - GameManager.GetHungerComponent().GetCalorieReserves() < 10f)
            {
                GameAudioManager.PlayGUIError();
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Youaretoofulltoeat"), false, true);
                return;
            }

            if (flask.TryRemoveItem(preferred))
            {
                int oldDrinkId = preferred.GetInstanceID();
                drinkToFlaskMap.Remove(oldDrinkId);

                PlayerManager pm = GameManager.GetPlayerManagerComponent();
                pm.UseFoodInventoryItem(preferred);
                if (!pm.ShouldDestroyFoodAfterEating(pm.m_FoodItemEaten))
                {
                    flask.TryAddItem(pm.m_FoodItemEaten);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), nameof(InsulatedFlask.CalculateHeatLoss))]
    internal class FlaskHeatLossPatch
    {
        private static void Prefix(ref float deltaTODHours)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.modifyFlaskHeatLoss)
                return;

            deltaTODHours *= Settings.options.flaskHeatLossMultiplier;
        }
    }

    [HarmonyPatch(typeof(Panel_ActionsRadial), nameof(Panel_ActionsRadial.GetDrinkItemsInInventory))]
    internal class AddFlaskToRadialPatch
    {
        private static void Postfix(Panel_ActionsRadial __instance, ref Il2CppSystem.Collections.Generic.List<GearItem> __result)
        {
            FlaskHelper.drinkToFlaskMap.Clear();

            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.showFlaskInRadial)
                return;

            if (__result == null)
                return;

            var inventory = GameManager.GetInventoryComponent();
            if (inventory == null)
                return;

            for (int i = 0; i < inventory.m_Items.Count; i++)
            {
                GearItem flaskGear = inventory.m_Items[i];
                if (flaskGear == null || flaskGear.m_InsulatedFlask == null) continue;
                if (flaskGear.m_InsulatedFlask.m_VolumeLitres <= ItemLiquidVolume.Zero) continue;

                GearItem drink = FlaskHelper.GetPreferredDrink(flaskGear.m_InsulatedFlask);
                if (drink == null) continue;
                if (__result.Contains(drink)) continue;

                int drinkId = drink.GetInstanceID();
                FlaskHelper.drinkToFlaskMap[drinkId] = flaskGear;

                int insertIndex = 0;
                for (int j = 0; j < __result.Count; j++)
                {
                    if (__result[j] != null && __result[j].m_WaterSupply != null)
                    {
                        insertIndex = j + 1;
                        break;
                    }
                }

                __result.Insert(insertIndex, drink);
            }
        }
    }

    [HarmonyPatch(typeof(RadialMenuArm), nameof(RadialMenuArm.SetRadialInfoGear))]
    internal class ReplaceFlaskDrinkIconPatch
    {
        private static void Postfix(RadialMenuArm __instance)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.showFlaskInRadial)
                return;

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
            if (flaskTexture == null)
                return;

            UITexture uiTexture = __instance.GetComponentInChildren<UITexture>(true);
            if (uiTexture != null)
            {
                uiTexture.mainTexture = flaskTexture;
            }
        }
    }

    [HarmonyPatch(typeof(Panel_ActionsRadial), nameof(Panel_ActionsRadial.UseItem))]
    internal class FlaskUseItemPatch
    {
        private static int lastDrinkId = -1;
        private static int lastFrame = -1;

        private static bool Prefix(ref GearItem gi)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.showFlaskInRadial)
                return true;

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

            FlaskHelper.ConsumeFromFlask(flaskGear.m_InsulatedFlask);

            var inv = InterfaceManager.GetPanel<Panel_Inventory>();
            if (inv != null) inv.UpdateFilteredInventoryList();

            return false;
        }
    }

    internal static class FlaskHeatStateHelper
    {
        private static readonly System.Reflection.MethodInfo IsNearFireMethod =
            AccessTools.Method(typeof(InsulatedFlask), "IsNearFire");

        public static bool IsNearFire(InsulatedFlask flask)
        {
            if (flask == null || IsNearFireMethod == null)
                return false;

            try
            {
                return (bool)IsNearFireMethod.Invoke(flask, null);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsIndoor()
        {
            PlayerManager pm = GameManager.GetPlayerManagerComponent();

            if (pm != null && pm.m_IndoorSpaceTrigger != null)
                return true;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return !GameManager.IsOutDoorsScene(sceneName);
        }

        public static string GetHeatTrendText(InsulatedFlask flask)
        {
            bool nearFire = IsNearFire(flask);
            bool isIndoor = IsIndoor();

            if (nearFire)
            {
                if (Settings.options != null && Settings.options.enableFireHeating)
                    return isIndoor ? " [ff9900]↑↑[-]" : " [ff9900]↑[-]";

                return " [-]";
            }

            return isIndoor ? " [66ccff]↓[-]" : " [3399ff]↓↓[-]";
        }
    }

    [HarmonyPatch(typeof(GearItem), "get_HoverText")]
    internal class FlaskHoverHeatTextPatch
    {
        private static void Postfix(GearItem __instance, ref string __result)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.showHeatText)
                return;

            if (__instance == null || __instance.m_InsulatedFlask == null)
                return;

            if (__instance.m_InPlayerInventory || __instance.IsInsideContainer())
                return;

            float heat = __instance.m_InsulatedFlask.GetHeatPercent();

            int heatPercent = Mathf.RoundToInt(Mathf.Clamp(heat, 0f, 100f));
            string heatColor = heatPercent <= 0 ? "3399ff" : "ff9900";
            string trendText = FlaskHeatStateHelper.GetHeatTrendText(__instance.m_InsulatedFlask);

            __result += $" [{heatColor}]{heatPercent}%[-]{trendText}";
        }
    }

    [HarmonyPatch(typeof(InsulatedFlask), nameof(InsulatedFlask.ManualUpdate))]
    internal class FlaskHeatGainNearFirePatch
    {
        private static void Postfix(InsulatedFlask __instance, float __0)
        {
            if (Settings.options == null || !Settings.options.enableFlaskMod || !Settings.options.enableFireHeating)
                return;

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

            float heatGain = heatGainPerMinute * deltaMinutes * Settings.options.fireHeatingMultiplier;
            float heatAfter = Mathf.Clamp(heatBefore + heatGain, 0f, 100f);

            __instance.m_HeatPercent = heatAfter;
        }
    }
}