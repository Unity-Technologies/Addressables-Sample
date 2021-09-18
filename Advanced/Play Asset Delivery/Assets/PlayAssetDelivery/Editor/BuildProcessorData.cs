using System;
using System.Collections.Generic;

namespace AddressablesPlayAssetDelivery.Editor
{
    [Serializable]
    public class BuildProcessorDataEntry
    {
        public string BundleBuildPath;

        public string AssetsSubfolderPath;

        public BuildProcessorDataEntry(string bundleBuildPath, string assetsSubfolderPath)
        {
            BundleBuildPath = bundleBuildPath;
            AssetsSubfolderPath = assetsSubfolderPath;
        }
    }

    [Serializable]
    public class BuildProcessorData
    {
        public List<BuildProcessorDataEntry> Entries;

        public BuildProcessorData(List<BuildProcessorDataEntry> entries)
        {
            Entries = new List<BuildProcessorDataEntry>(entries);
        }
    }
}
