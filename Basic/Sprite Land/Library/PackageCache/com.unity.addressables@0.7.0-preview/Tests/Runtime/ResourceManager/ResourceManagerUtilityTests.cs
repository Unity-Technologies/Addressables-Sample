using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;

namespace UnityEngine.ResourceManagement.Tests
{
    public class DelayedActionManagerTests
    {

        class DamTest
        {
            public bool methodInvoked;
            public int frameInvoked;
            public float timeInvoked;
            public void Method()
            {
                frameInvoked = Time.frameCount;
                timeInvoked = Time.unscaledTime;
                methodInvoked = true;
            }

            public void MethodWithParams(int p1, string p2, bool p3, float p4)
            {
                Assert.AreEqual(p1, 5);
                Assert.AreEqual(p2, "testValue");
                Assert.AreEqual(p3, true);
                Assert.AreEqual(p4, 3.14f);
            }

        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeSameFrame()
        {
            var testObj = new DamTest();
            int frameCalled = Time.frameCount;
            DelayedActionManager.AddAction((Action)testObj.Method);
            yield return null;
            Assert.AreEqual(frameCalled, testObj.frameInvoked);
        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeDelayed()
        {
            var testObj = new DamTest();
            float timeCalled = Time.unscaledTime;
            DelayedActionManager.AddAction((Action)testObj.Method, 2);
            while(!testObj.methodInvoked)
                yield return null;
            //make sure delay was at least 1 second (to account for test slowness)
            Assert.Greater(testObj.timeInvoked, timeCalled + 1);
        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeWithParameters()
        {
            var testObj = new DamTest();
            DelayedActionManager.AddAction((Action<int, string, bool, float>)testObj.MethodWithParams, 0, 5, "testValue", true, 3.14f);
            yield return null;
        }
    }

    public class LinkedListNodeCacheTests
    {
        LinkedListNodeCache<T> CreateCache<T>(int count)
        {
            var cache = new LinkedListNodeCache<T>();
            var temp = new List<LinkedListNode<T>>();
            for (int i = 0; i < count; i++)
                temp.Add(cache.Acquire(default(T)));
            Assert.AreEqual(count, cache.CreatedNodeCount);
            foreach (var t in temp)
                cache.Release(t);
            Assert.AreEqual(count, cache.CachedNodeCount);
            return cache;
        }

        void PopulateCache_AddRemove<T>()
        {
            var cache = CreateCache<T>(1);
            Assert.That(() =>
            {
                cache.Release(cache.Acquire(default(T)));
            }, TestTools.Constraints.Is.Not.AllocatingGCMemory(), "GC Allocation detected");
            Assert.AreEqual(1, cache.CreatedNodeCount);
            Assert.AreEqual(1, cache.CachedNodeCount);
        }

        [Test]
        public void WhenRefTypeAndCacheNotEmpty_AddRemove_DoesNotAlloc()
        {
            PopulateCache_AddRemove<string>();
        }

        [Test]
        public void WhenValueTypeAndCacheNotEmpty_AddRemove_DoesNotAlloc()
        {
            PopulateCache_AddRemove<int>();
        }

        [Test]
        public void Release_ResetsValue()
        {
            var cache = new LinkedListNodeCache<string>();
            var node = cache.Acquire(null);
            Assert.IsNull(node.Value);
            node.Value = "TestString";
            cache.Release(node);
            Assert.IsNull(node.Value);
        }
    }

    public class DelegateListTests
    {
        [Test]
        public void WhenDelegateRemoved_DelegateIsNotInvoked()
        {
            var cache = new LinkedListNodeCache<Action<string>>();
            var delList = new DelegateList<string>(cache.Acquire, cache.Release);
            bool called = false;
            Action<string> del = s => { called = true; };
            delList.Add(del);
            delList.Remove(del);
            delList.Invoke(null);
            Assert.IsFalse(called);
            Assert.AreEqual(cache.CreatedNodeCount, cache.CreatedNodeCount);
        }

        [Test]
        public void WhenAddInsideInvoke_NewDelegatesAreCalled()
        {
            bool addedDelegateCalled = false;
            var delList = CreateDelegateList<string>();
            delList.Add(s => delList.Add(s2 => addedDelegateCalled = true));
            delList.Invoke(null);
            Assert.IsTrue(addedDelegateCalled);
        }

        [Test]
        public void WhenCleared_DelegateIsNotInvoked()
        {
            var delList = CreateDelegateList<string>();
            int invocationCount = 0;
            delList.Add(s => invocationCount++);
            delList.Clear();
            delList.Invoke(null);
            Assert.AreEqual(0, invocationCount);
        }

        [Test]
        public void DuringInvoke_CanRemoveNextDelegate()
        {
            var delList = CreateDelegateList<string>();
            bool del1Called = false;
            Action<string> del1 = s => { del1Called = true; };
            Action<string> del2 = s => delList.Remove(del1);
            delList.Add(del2);
            delList.Add(del1);
            delList.Invoke(null);
            Assert.IsFalse(del1Called);
        }
        
        DelegateList<T> CreateDelegateList<T>()
        {
            var cache = new LinkedListNodeCache<Action<T>>();
            return new DelegateList<T>(cache.Acquire, cache.Release);
        }
        void InvokeAllocTest<T>(T p)
        {
            var delList = CreateDelegateList<T>();
            delList.Add(s => { });
            Assert.That(() =>
            {
                delList.Invoke(p);
            }, TestTools.Constraints.Is.Not.AllocatingGCMemory(), "GC Allocation detected");
        }

        [Test]
        public void DelegateNoGCWithRefType()
        {
            InvokeAllocTest<string>(null);
        }

        [Test]
        public void DelegateNoGCWithValueType()
        {
            InvokeAllocTest<int>(0);
        }
    }
}
