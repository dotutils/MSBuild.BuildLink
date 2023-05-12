# Build description metadata - buildlink.json

detailed info - TBD

## Sample description metadata:

```json
{
  "WorkingCopyInitScript": null,
  "PreBuildScript": null,
  "BuildScript": {
    "WINDOWS": {
      "ScriptFilePath": "build\\build_all.ps1",
      "ScriptType": "PowerShell"
    }
  },
  "WorkingCopySolutionFile": "src\\foobar.sln",
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