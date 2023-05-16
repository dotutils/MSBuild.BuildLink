# Build description metadata - buildlink.json

**Under construction**

BuildLink json metadata file (`buildlink.json`) is a descriptor of a process of turning source repository to binary assets of nuget packages (the assemblies within the `lib` folder of a nuget package).

```mermaid
flowchart LR
    Package[Package]-. SourceLink .-> Source[Source Code]
    Source-. BuildLink .-> Package
```

[SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) can be used to discover source code files and project repository for a given nuget package or individual binaries. The process of building and compiling of those sources to reproduce the binaries and/or packages might be nonobvious and relying on manual interventions.

The BuildLink metadata file tries to capture those hidden information in a structured way - so that they can possibly be used by other future automations ([composite builds](https://docs.gradle.org/current/userguide/composite_builds.html), [.net source build](https://github.com/dotnet/source-build), ['monkey patching'](https://khalidabuhakmeh.com/fix-dotnet-dependencies-with-monkey-patching), etc.). 

Currently those information are captured:
* Source repository and build initialization steps
* Custom tooling versions
* Multiple nuget packages being build from a single repository
* Multitargeted nuget lib files being produced by different project files
* Location of MSBuild poroject files required to build individual lib assets

## Sample description metadata:


### Serilog
```json
{
  "WorkingCopyInitScript": null,
  "PreBuildScript": null,
  "ToolingVersionInfo": {
    "GlobalJsonPresent": true,
    "VersionFromCompilerFlags": null
  },
  "BuildScript": {
    "WINDOWS": {
      "ScriptFilePath": "Build.ps1",
      "ScriptType": "PowerShell"
    },
    "LINUX": {
      "ScriptFilePath": "build.sh",
      "ScriptType": "Shell"
    }
  },
  "WorkingCopySolutionFile": "Serilog.sln",
  "NugetBuildDescriptors": [
    {
      "PackageName": "Serilog",
      "BuildScript": null,
      "MsBuildProject": "src\\Serilog\\Serilog.csproj",
      "BuildScriptPerLibAsset": {},
      "MsBuildProjectFilePerLibAsset": {}
    }
  ]
}
```

### Made up sample
```json
{
  "WorkingCopyInitScript": {
    "WINDOWS": {
      "ScriptFilePath": "build/init.ps1",
      "ScriptType": "PowerShell"
    },
    "LINUX": {
      "ScriptFilePath": "build/init.sh",
      "ScriptType": "Shell"
    }
  },
  "PreBuildScript": {
    "ALL-PLATFORMS": {
      "ScriptFilePath": "build/restore.cake",
      "ScriptType": "Cake"
    }
  },
  "ToolingVersionInfo": {
    "GlobalJsonPresent": true,
    "VersionFromCompilerFlags": "Roslyn:4.4.0"
  },
  "BuildScript": {
    "WINDOWS": {
      "ScriptFilePath": "build/build.ps1",
      "ScriptType": "PowerShell"
    },
    "LINUX": {
      "ScriptFilePath": "build/build.sh",
      "ScriptType": "Shell"
    },
    "ALL-PLATFORMS": {
      "ScriptFilePath": "build/build.cake",
      "ScriptType": "Cake"
    }
  },
  "WorkingCopySolutionFile": "src/my-project.sln",
  "NugetBuildDescriptors": [
    {
      "PackageName": "Package01",
      "BuildScript": {
        "ALL-PLATFORMS": {
          "ScriptFilePath": "src/package01/build.cake",
          "ScriptType": "Cake"
        }
      },
      "MsBuildProject": "src/package01/package01.csproj",
      "BuildScriptPerLibAsset": {},
      "MsBuildProjectFilePerLibAsset": {}
    },
    {
      "PackageName": "Package02",
      "BuildScript": null,
      "MsBuildProject": "src/package01/package02.fsproj",
      "BuildScriptPerLibAsset": {},
      "MsBuildProjectFilePerLibAsset": {}
    },
    {
      "PackageName": "Package03",
      "BuildScript": null,
      "MsBuildProject": "",
      "BuildScriptPerLibAsset": {},
      "MsBuildProjectFilePerLibAsset": {
        "net6/Package03.dll": "src/package03/net6/package03.vbproj",
        "net7/Package03.dll": "src/package03/net7/package03.csproj"
      }
    }
  ]
}
```

### Schema

**TBD**