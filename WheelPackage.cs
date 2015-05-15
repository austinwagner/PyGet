namespace PackageManagement
{
    using System;

    using Semver;

    internal class WheelPackage
    {
        public string Name { get; private set; }

        public SemVersion Version { get; private set; }

        public SemVersion PythonVersion { get; private set; }

        public Uri DownloadUri { get; private set; }

        public Bitness Bitness { get; private set; }

        public WheelPackage(string name, SemVersion version, SemVersion pythonVersion, Uri uri, Bitness bitness)
        {
            this.Name = name;
            this.Version = version;
            this.PythonVersion = pythonVersion;
            this.DownloadUri = uri;
            this.Bitness = bitness;
        }
    }
}
