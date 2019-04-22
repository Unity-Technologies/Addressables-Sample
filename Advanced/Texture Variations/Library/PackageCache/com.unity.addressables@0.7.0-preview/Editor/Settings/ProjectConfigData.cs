using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    class ProjectConfigData
    {
        [Serializable]
        class ConfigSaveData
        {
            [FormerlySerializedAs("m_postProfilerEvents")]
            [SerializeField]
            internal bool postProfilerEventsInternal;
            [FormerlySerializedAs("m_localLoadSpeed")]
            [SerializeField]
            internal long localLoadSpeedInternal = 1024 * 1024 * 10;
            [FormerlySerializedAs("m_remoteLoadSpeed")]
            [SerializeField]
            internal long remoteLoadSpeedInternal = 1024 * 1024 * 1;
            [FormerlySerializedAs("m_hierarchicalSearch")]
            [SerializeField]
            internal bool hierarchicalSearchInternal;
        }

        static ConfigSaveData s_Data;

        public static bool postProfilerEvents
        {
            get
            {
                ValidateData();
                return s_Data.postProfilerEventsInternal;
            }
            set
            {
                ValidateData();
                s_Data.postProfilerEventsInternal = value;
                SaveData();
            }
        }
        public static long localLoadSpeed
        {
            get
            {
                ValidateData();
                return s_Data.localLoadSpeedInternal;
            }
            set
            {
                ValidateData();
                s_Data.localLoadSpeedInternal = value;
                SaveData();
            }
        }
        public static long remoteLoadSpeed
        {
            get
            {
                ValidateData();
                return s_Data.remoteLoadSpeedInternal;
            }
            set
            {
                ValidateData();
                s_Data.remoteLoadSpeedInternal = value;
                SaveData();
            }
        }
        public static bool hierarchicalSearch
        {
            get
            {
                ValidateData();
                return s_Data.hierarchicalSearchInternal;
            }
            set
            {
                ValidateData();
                s_Data.hierarchicalSearchInternal = value;
                SaveData();
            }
        }

        internal static void SerializeForHash(Stream stream)
        {
            ValidateData();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, s_Data);
        }

        static void ValidateData()
        {
            if (s_Data == null)
            {
                var dataPath = Path.GetFullPath(".");
                dataPath = dataPath.Replace("\\", "/");
                dataPath += "/Library/AddressablesConfig.dat";

                if (File.Exists(dataPath))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    try
                    {
                        using (FileStream file = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                        {
                            var data = bf.Deserialize(file) as ConfigSaveData;
                            if (data != null)
                            {
                                s_Data = data;
                            }
                        }
                    }
                    catch
                    {
                        //if the current class doesn't match what's in the file, Deserialize will throw. since this data is non-critical, we just wipe it
                        Addressables.LogWarning("Error reading Addressable Asset project config (play mode, etc.). Resetting to default.");
                        File.Delete(dataPath);
                    }
                }

                //check if some step failed.
                if (s_Data == null)
                {
                    s_Data = new ConfigSaveData();
                }
            }
        }

        static void SaveData()
        {
            if (s_Data == null)
                return;

            var dataPath = Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AddressablesConfig.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, s_Data);
            file.Close();
        }
    }
}
