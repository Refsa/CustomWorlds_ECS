using Unity.Entities;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.Game)]
    public class GameWorld : CustomWorldBase<CustomWorldType, CustomWorldTypeAttribute>
    {
        public override World Initialize()
        {
            return SetupDefaultWorldType(GetType().Name, CustomWorldType.Game);
        }
    }
}