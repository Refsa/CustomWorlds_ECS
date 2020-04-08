using Unity.Entities;
using UnityEngine.Scripting;

namespace Refsa.CustomWorld.Examples
{
    [CustomWorldType(CustomWorldType.MainMenu)]
    public class MainMenuSystemGroup : ComponentSystemGroup
    {
        [Preserve]
	    public MainMenuSystemGroup()
	    {
            
	    }
    }
}