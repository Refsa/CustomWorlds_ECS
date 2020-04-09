# DOCUMENTATION

## Content Overview
The use of this package should be as smooth as possible. As such the core stuff in this package
will handle all the default setup of worlds, but also allows the user to further extend how worlds are setup.

## Auto Setup
To help with creating all the required types for using the custom worlds framework there exists a context
menu item on "Assets/Create/Custom World". Under this menu there is a Setup item and a New Custom World item.

### Setup - Context Menu Item
Setup will create the core components required to use the framework:

#### CustomBootstrapBase class
It will create a class deriving from CustomBootstrapBase in order to override the internal world creation from Unity.

#### CustomWorldType enum
It will create a new enum with the name CustomWorldType that you can populate with the custom worlds you want to 
implement.

#### CustomWorldTypeAttribute Attribute
It will create the appropriate attribute in order to mark Systems and SystemGroups to be spawned in a Custom World.

### New Custom World - Context Menu Item
New Custom World will create a new class deriving from the CustomWorldBase class. To make use of this functionality
you have to implement more world types in the CustomWorldType enum. Only one class deriving CustomWorldBase can exist
per World tag in the CustomWorldType enum.

## How Does It Work
To override the internal world setup by unity this package implements the ICustomBootstrap interface.
Unity will look for and use the first Class that derives from this interface. If more than one class
deriving from this interface exists Unity will only use the first one that returns true from ICustomBootstrap::Initialize.

## How To

### World Type Enum
You need to setup an enum you will use to tag your systems and worlds with.

Example Enum:
```csharp
public enum CustomWorldType
{
    Default = 0,
    MainMenu,
    Game
}
```

### ICustomWorldTypeAttribute
You will need to create an attribute class that derives from this interface. This will be used later on
so you can tag your systems and worlds with an Enum that identifies it.

Attribute Setup Example:
```csharp
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
```

### CustomBootstrapBase
In order to override the internal Unity you need to create a class that derives from this base class.
It needs two generic parameters, one which is the World Type enum and the other an Attribute.

This class contains everything it needs to automatically do the setup for you, but exists to allow
you to extend it's functonality. Further examples on use-cases for this is planned for the future.

Example:
```csharp
public class CustomWorldBootstrapExample : CustomBootstrapBase<CustomWorldType, CustomWorldTypeAttribute>
{
    
}
```

### CustomWorldBootstrapBase
To allow you to create more worlds the package contains an ICustomWorldBootstrap interface. CustomBootstrapBase will look
for classes deriving from this in order to create additional worlds. 

To make the process easier a CustomWorldBootstrapBase exists that contains additional helpers to automatically find
and add tagged systems. It also contains helpers to add the internal Unity systems required (Initialize, Update and Presentation).

IMPORTANT: For CustomBootstrapBase to actually spawn your worlds the class deriving from CustomWorldBootstrapBase also needs
to be tagged with the World Type Attribute.

Example World:
```csharp
[CustomWorldType(CustomWorldType.Game)]
public class GameWorld : CustomWorldBootstrapBase<CustomWorldType, CustomWorldTypeAttribute>
{
    public override World Initialize()
    {
        return SetupDefaultWorldType(GetType().Name, CustomWorldType.Game);
    }
}
```