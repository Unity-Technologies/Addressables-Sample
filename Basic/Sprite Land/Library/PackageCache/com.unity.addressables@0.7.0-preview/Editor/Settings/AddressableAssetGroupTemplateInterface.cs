namespace UnityEditor.AddressableAssets.Settings
{
    public interface IGroupTemplate
    {
        /// <summary>
        /// The name of the group, used for the menu and newly created group name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of the Template, to be used as a tooltip
        /// </summary>
        string Description { get; }
    }
}