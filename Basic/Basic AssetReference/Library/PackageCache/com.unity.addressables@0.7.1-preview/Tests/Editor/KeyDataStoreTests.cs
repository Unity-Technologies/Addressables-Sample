using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Tests
{
    public class KeyDataStoreTests
    {

        [Serializable]
        class CustomTestClass1
        {
            public string name;
            public int intValue;
            public float floatValue;
        }

        [Serializable]
        class CustomTestClass2
        {
            public string name;
            public int intValue;
            public float floatValue;
            public CustomTestClass1 objectValue;
            public List<CustomTestClass1> listValue;
        }

        public void Serialize<T>(T val)
        {
            var store = new KeyDataStore();
            store.SetData("key", val);
            store.OnBeforeSerialize();
            store.OnAfterDeserialize();
            var v = store.GetData("key", default(T));
            Assert.AreEqual(val, v);
        }

        enum TestEnumX
        {
            EnumValue1,
            EnumValue2
        }

        [Test]
        public void SerializePodType()
        {
            Serialize(5);
            Serialize("test string");
            Serialize(5.2324f);
            Serialize(5.3);
            Serialize((byte)4);
            Serialize((uint)4);
            Serialize(2345235L);
            Serialize(true);
            Serialize(TestEnumX.EnumValue1);
            Serialize(TestEnumX.EnumValue2);
        }

        [Test]
        public void SerializeComplexType()
        {
            var store = new KeyDataStore();
            var obj = new CustomTestClass2 { floatValue = 3.14f, intValue = 7, name = "test object", objectValue = new CustomTestClass1 { name = "sub object", intValue = 14, floatValue = .99999f } };
            obj.listValue = new List<CustomTestClass1> { new CustomTestClass1 { name = "list item 1", intValue = 33, floatValue = .234534f } };
            store.SetData("obj", obj);
            store.OnBeforeSerialize();
            store.OnAfterDeserialize();
            var v = store.GetData<CustomTestClass2>("obj", null);
            Assert.AreEqual(obj.name, v.name);
            Assert.AreEqual(obj.intValue, v.intValue);
            Assert.AreEqual(obj.floatValue, v.floatValue);
            Assert.AreEqual(v.objectValue.name, obj.objectValue.name);
            Assert.AreEqual(v.objectValue.intValue, obj.objectValue.intValue);
            Assert.AreEqual(v.objectValue.floatValue, obj.objectValue.floatValue);
            Assert.AreEqual(v.listValue[0].name, obj.listValue[0].name);
            Assert.AreEqual(v.listValue[0].intValue, obj.listValue[0].intValue);
            Assert.AreEqual(v.listValue[0].floatValue, obj.listValue[0].floatValue);
        }
    }
}