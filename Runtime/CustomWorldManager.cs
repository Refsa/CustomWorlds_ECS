using Unity.Entities;

namespace Refsa.CustomWorld
{
    public static class CustomWorldManager
    {
        /// <summary>
        /// Toggles the Enabled flag on all systems in a World
        /// </summary>
        /// <param name="worldName">Name of world</param>
        /// <param name="status">Status to set worlds systems Enabled flag to</param>
        public static void EnableWorldSystems(string worldName, bool status = false)
        {
            World world = GetWorld(worldName);

            if (world == null)
            {
                throw new System.ArgumentNullException($"Couldn't find world of name {worldName}");
            }

            foreach (var system in world.Systems)
            {
                system.Enabled = status;
            }
        }

        /// <summary>
        /// Finds the world of the given name
        /// </summary>
        /// <param name="worldName">Name of the world to look for</param>
        /// <returns>World if it was found, null if not</returns>
        public static World GetWorld(string worldName)
        {
            foreach (var world in World.All)
            {
                if (world.Name == worldName) return world;
            }

            return null;
        }
    }
}
