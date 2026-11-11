#nullable disable
using ModSettings;
using System.Reflection;

namespace BetterFlasks
{
    internal class BetterFlasksSettings : JsonModSettings
    {
        [Section("保温杯设置")]

        [Name("启用模组")]
        [Description("关闭后所有保温杯功能禁用")]
        public bool enableFlaskMod = true;

        [Name("在径向菜单中显示")]
        [Description("开启后，保温杯会出现在饮料径向菜单中")]
        public bool showFlaskInRadial = true;

        [Name("饮品选择偏好")]
        [Description("在径向菜单中显示保温杯信息时，优先显示哪种饮品：最高卡路里或最低卡路里。也影响饮用时选择哪种饮品。")]
        [Choice("最高卡路里", "最低卡路里")]
        public int flaskSelectionMode = 1; // 0 = 最高卡路里（默认）, 1 = 最低卡路里

        [Name("修改掉温速率")]
        [Description("开启后允许调整保温杯温度损失速度")]
        public bool modifyFlaskHeatLoss = true;

        [Name("掉温比")]
        [Description("0 = 不会冷, 0.1 = 10倍保温, 2 = 2倍掉温")]
        [Slider(0f, 2f, 21)]
        public float flaskHeatLossMultiplier = 1f;

        [Name("显示热量文本")]
        [Description("放置在外部时，悬停显示保温杯热量百分比")]
        public bool showHeatText = true;

        [Name("火堆旁加热")]
        [Description("放在火堆旁时，保温杯会逐渐回温")]
        public bool enableFireHeating = true;

        [Name("火堆加热倍率")]
        [Description("火堆旁回温倍率。基础速率：室内按室外掉温0.5%/分钟，室外按室内掉温0.25%/分钟。")]
        [Slider(1, 50)]
        public int fireHeatingMultiplier = 50;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            base.OnChange(field, oldValue, newValue);

        // 在 BetterFlasksSettings.OnChange 中：
        if (field.Name == nameof(enableFlaskMod))
        {
            bool isEnabled = (bool)newValue;
            SetFieldVisible(nameof(showFlaskInRadial), isEnabled);
            SetFieldVisible(nameof(flaskSelectionMode), isEnabled && showFlaskInRadial); // 新增依赖
            SetFieldVisible(nameof(modifyFlaskHeatLoss), isEnabled);
            SetFieldVisible(nameof(flaskHeatLossMultiplier), isEnabled && modifyFlaskHeatLoss);
            SetFieldVisible(nameof(showHeatText), isEnabled);
            SetFieldVisible(nameof(enableFireHeating), isEnabled);
            SetFieldVisible(nameof(fireHeatingMultiplier), isEnabled && enableFireHeating);
        }

        // 新增 showFlaskInRadial 变更的处理
        if (field.Name == nameof(showFlaskInRadial))
        {
            SetFieldVisible(nameof(flaskSelectionMode), enableFlaskMod && (bool)newValue);
        }

        // 保持原 modifyFlaskHeatLoss 的处理不变

            if (field.Name == nameof(modifyFlaskHeatLoss))
            {
                SetFieldVisible(nameof(flaskHeatLossMultiplier), enableFlaskMod && (bool)newValue);
            }

            if (field.Name == nameof(enableFireHeating))
            {
                SetFieldVisible(nameof(fireHeatingMultiplier), enableFlaskMod && (bool)newValue);
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
            options.SetFieldVisible(nameof(options.showFlaskInRadial), isEnabled);
            options.SetFieldVisible(nameof(options.flaskSelectionMode), isEnabled && options.showFlaskInRadial);
            options.SetFieldVisible(nameof(options.modifyFlaskHeatLoss), isEnabled);
            options.SetFieldVisible(nameof(options.flaskHeatLossMultiplier), isEnabled && options.modifyFlaskHeatLoss);
            options.SetFieldVisible(nameof(options.showHeatText), isEnabled);
            options.SetFieldVisible(nameof(options.enableFireHeating), isEnabled);
            options.SetFieldVisible(nameof(options.fireHeatingMultiplier), isEnabled && options.enableFireHeating);
        }

        internal static void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke();
        }
    }
}