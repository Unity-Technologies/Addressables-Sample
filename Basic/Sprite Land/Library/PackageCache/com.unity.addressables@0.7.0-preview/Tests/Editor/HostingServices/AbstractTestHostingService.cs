using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.HostingServices
{
    public abstract class AbstractTestHostingService : IHostingService
    {
        public string DescriptiveName { get; set; }
        public int InstanceId { get; set; }
        public List<string> HostingServiceContentRoots { get; protected set; }
        public Dictionary<string, string> ProfileVariables { get; protected set; }
        public bool IsHostingServiceRunning { get; protected set; }

        public ILogger Logger { get; set; }

        protected AbstractTestHostingService()
        {
            HostingServiceContentRoots = new List<string>();
            ProfileVariables = new Dictionary<string, string>();
        }

        public virtual void StartHostingService()
        {
        }

        public virtual void StopHostingService()
        {
        }

        public virtual void OnBeforeSerialize(KeyDataStore dataStore)
        {
        }

        public virtual void OnAfterDeserialize(KeyDataStore dataStore)
        {
        }

        public string EvaluateProfileString(string key)
        {
            return null;
        }

        public virtual void OnGUI()
        {
        }
    }
}