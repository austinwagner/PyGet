namespace PackageManagement
{
    using System;

    /// <summary>
    ///     A wrapper around <see cref="Exception" />.
    /// </summary>
    [Serializable]
    public class PythonInstallationNotFound : Exception
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonInstallationNotFound"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message.
        /// </param>
        public PythonInstallationNotFound(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonInstallationNotFound"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public PythonInstallationNotFound(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}
