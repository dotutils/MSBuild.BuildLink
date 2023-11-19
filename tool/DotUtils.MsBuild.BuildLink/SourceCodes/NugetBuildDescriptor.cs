using DotUtils.MsBuild.BuildLink.Utils;

namespace DotUtils.MsBuild.BuildLink.SourceCodes;

internal class NugetBuildDescriptor
{
    public NugetBuildDescriptor(string packageName, ScriptGroup buildScript, string msBuildProject, IReadOnlyDictionary<string, ScriptGroup> buildScriptPerLibAsset, IReadOnlyDictionary<string, string> msBuildProjectFilePerLibAsset)
    {
        PackageName = packageName;
        BuildScript = buildScript;
        MsBuildProject = msBuildProject;
        BuildScriptPerLibAsset = buildScriptPerLibAsset;
        MsBuildProjectFilePerLibAsset = msBuildProjectFilePerLibAsset;
    }

    public NugetBuildDescriptor(string packageName, string msBuildProject,
        IReadOnlyDictionary<string, string> msBuildProjectFilePerLibAsset)
        : this(packageName, ScriptGroup.NullScript, msBuildProject, new Dictionary<string, ScriptGroup>(),
            msBuildProjectFilePerLibAsset)
    { }

    // for deserialization
    private NugetBuildDescriptor()
    { }

    public NugetBuildDescriptor? CreateSingleLibAssetCopy(string? libAssemblyName)
    {
        if (string.IsNullOrEmpty(libAssemblyName))
        {
            return this;
        }

        // there is no other lib asset explicitly mentioned
        if (
            BuildScriptPerLibAsset.Keys.All(k =>
                k.Equals(libAssemblyName, StringComparison.CurrentCultureIgnoreCase)) &&
            MsBuildProjectFilePerLibAsset.Keys.All(k =>
                k.Equals(libAssemblyName, StringComparison.CurrentCultureIgnoreCase))
        )
        {
            return this;
        }

        var res = new NugetBuildDescriptor(
            PackageName,
            BuildScript,
            MsBuildProject,
            BuildScriptPerLibAsset.FilterByKey(libAssemblyName),
            MsBuildProjectFilePerLibAsset.FilterByKey(libAssemblyName));

        // There was some targeted build info before, and it's all filtered out now
        if (
            this.BuildScriptPerLibAsset.Any() &&
            this.MsBuildProjectFilePerLibAsset.Any() &&
            !res.BuildScriptPerLibAsset.Any() &&
            !res.MsBuildProjectFilePerLibAsset.Any()
        )
        {
            return null;
        }

        return res;
    }

    /// <summary>
    /// Name of the package for which this section is applicable
    /// </summary>
    public string PackageName { get; init; }
    /// <summary>
    /// Optional path to build script - applicable to all lib assets of the package.
    /// </summary>
    public ScriptGroup BuildScript { get; init; }
    /// <summary>
    /// Optional path to MSBuild file - applicable to all lib assets of the package.
    /// </summary>
    public string MsBuildProject { get; init; }
    /// <summary>
    /// Optional lookup of paths to build script - indexable by subpath of a lib asset (or it's filename).
    /// </summary>
    public IReadOnlyDictionary<string, ScriptGroup> BuildScriptPerLibAsset { get; init; }
    /// <summary>
    /// Optional lookup of paths to MSBuild files - indexable by subpath of a lib asset (or it's filename).
    /// </summary>
    public IReadOnlyDictionary<string, string> MsBuildProjectFilePerLibAsset { get; init; }
}
