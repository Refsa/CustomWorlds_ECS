using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Refsa.CustomWorld
{
    /// <summary>
    /// Static helpers to setup Worlds in Unity ECS
    /// </summary>
    public static class CustomWorldHelpers
    {
        /// <summary>
        /// Looks for all systems with the given requirements
        /// </summary>
        /// <param name="filterFlags">Unity's filter flag for worlds</param>
        /// <param name="worldType">Enum with the wanted custom world flag</param>
        /// <param name="requireExecuteAlways">Only look for systems with ExecuteAlways attribute</param>
        /// <typeparam name="T">Enum type</typeparam>
        /// <typeparam name="A">Attribute that stores enum type</typeparam>
        /// <returns>A list of all the found Types</returns>
        public static IReadOnlyList<Type> GetAllSystemsDirect<T, A>(
            WorldSystemFilterFlags filterFlags, 
            T worldType = default,
            bool requireExecuteAlways = false) 
                where T : Enum where A : Attribute, ICustomWorldTypeAttribute<T>
        {
            /* var filteredSystemTypes =
                (from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                where TypeManager.IsAssemblyReferencingEntities(a)
                    from t in a.GetTypes().AsParallel()
                    where typeof(ComponentSystemBase).IsAssignableFrom(t)
                    where FilterSystemType<T, A>(t, filterFlags, requireExecuteAlways, worldType)
                select t).ToList(); */
            
            var filteredSystemTypes = new List<Type>();
            var allSystemTypes = GetTypesDerivedFrom(typeof(ComponentSystemBase));
            foreach (var systemType in allSystemTypes)
            {
               if (FilterSystemType<T, A>(systemType, filterFlags, requireExecuteAlways, worldType))
                   filteredSystemTypes.Add(systemType);
            }

            return filteredSystemTypes;
        }

        static bool FilterSystemType<T, A>(
            Type type, WorldSystemFilterFlags filterFlags,
            bool requireExecuteAlways, T customWorldType)
                where T : Enum where A : Attribute, ICustomWorldTypeAttribute<T>
        {
            // the entire assembly can be marked for no-auto-creation (test assemblies are good candidates for this)
            var disableAllAutoCreation = Attribute.IsDefined(type.Assembly, typeof(DisableAutoCreationAttribute));
            var disableTypeAutoCreation = Attribute.IsDefined(type, typeof(DisableAutoCreationAttribute), false);
            var hasCustomWorldType = Attribute.IsDefined(type, typeof(A));

            // these types obviously cannot be instantiated
            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                if (disableTypeAutoCreation)
                    Debug.LogWarning($"Invalid [DisableAutoCreation] on {type.FullName} (only concrete types can be instantiated)");

                return false;
            }

            // only derivatives of ComponentSystemBase are systems
            if (!type.IsSubclassOf(typeof(ComponentSystemBase)))
                throw new System.ArgumentException($"{type} must already be filtered by ComponentSystemBase");

            if (requireExecuteAlways)
            {
                if (Attribute.IsDefined(type, typeof(ExecuteInEditMode)))
                    Debug.LogError($"{type} is decorated with {typeof(ExecuteInEditMode)}. Support for this attribute will be deprecated. Please use {typeof(ExecuteAlways)} instead.");
                if (!Attribute.IsDefined(type, typeof(ExecuteAlways)))
                    return false;
            }

            // the auto-creation system instantiates using the default ctor, so if we can't find one, exclude from list
            if (type.GetConstructor(System.Type.EmptyTypes) == null)
            {
                // we want users to be explicit
                if (!disableTypeAutoCreation && !disableAllAutoCreation)
                    Debug.LogWarning($"Missing default ctor on {type.FullName} (or if you don't want this to be auto-creatable, tag it with [DisableAutoCreation])");

                return false;
            }

            if (disableTypeAutoCreation || disableAllAutoCreation)
            {
                if (disableTypeAutoCreation && disableAllAutoCreation)
                    Debug.LogWarning($"Redundant [DisableAutoCreation] on {type.FullName} (attribute is already present on assembly {type.Assembly.GetName().Name}");

                return false;
            }

            if ((!hasCustomWorldType && !customWorldType.Equals(default(T))) || 
                (hasCustomWorldType && customWorldType.Equals(default(T))))
            {
                return false;
            }

            if (hasCustomWorldType)
            {
                bool hasAttributeWithWorldType = false;
                foreach (var attribute in (ICustomWorldTypeAttribute<T>[]) Attribute.GetCustomAttributes(type, typeof(A)))
                {
                    if (attribute.GetCustomWorldType.Equals(customWorldType))
                    {
                        hasAttributeWithWorldType = true;
                        break;
                    }
                }

                if (!hasAttributeWithWorldType) return false;
            }

            var systemFlags = WorldSystemFilterFlags.Default;
            if (Attribute.IsDefined(type, typeof(WorldSystemFilterAttribute), true))
                systemFlags = type.GetCustomAttribute<WorldSystemFilterAttribute>(true).FilterFlags;

            return (filterFlags & systemFlags) != 0;
        }

        /// <summary>
        /// Gets all types that the given type can be derived from
        /// </summary>
        /// <param name="type">Type to find related types from</param>
        /// <returns>A list of types</returns>
        public static IEnumerable<System.Type> GetTypesDerivedFrom(Type type)
        {
            #if UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesDerivedFrom(type);
            #else

            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!TypeManager.IsAssemblyReferencingEntities(assembly))
                    continue;

                try
                {
                    var assemblyTypes = assembly.GetTypes();
                    foreach (var t in assemblyTypes)
                    {
                        if (type.IsAssignableFrom(t))
                            types.Add(t);
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var t in e.Types)
                    {
                        if (t != null && type.IsAssignableFrom(t))
                            types.Add(t);
                    }

                    Debug.LogWarning($"DefaultWorldInitialization failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
                }
            }

            return types;
            #endif
        }

        public static ComponentSystemBase GetOrCreateManagerAndLogException(World world, Type type)
        {
            try
            {
                return world.GetOrCreateSystem(type);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Adds the collection of systems to the world by injecting them into the root level system groups
        /// (InitializationSystemGroup, SimulationSystemGroup and PresentationSystemGroup)
        /// </summary>
        public static void AddSystemsToRootLevelSystemGroups(World world, IEnumerable<Type> systems)
        {
            // create presentation system and simulation system
            var initializationSystemGroup = world.GetOrCreateSystem<InitializationSystemGroup>();
            var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();
            var presentationSystemGroup = world.GetOrCreateSystem<PresentationSystemGroup>();

            // Add systems to their groups, based on the [UpdateInGroup] attribute.
            foreach (var type in systems)
            {
                // Skip the built-in root-level system groups
                if (type == typeof(InitializationSystemGroup) ||
                    type == typeof(SimulationSystemGroup) ||
                    type == typeof(PresentationSystemGroup))
                {
                    continue;
                }

                var groups = type.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                if (groups.Length == 0)
                {
                    simulationSystemGroup.AddSystemToUpdateList(GetOrCreateManagerAndLogException(world, type));
                }

                foreach (var g in groups)
                {
                    var group = g as UpdateInGroupAttribute;
                    if (group == null)
                        continue;

                    if (!(typeof(ComponentSystemGroup)).IsAssignableFrom(group.GroupType))
                    {
                        Debug.LogError($"Invalid [UpdateInGroup] attribute for {type}: {group.GroupType} must be derived from ComponentSystemGroup.");
                        continue;
                    }

                    // Warn against unexpected behaviour combining DisableAutoCreation and UpdateInGroup
                    var parentDisableAutoCreation = group.GroupType.GetCustomAttribute<DisableAutoCreationAttribute>() != null;
                    if (parentDisableAutoCreation)
                    {
                        Debug.LogWarning($"A system {type} wants to execute in {group.GroupType} but this group has [DisableAutoCreation] and {type} does not.");
                    }

                    var groupMgr = GetOrCreateManagerAndLogException(world, group.GroupType);
                    if (groupMgr == null)
                    {
                        Debug.LogWarning(
                            $"Skipping creation of {type} due to errors creating the group {group.GroupType}. Fix these errors before continuing.");
                        continue;
                    }

                    var groupSys = groupMgr as ComponentSystemGroup;
                    if (groupSys != null)
                    {
                        groupSys.AddSystemToUpdateList(GetOrCreateManagerAndLogException(world, type));
                    }
                }
            }

            // Update player loop
            initializationSystemGroup.SortSystemUpdateList();
            simulationSystemGroup.SortSystemUpdateList();
            presentationSystemGroup.SortSystemUpdateList();
        }
    }
}