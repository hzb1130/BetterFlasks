#nullable disable
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    internal static class HotWaterDrinkEffect
    {
        private const float ReferenceVolumeLiters = 0.1f;
        private const float WarmingBuffHoursPerReferenceVolume = 1f;
        private const float WarmingBuffRateScale = 0.5f;

        internal static void OnHotWaterConsumed(
            InsulatedFlask flask,
            GearItem waterItem,
            float heatPercentBeforeDrink,
            ItemLiquidVolume volumeConsumed)
        {
            if (Settings.options == null ||
                !Settings.options.enableFlaskMod ||
                !Settings.options.enableHotWaterEffect ||
                heatPercentBeforeDrink <= 0f ||
                volumeConsumed <= ItemLiquidVolume.Zero)
            {
                return;
            }

            PlayerManager player = GameManager.GetPlayerManagerComponent();
            Freezing freezing = GameManager.GetFreezingComponent();
            if (player == null || freezing == null)
                return;

            float consumedLiters = volumeConsumed / ItemLiquidVolume.Liter;
            float volumeScale = consumedLiters / ReferenceVolumeLiters;
            float temperaturePercentPerReferenceVolume = Mathf.Clamp(
                Settings.options.hotWaterTemperaturePercent,
                20,
                40);
            float temperaturePercent =
                temperaturePercentPerReferenceVolume * volumeScale;
            float buffHours =
                WarmingBuffHoursPerReferenceVolume * volumeScale;
            float currentBuffHours = Mathf.Max(
                0f,
                player.GetFreezingBuffTimeRemainingHours());
            float buffMaxHours = currentBuffHours + buffHours;
            float freezingBefore = freezing.m_CurrentFreezing;

            try
            {
                player.ApplyFreezingBuff(
                    temperaturePercent,
                    WarmingBuffRateScale,
                    buffHours,
                    buffMaxHours);

                MelonLogger.Msg(
                    $"[HotWater] Effect applied: consumed={consumedLiters:0.###}L, " +
                    $"heat={heatPercentBeforeDrink:0.#}%, " +
                    $"temperature+={temperaturePercent:0.#}%, " +
                    $"warmingBuff+={buffHours:0.##}h, " +
                    $"warmingBuffRemaining=" +
                    $"{player.GetFreezingBuffTimeRemainingHours():0.##}h, " +
                    $"freezing={freezingBefore:0.#}->{freezing.m_CurrentFreezing:0.#}");
            }
            catch (System.Exception exception)
            {
                MelonLogger.Error(
                    $"[HotWater] Failed to apply effect: {exception}");
            }
        }
    }
}
