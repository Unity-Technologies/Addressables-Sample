using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    public class GroupSchemaTests : AddressableAssetTestBase
    {
        CustomTestSchema m_TestSchemaObject;
        CustomTestSchemaSubClass m_TestSchemaObjectSubClass;
        protected override bool PersistSettings { get { return true; } }
        protected override void OnInit()
        {
            m_TestSchemaObject = ScriptableObject.CreateInstance<CustomTestSchema>();
            AssetDatabase.CreateAsset(m_TestSchemaObject, k_TestConfigFolder + "/testSchemaObject.asset");
            m_TestSchemaObjectSubClass = ScriptableObject.CreateInstance<CustomTestSchemaSubClass>();
            AssetDatabase.CreateAsset(m_TestSchemaObjectSubClass, k_TestConfigFolder + "/testSchemaObjectSubClass.asset");
        }

        [Test]
        public void CanAddSchemaWithSavedAsset()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var newSchema = group.AddSchema(m_TestSchemaObject);
            Assert.IsNotNull(newSchema);
            Assert.AreNotEqual(newSchema, m_TestSchemaObject);
            Assert.IsTrue(group.HasSchema(m_TestSchemaObject.GetType()));
            Assert.IsTrue(group.RemoveSchema(m_TestSchemaObject.GetType()));
        }

        [Test]
        public void CanAddSchemaWithSavedAssetGeneric()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var newSchema = group.AddSchema(m_TestSchemaObject);
            Assert.IsNotNull(newSchema);
            Assert.AreNotEqual(newSchema, m_TestSchemaObject);
            Assert.IsTrue(group.HasSchema<CustomTestSchema>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchema>());
        }

        [Test]
        public void CanAddSchemaWithNonSavedAsset()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var templateSchema = ScriptableObject.CreateInstance<CustomTestSchema>();
            var newSchema = group.AddSchema(templateSchema);
            Assert.IsNotNull(newSchema);
            Assert.AreNotEqual(newSchema, templateSchema);
            Assert.IsTrue(group.HasSchema(templateSchema.GetType()));
            Assert.IsTrue(group.RemoveSchema(templateSchema.GetType()));
        }

        [Test]
        public void CanAddAndRemoveSchemaObjectByType()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var s = group.AddSchema(typeof(CustomTestSchema));
            Assert.IsNotNull(s);
            string guid;
            long lfid;
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out guid, out lfid));
            var path = AssetDatabase.GUIDToAssetPath(guid);
            FileAssert.Exists(path);
            Assert.IsTrue(group.RemoveSchema(typeof(CustomTestSchema)));
            FileAssert.DoesNotExist(path);
        }

        [Test]
        public void CanAddAndRemoveSchemaObjectByGenericType()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var s = group.AddSchema<CustomTestSchema>();
            Assert.IsNotNull(s);
            string guid;
            long lfid;
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out guid, out lfid));
            var path = AssetDatabase.GUIDToAssetPath(guid);
            FileAssert.Exists(path);
            Assert.IsTrue(group.RemoveSchema<CustomTestSchema>());
            FileAssert.DoesNotExist(path);
        }

        [Test]
        public void CanCheckSchemaObjectByGenericType()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            Assert.IsNotNull(group.AddSchema<CustomTestSchema>());
            Assert.IsTrue(group.HasSchema<CustomTestSchema>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchema>());
        }

        [Test]
        public void CanCheckSchemaObjectAsSubclass()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            Assert.IsNotNull(group.AddSchema<CustomTestSchemaSubClass>());
            Assert.IsFalse(group.HasSchema<CustomTestSchema>());
            Assert.IsTrue(group.HasSchema<CustomTestSchemaSubClass>());
            Assert.IsFalse(group.RemoveSchema<CustomTestSchema>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchemaSubClass>());
        }

        [Test]
        public void CanCheckSchemaObjectAsBaseclass()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            Assert.IsNotNull(group.AddSchema<CustomTestSchema>());
            Assert.IsTrue(group.HasSchema<CustomTestSchema>());
            Assert.IsFalse(group.HasSchema<CustomTestSchemaSubClass>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchema>());
            Assert.IsFalse(group.RemoveSchema<CustomTestSchemaSubClass>());
        }

        [Test]
        public void CanNotAddDuplicateSchemaObjects()
        {
            var group = Settings.CreateGroup("TestGroup", false, false, false, null);
            var added = group.AddSchema<CustomTestSchemaSubClass>();
            Assert.IsNotNull(added);
            Assert.AreEqual(added, group.AddSchema<CustomTestSchemaSubClass>());
            Assert.IsNotNull(group.AddSchema<CustomTestSchema>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchemaSubClass>());
            Assert.IsTrue(group.RemoveSchema<CustomTestSchema>());
        }
    }

}
