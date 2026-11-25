#nullable disable
using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(BetterFlasks.BetterFlasksMain), "BetterFlasks", "1.0.1", "hzb1130")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace BetterFlasks
{
    public class BetterFlasksMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Settings.OnLoad();
            Settings.OnSettingsChanged += RefreshActiveRadialMenu;
        }

        private static void RefreshActiveRadialMenu()
        {
            var radial = InterfaceManager.GetPanel<Panel_ActionsRadial>();
            if (radial != null && radial.isActiveAndEnabled)
                radial.Initialize();
        }
    }
}
