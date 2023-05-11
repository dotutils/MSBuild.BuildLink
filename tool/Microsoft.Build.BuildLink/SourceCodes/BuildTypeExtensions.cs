using System.Runtime.InteropServices;

namespace Microsoft.Build.BuildLink.SourceCodes;

internal static class BuildTypeExtensions
{
    public static readonly string SlnExtension = ".sln";
    public static readonly string[] ProjectExtensions = new[] { ".csproj", ".vbproj", ".fsproj" };
    public static readonly string[] ScriptExtensions = new[] { ".cmd", ".ps1", ".bat", ".sh", ".cake" };

    public static string ToShortString(this BuildType buildType)
        =>
            buildType switch
            {
                BuildType.ProjectFile => "Proj",
                BuildType.SolutionFile => "Sln",
                BuildType.BuildScript => "Script",
                _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
            };

    public static string ToHumanReadableString(this BuildType buildType)
        =>
            buildType switch
            {
                BuildType.ProjectFile => "MSBuild project file",
                BuildType.SolutionFile => "MSBuild solution file",
                BuildType.BuildScript => "build script",
                _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
            };

    public static string ToShortAlignedString(this BuildType buildType)
        =>
            buildType switch
            {
                BuildType.ProjectFile => "Proj  ",
                BuildType.SolutionFile => "Sln   ",
                BuildType.BuildScript => "Script",
                _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
            };

    public static ScriptType ToScriptType(this string filePath)
        =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".cmd" => ScriptType.Command,
                ".ps1" => ScriptType.PowerShell,
                ".bat" => ScriptType.Command,
                ".proj" => ScriptType.MsBuild,
                ".sh" => ScriptType.Shell,
                ".cake" => ScriptType.Cake,
                _ => ScriptType.Other
            };

    public static OSPlatform ToDefaultOsPlatform(this ScriptType scriptType)
        =>
            scriptType switch
            {
                ScriptType.Command => OSPlatform.Windows,
                ScriptType.PowerShell => OSPlatform.Windows,
                ScriptType.MsBuild => OSPlatformUtils.AnyPlatform,
                ScriptType.Cake => OSPlatformUtils.AnyPlatform,
                ScriptType.Shell => OSPlatform.Linux,
                ScriptType.Bash => OSPlatform.Linux,
                ScriptType.Other => OSPlatformUtils.AnyPlatform,
                _ => OSPlatformUtils.AnyPlatform
            };

    public static bool IsNull(this Script script) => object.ReferenceEquals(script, Script.NullScript) || string.IsNullOrEmpty(script.ScriptFilePath);

    public static bool IsNull(this ScriptGroup script) => object.ReferenceEquals(script, ScriptGroup.NullScript) || script.Count == 0;

    public static bool HasNonMsBuildInfo(this WorkingCopyBuildDescriptor descriptor)
        => !descriptor.BuildScript.IsNull() ||
           !descriptor.PreBuildScript.IsNull() ||
           !descriptor.WorkingCopyInitScript.IsNull();
}
