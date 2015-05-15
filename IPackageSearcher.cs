namespace PackageManagement
{
    using System.Collections.Generic;

    using Semver;

    interface IPackageSearcher
    {
        IEnumerable<Package> SearchPackages(string name, SemVersion pythonVersion, Bitness bitness);
    }
}
