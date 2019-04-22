using System;

namespace UnityEditor.AddressableAssets.Diagnostics.Data
{
    [Serializable]
    struct EventDataSample
    {
        int m_Frame;
        int m_Data;
        internal int frame { get { return m_Frame; } }
        internal int data { get { return m_Data; } }

        internal EventDataSample(int frame, int value)
        {
            m_Frame = frame;
            m_Data = value;
        }
    }
}