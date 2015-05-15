namespace PackageManagement.Rpc
{
    using CookComputing.XmlRpc;

    /// <summary>
    ///     XML-RPC .NET interface for Pypi RPC methods. Only contains the methods required by PyGet.
    /// </summary>
    public interface IPypiXmlRpc : IXmlRpcProxy
    {
        #region Public Methods and Operators

        /// <summary>
        /// Gets the released package versions.
        /// </summary>
        /// <param name="packageName">
        /// The package name.
        /// </param>
        /// <param name="showHidden">
        /// Whether to show hidden (usually older) versions.
        /// </param>
        /// <returns>
        /// An array of versions.
        /// </returns>
        [XmlRpcMethod("package_releases")]
        string[] GetPackageReleases(string packageName, bool showHidden);

        /// <summary>
        /// Searches for a package using the given parameters.
        /// </summary>
        /// <param name="filter">
        /// The search parameters.
        /// </param>
        /// <returns>
        /// An array of matching packages.
        /// </returns>
        [XmlRpcMethod("search")]
        SearchResult[] Search(SearchParams filter);

        #endregion
    }
}