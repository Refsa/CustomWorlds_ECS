using System;

namespace Refsa.CustomWorld.Examples
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomWorldTypeAttribute : Attribute, ICustomWorldTypeAttribute<CustomWorldType>
    {
        public CustomWorldTypeAttribute(CustomWorldType customWorldType)
        {
            this.customWorldType = customWorldType;
        }

        CustomWorldType customWorldType;

        public CustomWorldType GetCustomWorldType => customWorldType;
    }
}