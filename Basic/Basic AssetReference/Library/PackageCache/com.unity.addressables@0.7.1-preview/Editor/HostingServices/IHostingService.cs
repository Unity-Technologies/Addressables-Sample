using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.HostingServices
{
    /// <summary>
    /// <see cref="IHostingService"/> implementations serve Addressable content from the Unity Editor to players running
    /// locally or on devices with network access to the Editor.
    /// </summary>
    public interface IHostingService
    {
        /// <summary>
        /// Get the list of root directories being served by this hosting service
        /// </summary>
        List<string> HostingServiceContentRoots { get; }

        /// <summary>
        /// Get a map of all profile variables and their current values
        /// </summary>
        Dictionary<string, string> ProfileVariables { get; }

        /// <summary>
        /// Get a boolean that indicates if this hosting service is running
        /// </summary>
        bool IsHostingServiceRunning { get; }

        /// <summary>
        /// Start the hosting service
        /// </summary>
        void StartHostingService();

        /// <summary>
        /// Stop the hosting service
        /// </summary>
        void StopHostingService();

        /// <summary>
        /// Called by the HostingServicesManager before a domain reload, giving the hosting service
        /// an opportunity to persist state information.
        /// </summary>
        /// <param name="dataStore">A key/value pair data store for use in persisting state information</param>
        void OnBeforeSerialize(KeyDataStore dataStore);

        /// <summary>
        /// Called immediatley following a domain reload by the HostingServicesManager, for restoring state information
        /// after the service is recreated.
        /// </summary>
        /// <param name="dataStore">A key/value pair data store for use in restoring state information</param>
        void OnAfterDeserialize(KeyDataStore dataStore);

        /// <summary>
        /// Expand special variables from Addressable profiles
        /// </summary>
        /// <param name="key">Key name to match</param>
        /// <returns>replacement string value for key, or null if no match</returns>
        string EvaluateProfileString(string key);

        /// <summary>
        /// The ILogger instance to use for debug log output
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Draw configuration GUI elements
        /// </summary>
        // ReSharper disable once InconsistentNaming
        void OnGUI();

        /// <summary>
        /// Set by the HostingServicesManager, primarily used to disambiguate multiple instances of the same service
        /// in the GUI.
        /// </summary>
        string DescriptiveName { get; set; }

        /// <summary>
        /// uniquely identifies this service within the scope of the HostingServicesManager
        /// </summary>
        int InstanceId { get; set; }
    }
}