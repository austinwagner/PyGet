namespace PackageManagement
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Represents a package by source, name, and version.
    /// </summary>
    internal class FastPath
    {
        #region Static Fields

        /// <summary>
        ///     Matches a fast path in the format source/package/version. Slashes (/) and backslashes (\) in the
        ///     components must be escaped with a backslash.
        /// </summary>
        private static readonly Regex FastPathMatch =
            new Regex(@"^(?<source>.*?(?<!\\)(?:\\\\)*)/(?<package>.*?(?<!\\)(?:\\\\)*)/(?<version>.*?(?<!\\)(?:\\\\)*)(?:$|/)(?<uri>.*)");

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FastPath"/> class.
        /// </summary>
        /// <param name="source">
        /// The package source name.
        /// </param>
        /// <param name="package">
        /// The package name.
        /// </param>
        /// <param name="version">
        /// The package version.
        /// </param>
        public FastPath(string source, string package, string version)
        {
            this.Source = source;
            this.Package = package;
            this.Version = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastPath"/> class.
        /// </summary>
        /// <param name="fastPath">
        /// The fast path.
        /// </param>
        public FastPath(string fastPath)
        {
            GroupCollection matches = FastPathMatch.Match(fastPath).Groups;
            this.Source = UnescapeFastpath(matches["source"].Value);
            this.Package = UnescapeFastpath(matches["package"].Value);
            this.Version = UnescapeFastpath(matches["version"].Value);
            this.DownloadUri = matches["uri"].Success ? new Uri(matches["uri"].Value) : null;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the name of the package.
        /// </summary>
        public string Package { get; private set; }

        /// <summary>
        ///     Gets the name of the package source.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        ///     Gets the version of the package.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        ///     Gets the URI to download the package from.
        /// </summary>
        public Uri DownloadUri { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a fast path string from its components. Escapes necessary characters.
        /// </summary>
        /// <param name="source">
        /// The package source.
        /// </param>
        /// <param name="package">
        /// The name of the package.
        /// </param>
        /// <param name="version">
        /// The package version.
        /// </param>
        /// <param name="downloadUri">
        /// The location where the package can be downloaded from.
        /// </param>
        /// <returns>
        /// The fast path for the package.
        /// </returns>
        public static string FromParts(string source, string package, string version, Uri downloadUri = null)
        {
            var fp = string.Format(
                "{0}/{1}/{2}",
                EscapeFastpath(source),
                EscapeFastpath(package),
                EscapeFastpath(version));
            if (downloadUri != null)
            {
                fp += "/" + EscapeFastpath(downloadUri.ToString());
            }

            return fp;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return FromParts(this.Source, this.Package, this.Version, this.DownloadUri);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Escapes a string for use in a fast path.
        /// </summary>
        /// <param name="str">
        /// The string to escape.
        /// </param>
        /// <returns>
        /// The escaped string.
        /// </returns>
        private static string EscapeFastpath(string str)
        {
            return str.Replace("\\", "\\\\").Replace("/", "\\/");
        }

        /// <summary>
        /// Strips fast path escaping from a string.
        /// </summary>
        /// <param name="fastpath">
        /// The fast path to un-escape.
        /// </param>
        /// <returns>
        /// The fast path with escape characters removed.
        /// </returns>
        private static string UnescapeFastpath(string fastpath)
        {
            return fastpath.Replace("\\/", "/").Replace("\\\\", "\\");
        }

        #endregion
    }
}
