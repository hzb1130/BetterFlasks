#nullable disable
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;

namespace BetterFlasks
{
    internal static class FlaskLiquidRules
    {
        internal static bool IsPotable(GearItem item)
        {
            return FlaskWaterHelper.IsPotableWater(item);
        }

        internal static bool FlaskContainsWater(InsulatedFlask flask)
        {
            if (flask == null)
                return false;

            LiquidType potable = FlaskWaterHelper.PotableWater;
            return flask.GetVolumeLitres() > ItemLiquidVolume.Zero &&
                   flask.m_LiquidType != null &&
                   potable != null &&
                   (flask.m_LiquidType == potable || flask.m_LiquidType.Equals(potable));
        }

        internal static bool FlaskHasOtherContents(InsulatedFlask flask)
        {
            if (flask == null)
                return false;

            Il2CppSystem.Collections.Generic.List<GearItem> items =
                FlaskHelper.GetItems(flask);

            for (int i = 0; i < items.Count; i++)
            {
                GearItem item = items[i];
                if (item != null && item.m_WaterSupply == null)
                    return true;
            }

            ItemLiquidVolume volume = flask.GetVolumeLitres();
            if (volume > ItemLiquidVolume.Zero && !FlaskContainsWater(flask))
                return true;

            return false;
        }

        internal static bool CanAddPotableWater(InsulatedFlask flask)
        {
            if (flask == null)
                return false;

            return !FlaskHasOtherContents(flask);
        }
    }
}
