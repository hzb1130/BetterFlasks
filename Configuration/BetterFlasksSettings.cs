#nullable disable
using ModSettings;
using System.Reflection;

namespace BetterFlasks
{
    internal class BetterFlasksSettings : JsonModSettings
    {
        [Section("General Settings | 常规设置")]

            [Name("Enable Mod | 启用模组")]
            [Description("Allows drinking from flasks via radial menu | 开启后，保温杯可在径向菜单饮用")]
            public bool enableFlaskMod = false;

            [Section("Drink Selection | 饮品选择")]

            [Name("Drink Preference | 饮品选择偏好")]
            [Description("Priority: Highest / Lowest calories for radial menu & drinking | 径向菜单和饮用时优先选择：最高卡路里 / 最低卡路里")]
            [Choice("Highest Calories | 最高卡路里", "Lowest Calories | 最低卡路里")]
            public int flaskSelectionMode = 1;

            [Section("Flask Temperature | 保温杯调节")]

            [Name("Adjust Cooling Rate | 修改掉温速率")]
            [Description("Enable custom flask heat loss speed | 开启后可自定义保温杯降温速度")]
            public bool modifyFlaskHeatLoss = false;

            [Name("Heat Loss Multiplier | 掉温比")]
            [Description("0 = no cooling, 0.1 = 10x insulation, 2 = 2x faster cooling | 0=永不冷却，0.1=10倍保温，2=2倍降温")]
            [Slider(0f, 2f, 21)]
            public float flaskHeatLossMultiplier = 1f;

            [Name("Show Heat Percentage | 显示热量文本")]
            [Description("Show flask heat percentage on hover | 物品悬停时显示保温杯温度占比")]
            public bool showHeatText = true;

            [Name("Fire Heating | 火堆旁加热")]
            [Description("Flask warms near fire; Required for hot water feature | 放置火堆旁可回温；热水功能依赖此项")]
            public bool enableFireHeating = false;

            [Name("Fire Heating Multiplier | 火堆加热倍率")]
            [Description("Multiplier for warming speed beside fire | 火堆回温速度倍率")]
            [Slider(1, 50)]
            public int fireHeatingMultiplier = 50;

            [Section("Hot Water Effect | 热水功能")]

            [Name("Enable Hot Water | 启用热水功能")]
            [Description("Fill hot water, restore warmth & buff; Requires Fire Heating | 可装入热水，饮用恢复体温并获得增益；依赖火堆加热")]
            public bool enableHotWaterEffect = false;

            [Name("Warmth Per 0.1L | 热水升温值")]
            [Description("Warmth percentage restored per 0.1L hot water, scaled by volume | 每0.1L热水恢复体温百分比，按饮水量折算")]
            [Slider(20, 40)]
            public int hotWaterTemperaturePercent = 20;


        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            base.OnChange(field, oldValue, newValue);

            if (field.Name == nameof(enableFlaskMod))
            {
                bool isEnabled = (bool)newValue;
                SetFieldVisible(nameof(flaskSelectionMode), isEnabled);
                SetFieldVisible(nameof(modifyFlaskHeatLoss), isEnabled);
                SetFieldVisible(nameof(flaskHeatLossMultiplier), isEnabled && modifyFlaskHeatLoss);
                SetFieldVisible(nameof(showHeatText), isEnabled);
                SetFieldVisible(nameof(enableFireHeating), isEnabled);
                SetFieldVisible(nameof(fireHeatingMultiplier), isEnabled && enableFireHeating);
                SetFieldVisible(nameof(enableHotWaterEffect), isEnabled);
                SetFieldVisible(nameof(hotWaterTemperaturePercent), isEnabled && enableHotWaterEffect);
            }

            if (field.Name == nameof(modifyFlaskHeatLoss))
            {
                SetFieldVisible(nameof(flaskHeatLossMultiplier), enableFlaskMod && (bool)newValue);
            }

            if (field.Name == nameof(enableFireHeating))
            {
                SetFieldVisible(nameof(fireHeatingMultiplier), enableFlaskMod && (bool)newValue);
            }

            if (field.Name == nameof(enableHotWaterEffect))
            {
                SetFieldVisible(nameof(hotWaterTemperaturePercent), enableFlaskMod && (bool)newValue);
            }

            Settings.NotifySettingsChanged();
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();
            Settings.NotifySettingsChanged();
        }
    }

    internal static class Settings
    {
        public static BetterFlasksSettings options;
        public static event System.Action OnSettingsChanged;

        public static void OnLoad()
        {
            options = new BetterFlasksSettings();
            options.AddToModSettings("Better Flasks | 保温杯修改");
            ApplyInitialVisibility();
        }

        internal static void ApplyInitialVisibility()
        {
            bool isEnabled = options.enableFlaskMod;
            options.SetFieldVisible(nameof(options.flaskSelectionMode), isEnabled);
            options.SetFieldVisible(nameof(options.modifyFlaskHeatLoss), isEnabled);
            options.SetFieldVisible(nameof(options.flaskHeatLossMultiplier), isEnabled && options.modifyFlaskHeatLoss);
            options.SetFieldVisible(nameof(options.showHeatText), isEnabled);
            options.SetFieldVisible(nameof(options.enableFireHeating), isEnabled);
            options.SetFieldVisible(nameof(options.fireHeatingMultiplier), isEnabled && options.enableFireHeating);
            options.SetFieldVisible(nameof(options.enableHotWaterEffect), isEnabled);
            options.SetFieldVisible(nameof(options.hotWaterTemperaturePercent), isEnabled && options.enableHotWaterEffect);
        }

        internal static void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke();
        }
    }
}
