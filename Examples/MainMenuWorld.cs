using Unity.Entities;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.MainMenu)]
    public class MainMenuWorld : CustomWorldBootstrapBase<CustomWorldType, CustomWorldTypeAttribute>
    {
        public override World Initialize()
        {
            var world = new World(GetType().Name);
            
            var worldSystems = GetAllSystems(CustomWorldType.MainMenu);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, worldSystems);

            return world;
        }

        void CustomSystemInitialization()
        {
            /* var mainMenuSystemGroup = world.GetOrCreateSystem<MainMenuSystemGroup>();

            foreach (var system in worldSystems)
            {
                UnityEngine.Debug.Log($"\tSystem: {system.Name}");
                if (system == typeof(MainMenuSystemGroup)) continue;

                mainMenuSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem(system));
            }

            mainMenuSystemGroup.SortSystemUpdateList(); */
        }
    }
}