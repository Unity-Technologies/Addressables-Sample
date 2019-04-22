using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEditor.AddressableAssets.Diagnostics.GUI.Graph;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics
{
    public class SegmentIterationTests
    {
        static bool IsContinuationOfSegment(int prevData, int newData)
        {
            return prevData != newData;
        }

        [Test]
        public void EmptyStream_ReturnsNoSegments()
        {
            Assert.AreEqual(0, GraphUtility.IterateSegments(new EventDataSetStream(), 0, 100, null).Count());
        }

        [Test]
        public void FirstSampleBeforeStartFrame_IsFirstSegmentAndCropped()
        {
            EventDataSetStream stream = new EventDataSetStream();
            stream.AddSample(0, 99);
            GraphUtility.Segment[] segs = GraphUtility.IterateSegments(stream, 25, 100, IsContinuationOfSegment).ToArray();
            Assert.AreEqual(1, segs.Length);
            Assert.AreEqual(25, segs[0].frameStart);
            Assert.AreEqual(100, segs[0].frameEnd);
            Assert.AreEqual(99, segs[0].data);
        }

        [Test]
        public void LastSampleBeforeLastEndFrame_LastSegmentSpansToEndOfFrame()
        {
            EventDataSetStream stream = new EventDataSetStream();
            stream.AddSample(50, 99);
            GraphUtility.Segment[] segs = GraphUtility.IterateSegments(stream, 0, 100, IsContinuationOfSegment).ToArray();
            Assert.AreEqual(1, segs.Length);
            Assert.AreEqual(50, segs[0].frameStart);
            Assert.AreEqual(100, segs[0].frameEnd);
            Assert.AreEqual(99, segs[0].data);
        }

        void AddAlternativeSegments(EventDataSetStream stream, int data1, int data2, int startFrame, int frameIncrement, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int sample = (i % 2) == 0 ? data1 : data2;
                stream.AddSample(startFrame + i * frameIncrement, sample);
            }
        }

        [Test]
        public void MultipleSamples_SegmentPerSample()
        {
            EventDataSetStream stream = new EventDataSetStream();
            const int kSampleCount = 20;
            const int kFrameIncrement = 100;
            AddAlternativeSegments(stream, -99, 99, 0, kFrameIncrement, kSampleCount);
            GraphUtility.Segment[] segs = GraphUtility.IterateSegments(stream, 0, kSampleCount * kFrameIncrement, IsContinuationOfSegment).ToArray();
            Assert.AreEqual(kSampleCount, segs.Length);
            for(int i = 0; i < segs.Length; i++)
            {
                Assert.AreEqual(segs[i].data, (i % 2 == 0) ? -99 : 99);
                Assert.AreEqual(segs[i].frameStart, i * kFrameIncrement);
                Assert.AreEqual(segs[i].frameEnd, (i+1) * kFrameIncrement);
            }
        }

        [Test]
        public void SegmentBeforeStartFrame_IsIgnored()
        {
            EventDataSetStream stream = new EventDataSetStream();
            stream.AddSample(0, 99);
            stream.AddSample(50, 0);
            GraphUtility.Segment[] segs = GraphUtility.IterateSegments(stream, 100, 200, IsContinuationOfSegment).ToArray();
            Assert.AreEqual(1, segs.Length);
            Assert.AreEqual(0, segs[0].data);
            Assert.AreEqual(100, segs[0].frameStart);
            Assert.AreEqual(200, segs[0].frameEnd);
        }
    }
}