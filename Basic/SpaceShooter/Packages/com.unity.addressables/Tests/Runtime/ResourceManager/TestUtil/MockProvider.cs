using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.ResourceManagement.Tests
{
    class MockProvider : IResourceProvider, IUpdateReceiver
    {
        public string _ProviderId = "MockProvider";
        public ProviderBehaviourFlags _BehaviourFlags = ProviderBehaviourFlags.None;
        public List<KeyValuePair<IResourceLocation, object>> ReleaseLog = new List<KeyValuePair<IResourceLocation, object>>();
        public List<IResourceLocation> ProvideLog = new List<IResourceLocation>();

        public int UpdateCount = 0;

        public string ProviderId { get { return _ProviderId; } }

        public ProviderBehaviourFlags BehaviourFlags { get { return _BehaviourFlags; } }

        public Action<ProvideHandle> ProvideCallback;
        public Type DefaultType = typeof(object);

        public Func<Type, IResourceLocation, bool> CanProvideCallback = (x, y) => true;

        public void Update(float unscaledDeltaTime)
        {
            UpdateCount++;
        }

        public void Release(IResourceLocation location, object asset)
        {
            ReleaseLog.Add(new KeyValuePair<IResourceLocation, object>(location, asset));
        }

        public void Provide(ProvideHandle provideHandle)
        {
            ProvideLog.Add(provideHandle.Location);
            if (ProvideCallback != null && (ProvideCallback as Action<ProvideHandle>) != null)
            {
                ProvideCallback(provideHandle);
                return;
            }
            throw new NotImplementedException();
        }

        public Type GetDefaultType(IResourceLocation location)
        {
            return DefaultType;
        }

        public bool CanProvide(Type t, IResourceLocation location)
        {
            return CanProvideCallback(t, location);
        }
    }
}