using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    [Serializable]
    class LabelTable
    {
        [FormerlySerializedAs("m_labelNames")]
        [SerializeField]
        List<string> m_LabelNames = new List<string>(new[] { "default" });

        internal List<string> labelNames { get { return m_LabelNames; } }
        const int k_KNameCountCap = 3;
        
        internal void AddLabelName(string name)
        {
            if(!m_LabelNames.Contains(name))
            { 
                m_LabelNames.Add(name);
            }
        }

        internal string GetUniqueLabelName(string name)
        {
            var newName = name;
            int counter = 1;
            while (counter < 100)
            {
                if (!m_LabelNames.Contains(newName))
                    return newName;
                newName = name + counter;
                counter++;
            }
            return string.Empty;
        }

        internal bool RemoveLabelName(string name)
        {
            return m_LabelNames.Remove(name);
        }

        internal string GetString(HashSet<string> val, float width) //TODO - use width to add the "..." in the right place.
        {
            StringBuilder sb = new StringBuilder();
            int counter = 0;
            foreach (var v in m_LabelNames)
            {
                if (val.Contains(v))
                {
                    if (counter >= k_KNameCountCap)
                    {
                        sb.Append("...");
                        break;
                    }

                    if (counter > 0)
                        sb.Append(", ");
                    sb.Append(v);
                    counter++;
                }
            }
            return sb.ToString();
        }

        internal long GetMask(HashSet<string> maskSet)
        {
            if (maskSet.Count == 0)
                return 0;
            long one = 1;
            long val = 0;
            for (int i = 0; i < m_LabelNames.Count; i++)
                if (maskSet.Contains(m_LabelNames[i]))
                    val |= one << i;
            return val;
        }
    }
}
