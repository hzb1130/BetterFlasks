#nullable disable
using UnityEngine;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using System.Collections.Generic;

namespace BetterFlasks
{
    internal static class FlaskHelper
    {
        private const float WaterLitersPerDrink = 0.1f;

        internal static readonly Dictionary<int, GearItem> drinkToFlaskMap =
            new Dictionary<int, GearItem>();

        private static InsulatedFlask pendingWaterFlask;
        private static GearItem pendingWaterItem;
        private static ItemLiquidVolume pendingWaterVolume;
        private static float pendingWaterHeatPercent;

        public static bool HasContent(InsulatedFlask flask)
        {
            if (flask == null)
                return false;

            try
            {
                var items = new Il2CppSystem.Collections.Generic.List<GearItem>();
                flask.GetAllItems(items);
                return items.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public static Il2CppSystem.Collections.Generic.List<GearItem> GetItems(InsulatedFlask flask)
        {
            var result = new Il2CppSystem.Collections.Generic.List<GearItem>();
            if (flask == null)
                return result;

            try
            {
                flask.GetAllItems(result);
            }
            catch
            {
            }

            return result;
        }

        public static GearItem GetPreferredDrink(InsulatedFlask flask)
        {
            var items = GetItems(flask);
            if (items.Count == 0)
                return null;

            GearItem best = null;
            float bestCal = -1f;
            bool useHighest = Settings.options != null &&
                              Settings.options.flaskSelectionMode == 0;

            for (int i = 0; i < items.Count; i++)
            {
                GearItem gear = items[i];
                if (gear == null || gear.m_FoodItem == null)
                    continue;

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

        public static void ConsumeFromFlask(
            InsulatedFlask flask,
            GearItem selectedItem = null)
        {
            if (TryDrinkWaterFromFlask(flask, selectedItem))
                return;

            GearItem preferred = GetPreferredDrink(flask);
            if (preferred == null)
            {
                GameAudioManager.PlayGUIError();
                HUDMessage.AddMessage("Flask is empty.");
                return;
            }

            FoodItem consumable = preferred.m_FoodItem;
            if (consumable == null)
                return;

            if (consumable.m_IsDrink &&
                GameManager.GetThirstComponent().m_CurrentThirst /
                GameManager.GetThirstComponent().m_MaxThirst < 0.01f)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Youarenotthirsty"), false, true);
                GameAudioManager.PlayGUIError();
                return;
            }
            else if (!consumable.m_IsDrink &&
                     GameManager.GetHungerComponent().m_MaxReserveCalories -
                     GameManager.GetHungerComponent().GetCalorieReserves() < 10f)
            {
                GameAudioManager.PlayGUIError();
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Youaretoofulltoeat"), false, true);
                return;
            }

            if (flask.TryRemoveItem(preferred))
            {
                int oldDrinkId = preferred.GetInstanceID();
                drinkToFlaskMap.Remove(oldDrinkId);

                PlayerManager player = GameManager.GetPlayerManagerComponent();
                player.UseFoodInventoryItem(preferred);
                if (!player.ShouldDestroyFoodAfterEating(player.m_FoodItemEaten))
                {
                    flask.TryAddItem(player.m_FoodItemEaten);
                }
            }
        }

        private static bool TryDrinkWaterFromFlask(
            InsulatedFlask flask,
            GearItem selectedItem)
        {
            if (flask == null ||
                selectedItem == null ||
                selectedItem.m_WaterSupply == null ||
                !FlaskWaterHelper.IsPotableWater(selectedItem))
            {
                return false;
            }

            PlayerManager player = GameManager.GetPlayerManagerComponent();
            if (player == null)
                return true;

            ItemLiquidVolume available =
                FlaskWaterHelper.GetWaterVolume(selectedItem.m_WaterSupply);
            ItemLiquidVolume flaskVolume = flask.GetVolumeLitres();

            if (available <= ItemLiquidVolume.Zero &&
                flaskVolume > ItemLiquidVolume.Zero)
            {
                available = flaskVolume;
                FlaskWaterHelper.SetWaterVolume(
                    selectedItem.m_WaterSupply,
                    available);
            }

            if (available <= ItemLiquidVolume.Zero)
            {
                GameAudioManager.PlayGUIError();
                HUDMessage.AddMessage("Flask is empty.");
                return true;
            }

            pendingWaterFlask = flask;
            pendingWaterItem = selectedItem;
            pendingWaterVolume = available;
            pendingWaterHeatPercent = flask.GetHeatPercent();

            ItemLiquidVolume volumeToDrink = ItemLiquidVolume.Min(
                available,
                ItemLiquidVolume.FromLiters(WaterLitersPerDrink));

            bool started;
            try
            {
                started = player.DrinkFromWaterSupply(
                    selectedItem.m_WaterSupply,
                    volumeToDrink);
            }
            catch (System.Exception exception)
            {
                ClearPendingWaterDrink();
                MelonLogger.Error(
                    $"Failed to drink water from flask: {exception}");
                return true;
            }

            if (!started)
                ClearPendingWaterDrink();

            return true;
        }

        internal static void SyncPendingWaterDrink()
        {
            InsulatedFlask flask = pendingWaterFlask;
            GearItem waterItem = pendingWaterItem;
            ItemLiquidVolume volumeBefore = pendingWaterVolume;
            float heatPercentBeforeDrink = pendingWaterHeatPercent;

            ClearPendingWaterDrink();

            if (flask == null ||
                waterItem == null ||
                waterItem.m_WaterSupply == null)
            {
                return;
            }

            ItemLiquidVolume remaining =
                FlaskWaterHelper.GetWaterVolume(waterItem.m_WaterSupply);

            if (remaining <= ItemLiquidVolume.Zero)
            {
                remaining = ItemLiquidVolume.Zero;

                if (flask.m_Items != null)
                {
                    for (int i = flask.m_Items.Count - 1; i >= 0; i--)
                    {
                        GearItem item = flask.m_Items[i];
                        if (item == waterItem)
                            flask.m_Items.RemoveAt(i);
                    }
                }

                flask.m_LiquidType = null;
                flask.m_HeatPercent = 0f;
                drinkToFlaskMap.Remove(waterItem.GetInstanceID());
            }
            else
            {
                flask.m_LiquidType = FlaskWaterHelper.PotableWater;
            }

            flask.m_VolumeLitres = remaining;

            ItemLiquidVolume volumeConsumed = volumeBefore - remaining;
            if (volumeConsumed > ItemLiquidVolume.Zero)
            {
                HotWaterDrinkEffect.OnHotWaterConsumed(
                    flask,
                    waterItem,
                    heatPercentBeforeDrink,
                    volumeConsumed);
            }

            FlaskWaterHelper.SyncFlask(flask);

            Panel_Inventory inventory =
                InterfaceManager.GetPanel<Panel_Inventory>();
            if (inventory != null)
                inventory.UpdateFilteredInventoryList();
        }

        private static void ClearPendingWaterDrink()
        {
            pendingWaterFlask = null;
            pendingWaterItem = null;
            pendingWaterVolume = ItemLiquidVolume.Zero;
            pendingWaterHeatPercent = 0f;
        }
    }
}
