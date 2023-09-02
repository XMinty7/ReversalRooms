using ReversalRooms.Engine.Resources;
using ReversalRooms.Engine.Utils;
using System;
using System.Collections.Generic;

namespace ReversalRooms.Engine.Modules
{
    /// <summary>
    /// Contains methods to manage modules and module operations
    /// </summary>
    public static class ModuleRegistry
    {
        internal static Dictionary<Type, ModuleMetadata> MetadataPerType = new();

        public static List<ModuleMetadata> AllModules;
        public static List<ModuleMetadata> FailedModules;
        public static Dictionary<string, ModuleMetadata> Modules;

        #region Module Loading
        public static void LoadModules()
        {
            // List of mods waiting for some mod to load
            var waitList = new Dictionary<string, List<ModuleDepdenency>>();
            // List of all modules indexed by mod IDs
            var modsById = new Dictionary<string, ModuleMetadata>();
            // List of all modules
            var allMods = new List<ModuleMetadata>();

            var yamlFiles = FileSystem.EnumerateFilesInDirectory(FileSystem.ModulesPath, ".", true, "yaml");
            foreach (var yamlFile in yamlFiles)
            {
                if (yamlFile.Name.ToLowerInvariant() != "module.yaml")
                {
                    yamlFile.Contents.Close();
                    continue;
                }

                // TODO: Validate metadata
                var mod = yamlFile.Contents.DeserializeYaml<ModuleMetadata>(true);
                mod.Depdenencies ??= new ModuleDepdenency[0];

                // Enlist the metadata, prefer newer version
                if (modsById.TryGetValue(mod.ID, out var otherMod))
                {
                    if (mod.Version.Outdates(otherMod.Version))
                    {
                        modsById[mod.ID] = mod;
                        allMods.Remove(otherMod);
                    }
                    else continue;
                }
                else
                {
                    modsById.Add(mod.ID, mod);
                }
                allMods.Add(mod);
            }

            // The current path that is being searched, potentially a circle
            Stack<ModuleMetadata> currentPath = new();
            // Same path but in dependencies instead of modules
            Stack<ModuleDepdenency> currentPathDeps = new();

            void searchForCircles(ModuleMetadata startNode, ModuleMetadata currentNode)
            {
                // Update the current dependency path we're searching
                currentPath.Push(currentNode);

                // Check all current node dependencies
                foreach (var dep in currentNode.Depdenencies)
                {
                    // It's a circular dependency we found before, skip
                    if (dep.State == ModuleDepdenencyState.Circular) continue;

                    // We have a circular dependency?
                    if (dep.ID == startNode.ID)
                    {
                        // Definitely a circular dependency
                        if (startNode.Version.MeetsVersion(dep.Version))
                        {
                            var circle = new List<ModuleDepdenency>(currentPathDeps);
                            foreach (var superDep in currentPathDeps)
                            {
                                superDep.State = ModuleDepdenencyState.Circular;
                                superDep.Circle = circle;
                            }
                        }
                        // Maybe not a circular dependency, it's an old version
                        else
                        {
                            dep.State = ModuleDepdenencyState.OldVersion;
                        }
                    }
                    // Normal dependency, try to find it
                    else if (modsById.TryGetValue(dep.ID, out var depMod))
                    {
                        // Recurse
                        currentPathDeps.Push(dep);
                        searchForCircles(startNode, depMod);
                        currentPathDeps.Pop();
                    }
                }

                // We're going back up one dependency layer
                currentPath.Pop();
            }

            foreach (var mod in modsById.Values)
            {
                // Check for dependency circles this mod adds
                searchForCircles(mod, mod);

                // Create wait list for the mod
                foreach (var dep in mod.Depdenencies)
                {
                    if (waitList.TryGetValue(dep.ID, out var list)) list.Add(dep);
                    else waitList.Add(dep.ID, new() { dep });
                }
            }

            Modules = new();
            FailedModules = new();

            while (modsById.Count > 0)
            {
                // Copy so we can modify while iterating
                var values = new ModuleMetadata[modsById.Count];
                modsById.Values.CopyTo(values, 0);
                foreach (var mod in values)
                {
                    // Check the state of dependencies
                    bool allResolved = true;
                    bool nonePending = true;
                    foreach (var dep in mod.Depdenencies)
                    {
                        if (dep.State == ModuleDepdenencyState.Resolved)
                        {
                            continue;
                        }
                        else if (dep.State == ModuleDepdenencyState.Circular)
                        {
                            allResolved = false;
                            continue;
                        }
                        else if (!modsById.TryGetValue(dep.ID, out var dependee))
                        {
                            dep.State = ModuleDepdenencyState.NotFound;
                        }
                        else if (!dependee.Version.MeetsVersion(dep.Version))
                        {
                            dep.State = ModuleDepdenencyState.OldVersion;
                        }
                        else if (dep.State == ModuleDepdenencyState.Pending)
                        {
                            nonePending = false;
                        }
                        allResolved = false;
                    }

                    // All are resolved, we can load the mod now
                    if (allResolved)
                    {
                        LoadModule(mod);
                        if (waitList.TryGetValue(mod.ID, out var dependents))
                        {
                            foreach (var dependent in dependents)
                            {
                                dependent.State = ModuleDepdenencyState.Resolved;
                            }
                        }
                        Modules.Add(mod.ID, mod);
                        modsById.Remove(mod.ID);
                    }
                    // Not all are resolved and none are pending
                    // All dependencies have been exhausted and the mod cannot be loaded
                    else if (nonePending)
                    {
                        if (waitList.TryGetValue(mod.ID, out var dependents))
                        {
                            foreach (var dependent in dependents)
                            {
                                dependent.State = ModuleDepdenencyState.NotFound;
                            }
                        }
                        FailedModules.Add(mod);
                        modsById.Remove(mod.ID);
                    }
                }
            }

            // Done loading
            AllModules = allMods;
            CodeModules = new();
        }

        public static void LoadModule(ModuleMetadata metadata)
        {
            // TODO: Loading logic
            Console.Write("Loading: ");
            Console.WriteLine(metadata.ID);
        }
        #endregion
    }
}
