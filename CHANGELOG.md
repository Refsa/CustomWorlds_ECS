# Change Log
Notable changes to the project will be documented here

Follows versioning (SemVers) of https://docs.unity3d.com/Manual/upm-semver.html

## [Unreleased]
### Added
- 

## 1.3.1
### Fixed
- Previous version broke flex-shrink on some elements in the editor window, this is now fixed

## 1.3.0
### Added
- Added com.unity.editorcoroutines package to dependencies
- Adding toast messages to display feedback messages
- Added simple animation helpers using editor coroutines to allow easier animation
of Editor UIElements
- Added feedback messages to display in the toast message popup. 
    - Feedback for adding worlds
    - Feedback for removing worlds
    - Feedback for changing system world type

### Changes
- Cleaned up USS style for editor window
- Added in using vars to centralize the theming of the editor window
- Toast messages will now wrap instead of overflowing the containing box

## 1.2.3
### Added
- Window can now be resized between (800, 600) and (1600, 1000)

### Fixed
- Fixed flex-shrink css property on some elements
- Fixed margin and width on AddNewWorldContainer children

### Changes
- Updated documentation to reflect the new changes to the Editor Window
- Updated some styling on editor window to be more readable
- Added checking for package path, throwing an error message if package wasn't found
- Removed some unecessary console logs

## 1.2.2
### Added
- Added setup bootstrap button to new UIElements implementation

### Fixes
- Previous update to fix package path has been fixed properly
- No longer attempting to draw world and system info if bootstrap is not setup

## 1.2.1
### Fixes
- Fixed retreiving UXML and USS when library is used as package

## 1.2.0
### Added
- Added overview of systems and systemgroups to Custom World Editor Window
    - Adding in ability to set the attribute on classes through the editor tool

### Changes
- Rewriting Custom World editor window to UIElements

### Future
- Custom World editor window lacks toast message and additional barriers to protect
removing worlds that are assigned to systems

## 1.1.2
### Changes
- Cleaned up CustomWorldEditorWindow, removed unused functionality and added comments
- Clicking the "Add World Type" button now both adds an entry to the enum and creates the class file
- Changed key entries in EditorPrefs to something more unique to avoid key collision

## 1.1.1
### Added
- Adding functionality to remove world types from Custom World editor window
    - It will remove the enum entry and any associated class

### Changes
- Changes to Custom World editor window to make the process of setting up and creating new worlds less cumbersome

### Fixes
- Fix to not finding project path after script reload
- Fixed adding enum entry Linq Aggregate to property restructure file

### Known Issues
- Sometimes when removing a World the enum entry doesn't get removed.

## 1.1.0
### Changes
- Removed NewWorldWindow.cs from Editor project
- Cleaned up CustomWorldsEditorHelpers.cs

## 1.0.2
### Changes
- Custom World editor window helper is now moved into a single window
- Custom World editor window can now add new entires to the CustomWorldType enum
- New context menu path of Custom World editor window is "Assets/Create/CustomWorld"

### Known Issues
- If Custom World editor window is kept open after recompilation it will no longer hold the
correct project path to save new files. There is a temporary fix for this until I can find a
way to retreive current project path from the Project window.

## 1.0.1
### Fixed
- Fixed Setup menu item to look for already existing enum and attribute to avoid overriding 
user created content in World Type enum

### Changes
- Changed sorting order of Custom World context menu items in Assets/Create/ to be between
Assets/Create/Folder and Assets/Create/C# Script
- Updates to DOCUMENTATION.md showing how to use the framework.

### Additions
- Added better selector for auto-generating new worlds

## 1.0.0
### Fixed
- File setup for base bootstrap didn't return if bootstrap classes already exist

### Changes
- Added support for multiple World Type Attributes on systems and system groups to allow automatically creating
them in multiple worlds.
- Changed name of CustomWorldBootstrapBase to CustomWorldBase
- Changed name of ICustomWorldBootstrap to ICustomWorld

## 0.0.6
### Added
- Adding Editor Context Menu Items to help setup custom worlds scripts:
    - Assets/Custom World/Setup to setup all the required base files (Bootstrap, Enum and Attribute)
    - Assets/Custom World/New Custom World to create a new custom world from one of the types in Enum

## 2020-09-04
### Removed
- Removed Prototype folder from root folder

### Added
- Added compiler define flag to enable example (ENABLE_CUSTOM_WORLDS_ECS_EXAMPLE)
- Added asmdefs for empty folders
- Added base for documentation folder
- Changed access modifier on CustomBootstrapBase::CreateCustomBootstrap to protected
- Changed name of CustomBootstrapBase::CreateCustomBootstrap to CreateCustomWorlds