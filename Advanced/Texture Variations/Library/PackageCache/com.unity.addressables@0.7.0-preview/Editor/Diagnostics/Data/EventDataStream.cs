using System;
using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Diagnostics.Data
{
    [Serializable]
    class EventDataSetStream
    {
        internal int maxValue;
        internal List<EventDataSample> samples = new List<EventDataSample>();
        internal void AddSample(int frame, int val)
        {
            if (val > maxValue)
                maxValue = val;
            if (samples.Count > 0 && samples[samples.Count - 1].frame == frame)
                samples[samples.Count - 1] = new EventDataSample(frame, val);
            else
                samples.Add(new EventDataSample(frame, val));
        }

        internal int GetValue(int f)
        {
            if (samples.Count == 0 || f < samples[0].frame)
                return 0;
            if (f >= samples[samples.Count - 1].frame)
                return samples[samples.Count - 1].data;
            for (int i = 1; i < samples.Count; i++)
            {
                if (samples[i].frame > f)
                    return samples[i - 1].data;
            }
            return samples[0].data;
        }

        internal bool HasDataAfterFrame(int frame)
        {
            if (samples.Count == 0)
                return false;
            EventDataSample lastSample = samples[samples.Count - 1];
            return lastSample.frame > frame || lastSample.data > 0;
        }
    }

}