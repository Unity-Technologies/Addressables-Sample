using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.Utility
{
    internal class DiagnosticInfo
    {
        public string DisplayName;
        public int ObjectId;
        public int[] Dependencies;
        public DiagnosticEvent CreateEvent(string category, ResourceManager.DiagnosticEventType eventType, int frame, int val)
        {
            return new DiagnosticEvent(category, DisplayName, ObjectId, (int)eventType, frame, val, Dependencies);
        }
    }

    internal class ResourceManagerDiagnostics
    {

        DiagnosticEventCollector m_eventCollector;

        /// <summary>
        /// This class is responsible for passing events from the resource manager to the event collector, 
        /// </summary>
        /// <param name="resourceManager"></param>
        public ResourceManagerDiagnostics(ResourceManager resourceManager)
        {
            resourceManager.RegisterDiagnosticCallback(OnResourceManagerDiagnosticEvent);
            m_eventCollector = DiagnosticEventCollector.FindOrCreateGlobalInstance();
        }
        Dictionary<int, DiagnosticInfo> m_cachedDiagnosticInfo = new Dictionary<int, DiagnosticInfo>();
        List<AsyncOperationHandle> m_dependencyBuffer = new List<AsyncOperationHandle>();

        void OnResourceManagerDiagnosticEvent(AsyncOperationHandle op, ResourceManager.DiagnosticEventType type, int eventValue, object context)
        {
            var hashCode = op.GetHashCode();
            DiagnosticInfo diagInfo = null;

            if (type == ResourceManager.DiagnosticEventType.AsyncOperationDestroy)
            {
                if (m_cachedDiagnosticInfo.TryGetValue(hashCode, out diagInfo))
                    m_cachedDiagnosticInfo.Remove(hashCode);
            }
            else
            {
                if (!m_cachedDiagnosticInfo.TryGetValue(hashCode, out diagInfo))
                {
                    m_dependencyBuffer.Clear();
                    op.GetDependencies(m_dependencyBuffer);
                    var depIds = new int[m_dependencyBuffer.Count];
                    for (int i = 0; i < depIds.Length; i++)
                        depIds[i] = m_dependencyBuffer[i].GetHashCode();
                    m_cachedDiagnosticInfo.Add(hashCode, diagInfo = new DiagnosticInfo() { ObjectId = hashCode, DisplayName = op.DebugName, Dependencies = depIds});
                }
            }

            m_eventCollector.PostEvent(diagInfo.CreateEvent("ResourceManager", type, Time.frameCount, eventValue));
        }
    }
}
