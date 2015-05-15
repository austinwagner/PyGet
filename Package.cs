using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageManagement
{
    public class Package
    {
        public string Fastpath { get; }

        public string Name { get; }

        public string Version { get; }

        public string VersionScheme { get; }

        public string Summary { get; }

        public string Source { get; }

        public Package(string fastpath, string name, string version, string versionScheme, string summary, string source)
        {
            this.Fastpath = fastpath;
            this.Name = name;
            this.Version = version;
            this.VersionScheme = versionScheme;
            this.Summary = summary;
            this.Source = source;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Package;
            if (other == null) return false;
            return this.Fastpath == other.Fastpath;
        }

        public override int GetHashCode()
        {
            return this.Fastpath.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Name} v{this.Version} ({this.Source})";
        }
    }
}
