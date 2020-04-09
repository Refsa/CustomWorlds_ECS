using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace Refsa.CustomWorld
{
    /// <summary>
    /// Base class to help create and setup Worlds for the Unity ECS ecosystem
    /// 
    /// Sub-classes will be called by the internal Unity setup. More than one class that inherits from this class
    /// will currently cause issues. Only the first one found will be called by the internal DefaultWorldInitialization class.
    /// </summary>
    /// <typeparam name="T">Enum describing the World Types of the Worlds to be spawned</typeparam>
    /// <typeparam name="A">Attribute used to look for Systems linked to each World Type</typeparam>
    public abstract class CustomBootstrapBase<T, A> : ICustomBootstrap where T : Enum where A : Attribute, ICustomWorldTypeAttribute<T>
    {
        /// <summary>
        /// Override this to customize how worlds are setup.
        /// Leave as is and it will automatically spawn and setup the worlds described in the T enum
        /// </summary>
        /// <param name="defaultWorldName">Passed in from Unity's DefaultWorldInitialization class</param>
        /// <returns>Should return True in order to override Unity's internal world setup</returns>
        public virtual bool Initialize(string defaultWorldName)
        {
            CreateDefaultWorld(defaultWorldName);

            CreateCustomWorlds();

            return true;
        }

        /// <summary>
        /// Helper to simplify retreiving related Systems of a World Type tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>A list of related systems to the tag</returns>
        protected IReadOnlyList<Type> GetAllSystems(T tag)
        {
            return CustomWorldHelpers.GetAllSystemsDirect<T, A>(WorldSystemFilterFlags.Default, tag, false);
        }

        /// <summary>
        /// Sets up the default world, works similar to how it's done in Unity's DefaultWorldInitialization.Initialise() method
        /// </summary>
        /// <param name="defaultWorldName"></param>
        protected void CreateDefaultWorld(string defaultWorldName)
        {
            var world = new World(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = world;

            var systems = GetAllSystems(default(T));

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
        }

        /// <summary>
        /// Automatically looks for all classes using the ICustomWorldBootstraps interface
        /// and runs the initialize method on them.
        /// 
        /// They need the A attribute in order to be considered for initialization.
        /// </summary>
        protected static void CreateCustomWorlds()
        {
            var bootstrapTypes = CustomWorldHelpers.GetTypesDerivedFrom(typeof(ICustomWorld));
            List<Type> selectedTypes = new List<Type>();

            foreach (var bootType in bootstrapTypes)
            {
                if (bootType.IsAbstract || bootType.ContainsGenericParameters || bootType.GetCustomAttribute(typeof(A)) == null)
                    continue;

                selectedTypes.Add(bootType);
            }

            selectedTypes
                .Distinct()
                .Select(e => Activator.CreateInstance(e) as ICustomWorld)
                .ToList()
                .ForEach(
                    e => ScriptBehaviourUpdateOrder.UpdatePlayerLoop(e.Initialize(), ScriptBehaviourUpdateOrder.CurrentPlayerLoop)
                );
        }
    }
}