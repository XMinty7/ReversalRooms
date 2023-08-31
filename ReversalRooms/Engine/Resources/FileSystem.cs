using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ReversalRooms.Engine.Resources
{
    public static class FileSystem
    {
        /// <summary>
        /// Represents a file
        /// </summary>
        public struct File
        {
            /// <summary>
            /// Readable stream containing contents of the file
            /// </summary>
            public readonly Stream Contents;

            /// <summary>
            /// Name of the file, including the extension
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Full path to the file
            /// </summary>
            public readonly string Path;

            public File(Stream contents, string name, string path)
            {
                Contents = contents;
                Path = path;
                Name = name;
            }

            /// <summary>
            /// Reads out the contents of the file stream as an array of bytes
            /// </summary>
            /// <returns>The resultant bytes</returns>
            public byte[] GetBytes()
            {
                var bytes = new byte[Contents.Length];
                Contents.Read(bytes, 0, bytes.Length);
                return bytes;
            }

            /// <summary>
            /// Reads out the contents of the file stream as a string
            /// </summary>
            /// <param name="encoding">The encoding to use, UTF-8 is used if not specified</param>
            /// <returns>The resultant string</returns>
            public string GetString(Encoding encoding = null)
            {
                if (encoding == null) encoding = Encoding.UTF8;
                return encoding.GetString(GetBytes());
            }
        }

        /// <summary>
        /// Represents the root modules directory of the game
        /// </summary>
        public static readonly DirectoryInfo RootDirectory;

        /// <summary>
        /// Represents the path to the root modules directory of the game
        /// </summary>
        public static readonly string RootPath;
        
        private static Dictionary<string, ZipArchive> ZipCache = new();

        static FileSystem()
        {
            // Locate assembly
            var assemblyFile = new FileInfo(typeof(Program).Assembly.Location);
            var assemblyDir = assemblyFile.Directory;

            // Initiate paths
            RootDirectory = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "Modules"));
            RootPath = Path.Combine(RootDirectory.FullName);
        }

        /// <summary>
        /// Normalizes and validates a path relative to the modules root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the modules root path</param>
        /// <returns>A normalized full path</returns>
        public static string NormalizeRootPath(string path, out string indexPath)
        {
            return NormalizeRelativePath(RootPath, path, out indexPath);
        }

        /// <summary>
        /// Normalizes and validates a path relative to a specified root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="root">The root path to use</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the specified root path</param>
        /// <returns>A normalized full path</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified path is not derived from the specified root path</exception>
        public static string NormalizeRelativePath(string root, string path, out string indexPath)
        {
            // Use DirectoryInfo to perform some light validation on the root path and normalize platform separation characters
            root = new DirectoryInfo(root).FullName;

            // Set current directory to the specified root path for Path.GetFullPath to use that as a root path
            Directory.SetCurrentDirectory(root);

            // Remove any leading directory separation characters which would cause Path.GetFullPath to resolve from the system root
            while (path[0] == '/' || path[0] == '\\') path = path.Substring(1);

            // Resolve the path from the root
            path = Path.GetFullPath(path);

            // After resolving the path, it should start with the specified root path
            if (path.Length <= root.Length || !path.StartsWith(root))
            {
                throw new ArgumentOutOfRangeException("path", "Specified file path is not derived from the specified root path");
            }

            // Set index path as a normalized unix-style path string of the specified path relative to the specified root path
            indexPath = path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            return path;
        }

        /// <summary>
        /// Clears any ZIP archives cached in memory
        /// </summary>
        public static void ClearZipCache() => ZipCache.Clear();

        /// <summary>
        /// Retrieves from cache or reads a ZIP archive located at a specified path
        /// </summary>
        /// <param name="path">Path to the ZIP archive</param>
        /// <param name="useCache">Whether to check if the ZIP is loaded in cache or not</param>
        /// <returns>The resultant ZIP archive</returns>
        /// <exception cref="FileNotFoundException">Thrown if no file is located at the specified file path</exception>
        public static ZipArchive GetZipArchive(string path, bool useCache = true)
        {
            // Normalize
            path = NormalizeRootPath(path, out var indexPath);

            // Check cache if allowed
            if (useCache)
            {
                if (ZipCache.TryGetValue(indexPath, out var archive)) return archive;
            }

            // Check file system for the file
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                // Found, load and return
                var archive = new ZipArchive(fileInfo.OpenRead(), ZipArchiveMode.Read, false);
                if (ZipCache.ContainsKey(indexPath)) ZipCache[indexPath] = archive;
                else ZipCache.Add(indexPath, archive);
                return archive;
            }

            // Not found
            throw new FileNotFoundException("Specified file path does not exist", path);
        }

        /// <summary>
        /// Attempts to retrieve from cache or reads a ZIP archive located at a specified path
        /// </summary>
        /// <param name="path">Path to the ZIP archive</param>
        /// <param name="archive">The resultant ZIP archive if successful</param>
        /// <param name="useCache">Whether to check if the ZIP is loaded in cache or not</param>
        /// <returns>Whether a ZIp archive was successfully retrieved or read or not</returns>
        public static bool TryGetZipArchive(string path, out ZipArchive archive, bool useCache = true)
        {
            try
            {
                archive = GetZipArchive(path, useCache);
                return true;
            }
            catch
            {
                archive = null;
                return false;
            }
        }

        /// <summary>
        /// Opens a read-only stream of a file at a specified path
        /// </summary>
        /// <param name="path">Path to the file to read</param>
        /// <returns>A read-only stream of the specified file</returns>
        /// <exception cref="FileNotFoundException">Thrown if no file exists at the specified path</exception>
        public static Stream ReadFile(string path)
        {
            // Normalize path
            path = NormalizeRootPath(path, out _);

            // Check file system for the file
            var fileInfoNaive = new FileInfo(path);
            if (fileInfoNaive.Exists) return fileInfoNaive.OpenRead();
            // Nothing to read if the file doesn't exist and no ZIP file is specified as the path or part of the path
            if (!path.Contains(".zip")) throw new FileNotFoundException("Specified file path does not exist", path);

            // If a ZIP file is specified as the path or part of the path, check for it
            int zipSplit = path.LastIndexOf(".zip") + 4;
            var zipPath = path.Substring(0, zipSplit);
            // Try to read the ZIP archive
            if (!TryGetZipArchive(zipPath, out var archive))
            {
                throw new FileNotFoundException("Specified zip file path does not exist", zipPath);
            }

            // Look for the file inside the ZIP archive
            var subPath = path.Substring(zipSplit + 1).Replace(Path.DirectorySeparatorChar, '/');
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == subPath)
                {
                    return entry.Open();
                }
            }

            // Not found within ZIP
            throw new FileNotFoundException("Specified file path does not exist within the specified zip archive", zipPath + Path.PathSeparator + subPath);
        }

        /// <summary>
        /// Enumerates all files in a specified directory
        /// </summary>
        /// <param name="dir">Path to the directory whose files are to be enumerated</param>
        /// <param name="recursive">True to enumerate all files in the directory and all its subdirectories, false to enumerate files only in the directory itself</param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if no directory exists at the specified path</exception>
        /// <exception cref="FileNotFoundException">Thrown if a ZIP archive was specified as the path or part of the path but does not exist</exception>
        public static IEnumerable<File> EnumerateFilesInDirectory(string dir, bool recursive = false)
        {
            // Normalize
            dir = NormalizeRootPath(dir, out _);

            // Check file system for directory
            var dirInfo = new DirectoryInfo(dir);
            if (dirInfo.Exists)
            {
                // If it exists, simply enumerate it
                var entries = dirInfo.EnumerateFiles("*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var entry in entries)
                {
                    yield return new File(entry.OpenRead(), entry.Name, entry.FullName);
                }
            }
            else
            {
                // If it doesn't, check for ZIP archive
                if (!dir.Contains(".zip")) throw new DirectoryNotFoundException("Specified directory does not exist: " + dir);

                int zipSplit = dir.LastIndexOf(".zip") + 4;
                var zipPath = dir.Substring(0, zipSplit);

                // Try to read ZIP file
                if (!TryGetZipArchive(zipPath, out var archive)) throw new FileNotFoundException("Specified zip file path does not exist", zipPath);

                // If the path goes deeper than the ZIP file itself cut the leading slash out
                if (zipSplit != dir.Length) zipSplit++;

                // Normalize Unix-style
                var subPath = dir.Substring(zipSplit).Replace(Path.DirectorySeparatorChar, '/');
                var subPathLength = subPath.Length;

                // Enumerate within the ZIP archive
                foreach (var entry in archive.Entries)
                {
                    // Must not be a directory (doesn't end with a trailing slash)
                    // Must derive from the specified path inside the ZIP archive
                    // Must be a top level entry inside the specified directory if recursive mode is off
                    if (!entry.FullName.EndsWith("/") && entry.FullName.StartsWith(subPath) && (!recursive || recursive && entry.FullName.LastIndexOf('/') > subPathLength))
                    {
                        yield return new File(entry.Open(), entry.Name, Path.GetFullPath(Path.Combine(zipPath, entry.FullName)));
                    }
                }
            }
        }
    }
}
