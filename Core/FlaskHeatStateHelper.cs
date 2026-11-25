#nullable disable
using HarmonyLib;
using Il2Cpp;

namespace BetterFlasks
{
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
            PlayerManager player = GameManager.GetPlayerManagerComponent();
            if (player != null && player.m_IndoorSpaceTrigger != null)
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
}
