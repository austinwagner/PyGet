namespace PackageManagement
{
    using System.Collections.Generic;
    using System.Linq;

    using CookComputing.XmlRpc;

    using Rpc;

    using Semver;

    internal class PypiSearcher : IPackageSearcher
    {
        private readonly IPypiXmlRpc pypi = XmlRpcProxyGen.Create<IPypiXmlRpc>();

        private readonly string name;

        private readonly string url;

        public PypiSearcher(string name, string url)
        {
            this.name = name;
            this.url = url;
            this.pypi.Url = url;
        }

        public IEnumerable<Package> SearchPackages(string packageName, SemVersion pythonVersion, Bitness bitness)
        {
            var searchParams = new SearchParams { Name = packageName };
            return from result in this.pypi.Search(searchParams)
                   select
                       new Package(
                       FastPath.FromParts(this.name, result.Name, result.Version),
                       result.Name,
                       result.Version,
                       string.Empty,
                       result.Summary,
                       this.name);
        }
    }
}
