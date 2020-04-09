# Change Log
Notable changes to the project will be documented here

## [Unreleased]
### Added
- Marketing banner to prompt users to upgrade to a free account

### Changed

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