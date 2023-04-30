# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2023-01-31

### Added

- Added support for interface lists and arrays
- Added support for `Dependency` attribute. Can be used to specify type of dependency 
as well as moving normal serialized fields under **"Dependencies"** section for convenience
- Added generalized dependency management (for both interface dependencies and 
serialized properties)
- Added dependencies resolution stats block in the **"Dependencies"** section header
- Added dependency indicators for each displayed dependency
- Added `IProvider` interface for indirect dependency resolution
- Added `MonoBehaviourWithDependencies` and `ScriptableObjectWithDependencies` convenience classes. 
Both wrap the `InterfaceDependencies` prop.
- Added two samples: one with simple direct dependencies on MB or SO and another using Provider interface 

### Changed

- Fixed the bug with DnD not setting object reference and not setting undo/making scene dirty
- Fixed DnD serialization / deserialization bug
- Fixed bug that allowed dependencies from scene tree to be set in the persistent objects (like SO assets)
- 

## [0.1.0] - 2022-12-14

### Added

- Added serialization of interface dependencies via `InterfaceDependencies` type
- Added custom drawer for `InterfaceDependencies` type that displays all interface fields on a MonoBehvaiour or a ScriptableObject
- Added sample scene and scripts to illustrate the usage
