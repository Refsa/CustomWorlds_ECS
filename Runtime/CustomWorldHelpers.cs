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
                var customWorldAttribute = Attribute.GetCustomAttribute(type, typeof(A)) as ICustomWorldTypeAttribute<T>;
                if (!customWorldAttribute.GetCustomWorldType.Equals(customWorldType))
                {
                    return false;
                }
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
    }
}