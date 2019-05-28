using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.AddressableAssets
{
    public enum AddressablesPlatform
    {
        Unknown,
        Windows,
        OSX,
        Linux,
        PS4,
        Switch,
        XboxOne,
        WebGL,
        iOS,
        Android
    }

    public class PlatformMappingService
    {
#if UNITY_EDITOR
        private static readonly Dictionary<BuildTarget, AddressablesPlatform> s_BuildTargetMapping =
            new Dictionary<BuildTarget, AddressablesPlatform>()
            {
                {BuildTarget.XboxOne, AddressablesPlatform.XboxOne},
                {BuildTarget.Switch, AddressablesPlatform.Switch},
                {BuildTarget.PS4, AddressablesPlatform.PS4},
                {BuildTarget.iOS, AddressablesPlatform.iOS},
                {BuildTarget.Android, AddressablesPlatform.Android},
                {BuildTarget.WebGL, AddressablesPlatform.WebGL},
                {BuildTarget.StandaloneWindows, AddressablesPlatform.Windows},
                {BuildTarget.StandaloneWindows64, AddressablesPlatform.Windows},
                {BuildTarget.StandaloneOSX, AddressablesPlatform.OSX},
                {BuildTarget.StandaloneLinux64, AddressablesPlatform.Linux},
#if !UNITY_2019_2_OR_NEWER
                {BuildTarget.StandaloneLinux, AddressablesPlatform.Linux},
                {BuildTarget.StandaloneLinuxUniversal, AddressablesPlatform.Linux}
#endif
            };
#endif
        private static readonly Dictionary<RuntimePlatform, AddressablesPlatform> s_RuntimeTargetMapping =
            new Dictionary<RuntimePlatform, AddressablesPlatform>()
            {
                {RuntimePlatform.XboxOne, AddressablesPlatform.XboxOne},
                {RuntimePlatform.Switch, AddressablesPlatform.Switch},
                {RuntimePlatform.PS4, AddressablesPlatform.PS4},
                {RuntimePlatform.IPhonePlayer, AddressablesPlatform.iOS}, 
                {RuntimePlatform.Android, AddressablesPlatform.Android},
                {RuntimePlatform.WebGLPlayer, AddressablesPlatform.WebGL},
                {RuntimePlatform.WindowsPlayer, AddressablesPlatform.Windows},
                {RuntimePlatform.OSXPlayer, AddressablesPlatform.OSX},
                {RuntimePlatform.LinuxPlayer, AddressablesPlatform.Linux},
                 {RuntimePlatform.WindowsEditor, AddressablesPlatform.Windows},
                 {RuntimePlatform.OSXEditor, AddressablesPlatform.OSX},
                 {RuntimePlatform.LinuxEditor, AddressablesPlatform.Linux},
            };

#if UNITY_EDITOR
        internal static AddressablesPlatform GetAddressablesPlatformInternal(BuildTarget target)
        {
            if (s_BuildTargetMapping.ContainsKey(target))
                return s_BuildTargetMapping[target];
            return AddressablesPlatform.Unknown;
        }
#endif
        internal static AddressablesPlatform GetAddressablesPlatformInternal(RuntimePlatform platform)
        {
            if (s_RuntimeTargetMapping.ContainsKey(platform))
                return s_RuntimeTargetMapping[platform];
            return AddressablesPlatform.Unknown;
        }

        public static AddressablesPlatform GetPlatform()
        {

#if UNITY_EDITOR
            return GetAddressablesPlatformInternal(EditorUserBuildSettings.activeBuildTarget);
#else
            return GetAddressablesPlatformInternal(Application.platform);
#endif
        }
    }
}
