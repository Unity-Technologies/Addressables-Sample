using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;

namespace UnityEditor.AddressableAssets.Settings
{
    class AddressablesAssetPostProcessor : AssetPostprocessor
    {
        struct ImportSet
        {
            public string[] importedAssets;
            public string[] deletedAssets;
            public string[] movedAssets;
            public string[] movedFromAssetPaths;
        }

        static List<ImportSet> s_Buffer;
        static Action<string[], string[], string[], string[]> s_Handler;
        public static Action<string[], string[], string[], string[]> OnPostProcess
        {
            get
            {
                return s_Handler;
            }
            set
            {
                s_Handler = value;
                if (s_Handler != null && s_Buffer != null)
                {
                    foreach (var b in s_Buffer)
                        s_Handler(b.importedAssets, b.deletedAssets, b.movedAssets, b.movedFromAssetPaths);
                    s_Buffer = null;
                }
            }
        }
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            if (s_Handler != null)
            {
                s_Handler(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
            else
            {
                //only buffer imports if they will be consumed by the settings object
                if (AddressableAssetSettingsDefaultObject.SettingsExists)
                {
                    if (s_Buffer == null)
                        s_Buffer = new List<ImportSet>();
                    s_Buffer.Add(new ImportSet { importedAssets = importedAssets, deletedAssets = deletedAssets, movedAssets = movedAssets, movedFromAssetPaths = movedFromAssetPaths });
                }
            }
        }
    }
}