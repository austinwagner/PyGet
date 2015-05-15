namespace PackageManagement.Rpc
{
    using CookComputing.XmlRpc;

    /// <summary>
    /// The package search result. Only contains fields used by PyGet.
    /// </summary>
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public struct SearchResult
    {
        #region Fields

        /// <summary>
        /// The package name.
        /// </summary>
        [XmlRpcMember("name")]
        public string Name;

        /// <summary>
        /// A summary of the package.
        /// </summary>
        [XmlRpcMember("summary")]
        public string Summary;

        /// <summary>
        /// The package version.
        /// </summary>
        [XmlRpcMember("version")]
        public string Version;

        #endregion
    }
}
