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
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            var mainId = Settings.profileSettings.Reset();

            //Act 
            var secondId = Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Assert
            bool foundIt = false;
            foreach (var prof in Settings.profileSettings.profiles)
            {
                if (prof.profileName == "TestProfile")
                    foundIt = true;
            }
            Assert.IsTrue(foundIt);
            Assert.IsNotEmpty(secondId);

            //Act again
            Settings.profileSettings.RemoveProfile(secondId);

            //Assert again
            foundIt = false;
            foreach (var prof in Settings.profileSettings.profiles)
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
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            var mainId = Settings.profileSettings.Reset();
            var secondId = Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Act
            string path = "/Assets/Important";
            Settings.profileSettings.CreateValue("SomePath", path);

            //Assert
            Assert.AreEqual(path, Settings.profileSettings.GetValueByName(mainId, "SomePath"));
            Assert.AreEqual(path, Settings.profileSettings.GetValueByName(secondId, "SomePath"));
        }
        [Test]
        public void SetValueOnlySetsDesiredProfile()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            var mainId = Settings.profileSettings.Reset();
            string originalPath = "/Assets/Important";
            Settings.profileSettings.CreateValue("SomePath", originalPath);
            var secondId = Settings.profileSettings.AddProfile("TestProfile", mainId);

            //Act
            string newPath = "/Assets/LessImportant";
            Settings.profileSettings.SetValue(secondId, "SomePath", newPath);

            //Assert
            Assert.AreEqual(originalPath, Settings.profileSettings.GetValueByName(mainId, "SomePath"));
            Assert.AreEqual(newPath, Settings.profileSettings.GetValueByName(secondId, "SomePath"));
        }
        [Test]
        public void CanGetValueById()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            var mainId = Settings.profileSettings.Reset();
            string originalPath = "/Assets/Important";
            Settings.profileSettings.CreateValue("SomePath", originalPath);

            //Act
            string varId = null;
            foreach (var variable in Settings.profileSettings.profileEntryNames)
            {
                if (variable.ProfileName == "SomePath")
                {
                    varId = variable.Id;
                    break;
                }
            }

            //Assert
            Assert.AreEqual(originalPath, Settings.profileSettings.GetValueById(mainId, varId));
        }
        [Test]
        public void EvaluatingUnknownIdReturnsIdAsResult()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            Settings.profileSettings.Reset();

            //Act
            string badIdName = "BadIdName";


            //Assert
            Assert.AreEqual(badIdName, AddressableAssetProfileSettings.ProfileIdData.Evaluate(Settings.profileSettings, Settings.activeProfileId, badIdName));

        }
        [Test]
        public void MissingVariablesArePassThrough()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;

            //Act
            Settings.profileSettings.Reset();

            //Assert
            Assert.AreEqual("VariableNotThere", Settings.profileSettings.GetValueById("invalid key", "VariableNotThere"));
        }
        [Test]
        public void CanRenameEntry()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            Settings.profileSettings.Reset();
            string entryName = "SomeName";
            string newName = "NewerName";
            string originalPath = "/Assets/Important";
            Settings.profileSettings.CreateValue(entryName, originalPath);

            AddressableAssetProfileSettings.ProfileIdData currEntry = null;
            foreach(var entry in Settings.profileSettings.profileEntryNames)
            {
                if(entry.ProfileName == entryName)
                {
                    currEntry = entry;
                    break;
                }
            }

            //Act
            Assert.NotNull(currEntry);
            currEntry.SetName(newName, Settings.profileSettings);

            //Assert
            Assert.AreEqual(currEntry.ProfileName, newName);
        }
        [Test]
        public void CannotRenameEntryToDuplicateName()
        {
            //Arrange
            Assert.IsNotNull(Settings.profileSettings);
            Settings.activeProfileId = null;
            Settings.profileSettings.Reset();
            string entryName = "SomeName";
            string newName = "NewerName";
            string originalPath = "/Assets/Important";
            Settings.profileSettings.CreateValue(entryName, originalPath);
            Settings.profileSettings.CreateValue(newName, originalPath);

            AddressableAssetProfileSettings.ProfileIdData currEntry = null;
            foreach (var entry in Settings.profileSettings.profileEntryNames)
            {
                if (entry.ProfileName == entryName)
                {
                    currEntry = entry;
                    break;
                }
            }

            //Act
            Assert.NotNull(currEntry);
            currEntry.SetName(newName, Settings.profileSettings);

            //Assert
            Assert.AreNotEqual(currEntry.ProfileName, newName);
        }

    }
}