using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ReversalRooms.Engine.Resources
{
    public static class FileSystem
    {
        public static DirectoryInfo RootDirectory;

        static FileSystem()
        {
            var assemblyFile = new FileInfo(typeof(Program).Assembly.Location);
            var assemblyDir = assemblyFile.Directory;

            RootDirectory = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "Modules"));
        }
    }
}
