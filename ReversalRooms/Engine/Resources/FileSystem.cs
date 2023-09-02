using ReversalRooms.Engine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ReversalRooms.Engine.Resources
{
    /// <summary>
    /// Contains methods to manage file system operations
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Represents a file
        /// </summary>
        public readonly struct File
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
                return Contents.ReadAll();
            }

            /// <summary>
            /// Reads out the contents of the file stream as a string
            /// </summary>
            /// <param name="encoding">The encoding to use, UTF-8 is used if not specified</param>
            /// <returns>The resultant string</returns>
            public string GetString(Encoding encoding = null)
            {
                return Contents.ReadAllString(encoding: encoding);
            }
        }

        #region Paths
        /// <summary>
        /// Represents the root directory of the game
        /// </summary>
        public static readonly DirectoryInfo RootDirectory;

        /// <summary>
        /// Represents the path to the root directory of the game
        /// </summary>
        public static readonly string RootPath;

        /// <summary>
        /// Represents the saves directory of the game
        /// </summary>
        public static readonly DirectoryInfo SavesDirectory;

        /// <summary>
        /// Represents the path to the saves directory of the game
        /// </summary>
        public static readonly string SavesPath;

        /// <summary>
        /// Represents the modules directory of the game
        /// </summary>
        public static readonly DirectoryInfo ModulesDirectory;

        /// <summary>
        /// Represents the path to the modules directory of the game
        /// </summary>
        public static readonly string ModulesPath;
        #endregion

        private static readonly Dictionary<string, ZipArchive> ZipCache = new();

        static FileSystem()
        {
            // Locate assembly
            var assemblyFile = new FileInfo(typeof(Program).Assembly.Location);
            var assemblyDir = assemblyFile.Directory;

            // Initiate paths
            RootDirectory = assemblyDir;
            RootPath = assemblyDir.FullName;

            ModulesDirectory = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "Modules"));
            ModulesPath = Path.Combine(ModulesDirectory.FullName);

            SavesDirectory = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "Saves"));
            SavesPath = Path.Combine(SavesDirectory.FullName);
        }

        /// <summary>
        /// Normalizes the path and sets the current directory to it
        /// </summary>
        /// <param name="curdir">The path to set the current dirctory to</param>
        public static void SetCurrentDirectory(string curdir)
        {
            // Use DirectoryInfo to perform some light validation on the root path and normalize platform separation characters
            curdir = new DirectoryInfo(curdir).FullName;

            // Set current directory to the specified root path for Path.GetFullPath to use that as a root path
            Directory.SetCurrentDirectory(curdir);
        }

        /// <summary>
        /// Clears any ZIP archives cached in memory
        /// </summary>
        public static void ClearZipCache() => ZipCache.Clear();

        #region Normalizers
        /// <summary>
        /// Normalizes and validates a path relative to the root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the root path</param>
        /// <returns>A normalized full path</returns>
        public static string NormalizeRootPath(string path, out string indexPath)
        {
            return NormalizeRelativePath(RootPath, path, out indexPath);
        }

        /// <summary>
        /// Normalizes and validates a path relative to the modules root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the modules root path</param>
        /// <returns>A normalized full path</returns>
        public static string NormalizeModulesPath(string path, out string indexPath)
        {
            return NormalizeRelativePath(ModulesPath, path, out indexPath);
        }

        /// <summary>
        /// Normalizes and validates a path relative to the Saves root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the Saves root path</param>
        /// <returns>A normalized full path</returns>
        public static string NormalizeSavesPath(string path, out string indexPath)
        {
            return NormalizeRelativePath(SavesPath, path, out indexPath);
        }

        /// <summary>
        /// Normalizes and validates a path relative to the current directory
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the specified root path</param>
        /// <returns>A normalized full path</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified path is not derived from the specified root path</exception>
        public static string NormalizeRelativePath(string path, out string indexPath)
        {
            string root = Directory.GetCurrentDirectory();

            // Remove any leading directory separation characters which would cause Path.GetFullPath to resolve from the system root
            while (path.Length > 1 && (path[0] == '/' || path[0] == '\\')) path = path.Substring(1);

            // Resolve the path from the root
            path = Path.GetFullPath(path);

            // After resolving the path, it should start with the specified root path
            if (path.Length < root.Length || !path.StartsWith(root))
            {
                throw new ArgumentOutOfRangeException("path", "Specified file path is not derived from the specified root path");
            }

            // Set index path as a normalized unix-style path string of the specified path relative to the specified root path
            if (path.Length == root.Length) indexPath = ".";
            else indexPath = path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            return path;
        }

        /// <summary>
        /// Normalizes and validates a path relative to a specified root path
        /// </summary>
        /// <param name="path">The path to normalize and validate</param>
        /// <param name="root">The root path to use</param>
        /// <param name="indexPath">A Unix-style path string of the specfied path starting relative to the specified root path</param>
        /// <returns>A normalized full path</returns>
        public static string NormalizeRelativePath(string root, string path, out string indexPath)
        {
            SetCurrentDirectory(root);
            return NormalizeRelativePath(path, out indexPath);
        }
        #endregion

        #region ZIPs
        /// <summary>
        /// Retrieves from cache or reads a ZIP archive located at a specified path resolved relative to the current directory
        /// </summary>
        /// <param name="path">Path to the ZIP archive</param>
        /// <param name="useCache">Whether to check if the ZIP is loaded in cache or not</param>
        /// <returns>The resultant ZIP archive</returns>
        /// <exception cref="FileNotFoundException">Thrown if no file is located at the specified file path</exception>
        public static ZipArchive GetZipArchive(string path, bool useCache = true)
        {
            // Normalize
            path = NormalizeRelativePath(path, out var indexPath);

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
        /// Retrieves from cache or reads a ZIP archive located at a specified path resolved relative to a specified root path
        /// </summary>
        /// <param name="root">The root path to resolve the path from</param>
        /// <param name="path">Path to the ZIP archive</param>
        /// <param name="useCache">Whether to check if the ZIP is loaded in cache or not</param>
        /// <returns>The resultant ZIP archive</returns>
        public static ZipArchive GetZipArchive(string root, string path, bool useCache = true)
        {
            SetCurrentDirectory(root);
            return GetZipArchive(path, useCache);
        }

        /// <summary>
        /// Attempts to retrieve from cache or reads a ZIP archive located at a specified path resolved relative to the current directory
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
        /// Attempts to retrieve from cache or reads a ZIP archive located at a specified path resolved relative to a specified root path
        /// </summary>
        /// <param name="root">The root path to resolve the path from</param>
        /// <param name="path">Path to the ZIP archive</param>
        /// <param name="archive">The resultant ZIP archive if successful</param>
        /// <param name="useCache">Whether to check if the ZIP is loaded in cache or not</param>
        /// <returns>Whether a ZIp archive was successfully retrieved or read or not</returns>
        public static bool TryGetZipArchive(string root, string path, out ZipArchive archive, bool useCache = true)
        {
            try
            {
                archive = GetZipArchive(root, path, useCache);
                return true;
            }
            catch
            {
                archive = null;
                return false;
            }
        }
        #endregion

        #region Files
        /// <summary>
        /// Opens a read-only stream of a file at a specified path resolved relative to the current directory
        /// </summary>
        /// <param name="path">Path to the file to read</param>
        /// <returns>A read-only stream of the specified file</returns>
        /// <exception cref="FileNotFoundException">Thrown if no file exists at the specified path</exception>
        public static File ReadFile(string path)
        {
            // Normalize path
            path = NormalizeRelativePath(path, out _);

            // Check file system for the file
            var fileInfoNaive = new FileInfo(path);
            if (fileInfoNaive.Exists) return new File(fileInfoNaive.OpenRead(), fileInfoNaive.Name, fileInfoNaive.FullName);
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
                    return new File(entry.Open(), entry.Name, Path.GetFullPath(Path.Combine(zipPath, entry.FullName)));
                }
            }

            // Not found within ZIP
            throw new FileNotFoundException("Specified file path does not exist within the specified zip archive", zipPath + Path.PathSeparator + subPath);
        }


        /// <summary>
        /// Opens a read-only stream of a file at a specified path resolved relative to a specified root path
        /// </summary>
        /// <param name="root">The root path to resolve the path from</param>
        /// <param name="path">Path to the file to read</param>
        /// <returns>A read-only stream of the specified file</returns>
        public static File ReadFile(string root, string path)
        {
            SetCurrentDirectory(root);
            return ReadFile(path);
        }
        #endregion

        #region Directories
        /// <summary>
        /// Enumerates all files in a specified directory
        /// </summary>
        /// <param name="dir">Path to the directory whose files are to be enumerated</param>
        /// <param name="recursive">True to enumerate all files in the directory and all its subdirectories, false to enumerate files only in the directory itself</param>
        /// <param name="extension">An extension to filter the files by</param>
        /// <returns>An enumerable that iterates through the matched files</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if no directory exists at the specified path</exception>
        /// <exception cref="FileNotFoundException">Thrown if a ZIP archive was specified as the path or part of the path but does not exist</exception>
        public static IEnumerable<File> EnumerateFilesInDirectory(string dir, bool recursive = false, string extension = "*")
        {
            // Normalize
            dir = NormalizeRelativePath(dir, out _);

            // Check file system for directory
            var dirInfo = new DirectoryInfo(dir);
            if (dirInfo.Exists)
            {
                if (extension != "*") extension = (extension[0] == '.') ? "*" + extension : "*." + extension;
                // If it exists, simply enumerate it
                var entries = dirInfo.EnumerateFiles(extension, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
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
                    if (extension == "*") extension = "";
                    // Must not be a directory (doesn't end with a trailing slash)
                    // Must derive from the specified path inside the ZIP archive
                    // Must be a top level entry inside the specified directory if recursive mode is off
                    if (entry.FullName[entry.FullName.Length - 1] != '/' && entry.Name.EndsWith(extension) && entry.FullName.StartsWith(subPath) && (!recursive || recursive && entry.FullName.LastIndexOf('/') > subPathLength))
                    {
                        yield return new File(entry.Open(), entry.Name, Path.GetFullPath(Path.Combine(zipPath, entry.FullName)));
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all files in a specified directory
        /// </summary>
        /// <param name="root">The root path to resolve the path from</param>
        /// <param name="dir">Path to the directory whose files are to be enumerated</param>
        /// <param name="recursive">True to enumerate all files in the directory and all its subdirectories, false to enumerate files only in the directory itself</param>
        /// <param name="extension">An extension to filter the files by</param>
        /// <returns>An enumerable that iterates through the matched files</returns>
        public static IEnumerable<File> EnumerateFilesInDirectory(string root, string dir, bool recursive = false, string extension = "*")
        {
            SetCurrentDirectory(root);
            return EnumerateFilesInDirectory(dir, recursive, extension);
        }
        #endregion Directories
    }
}
