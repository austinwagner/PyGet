namespace PackageManagement
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    ///     Maintains <see cref="Source" /> in a configuration file.
    /// </summary>
    internal class SourceManager : ICollection<Source>
    {
        #region Fields

        /// <summary>
        ///     Path to the XML configuration.
        /// </summary>
        private readonly string xmlFile;

        /// <summary>
        ///     The XML configuration as LINQ-to-SQL.
        /// </summary>
        private XElement sources;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SourceManager" /> class.
        /// </summary>
        public SourceManager()
        {
            string pygetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PyGet");
            this.xmlFile = Path.Combine(pygetDir, "pyget.config");
            if (!File.Exists(this.xmlFile))
            {
                Directory.CreateDirectory(pygetDir);
                string defaultConfig =
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetAssembly(typeof(SourceManager)).Location)
                        ?? @"C:\Windows\System32\WindowsPowerShell\v1.0\Modules\OneGet",
                        "pyget.config");
                File.Copy(defaultConfig, this.xmlFile);
            }

            using (var fs = new FileStream(this.xmlFile, FileMode.Open, FileAccess.Read))
            {
                this.sources = XElement.Load(fs).Element("sources");
            }
        }

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return this.Elements.Count();
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the source elements.
        /// </summary>
        private IEnumerable<XElement> Elements
        {
            get
            {
                return this.sources.Elements("source");
            }
        }

        #endregion

        #region Public Indexers

        /// <summary>
        /// Gets a <see cref="Source"/> with the given name.
        /// </summary>
        /// <param name="s">
        /// The name of the source.
        /// </param>
        /// <returns>
        /// The matching <see cref="Source"/>.
        /// </returns>
        public Source this[string s]
        {
            get
            {
                return
                    new Source(
                        this.Elements.First(
                            x => s.Equals(x.Attribute("name").Value, StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Adds a source to the configuration.
        /// </summary>
        /// <param name="source">
        /// The <see cref="Source"/> to add.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if a source with the same name already exists.
        /// </exception>
        public void Add(Source source)
        {
            var old = this.Elements.FirstOrDefault(x => source.Name.EqualsIgnoreCase(x.Attribute("name").Value));
            if (old != null)
            {
                old.Remove();
            }

            this.sources.Add(source.ToXElement());
            this.Save();
        }

        /// <inheritdoc />
        public void Clear()
        {
            this.sources.RemoveAll();
            this.Save();
        }

        /// <inheritdoc />
        public bool Contains(Source item)
        {
            return this.Elements.Any(x => item.Name == x.Attribute("name").Value);
        }

        /// <inheritdoc />
        public void CopyTo(Source[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (var source in this)
            {
                array[i] = source;
                i++;
            }
        }

        /// <summary>
        ///     Gets the sources in the order they are defined.
        /// </summary>
        /// <returns>The configured sources.</returns>
        public IEnumerator<Source> GetEnumerator()
        {
            return this.Elements.Select(x => new Source(x)).GetEnumerator();
        }

        /// <summary>
        /// Gets searchers for the configured sources.
        /// </summary>
        /// <param name="trusted">If the sources must be trusted.</param>
        /// <returns>Searchers for the sources.</returns>
        public IEnumerable<IPackageSearcher> GetSearchers(bool trusted)
        {
            IEnumerable<Source> availableSources = this;
            if (trusted)
            {
                availableSources = availableSources.Where(x => x.Trusted);
            }

            foreach (var source in availableSources)
            {
                switch (source.Type)
                {
                    case SourceType.WheelListing:
                        yield return new InstallerSourceSearcher(source.Name, new Uri(source.Location));
                        break;
                    case SourceType.Pypi:
                        yield return new PyPISearcher(source.Name, source.Location);
                        break;
                    default:
                        throw new Exception("Unknown source type.");
                }
            }
        }

        /// <inheritdoc />
        public bool Remove(Source item)
        {
            return this.Remove(item.Name);
        }

        /// <summary>
        /// Removes a source from the configuration.
        /// </summary>
        /// <param name="name">
        /// The name of the source.
        /// </param>
        /// <returns>
        /// True if the value was removed, otherwise false.
        /// </returns>
        public bool Remove(string name)
        {
            XElement match = this.Elements.FirstOrDefault(x => name == x.Attribute("name").Value);
            if (match == null)
            {
                return false;
            }

            match.Remove();
            this.Save();
            return true;
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        ///     Gets the sources in the order they are defined.
        /// </summary>
        /// <returns>The configured sources.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Save the sources to a file.
        /// </summary>
        private void Save()
        {
            this.sources.Save(this.xmlFile);
        }

        #endregion
    }
}
