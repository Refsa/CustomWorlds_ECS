using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

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