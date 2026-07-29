namespace Alchemy.Hierarchy
{
    /// <summary>
    /// Specify how to handle HierarchyObject at runtime.
    /// </summary>
    public enum HierarchyObjectMode
    {
        /// <summary>
        /// Treated as a regular GameObject.
        /// </summary>
        None = 0,
        /// <summary>
        /// Removed in play mode.
        /// </summary>
        RemoveInPlayMode = 1,
        /// <summary>
        /// Removed in build.
        /// </summary>
        RemoveInBuild = 2
    }
}
