namespace PackageManagement.Rpc
{
    using System.Threading;

    using CookComputing.XmlRpc;

    /// <summary>
    ///     A thread-safe wrapper around <see cref="IPypiXmlRpc" />.
    /// </summary>
    public class Pypi
    {
        #region Fields

        /// <summary>
        ///     Thread-local instances of <see cref="IPypiXmlRpc" />
        /// </summary>
        private readonly ThreadLocal<IPypiXmlRpc> proxy;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Pypi" /> class.
        /// </summary>
        public Pypi()
        {
            this.proxy = new ThreadLocal<IPypiXmlRpc>(XmlRpcProxyGen.Create<IPypiXmlRpc>);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the released package versions.
        /// </summary>
        /// <param name="url">
        /// The URL of the package index.
        /// </param>
        /// <param name="packageName">
        /// The package name.
        /// </param>
        /// <param name="showHidden">
        /// Whether to show hidden (usually older) versions.
        /// </param>
        /// <returns>
        /// An array of versions.
        /// </returns>
        public string[] GetPackageReleases(string url, string packageName, bool showHidden)
        {
            this.proxy.Value.Url = url;
            return this.proxy.Value.GetPackageReleases(packageName, showHidden);
        }

        /// <summary>
        /// Searches for a package using the given parameters.
        /// </summary>
        /// <param name="url">
        /// The URL of the package index.
        /// </param>
        /// <param name="filter">
        /// The search parameters.
        /// </param>
        /// <returns>
        /// An array of matching packages.
        /// </returns>
        public SearchResult[] Search(string url, SearchParams filter)
        {
            this.proxy.Value.Url = url;
            return this.proxy.Value.Search(filter);
        }

        #endregion
    }
}
