using Unity.Entities;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.Game)]
    public class GameWorld : CustomWorldBootstrapBase<CustomWorldType, CustomWorldTypeAttribute>
    {
        public override World Initialize()
        {
            var world = new World(GetType().Name);
            
            var worldSystems = GetAllSystems(CustomWorldType.Game);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, worldSystems);

            return world;
        }
    }
}