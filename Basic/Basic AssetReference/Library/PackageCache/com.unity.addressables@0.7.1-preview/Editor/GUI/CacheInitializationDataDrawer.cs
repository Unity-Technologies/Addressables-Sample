using System;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomPropertyDrawer(typeof(CacheInitializationData), true)]
    class CacheInitializationDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var s = EditorStyles.label.CalcSize(label);
            EditorGUI.BeginProperty(position, label, property);
            var r = new Rect(position.x, position.y, position.width, s.y);

            {
                var prop = property.FindPropertyRelative("m_CompressionEnabled");
                prop.boolValue  = EditorGUI.Toggle(r, new GUIContent("Compress Bundles", "Bundles are recompressed into LZ4 format to optimize load times."), prop.boolValue);
                r.y += s.y + EditorGUIUtility.standardVerticalSpacing;
            }


            {
                var prop = property.FindPropertyRelative("m_CacheDirectoryOverride");
                prop.stringValue = EditorGUI.TextField(r, new GUIContent("Cache Directory Override", "Specify custom directory for cache.  Leave blank for default."), prop.stringValue);
                r.y += s.y + EditorGUIUtility.standardVerticalSpacing;
            }

            {
                var prop = property.FindPropertyRelative("m_ExpirationDelay");
                prop.intValue  = EditorGUI.IntSlider(r, new GUIContent("Expiration Delay (in seconds)", "Controls how long items are left in the cache before deleting."), prop.intValue, 0, 12960000);
                r.y += s.y + EditorGUIUtility.standardVerticalSpacing;
                var ts = new TimeSpan(0, 0, prop.intValue );
                EditorGUI.LabelField(new Rect(r.x + 16, r.y, r.width - 16, r.height), new GUIContent(NicifyTimeSpan(ts)));
                
                r.y += s.y + EditorGUIUtility.standardVerticalSpacing;
            }

            {
                var limProp = property.FindPropertyRelative("m_LimitCacheSize");
                limProp.boolValue = EditorGUI.ToggleLeft(r, new GUIContent("Limit Cache Size"), limProp.boolValue);
                if (limProp.boolValue)
                {
                    var prop = property.FindPropertyRelative("m_MaximumCacheSize");
                    if (prop.longValue == long.MaxValue)
                        prop.longValue = (1024 * 1024 * 1024);//default to 1GB

                    r.y += s.y + EditorGUIUtility.standardVerticalSpacing;
                    var mb = (prop.longValue / (1024 * 1024));
                    var val = EditorGUI.LongField(new Rect(r.x + 16, r.y, r.width - 16, r.height), new GUIContent("Maximum Cache Size (in MB)", "Controls how large the cache can get before deleting."), mb);
                    if (val != mb)
                        prop.longValue = val * (1024 * 1024);
                }
            }
            EditorGUI.EndProperty();
        }

        string NicifyTimeSpan(TimeSpan ts)
        {
            if (ts.Days >= 1)
                return string.Format("{0} days, {1} hours, {2} minutes, {3} seconds.", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            if (ts.Hours >= 1)
                return string.Format("{0} hours, {1} minutes, {2} seconds.", ts.Hours, ts.Minutes, ts.Seconds);
            if (ts.Minutes >= 1)
                return string.Format("{0} minutes, {1} seconds.", ts.Minutes, ts.Seconds);
            return string.Format("{0} seconds.", ts.Seconds);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var s = EditorStyles.label.CalcSize(label);
            return s.y * 5 + EditorGUIUtility.standardVerticalSpacing * 4;
        }
    }
}
