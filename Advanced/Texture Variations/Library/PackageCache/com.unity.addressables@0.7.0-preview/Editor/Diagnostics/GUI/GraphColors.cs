using System;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI
{
    static class GraphColors
    {
        static Color s_KWindowBackground = new Color(0.63f, 0.63f, 0.63f, 1.0f);
        static Color s_KLabelGraphLabelBackground = new Color(0.75f, 0.75f, 0.75f, 0.75f);

        static Color s_KWindowBackgroundPro = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        static Color s_KLabelGraphLabelBackgroundPro = new Color(0, 0, 0, .75f);

        internal static Color WindowBackground { get { return EditorGUIUtility.isProSkin ? s_KWindowBackgroundPro : s_KWindowBackground; } }
        internal static Color LabelGraphLabelBackground { get { return EditorGUIUtility.isProSkin ? s_KLabelGraphLabelBackgroundPro : s_KLabelGraphLabelBackground; } }
    }
}