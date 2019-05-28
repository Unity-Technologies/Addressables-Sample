using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings
{
    [InitializeOnLoad]
    static class GlobalInitialization
    {
        static bool m_IsInitialized = false;

        static GlobalInitialization()
        {
            InitializeGlobalState();
        }

        public static void InitializeGlobalState()
        {
            if (!m_IsInitialized)
            {
                AddressableScenesManager.InitializeGlobalState();
                m_IsInitialized = true;
            }
        }

        // This only gets called by testing code that wants to do isolated testing without active Addressables global hooks
        public static void ShutdownGlobalState()
        {
            if (m_IsInitialized)
            {
                AddressableScenesManager.ShutdownGlobalState();
                m_IsInitialized = false;
            }
        }
    }
}