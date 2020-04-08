using System;

namespace Refsa.CustomWorld
{
    /// <summary>
    /// Required on attributes to be placed on a system to identify it's World Type enum
    /// </summary>
    /// <typeparam name="T">Enum containing information about WorldType</typeparam>
    public interface ICustomWorldTypeAttribute<T> where T : Enum
    {
        T GetCustomWorldType {get;}
    }
}