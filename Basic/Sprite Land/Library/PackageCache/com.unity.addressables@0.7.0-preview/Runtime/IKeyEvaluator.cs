using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Interface for providing a key.  This allows for objects passed into the Addressables system to provied a key instead of being used directly.  
    /// </summary>
    public interface IKeyEvaluator
    {
        /// <summary>
        /// The runtime key to use.
        /// </summary>
        object RuntimeKey { get; }
    }
}

