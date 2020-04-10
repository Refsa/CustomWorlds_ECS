# Change Log
Notable changes to the project will be documented here

Follows versioning (SemVers) of https://docs.unity3d.com/Manual/upm-semver.html

## [Unreleased]
### Added
- 

## 1.0.2
### Changes
- Custom World editor window helper is now moved into a single window
- Custom World editor window can now add new entires to the CustomWorldType enum
- New context menu path of Custom World editor window is "Assets/Create/CustomWorld"

### Known Bugs
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