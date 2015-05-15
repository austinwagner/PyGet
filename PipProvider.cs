using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: Merge this code into PackageProvider.cs

namespace PackageManagement
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Win32;

    using MoreLinq;

    using PyGet.Rpc;
    using PyGet.Utility;

    using Semver;

    using PsCallback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>;

    /// <summary>
    ///     A OneGet provider that manages Python packages. Wraps Pip.
    /// </summary>
    public class PipProvider
    {
        #region Constants

        /// <summary>
        ///     The provider name for use by OneGet.
        /// </summary>
        public const string ProviderName = "Pip";

        #endregion

        #region Static Fields

        /// <summary>
        ///     Regex for the output of `pip list`.
        /// </summary>
        private static readonly Regex PipListRegex = new Regex(@"^(?<package>[^ ]+) \((?<version>.+)\)");

        /// <summary>
        ///     Represents the largest possible <see cref="SemVersion" />.
        /// </summary>
        private static readonly SemVersion MaxSemVersion = new SemVersion(int.MaxValue, int.MaxValue, int.MaxValue);

        /// <summary>
        ///     Represents the lowest possible <see cref="SemVersion" />.
        /// </summary>
        private static readonly SemVersion MinSemVersion = new SemVersion(0);

        /// <summary>
        ///     A list of all URL schemes supported by Pip.
        /// </summary>
        private static readonly string[] SupportedSchemes =
            {
                "git+git", "git+https", "git+ssh", "hg+http", "hg+https", "hg+ssh",
                "svn+svn", "svn+http", "bzr+http", "bzr+sftp", "bzr+ssh", "bzr+ftp",
                "bzr+lp"
            };

        /// <summary>
        ///     A list of all file types supported by Pip.
        /// </summary>
        private static readonly string[] SupportedFileTypes = { "zip", "tar.gz", "whl" };

        #endregion

        #region Fields

        /// <summary>
        ///     Provides PyPI RPC functions.
        /// </summary>
        private readonly PyPI pypi = new PyPI();

        /// <summary>
        ///     The source manager.
        /// </summary>
        private SourceManager sources = new SourceManager();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Adds a package source.
        /// </summary>
        /// <param name="name">
        /// The name of the package source.
        /// </param>
        /// <param name="location">
        /// The URL of the package index.
        /// </param>
        /// <param name="trusted">
        /// If the package source should be considered as trusted.
        /// </param>
        /// <param name="validateLocation">
        /// Whether or not the location should be verified as a proper source.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        public void AddPackageSource(string name, string location, bool trusted, bool validateLocation, PsCallback c)
        {
            using (var request = new Request(c))
            {
                var sourceType = SourceType.Pypi;
                string typeString;
                if (request.TryGetSwitch("SourceType", out typeString))
                {
                    sourceType = (SourceType)Enum.Parse(typeof(SourceType), typeString, true);
                }

                this.sources.Add(new Source(name, location, trusted, sourceType));

                request.YieldSource(name, location, trusted);
            }
        }

        /// <summary>
        /// Searches all of the package sources that implement the PyPI XML RPC API
        ///     for a package meeting the optional version requirements. Calls
        ///     <see cref="Request.YieldPackage"/> for each match.
        /// </summary>
        /// <param name="name">
        /// The string to search for.
        /// </param>
        /// <param name="requiredVersion">
        /// Optional. The explicit version requirement.
        /// </param>
        /// <param name="minimumVersion">
        /// Optional. The minimum version requirement.
        /// </param>
        /// <param name="maximumVersion">
        /// Optional. The maximum version requirement.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if a matching package was found, otherwise false.
        /// </returns>
        public bool FindPackage(
            string name,
            string requiredVersion,
            string minimumVersion,
            string maximumVersion,
            PsCallback c)
        {
            using (var request = new Request(c))
            {
                SemVersion reqVersion = ParseSemVersion(requiredVersion);
                SemVersion minVersion = ParseSemVersion(minimumVersion) ?? MinSemVersion;
                SemVersion maxVersion = ParseSemVersion(maximumVersion) ?? MaxSemVersion;

                var python = new Python(GetPythonPathFromSwitches(request));

                var results = from searcher in this.sources.GetSearchers(false)
                              from package in searcher.SearchPackages(name, python.Version, python.Bitness)
                              let sem = ParseSemVersion(package.Version)
                              where
                                  (reqVersion != null && sem == reqVersion)
                                  || (reqVersion == null && sem >= minVersion && sem <= maxVersion)
                              select package;

                results = from result in results
                          group result by new { result.Name, result.Source }
                          into g
                          select g.MaxBy(x => x.Version);

                foreach (var result in results)
                {
                    request.YieldPackage(result);
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a list of supported file extensions.
        /// </summary>
        /// <returns>A list of file extensions.</returns>
        public IEnumerable<string> GetFileExtensions()
        {
            return SupportedFileTypes;
        }

        /// <summary>
        /// Gets the available switches. Calls <see cref="Request.YieldOptionDefinition"/>
        ///     for each switch.
        /// </summary>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        public void GetOptionDefinitions(PsCallback c)
        {
            using (var request = new Request(c))
            {
                foreach (var category in Enum.GetValues(typeof(OptionCategory)).Cast<OptionCategory>())
                {
                    request.YieldOptionDefinition(category, "PythonPath", OptionType.String, false, null);
                    request.YieldOptionDefinition(category, "PythonVersion", OptionType.String, false, null);
                }
            }
        }

        /// <summary>
        /// Gets the installed packages. Calls <see cref="Request.YieldPackage"/> for each installed package.
        /// </summary>
        /// <param name="name">
        /// Optional. The name of a package.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if package listing succeeded, otherwise false.
        /// </returns>
        public bool GetInstalledPackages(string name, PsCallback c)
        {
            using (var request = new Request(c))
            {
                bool result = true;

                Process pip = ExecutePip(
                    "list",
                    request,
                    (sender, args) =>
                    {
                        GroupCollection groups = PipListRegex.Match(args.Data).Groups;
                        string package = groups["package"].Value;
                        string version = groups["version"].Value;
                        if (!string.IsNullOrEmpty(name)
                            && name.Equals(package, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            result &= request.YieldPackage(
                                FastPath.FromParts("pypi", package, version),
                                package,
                                version,
                                string.Empty,
                                string.Empty,
                                "pypi");
                        }
                    },
                        null);

                pip.WaitForExit();
                return result;
            }
        }

        /// <summary>
        /// Gets the registered package sources. Calls <see cref="Request.YieldSource"/> for each source.
        /// </summary>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if there are sources, otherwise false.
        /// </returns>
        public bool GetPackageSources(PsCallback c)
        {
            using (var request = new Request(c))
            {
                return this.sources.All(s => request.YieldSource(s.Name, s.Location, s.Trusted));
            }
        }

        /// <summary>
        /// Gets the name of this provider.
        /// </summary>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// The name of this provider.
        /// </returns>
        public string GetProviderName(PsCallback c)
        {
            return ProviderName;
        }

        /// <summary>
        /// Gets a list of supported URL schemes.
        /// </summary>
        /// <returns>A list of schemes.</returns>
        public IEnumerable<string> GetSchemes()
        {
            return SupportedSchemes;
        }

        /// <summary>
        /// Installs a package based on a fast path. Fast path is formatted as source/package/version. Slashes (/)
        ///     and backslashes (\) are escaped with a backslash.
        /// </summary>
        /// <param name="fastPath">
        /// The package fast path to install.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if the package installed successfully, otherwise false.
        /// </returns>
        public bool InstallPackageByFastpath(string fastPath, PsCallback c)
        {
            using (var request = new Request(c))
            {
                var parts = new FastPath(fastPath);
                Source source = this.sources[parts.Source];
                string parameters;
                string file = null;

                if (source.Type == SourceType.WheelListing)
                {
                    file = DownloadExeAsWheel(parts.DownloadUri, request);
                    parameters = "install " + file;
                }
                else
                {
                    parameters = string.Format("install \"{0}=={1}\"", parts.Package, parts.Version);
                }

                Process pip = ExecutePip(parameters, request);

                pip.WaitForExit();

                if (file != null)
                {
                    TryDelete(Path.GetDirectoryName(file));
                }

                return pip.ExitCode == 0;
            }
        }

        /// <summary>
        /// Installs a package directly from a file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file to install.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if the package installed successfully, otherwise false.
        /// </returns>
        public bool InstallPackageByFile(string filePath, PsCallback c)
        {
            using (var request = new Request(c))
            {
                string parameters = "install " + filePath;
                Process pip = ExecutePip(parameters, request);
                pip.WaitForExit();
                return pip.ExitCode == 0;
            }
        }

        /// <summary>
        /// Checks if a package source is marked as trusted.
        /// </summary>
        /// <param name="packageSource">
        /// The name of the package source.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if the source is trusted, otherwise false.
        /// </returns>
        public bool IsTrustedPackageSource(string packageSource, PsCallback c)
        {
            Source source =
                this.sources.FirstOrDefault(
                    s => s.Name.Equals(packageSource, StringComparison.InvariantCultureIgnoreCase));

            if (source == null)
            {
                throw new ArgumentException("No source with the name " + packageSource + " exists.", "packageSource");
            }

            return source.Trusted;
        }

        /// <summary>
        /// Checks if a package source is valid.
        /// </summary>
        /// <param name="packageSource">
        /// The name of the package source.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if the source is valid, otherwise false.
        /// </returns>
        public bool IsValidPackageSource(string packageSource, PsCallback c)
        {
            return this.sources.Any(s => s.Name.Equals(packageSource, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Removes a package source.
        /// </summary>
        /// <param name="packageSource">
        /// The name of the package source to remove.
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        public void RemovePackageSource(string packageSource, PsCallback c)
        {
            this.sources.Remove(packageSource);
        }

        /// <summary>
        /// Uninstalls a package.
        /// </summary>
        /// <param name="fastPath">
        /// The fast path of the package.
        ///     <seealso cref="InstallPackageByFastpath"/>
        /// </param>
        /// <param name="c">
        /// The PowerShell callback.
        /// </param>
        /// <returns>
        /// True if the package uninstalled successfully, otherwise false.
        /// </returns>
        public bool UninstallPackage(string fastPath, PsCallback c)
        {
            using (var request = new Request(c))
            {
                var fp = new FastPath(fastPath);
                Process proc = ExecutePip("uninstall " + fp.Package, request);
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if Pip is available. If not, Pip is installed.
        /// </summary>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <returns>
        /// The path to Pip.
        /// </returns>
        private static string CheckForAndInstallPip(Request request)
        {
            string path = GetPythonPathFromSwitches(request);
            string pip = Path.Combine(path, "Scripts", "pip.exe");

            if (!File.Exists(pip))
            {
                if (!request.AskPermission("Pip not found. Install?"))
                {
                    throw new Exception("Permission to install Pip denied by user.");
                }

                string easyInstall = Path.Combine(path, "Scripts", "easy_install.exe");
                if (!File.Exists(easyInstall))
                {
                    request.Message("Setuptools not found. Installing...");
                    InstallSetuptools(path, request);
                }

                var proc = Execute(easyInstall, "pip", request);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    throw new Exception("Pip installation failed.");
                }

                request.Message("Pip installed.");
            }

            return pip;
        }

        /// <summary>
        /// Checks if Wheel is available. If not, Wheel is installed.
        /// </summary>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <returns>
        /// The path to Wheel.
        /// </returns>
        private static string CheckForAndInstallWheel(Request request)
        {
            CheckForAndInstallPip(request);
            string path = GetPythonPathFromSwitches(request);
            string wheel = Path.Combine(path, "Scripts", "wheel.exe");

            if (!File.Exists(wheel))
            {
                if (!request.AskPermission("Wheel not found. Install?"))
                {
                    throw new Exception("Permission to install Wheel denied by user.");
                }

                var proc = ExecutePip("install wheel", request);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    throw new Exception("Wheel installation failed.");
                }

                request.Message("Wheel installed.");
            }

            return wheel;
        }

        /// <summary>
        /// Creates a temporary directory with a random name.
        /// </summary>
        /// <returns>The path to the directory.</returns>
        private static string CreateTempDir()
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(tempDir);
                    return tempDir;
                }
                catch (IOException)
                {
                    // Buy a lotto ticket if you manage to get here.
                    if (i >= 5)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Downloads an installer and converts it to a wheel.
        /// </summary>
        /// <param name="downloadUrl">The installer to download.</param>
        /// <param name="request">The PowerShell request.</param>
        /// <returns>The path to the wheel file.</returns>
        private static string DownloadExeAsWheel(Uri downloadUrl, Request request)
        {
            string wheel = CheckForAndInstallWheel(request);
            string dir = CreateTempDir();
            string file = Path.Combine(dir, downloadUrl.Segments.Last());
            request.DownloadFile(downloadUrl.ToString(), file);
            Process proc = Execute(wheel, "convert -d \"" + dir + "\" \"" + file + "\"", request);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Wheel conversion failed.");
            }

            TryDelete(file);

            return Directory.GetFiles(dir, "*.whl").First();
        }

        /// <summary>
        /// Runs Pip based off of the PowerShell command parameters.
        /// </summary>
        /// <param name="parameters">
        /// The arguments to send to Pip.
        /// </param>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <param name="stdOutHandler">
        /// Callback when a line is written to the standard output stream. Can be null.
        /// </param>
        /// <param name="stdErrHandler">
        /// Callback when a line is written to the standard error stream. Can be null.
        /// </param>
        /// <returns>
        /// The <see cref="Process"/> that was started.
        /// </returns>
        private static Process ExecutePip(
            string parameters,
            Request request,
            DataReceivedEventHandler stdOutHandler,
            DataReceivedEventHandler stdErrHandler)
        {
            string pip = CheckForAndInstallPip(request);
            return Helpers.Execute(pip, parameters, stdOutHandler, stdErrHandler);
        }

        /// <summary>
        /// Runs Pip based off of the PowerShell command parameters.
        /// </summary>
        /// <param name="parameters">
        /// The arguments to send to Pip.
        /// </param>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <returns>
        /// The <see cref="Process"/> that was started.
        /// </returns>
        private static Process ExecutePip(
            string parameters,
            Request request)
        {
            return ExecutePip(
                parameters,
                request,
                (sender, args) => request.Message(args.Data),
                (sender, args) => request.Error(args.Data));
        }


        /// <summary>
        /// Runs a program and redirects output to PowerShell.
        /// </summary>
        /// <param name="exe">
        /// The path to the file to execute.
        /// </param>
        /// <param name="parameters">
        /// The arguments to send to the program.
        /// </param>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <returns>
        /// The <see cref="Process"/> that was started.
        /// </returns>
        private static Process Execute(string exe, string parameters, Request request)
        {
            return Helpers.Execute(
                exe,
                parameters,
                (sender, args) => request.Message(args.Data),
                (sender, args) => request.Error(args.Data));
        }

        /// <summary>
        /// Searches the PATH environment variable for a file.
        /// </summary>
        /// <param name="file">
        /// The filename to search for.
        /// </param>
        /// <returns>
        /// The path to the first directory that contains the file.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown if the file is not in any directory on the PATH environment variable.
        /// </exception>
        private static string FindOnPath(string file)
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var result =
                path.Split(Path.PathSeparator)
                    .Select(x => new { Path = x, File = Path.Combine(x, file) })
                    .FirstOrDefault(x => File.Exists(x.File));
            if (result == null)
            {
                throw new FileNotFoundException("Unable to locate file on PATH.", file);
            }

            return result.Path;
        }

        /// <summary>
        ///     Searches the PATH environment variable for Python.
        /// </summary>
        /// <returns>
        ///     The path to the first directory that contains Python.
        /// </returns>
        /// <exception cref="PythonInstallationNotFound">
        ///     Thrown if the file is not in any directory on the PATH environment variable.
        /// </exception>
        private static string FindPythonOnPath()
        {
            try
            {
                return FindOnPath("python.exe");
            }
            catch (FileNotFoundException ex)
            {
                throw new PythonInstallationNotFound(ex.Message, ex);
            }
        }

        /// <summary>
        /// Gets the path to the desired Python installation from the given PowerShell switches.
        ///     -PythonPath gives an explicit path to an installation.
        ///     -PythonVersion gives the Major.Minor version to search for.
        /// </summary>
        /// <param name="request">
        /// The PowerShell request.
        /// </param>
        /// <returns>
        /// The path to a Python installation.
        /// </returns>
        /// <exception cref="PythonInstallationNotFound">
        /// Thrown if a Python installation is not found.
        /// </exception>
        private static string GetPythonPathFromSwitches(Request request)
        {
            string pythonPath;

            if (!request.TryGetSwitch("PythonPath", out pythonPath))
            {
                string pythonVersion;
                pythonPath = request.TryGetSwitch("PythonVersion", out pythonVersion)
                                 ? GetPythonPathFromVersion(pythonVersion)
                                 : FindPythonOnPath();
            }

            return pythonPath;
        }

        /// <summary>
        /// Gets the path to a Python installation based off of a version.
        /// </summary>
        /// <param name="version">
        /// The Major.Minor version of Python.
        /// </param>
        /// <returns>
        /// The path to a Python installation.
        /// </returns>
        /// <exception cref="PythonInstallationNotFound">
        /// Thrown if a Python installation could not be found for the given version.
        /// </exception>
        private static string GetPythonPathFromVersion(string version)
        {
            string regPath = "SOFTWARE\\Python\\PythonCore\\" + version + "InstallPath";
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(regPath) ?? Registry.LocalMachine.OpenSubKey(regPath);

            if (regKey == null)
            {
                throw new PythonInstallationNotFound("Could not find any Python installations.");
            }

            var path = regKey.GetValue(null) as string;
            if (path == null)
            {
                throw new PythonInstallationNotFound("Could not find installation of Python version " + version);
            }

            return path;
        }

        /// <summary>
        /// Downloads and installs Setuptools.
        /// </summary>
        /// <param name="path">The path to the Python installation to install Setuptools for.</param>
        /// <param name="request">The PowerShell request.</param>
        private static void InstallSetuptools(string path, Request request)
        {
            string python = Path.Combine(path, "python.exe");
            string tempFile = Path.GetTempFileName();
            request.DownloadFile("https://bootstrap.pypa.io/ez_setup.py", tempFile);

            var proc = Execute(python, tempFile, request);
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new Exception("Setuptools installation failed.");
            }

            TryDelete(tempFile);
        }

        /// <summary>
        /// Parses a <see cref="SemVersion"/> from a version string.
        /// </summary>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <returns>
        /// The <see cref="SemVersion"/> represented by the version string.
        /// </returns>
        private static SemVersion ParseSemVersion(string version)
        {
            if (version == null)
            {
                return null;
            }

            SemVersion semVersion;
            SemVersion.TryParse(version, out semVersion);
            return semVersion;
        }

        /// <summary>
        /// Attempt to delete the file or directory and ignore if it fails.
        /// </summary>
        /// <param name="path">The path to delete.</param>
        private static void TryDelete(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }

        #endregion
    }
}
