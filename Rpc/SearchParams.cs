namespace PackageManagement.Rpc
{
    using CookComputing.XmlRpc;

    /// <summary>
    /// Search parameters for PyPI search. Only contains fields used by PyGet.
    /// </summary>
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public struct SearchParams
    {
        #region Fields

        /// <summary>
        /// The package name or partial package name.
        /// </summary>
        [XmlRpcMember("name")]
        public string Name;

        #endregion
    }
}
