using System;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Analytics;

namespace UnityEditor.AddressableAssets
{
    internal static class AddressableAnalytics
    {
        private const string VendorKey = "unity.addressables";
        private const string EventName = "addressables";
        private static bool _eventRegistered = false;

        [Serializable]
        private struct AnalyticsData
        {
            public string BuildScriptName;
            public int NumberOfAddressableAssets;
        };

        internal static void Report(AddressableAssetSettings currentSettings)
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!UnityEngine.Analytics.Analytics.enabled)
                return;

            ReportImpl(currentSettings);
        }

        private static void ReportImpl(AddressableAssetSettings currentSettings)
        {
            if (!_eventRegistered)
            {
                //If the event isn't registered, attempt to register it.  If unsuccessful, return.
                if (!RegisterEvent())
                    return;
            }

            //Gather how many addressable assets we have
            int numberOfAddressableAssets = 0;
            foreach (var group in currentSettings.groups)
                numberOfAddressableAssets += group.entries.Count;

            AnalyticsData data = new AnalyticsData()
            {
                BuildScriptName = currentSettings.ActivePlayerDataBuilder.Name,
                NumberOfAddressableAssets = numberOfAddressableAssets,
            };

            //Report
            EditorAnalytics.SendEventWithLimit(EventName, data);
        }

        //Returns true if registering the event was successful
        private static bool RegisterEvent()
        {
            AnalyticsResult registerEvent = EditorAnalytics.RegisterEventWithLimit(EventName, 100, 100, VendorKey);
            if (registerEvent == AnalyticsResult.Ok)
                _eventRegistered = true;

            return _eventRegistered;
        }
    }
}
