# DOCUMENTATION

## What?
This package will help you create and manage additional worlds for Unity DOTS. It should work as smoothly as
possible and not affect your current project without you explicitly configuring it.

## How?
To override the internal world setup by unity this package implements the ICustomBootstrap interface.
Unity will look for and use the first Class that derives from this interface. If more than one class
deriving from this interface exists Unity will only use the first one that returns true from ICustomBootstrap::Initialize.

### Auto Setup
To help with creating all the **required** types for using the custom worlds framework there exists a context
menu item on "Assets/Create/Custom World". This will open an Editor Window to help setup and manage worlds.

#### Setup
First time opening the window it will show a button to setup the **required** components. It will creating these
in the last selected folder or the folder of the last selected asset in the project view. 

##### CustomBootstrapBase class
- It will create a class deriving from __CustomBootstrapBase__ in order to override the internal world creation from Unity.

##### CustomWorldType enum
- It will create a new __enum__ with the name __CustomWorldType__ that you can populate with the custom worlds you want to 
implement.

##### CustomWorldTypeAttribute Attribute
- It will create the appropriate __attribute__ in order to mark Systems and SystemGroups to be spawned in a Custom World.

### Adding and managing worlds

#### Creating and Managing worlds
After the bootstrap has been setup it will show two different views. The left side shows currently added worlds
and an option to add new worlds. The right side will show an overview over all classes deriving from ComponentSystemBase in
your current project.

#### Adding new worlds
Typing in a name and clicking on Add World will create a new entry into the CustomWorldType enum and create a new class
that inherits from __CustomWorldBase__. The name of the class will be \[\{WorldName\}World\]

#### Managing systems
To help with setting up the required attribute on classes the right side of the Editor Window will show all classes that derive
from ComponentSystemBase. Here you can change the world attribute tag on those systems. This will make it a bit easier to manage
and get an overview of the systems and what world they go into.

Doing it manually you can add in multiple attribute tags on systems to spawn them into multiple worlds. This is not supported by the 
editor at this time, but will come in a future release.

## How to setup without editor window
Using the above auto setup is not required to use this package. If you want direct control over it you can extend and change
it to your needs. 

### World Type Enum
**You need to setup an enum** you will use to tag your systems and worlds with. It should always contain a default entry
that all untagged systems will go into.

Example Enum:
```csharp
public enum CustomWorldType
{
    Default = 0,
    Game
}
```

### ICustomWorldTypeAttribute
**You will need to create an attribute class** that derives from this interface. This will be used later on
so you can tag your systems and worlds with an Enum that identifies it. Untagged systems go into the default 
world, so the attribute is not required.

Currently you can add multiple tags to a system for it to spawn into multiple worlds.

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
**In order to override the internal Unity you need to create a class that derives from this base class.**
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