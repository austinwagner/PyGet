namespace PackageManagement
{
    /// <summary>
    ///     Types of sources for Pip installation and searching.
    /// </summary>
    public enum SourceType
    {
        /// <summary>
        ///     A source implementing the PyPI API.
        /// </summary>
        Pypi,

        /// <summary>
        ///     Prebuilt wheel package in the same format as http://www.lfd.uci.edu/~gohlke/pythonlibs/
        /// </summary>
        WheelListing
    }
}
