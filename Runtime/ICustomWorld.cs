using Unity.Entities;
using UnityEngine.LowLevel;

namespace Refsa.CustomWorld
{
    public interface ICustomWorld
    {
        World Initialize();
    }
}