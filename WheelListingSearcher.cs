namespace PackageManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using Semver;

    // TODO: Adjust this class to download wheels, not installer exes
    /// <summary>
    ///     Provides methods for searching an installer listing.
    /// </summary>
    internal class WheelListingSearcher : IPackageSearcher
    {
        /// <summary>
        ///     Regex for packages from installer listings.
        /// </summary>
        private static readonly Regex InstallerListingRegex = new Regex(@"<li><a href='javascript:;' onclick='javascript:dl\(\[(?<encodedUrl>[^\]]*)\], ""(?<key>[^""]*)""\)' title='[^']*'>(?<name>\w+)&#8209;(?<version>(?:\w|\.)+)\.win(?<bitness>32|&#8209;amd64)&#8209;py(?<pythonVersion>(?:\w|\.)+).exe</a></li>", RegexOptions.IgnoreCase);

        /// <summary>
        ///     The URI to the installer listing.
        /// </summary>
        private readonly Uri uri;

        private readonly string sourceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerSourceSearcher"/> class.
        /// </summary>
        /// <param name="sourceName">
        /// The name of the installer listing.
        /// </param>
        /// <param name="uri">
        /// The URI to the installer listing.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if URI is not HTTP or HTTPS.
        /// </exception>
        public WheelListingSearcher(string sourceName, Uri uri)
        {
            if (uri.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase)
                && !uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Invalid URI scheme. Must be http or https.", nameof(uri));
            }

            this.uri = uri;
            this.sourceName = sourceName;
        }

        /// <summary>
        /// Searches for a package by name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="pythonVersion">The Python version to search for.</param>
        /// <param name="bitness">The bits of the Python installation.</param>
        /// <returns>A list of packages that match the search.</returns>
        public IEnumerable<Package> SearchPackages(string name, SemVersion pythonVersion, Bitness bitness)
        {
            return from package in this.GetPackages()
                   where
                       package.PythonVersion == pythonVersion && package.Name.ContainsIgnoreCase(name)
                       && package.Bitness == bitness
                   select
                       new Package(
                       FastPath.FromParts(
                           this.sourceName,
                           package.Name,
                           package.Version.ToString(),
                           package.DownloadUri),
                       package.Name,
                       package.Version.ToString(),
                       string.Empty,
                       string.Empty,
                       this.sourceName);
        }

        /// <summary>
        /// Gets a package list from the installer listing.
        /// </summary>
        /// <returns>A list of <see cref="InstallerPackage"/>.</returns>
        private IEnumerable<WheelPackage> GetPackages()
        {
            var req = (HttpWebRequest)WebRequest.Create(this.uri);
            var resp = (HttpWebResponse)req.GetResponse();

            return from line in resp.GetResponseStream().ReadLines()
                   where InstallerListingRegex.IsMatch(line)
                   select this.InstallerPacakgeFromString(line);
        }

        /// <summary>
        /// Creates an <see cref="InstallerPackage"/> from a line from the installer listing.
        /// </summary>
        /// <param name="s">The line to parse.</param>
        /// <returns>A new <see cref="InstallerPackage"/>.</returns>
        private WheelPackage InstallerPacakgeFromString(string s)
        {
            var match = InstallerListingRegex.Match(s);
            var encodedUrl = match.Groups["encodedUrl"].Value.Split(',').Select(int.Parse).ToArray();
            var key = match.Groups["key"].Value.Select(x => x - 48);

            var sb = new StringBuilder(match.Groups["encodedUrl"].Value.Length);
            foreach (var i in key)
            {
                sb.Append((char)encodedUrl[i]);
            }

            var downloadUri = new Uri(this.uri + "/" + sb);

            var bitness = match.Groups["bitness"].Value == "32" ? Bitness.X86 : Bitness.X64;

            return new WheelPackage(
                match.Groups["name"].Value,
                SemVersion.Parse(match.Groups["version"].Value),
                SemVersion.Parse(match.Groups["pythonVersion"].Value),
                downloadUri,
                bitness);
        }
    }
}
