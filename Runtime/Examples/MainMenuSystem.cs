using Unity.Entities;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.MainMenu)]
    [UpdateInGroup(typeof(MainMenuSystemGroup))]
    public class MainMenuSystem : SystemBase
    {
        protected override void OnUpdate()
        {

        }
    }
}