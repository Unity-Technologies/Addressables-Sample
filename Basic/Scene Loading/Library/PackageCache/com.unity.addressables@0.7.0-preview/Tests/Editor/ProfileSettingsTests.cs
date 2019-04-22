using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ProfileSettingsTests : AddressableAssetTestBase
    {
        [Test]
        public void AddRemoveProfile()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            var mainId = m_Settings.profileSettings.Reset();

            //Act 
            var secondId = m_Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Assert
            bool foundIt = false;
            foreach (var prof in m_Settings.profileSettings.profiles)
            {
                if (prof.profileName == "TestProfile")
                    foundIt = true;
            }
            Assert.IsTrue(foundIt);
            Assert.IsNotEmpty(secondId);

            //Act again
            m_Settings.profileSettings.RemoveProfile(secondId);

            //Assert again
            foundIt = false;
            foreach (var prof in m_Settings.profileSettings.profiles)
            {
                if (prof.profileName == "TestProfile")
                    foundIt = true;
            }
            Assert.IsFalse(foundIt);
        }

        [Test]
        public void CreateValuePropogtesValue()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            var mainId = m_Settings.profileSettings.Reset();
            var secondId = m_Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Act
            string path = "/Assets/Important";
            m_Settings.profileSettings.CreateValue("SomePath", path);

            //Assert
            Assert.AreEqual(path, m_Settings.profileSettings.GetValueByName(mainId, "SomePath"));
            Assert.AreEqual(path, m_Settings.profileSettings.GetValueByName(secondId, "SomePath"));
        }
        [Test]
        public void SetValueOnlySetsDesiredProfile()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            var mainId = m_Settings.profileSettings.Reset();
            string originalPath = "/Assets/Important";
            m_Settings.profileSettings.CreateValue("SomePath", originalPath);
            var secondId = m_Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Act
            string newPath = "/Assets/LessImportant";
            m_Settings.profileSettings.SetValue(secondId, "SomePath", newPath);

            //Assert
            Assert.AreEqual(originalPath, m_Settings.profileSettings.GetValueByName(mainId, "SomePath"));
            Assert.AreEqual(newPath, m_Settings.profileSettings.GetValueByName(secondId, "SomePath"));
        }
        [Test]
        public void CanGetValueById()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            var mainId = m_Settings.profileSettings.Reset();
            string originalPath = "/Assets/Important";
            m_Settings.profileSettings.CreateValue("SomePath", originalPath);

            //Act
            string varId = null;
            foreach (var variable in m_Settings.profileSettings.profileEntryNames)
            {
                if (variable.ProfileName == "SomePath")
                {
                    varId = variable.Id;
                    break;
                }
            }

            //Assert
            Assert.AreEqual(originalPath, m_Settings.profileSettings.GetValueById(mainId, varId));
        }
        [Test]
        public void EvaluatingUnknownIdReturnsIdAsResult()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            m_Settings.profileSettings.Reset();

            //Act
            string badIdName = "BadIdName";


            //Assert
            Assert.AreEqual(badIdName, AddressableAssetProfileSettings.ProfileIdData.Evaluate(m_Settings.profileSettings, m_Settings.activeProfileId, badIdName));

        }
        [Test]
        public void MissingVariablesArePassThrough()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;

            //Act
            m_Settings.profileSettings.Reset();

            //Assert
            Assert.AreEqual("VariableNotThere", m_Settings.profileSettings.GetValueById("invalid key", "VariableNotThere"));
        }
        [Test]
        public void CanRenameEntry()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            m_Settings.profileSettings.Reset();
            string entryName = "SomeName";
            string newName = "NewerName";
            string originalPath = "/Assets/Important";
            m_Settings.profileSettings.CreateValue(entryName, originalPath);

            AddressableAssetProfileSettings.ProfileIdData currEntry = null;
            foreach(var entry in m_Settings.profileSettings.profileEntryNames)
            {
                if(entry.ProfileName == entryName)
                {
                    currEntry = entry;
                    break;
                }
            }

            //Act
            Assert.NotNull(currEntry);
            currEntry.SetName(newName, m_Settings.profileSettings);

            //Assert
            Assert.AreEqual(currEntry.ProfileName, newName);
        }
        [Test]
        public void CannotRenameEntryToDuplicateName()
        {
            //Arrange
            Assert.IsNotNull(m_Settings.profileSettings);
            m_Settings.activeProfileId = null;
            m_Settings.profileSettings.Reset();
            string entryName = "SomeName";
            string newName = "NewerName";
            string originalPath = "/Assets/Important";
            m_Settings.profileSettings.CreateValue(entryName, originalPath);
            m_Settings.profileSettings.CreateValue(newName, originalPath);

            AddressableAssetProfileSettings.ProfileIdData currEntry = null;
            foreach (var entry in m_Settings.profileSettings.profileEntryNames)
            {
                if (entry.ProfileName == entryName)
                {
                    currEntry = entry;
                    break;
                }
            }

            //Act
            Assert.NotNull(currEntry);
            currEntry.SetName(newName, m_Settings.profileSettings);

            //Assert
            Assert.AreNotEqual(currEntry.ProfileName, newName);
        }

    }
}