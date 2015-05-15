namespace PackageManagement
{
    using System;
    using System.Diagnostics;

    public static class Helpers
    {
        /// <summary>
        /// Runs a program and redirects output to PowerShell.
        /// </summary>
        /// <param name="exe">
        /// The path to the file to execute.
        /// </param>
        /// <param name="parameters">
        /// The arguments to send to the program.
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
        public static Process Execute(
            string exe,
            string parameters,
            DataReceivedEventHandler stdOutHandler,
            DataReceivedEventHandler stdErrHandler)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = parameters,
                    CreateNoWindow = true,
                    RedirectStandardOutput = stdOutHandler != null,
                    RedirectStandardError = stdErrHandler != null,
                    UseShellExecute = false
                }
            };

            if (stdOutHandler != null)
            {
                proc.OutputDataReceived += stdOutHandler;
            }

            if (stdErrHandler != null)
            {
                proc.ErrorDataReceived += stdErrHandler;
            }

            if (!proc.Start())
            {
                throw new Exception($"Process {exe} failed to start.");
            }

            if (stdOutHandler != null)
            {
                proc.BeginOutputReadLine();
            }

            if (stdErrHandler != null)
            {
                proc.BeginErrorReadLine();
            }

            return proc;
        }
    }
}
