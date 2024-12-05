using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace Vi.Core
{
    public class AdaptivePerformanceManager : MonoBehaviour
    {
        private IAdaptivePerformance ap = null;

        void Start()
        {
            ap = Holder.Instance;
            if (!ap.Active)
                return;

            ap.ThermalStatus.ThermalEvent += OnThermalEvent;
        }

        void OnThermalEvent(ThermalMetrics ev)
        {
            Debug.Log("Thermal Warning Level: " + ev.WarningLevel + " Temperature Level: " + ev.TemperatureLevel + " Temperature Trend: " + ev.TemperatureTrend);

            //switch (ev.WarningLevel)
            //{
            //    case WarningLevel.NoWarning:
            //        QualitySettings.lodBias = 1;
            //        break;
            //    case WarningLevel.ThrottlingImminent:
            //        if (ev.TemperatureLevel > 0.8f)
            //            QualitySettings.lodBias = 0.75f;
            //        else
            //            QualitySettings.lodBias = 1.0f;
            //        break;
            //    case WarningLevel.Throttling:
            //        QualitySettings.lodBias = 0.5f;
            //        break;
            //}
        }
    }
}