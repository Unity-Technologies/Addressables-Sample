using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class RuntimePlatformMappingServiceTests
{
    [TestCase(RuntimePlatform.XboxOne, AddressablesPlatform.XboxOne)]
    [TestCase(RuntimePlatform.Switch, AddressablesPlatform.Switch)]
    [TestCase(RuntimePlatform.PS4, AddressablesPlatform.PS4)]
    [TestCase(RuntimePlatform.IPhonePlayer, AddressablesPlatform.iOS)]
    [TestCase(RuntimePlatform.Android, AddressablesPlatform.Android)]
    [TestCase(RuntimePlatform.WebGLPlayer, AddressablesPlatform.WebGL)]
    [TestCase(RuntimePlatform.WindowsPlayer, AddressablesPlatform.Windows)]
    [TestCase(RuntimePlatform.OSXPlayer, AddressablesPlatform.OSX)]
    [TestCase(RuntimePlatform.LinuxPlayer, AddressablesPlatform.Linux)]
    public void RuntimePlatformMappingService_EqualsDesiredAddressablesPlatform(RuntimePlatform platform, AddressablesPlatform desiredPlatform)
    {
        Assert.AreEqual(PlatformMappingService.GetAddressablesPlatformInternal(platform), desiredPlatform);
    }
}
