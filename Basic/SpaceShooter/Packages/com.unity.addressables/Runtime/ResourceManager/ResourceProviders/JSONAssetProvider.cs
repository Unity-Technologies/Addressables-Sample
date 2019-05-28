using System;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Converts JSON serialized text into the requested object.
    /// </summary>
    public class JsonAssetProvider : TextDataProvider
    {
        /// <summary>
        /// Converts raw text into requested object type via JSONUtility.FromJson.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>Converted object</returns>
        public override object Convert(Type type, string text)
        {
            try
            {
                return JsonUtility.FromJson(text, type);
            }
            catch (Exception e)
            {
                if (!IgnoreFailures)
                    Debug.LogException(e);

                return null;
            }
        }
    }
}