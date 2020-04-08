using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Refsa.CustomWorld.Examples;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Refsa.CustomWorld.Prototype
{
    public static class CustomWorldInitialization
    {
        static bool _UnloadOrPlayModeChangeShutdownRegistered = false;

        public static event Action<World> CustomWorldInitialized;
        public static event Action CustomWorldsDestroyed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void CleanupWorldBeforeSceneLoad()
        {
            DomainUnloadOrPlayModeChangeShutdown();
        }

        static void RegisterUnloadOrPlayModeChangeShutdown()
        {
            if (_UnloadOrPlayModeChangeShutdownRegistered)
                return;

            var go = new GameObject { hideFlags = HideFlags.HideInHierarchy };
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            else
                go.hideFlags = HideFlags.HideAndDontSave;

            go.AddComponent<CustomWorldInitializationProxy>().IsActive = true;

            _UnloadOrPlayModeChangeShutdownRegistered = true;
        }

        public static void DomainUnloadOrPlayModeChangeShutdown()
        {
            if (!_UnloadOrPlayModeChangeShutdownRegistered)
                return;

            World.DisposeAllWorlds();

            WordStorage.Instance.Dispose();
            WordStorage.Instance = null;
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);

            _UnloadOrPlayModeChangeShutdownRegistered = false;

            CustomWorldsDestroyed?.Invoke();
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

        public static void Initialize(string defaultWorldName, bool editorWorld)
        {
            RegisterUnloadOrPlayModeChangeShutdown();

            var world = new World(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = world;

            var systems = GetAllSystemsDirect(WorldSystemFilterFlags.Default, editorWorld);
            AddSystemsToRootLevelSystemGroups(world, systems);

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);

            CustomWorldInitialized?.Invoke(world);

            if (!editorWorld)
            {
                CreateCustomWorldBootStrap();
            }

            // foreach (var iworld in World.All) UnityEngine.Debug.Log($"World Name: {iworld.Name}");
        }

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

        public static IReadOnlyList<Type> GetAllSystemsDirect(WorldSystemFilterFlags filterFlags, bool requireExecuteAlways = false, CustomWorldType customWorldType = CustomWorldType.Default)
        {
            var filteredSystemTypes = new List<Type>();
            var allSystemTypes = GetTypesDerivedFrom(typeof(ComponentSystemBase));
            foreach (var systemType in allSystemTypes)
            {
                if (FilterSystemType(systemType, filterFlags, requireExecuteAlways, customWorldType))
                    filteredSystemTypes.Add(systemType);
            }

            return filteredSystemTypes;
        }

        static bool FilterSystemType(Type type, WorldSystemFilterFlags filterFlags, bool requireExecuteAlways, CustomWorldType customWorldType = CustomWorldType.Default)
        {
            // IMPORTANT: keep this logic in sync with SystemTypeGen.cs for DOTS Runtime

            // the entire assembly can be marked for no-auto-creation (test assemblies are good candidates for this)
            var disableAllAutoCreation = Attribute.IsDefined(type.Assembly, typeof(DisableAutoCreationAttribute));
            var disableTypeAutoCreation = Attribute.IsDefined(type, typeof(DisableAutoCreationAttribute), false);
            var hasCustomWorldType = Attribute.IsDefined(type, typeof(CustomWorldTypeAttribute));

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

            if ((!hasCustomWorldType && customWorldType != CustomWorldType.Default) || 
                (hasCustomWorldType && customWorldType == CustomWorldType.Default))
            {
                return false;
            }

            var systemFlags = WorldSystemFilterFlags.Default;
            if (Attribute.IsDefined(type, typeof(WorldSystemFilterAttribute), true))
                systemFlags = type.GetCustomAttribute<WorldSystemFilterAttribute>(true).FilterFlags;

            return (filterFlags & systemFlags) != 0;
        }

        static IEnumerable<System.Type> GetTypesDerivedFrom(Type type)
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

        static List<ICustomWorldBootstrap> CreateCustomWorldBootStrap()
        {
            var bootstrapTypes = GetTypesDerivedFrom(typeof(ICustomWorldBootstrap));
            List<Type> selectedTypes = new List<Type>();

            foreach (var bootType in bootstrapTypes)
            {
                if (bootType.IsAbstract || bootType.ContainsGenericParameters || bootType.GetCustomAttribute(typeof(CustomWorldTypeAttribute)) == null)
                    continue;

                selectedTypes.Add(bootType);
            }

            List<ICustomWorldBootstrap> bootstraps = new List<ICustomWorldBootstrap>();

            selectedTypes
                .Distinct()
                .ToList()
                .ForEach(t => bootstraps.Add(Activator.CreateInstance(t) as ICustomWorldBootstrap));

            bootstraps.ForEach(e => ScriptBehaviourUpdateOrder.UpdatePlayerLoop(e.Initialize(), ScriptBehaviourUpdateOrder.CurrentPlayerLoop));

            return bootstraps;
        }
    }
}