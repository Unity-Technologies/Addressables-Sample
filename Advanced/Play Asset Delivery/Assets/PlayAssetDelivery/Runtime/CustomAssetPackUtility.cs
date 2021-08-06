using System.Collections.Generic;
#if UNITY_EDITOR && UNITY_2021_2_OR_NEWER
using Unity.Android.Types;
#endif

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Serializable representation of 'Unity.Android.Types.AndroidAssetPackDeliveryType'.
    /// </summary>
    public enum DeliveryType
    {
        /// <summary>
        /// No delivery type specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Content is downloaded when the app is installed.
        /// </summary>
        InstallTime = 1,

        /// <summary>
        /// Content is downloaded automatically as soon as the the app is installed.
        /// </summary>
        FastFollow = 2,

        /// <summary>
        /// Content is downloaded while the app is running.
        /// </summary>
        OnDemand = 3
    }

    public class CustomAssetPackUtility
    {
#if UNITY_EDITOR
#if UNITY_2021_2_OR_NEWER
        static readonly Dictionary<DeliveryType, AndroidAssetPackDeliveryType> k_DeliveryTypeToGradleString = new Dictionary<DeliveryType, AndroidAssetPackDeliveryType>()
        {
            { DeliveryType.InstallTime, AndroidAssetPackDeliveryType.InstallTime },
            { DeliveryType.FastFollow, AndroidAssetPackDeliveryType.FastFollow },
            { DeliveryType.OnDemand, AndroidAssetPackDeliveryType.OnDemand },
        };

        public static string DeliveryTypeToGradleString(DeliveryType deliveryType)
        {
            return k_DeliveryTypeToGradleString[deliveryType].Name;
        }
#else
        static readonly Dictionary<DeliveryType, string> k_DeliveryTypeToGradleString = new Dictionary<DeliveryType, string>()
        {
            { DeliveryType.InstallTime, "install-time" },
            { DeliveryType.FastFollow, "fast-follow" },
            { DeliveryType.OnDemand, "on-demand" },
        };

        public static string DeliveryTypeToGradleString(DeliveryType deliveryType)
        {
            return k_DeliveryTypeToGradleString[deliveryType];
        }
#endif
#endif
    }
}
