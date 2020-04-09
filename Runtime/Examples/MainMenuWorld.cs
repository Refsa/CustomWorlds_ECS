using System.Linq;
using Unity.Entities;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.MainMenu)]
    public class MainMenuWorld : CustomWorldBootstrapBase<CustomWorldType, CustomWorldTypeAttribute>
    {
        public override World Initialize()
        {
            SetupBaseWorld(this.GetType().Name, CustomWorldType.MainMenu);
            AddSimulationSystemGroup();
            return Build();

            // return SetupDefaultWorldType(GetType().Name, CustomWorldType.MainMenu);
        }
    }
}