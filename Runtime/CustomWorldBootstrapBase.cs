using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Refsa.CustomWorld
{
    /// <summary>
    /// Helper to setup custom Worlds
    /// 
    /// This is automatically called from a class inheriting from CustomBootstrapBase.
    /// The sub-class will need an Attribute of A as well in order for CustomBootstrapBase to find it.
    /// </summary>
    /// <typeparam name="T">Enum of the World Type Tag to construct from</typeparam>
    /// <typeparam name="A">Attribute that is used on systems that this setup will use</typeparam>
    public abstract class CustomWorldBootstrapBase<T, A> : ICustomWorldBootstrap where T : Enum where A : Attribute, ICustomWorldTypeAttribute<T>
    {
        public abstract World Initialize();

        World world;
        List<Type> worldSystems;

        InitializationSystemGroup initializationSystemGroup;
        SimulationSystemGroup simulationSystemGroup;
        PresentationSystemGroup presentationSystemGroup;

        protected void SetupBaseWorld(string worldName, T worldType)
        {
            world = new World(GetType().Name);
            worldSystems = GetAllSystems(worldType).ToList();
        }

        ComponentSystemGroup GetSystemGroup(Type t)
        {
            if (t == typeof(InitializationSystemGroup) && initializationSystemGroup != null)
            {
                return initializationSystemGroup;
            }
            else if (t == typeof(SimulationSystemGroup) && simulationSystemGroup != null)
            {
                return simulationSystemGroup;
            }
            else if (t == typeof(PresentationSystemGroup) && presentationSystemGroup != null)
            {
                return presentationSystemGroup;
            }

            return null;
        }

        protected World Build()
        {
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, worldSystems);

            return world;
        }

        protected void AddInitializationSystemGroup(bool addBufferSystems = true)
        {
            initializationSystemGroup = world.GetOrCreateSystem<InitializationSystemGroup>();

            if (addBufferSystems)
            {
                worldSystems.Add(typeof(BeginInitializationEntityCommandBufferSystem));
                worldSystems.Add(typeof(EndInitializationEntityCommandBufferSystem));
            }
        }

        protected void AddSimulationSystemGroup(bool addBufferSystems = true)
        {
            simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

            if (addBufferSystems)
            {
                worldSystems.Add(typeof(BeginSimulationEntityCommandBufferSystem));
                worldSystems.Add(typeof(EndSimulationEntityCommandBufferSystem));
            }
        }

        protected void AddPresentationSystemGroup(bool addBufferSystems = true)
        {
            presentationSystemGroup = world.GetOrCreateSystem<PresentationSystemGroup>();

            if (addBufferSystems)
            {
                worldSystems.Add(typeof(BeginPresentationEntityCommandBufferSystem));
            }
        }

        protected void AddWorldTimeSystem()
        {
            worldSystems.Add(typeof(UpdateWorldTimeSystem));
        }

        protected World SetupDefaultWorldType(string name, T worldType)
        {
            SetupBaseWorld(name, worldType);

            AddInitializationSystemGroup();
            AddSimulationSystemGroup();
            AddPresentationSystemGroup();
            
            AddWorldTimeSystem();

            return Build();
        }

        /// <summary>
        /// Helper to retreive systems with the related World Type Tag
        /// </summary>
        /// <param name="tag">Enum describing the World Type Tag to find systems for</param>
        /// <returns>A list of related systems</returns>
        protected IReadOnlyList<Type> GetAllSystems(T tag)
        {
            return CustomWorldHelpers.GetAllSystemsDirect<T, A>(WorldSystemFilterFlags.Default, tag, false);
        }
    }
}