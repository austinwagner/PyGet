namespace PackageManagement
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Semver;

    internal class Python
    {
        private enum MachineType
        {
            Native = 0, I386 = 0x014c, Itanium = 0x0200, X64 = 0x8664
        }

        public SemVersion Version
        {
            get
            {
                string version = null;
                Process proc = Helpers.Execute(
                    System.IO.Path.Combine(this.Path, "python.exe"),
                    "-V",
                    (sender, args) => version = args.Data.Split(' ')[1],
                    null);
                proc.WaitForExit();

                if (proc.ExitCode != 0 || version == null)
                {
                    throw new Exception("Could not start Python to get version.");
                }

                return SemVersion.Parse(version);
            }
        }

        public string Path { get; private set; }

        public Bitness Bitness
        {
            get
            {
                var machineType = GetMachineType();
                switch (machineType)
                {
                    case MachineType.I386:
                        return Bitness.X86;
                    case MachineType.X64:
                        return Bitness.X64;
                    default:
                        throw new Exception("Unknown machine type.");
                }
            }
        }

        public Python(string path)
        {
            this.Path = path;
        }

        private MachineType GetMachineType()
        {
            const int PePointerOffset = 60;
            const int MachineOffset = 4;
            byte[] data = new byte[4096];
            using (Stream s = new FileStream(this.Path, FileMode.Open, FileAccess.Read))
            {
                s.Read(data, 0, 4096);
            }

            // dos header is 64 bytes, last element, long (4 bytes) is the address of the PE header
            int peHeaderAddr = BitConverter.ToInt32(data, PePointerOffset);
            int machineUint = BitConverter.ToUInt16(data, peHeaderAddr + MachineOffset);
            return (MachineType)machineUint;
        }
    }
}
