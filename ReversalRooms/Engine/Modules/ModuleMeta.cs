using Semver;
using System.Collections.Generic;

namespace ReversalRooms.Engine.Modules
{
    /// <summary>
    /// Describes the metadata of a module
    /// </summary>
    public record ModuleMetadata
    {
        /// <summary>
        /// Represents a unique identifying string for the module
        /// </summary>
        public string ID;

        /// <summary>
        /// Represents the name of the module that is display on UIs
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Contains a short description of the module
        /// </summary>
        public string Description;

        /// <summary>
        /// Enlists the author(s) of the module
        /// </summary>
        public string Author;

        /// <summary>
        /// Represents a link to a website of the module (e.g. a GitHub repository)
        /// </summary>
        public string Website;

        /// <summary>
        /// Represents the version of the module
        /// </summary>
        public SemVersion Version;

        /// <summary>
        /// Represents a relative path to the assembly of the module
        /// </summary>
        public string Assembly;

        /// <summary>
        /// Reresents the full path to the directory of the module
        /// </summary>
        public string Directory;

        /// <summary>
        /// Lists the dependencies of the mods
        /// </summary>
        public ModuleDepdenency[] Depdenencies;
    }
    public enum ModuleDepdenencyState
    {
        Pending = 0, Resolved, NotFound, OldVersion, Circular
    }

    public record ModuleDepdenency
    {
        /// <summary>
        /// Specifies the ID of the dependee module
        /// </summary>
        public string ID;

        /// <summary>
        /// Specifies the minimum version of the dependee module
        /// </summary>
        public SemVersion Version;

        /// <summary>
        /// Describes the resolution state of the dependency
        /// </summary>
        public ModuleDepdenencyState State = ModuleDepdenencyState.Pending;

        /// <summary>
        /// Specifies the dependency circle this dependency is involved in, if any
        /// </summary>
        public List<ModuleDepdenency> Circle;
    }
}
