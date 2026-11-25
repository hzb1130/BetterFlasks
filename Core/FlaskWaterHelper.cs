#nullable disable
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    internal static class FlaskWaterHelper
    {
        private static readonly System.Reflection.MethodInfo SelectedItemMethod =
            AccessTools.Method(typeof(Panel_InsulatedFlask), "GetCurrentlySelectedGearItem");

        private static readonly System.Reflection.MethodInfo RefreshTablesMethod =
            AccessTools.Method(typeof(Panel_InsulatedFlask), "RefreshTables");

        private static readonly System.Reflection.MethodInfo RefreshWeightMethod =
            AccessTools.Method(typeof(Panel_InsulatedFlask), "RefreshWeight");

        internal const string WaterPrefabName = "GEAR_WaterSupplyPotable";

        internal static LiquidType PotableWater
        {
            get
            {
                try
                {
                    return LiquidType.GetPotableWater();
                }
                catch
                {
                    return null;
                }
            }
        }

        internal static InsulatedFlask GetFlask(Panel_InsulatedFlask panel)
        {
            return panel == null ? null : panel.m_InsulatedFlask;
        }

        internal static GearItem GetSelectedItem(Panel_InsulatedFlask panel)
        {
            if (panel == null || SelectedItemMethod == null)
                return null;

            try
            {
                return SelectedItemMethod.Invoke(panel, null) as GearItem;
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsPotableWater(GearItem item)
        {
            if (item == null || item.m_WaterSupply == null)
                return false;

            LiquidType sourceType = ReadWaterType(item);
            LiquidType potable = PotableWater;

            return sourceType != null &&
                   potable != null &&
                   (sourceType == potable || sourceType.Equals(potable));
        }

        internal static LiquidType ReadWaterType(GearItem item)
        {
            if (item == null || item.m_WaterSupply == null)
                return null;

            object value = ReadMember(item.m_WaterSupply, "m_LiquidType");
            return value as LiquidType;
        }

        internal static ItemLiquidVolume GetWaterVolume(WaterSupply supply)
        {
            if (supply == null)
                return ItemLiquidVolume.Zero;

            try
            {
                object value = ReadMember(supply, "m_VolumeInLiters");
                if (value is ItemLiquidVolume)
                    return (ItemLiquidVolume)value;
            }
            catch
            {
            }

            return ItemLiquidVolume.Zero;
        }

        internal static bool SetWaterVolume(WaterSupply supply, ItemLiquidVolume volume)
        {
            if (supply == null)
                return false;

            return WriteMember(supply, "m_VolumeInLiters", volume);
        }

        internal static float CalculateMixedHeatPercent(
            ItemLiquidVolume currentVolume,
            float currentHeatPercent,
            ItemLiquidVolume addedVolume)
        {
            float currentLiters = Mathf.Max(
                0f,
                currentVolume / ItemLiquidVolume.Liter);
            float addedLiters = Mathf.Max(
                0f,
                addedVolume / ItemLiquidVolume.Liter);
            float totalLiters = currentLiters + addedLiters;

            if (totalLiters <= 0f)
                return 0f;

            float currentHeat = Mathf.Clamp(currentHeatPercent, 0f, 100f);
            return Mathf.Clamp(
                currentLiters * currentHeat / totalLiters,
                0f,
                100f);
        }

        private static bool SaveDataMentionsWater(
            InsulatedFlask.SaveData saveData)
        {
            if (saveData == null)
                return false;

            if (!string.IsNullOrEmpty(saveData.m_LiquidTypeName) &&
                saveData.m_LiquidTypeName.IndexOf(
                    "Water",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (saveData.m_SerializedItems == null)
                return false;

            for (int i = 0; i < saveData.m_SerializedItems.Count; i++)
            {
                InsulatedFlask.ItemSaveData itemData =
                    saveData.m_SerializedItems[i];
                if (itemData != null &&
                    !string.IsNullOrEmpty(itemData.m_PrefabName) &&
                    itemData.m_PrefabName.IndexOf(
                        "WaterSupply",
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null)
                return null;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            System.Type type = target.GetType();
            while (type != null)
            {
                System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
                if (property != null)
                {
                    try
                    {
                        return property.GetValue(target, null);
                    }
                    catch
                    {
                    }
                }

                System.Reflection.FieldInfo field = type.GetField(name, flags);
                if (field != null)
                {
                    try
                    {
                        return field.GetValue(target);
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool WriteMember(object target, string name, object value)
        {
            if (target == null)
                return false;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            System.Type type = target.GetType();
            while (type != null)
            {
                System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        property.SetValue(target, value, null);
                        return true;
                    }
                    catch
                    {
                    }
                }

                System.Reflection.FieldInfo field = type.GetField(name, flags);
                if (field != null)
                {
                    try
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static GearItemSaveDataProxy SerializeGearItem(GearItem source)
        {
            try
            {
                System.Reflection.MethodInfo serialize = AccessTools.Method(
                    typeof(GearItem),
                    "Serialize",
                    new System.Type[] { typeof(bool) });

                if (serialize != null && source != null)
                {
                    return serialize.Invoke(
                        source,
                        new object[] { false }) as GearItemSaveDataProxy;
                }
            }
            catch
            {
            }

            return null;
        }

        private static GearItem SerializeAndCloneWater(GearItem source)
        {
            GearItemSaveDataProxy proxy = SerializeGearItem(source);
            GearItem clone = InstantiateWater(proxy);
            if (clone != null)
                return clone;

            return InstantiateEmptyWater();
        }

        private static GearItem InstantiateWater(
            GearItemSaveDataProxy proxy)
        {
            try
            {
                System.Reflection.MethodInfo instantiate = AccessTools.Method(
                    typeof(GearItem),
                    "InstantiateAndDeserializeGearItem",
                    new System.Type[]
                    {
                        typeof(string),
                        typeof(GearItemSaveDataProxy),
                        typeof(Transform),
                        typeof(bool),
                        typeof(bool)
                    });

                if (instantiate != null && proxy != null)
                {
                    return instantiate.Invoke(
                        null,
                        new object[]
                        {
                            WaterPrefabName,
                            proxy,
                            null,
                            false,
                            false
                        }) as GearItem;
                }
            }
            catch
            {
            }

            return null;
        }

        private static GearItem InstantiateEmptyWater()
        {
            try
            {
                System.Reflection.MethodInfo instantiate = AccessTools.Method(
                    typeof(GearItem),
                    "InstantiateGearItem",
                    new System.Type[] { typeof(string) });

                if (instantiate != null)
                {
                    return instantiate.Invoke(
                        null,
                        new object[] { WaterPrefabName }) as GearItem;
                }
            }
            catch
            {
            }

            return null;
        }

        private static GearItem FindWaterContent(InsulatedFlask flask)
        {
            if (flask == null || flask.m_Items == null)
                return null;

            LiquidType potable = PotableWater;
            for (int i = 0; i < flask.m_Items.Count; i++)
            {
                GearItem item = flask.m_Items[i];
                if (item == null || item.m_WaterSupply == null)
                    continue;

                LiquidType type = ReadWaterType(item);
                if (type != null &&
                    potable != null &&
                    (type == potable || type.Equals(potable)))
                {
                    return item;
                }
            }

            return null;
        }

        internal static void NormalizeWaterSaveData(
            InsulatedFlask.SaveData saveData)
        {
            if (saveData == null || saveData.m_SerializedItems == null)
                return;

            for (int i = 0; i < saveData.m_SerializedItems.Count; i++)
            {
                InsulatedFlask.ItemSaveData itemData =
                    saveData.m_SerializedItems[i];

                if (itemData == null ||
                    string.IsNullOrEmpty(itemData.m_PrefabName) ||
                    itemData.m_PrefabName.IndexOf(
                        WaterPrefabName,
                        System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                itemData.m_PrefabName = WaterPrefabName;
            }
        }

        internal static void EnsureWaterSaveData(
            InsulatedFlask flask,
            InsulatedFlask.SaveData saveData)
        {
            if (flask == null || saveData == null)
                return;

            GearItem content = FindWaterContent(flask);
            LiquidType potable = PotableWater;
            bool declaresWater = flask.m_LiquidType != null &&
                                 potable != null &&
                                 (flask.m_LiquidType == potable ||
                                  flask.m_LiquidType.Equals(potable));

            if (content == null && !declaresWater)
                return;

            if (content == null || content.m_WaterSupply == null)
                return;

            GearItemSaveDataProxy serializedGear = SerializeGearItem(content);
            if (serializedGear == null)
                return;

            if (saveData.m_SerializedItems == null)
            {
                saveData.m_SerializedItems =
                    new Il2CppSystem.Collections.Generic.List<
                        InsulatedFlask.ItemSaveData>();
            }

            InsulatedFlask.ItemSaveData waterData = null;
            for (int i = 0; i < saveData.m_SerializedItems.Count; i++)
            {
                InsulatedFlask.ItemSaveData itemData =
                    saveData.m_SerializedItems[i];

                if (itemData != null &&
                    !string.IsNullOrEmpty(itemData.m_PrefabName) &&
                    itemData.m_PrefabName.IndexOf(
                        WaterPrefabName,
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    waterData = itemData;
                    break;
                }
            }

            if (waterData == null)
            {
                waterData = new InsulatedFlask.ItemSaveData();
                saveData.m_SerializedItems.Add(waterData);
            }

            waterData.m_PrefabName = WaterPrefabName;
            waterData.m_SerializedGear = serializedGear;
        }

        internal static bool RestoreWaterContentFromItems(
            InsulatedFlask flask)
        {
            GearItem content = FindWaterContent(flask);
            if (content == null || content.m_WaterSupply == null)
                return false;

            ItemLiquidVolume volume = GetWaterVolume(content.m_WaterSupply);
            if (volume <= ItemLiquidVolume.Zero)
                return false;

            volume = ItemLiquidVolume.Min(volume, flask.GetCapacityLitres());
            SetWaterVolume(content.m_WaterSupply, volume);

            flask.m_LiquidType = PotableWater;
            flask.m_VolumeLitres = volume;
            InvokePrivate(flask, "UpdateWeight");
            return true;
        }

        internal static bool RestoreWaterContentFromSaveData(
            InsulatedFlask flask,
            InsulatedFlask.SaveData saveData)
        {
            if (!SaveDataMentionsWater(saveData) &&
                FindWaterContent(flask) == null)
            {
                return false;
            }

            if (RestoreWaterContentFromItems(flask))
                return true;

            if (flask == null ||
                saveData == null ||
                saveData.m_SerializedItems == null)
            {
                return false;
            }

            for (int i = 0; i < saveData.m_SerializedItems.Count; i++)
            {
                InsulatedFlask.ItemSaveData itemData =
                    saveData.m_SerializedItems[i];

                if (itemData == null ||
                    itemData.m_SerializedGear == null ||
                    string.IsNullOrEmpty(itemData.m_PrefabName) ||
                    itemData.m_PrefabName.IndexOf(
                        WaterPrefabName,
                        System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                GearItem content = InstantiateWater(itemData.m_SerializedGear);
                if (content == null)
                    return false;

                if (flask.m_Items == null)
                {
                    flask.m_Items =
                        new Il2CppSystem.Collections.Generic.List<GearItemObject>();
                }

                flask.m_Items.Add(new GearItemObject(content));
                return RestoreWaterContentFromItems(flask);
            }

            return false;
        }

        internal static void RestoreWaterAfterGearDeserialize(
            GearItem gear,
            InsulatedFlask.SaveData saveData)
        {
            if (gear == null || gear.m_InsulatedFlask == null)
                return;

            if (RestoreWaterContentFromSaveData(
                    gear.m_InsulatedFlask,
                    saveData))
            {
                MelonCoroutines.Start(
                    RestoreWaterAfterLoadFrames(gear, saveData));
            }
        }

        private static System.Collections.IEnumerator RestoreWaterAfterLoadFrames(
            GearItem gear,
            InsulatedFlask.SaveData saveData)
        {
            yield return null;
            yield return null;

            if (gear == null || gear.m_InsulatedFlask == null)
                yield break;

            RestoreWaterContentFromSaveData(
                gear.m_InsulatedFlask,
                saveData);
        }

        internal static GearItem EnsureWaterContent(
            InsulatedFlask flask,
            GearItem template,
            ItemLiquidVolume volume)
        {
            if (flask == null || flask.m_Items == null)
                return null;

            GearItem content = FindWaterContent(flask);
            if (content == null)
            {
                content = SerializeAndCloneWater(template);
                if (content == null)
                    return null;

                GearItemObject contentObject = new GearItemObject(content);
                flask.m_Items.Add(contentObject);
            }

            if (content.m_WaterSupply != null)
                SetWaterVolume(content.m_WaterSupply, volume);

            return content;
        }

        private static void InvokePrivate(object instance, string methodName)
        {
            if (instance == null)
                return;

            try
            {
                System.Reflection.MethodInfo method =
                    AccessTools.Method(instance.GetType(), methodName);

                if (method != null)
                    method.Invoke(instance, null);
            }
            catch
            {
            }
        }

        internal static void SyncFlask(InsulatedFlask flask)
        {
            if (flask == null)
                return;

            InvokePrivate(flask, "UpdateCachedValues");
            InvokePrivate(flask, "UpdateVolume");
            InvokePrivate(flask, "UpdateWeight");

            try
            {
                flask.Serialize();
            }
            catch
            {
            }
        }

        internal static void RefreshPanel(Panel_InsulatedFlask panel)
        {
            if (panel == null)
                return;

            try
            {
                panel.UpdateInsulatedFlaskList();
                panel.UpdateInventoryList();

                if (RefreshWeightMethod != null)
                    RefreshWeightMethod.Invoke(panel, null);

                if (RefreshTablesMethod != null)
                    RefreshTablesMethod.Invoke(panel, null);
            }
            catch
            {
            }
        }

        internal static void RefreshPanelNextFrame(Panel_InsulatedFlask panel)
        {
            if (panel == null)
                return;

            MelonCoroutines.Start(RefreshPanelAfterFrame(panel));
        }

        private static System.Collections.IEnumerator RefreshPanelAfterFrame(
            Panel_InsulatedFlask panel)
        {
            yield return null;

            if (panel != null)
                RefreshPanel(panel);
        }

        internal static bool TransferWater(
            Panel_InsulatedFlask panel,
            GearItem sourceItem)
        {
            if (panel == null ||
                sourceItem == null ||
                sourceItem.m_WaterSupply == null)
            {
                return false;
            }

            if (!IsPotableWater(sourceItem))
                return false;

            InsulatedFlask flask = GetFlask(panel);
            PlayerManager player = GameManager.GetPlayerManagerComponent();

            LiquidType potable = PotableWater;
            if (flask == null || player == null || potable == null)
                return false;

            ItemLiquidVolume current = flask.GetVolumeLitres();
            ItemLiquidVolume capacity = flask.GetCapacityLitres();
            ItemLiquidVolume sourceVolume = GetWaterVolume(sourceItem.m_WaterSupply);
            ItemLiquidVolume remaining = capacity - current;
            ItemLiquidVolume requested = sourceVolume <= remaining
                ? sourceVolume
                : remaining;

            if (requested <= ItemLiquidVolume.Zero)
                return false;

            float mixedHeatPercent = CalculateMixedHeatPercent(
                current,
                flask.GetHeatPercent(),
                requested);
            ItemLiquidVolume newVolume = current + requested;
            GearItem content = EnsureWaterContent(flask, sourceItem, newVolume);
            if (content == null)
                return false;

            flask.m_LiquidType = potable;
            flask.m_VolumeLitres = newVolume;
            flask.m_HeatPercent = mixedHeatPercent;
            SetWaterVolume(content.m_WaterSupply, newVolume);

            player.RemoveWater(sourceItem.m_WaterSupply, requested);

            SyncFlask(flask);
            RefreshPanelNextFrame(panel);
            return true;
        }

        internal static bool RemoveWater(
            Panel_InsulatedFlask panel,
            GearItem selected)
        {
            if (panel == null ||
                selected == null ||
                selected.m_WaterSupply == null)
            {
                return false;
            }

            if (!IsPotableWater(selected))
                return false;

            InsulatedFlask flask = GetFlask(panel);
            if (flask == null)
                return false;

            ItemLiquidVolume current = flask.GetVolumeLitres();
            if (current <= ItemLiquidVolume.Zero)
                return false;

            Inventory inventory = GameManager.GetInventoryComponent();
            if (inventory == null)
                return false;

            bool addedToInventory = inventory.AddToPotableWaterSupply(current);
            if (!addedToInventory)
                return false;

            if (flask.m_Items != null)
            {
                for (int i = flask.m_Items.Count - 1; i >= 0; i--)
                {
                    GearItem item = flask.m_Items[i];
                    if (item != null &&
                        item.m_WaterSupply != null &&
                        IsPotableWater(item))
                    {
                        flask.m_Items.RemoveAt(i);
                    }
                }
            }

            flask.m_LiquidType = null;
            flask.m_VolumeLitres = ItemLiquidVolume.Zero;
            flask.m_HeatPercent = 0f;

            SyncFlask(flask);
            RefreshPanelNextFrame(panel);
            return true;
        }
    }
}
