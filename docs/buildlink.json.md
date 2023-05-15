# Build description metadata - buildlink.json

**Under construction**

BuildLink json metadata file (`buildlink.json`) is a descriptor of a process of turning source repository to binary assets of nuget packages (the assemblies within the `lib` folder of a nuget package).

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
  "WorkingCopyInitScript": null,
  "PreBuildScript": null,
  "ToolingVersionInfo": {
    "GlobalJsonPresent": true,
    "VersionFromCompilerFlags": "Roslyn:4.4.0"
  },
  "BuildScript": {
    "WINDOWS": {
      "ScriptFilePath": "build\\build_all.ps1",
      "ScriptType": "PowerShell"
    }
  },
  "WorkingCopySolutionFile": "src\\contoso.sln",
  "NugetBuildDescriptors": [
    {
      "PackageName": "SamplePackage01",
      "BuildScript": {
        "WINDOWS": {
          "ScriptFilePath": "a\\bcd\\e.cmd",
          "ScriptType": "Command"
        }
      },
      "MsBuildProject": "c\\build.proj",
      "BuildScriptPerLibAsset": {
        "lib01.dll": {
          "WINDOWS": {
            "ScriptFilePath": "a\\bcd\\lib01.cmd",
            "ScriptType": "Command"
          },
          "LINUX": {
            "ScriptFilePath": "a\\bcd\\lib01.sh",
            "ScriptType": "Shell"
          }
        },
        "lib02.dll": {
          "ALL-PLATFORMS": {
            "ScriptFilePath": "a\\bcd\\lib02.cake",
            "ScriptType": "Cake"
          }
        }
      },
      "MsBuildProjectFilePerLibAsset": {
        "lib01.dll": "a\\bcd\\lib01.csproj",
        "lib02.dll": "a\\bcd\\lib02.csproj"
      }
    },
    {
      "PackageName": "SamplePackage02",
      "BuildScript": null,
      "MsBuildProject": "x\\y\\secondproj.csproj",
      "BuildScriptPerLibAsset": {},
      "MsBuildProjectFilePerLibAsset": {}
    }
  ]
}
```

### Schema

**TBD**