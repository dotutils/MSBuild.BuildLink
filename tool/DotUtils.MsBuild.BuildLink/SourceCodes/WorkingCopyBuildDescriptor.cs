namespace DotUtils.MsBuild.BuildLink.SourceCodes;

internal class WorkingCopyBuildDescriptor
{
    public WorkingCopyBuildDescriptor(
        ScriptGroup workingCopyInitScript,
        ScriptGroup preBuildScript,
        ToolingVersionInfo? toolingVersionInfo,
        ScriptGroup buildScript,
        string workingCopySolutionFile,
        NugetBuildDescriptor[] nugetBuildDescriptors)
    {
        WorkingCopyInitScript = workingCopyInitScript;
        PreBuildScript = preBuildScript;
        ToolingVersionInfo = toolingVersionInfo;
        BuildScript = buildScript;
        WorkingCopySolutionFile = workingCopySolutionFile;
        NugetBuildDescriptors = nugetBuildDescriptors;
    }

    public WorkingCopyBuildDescriptor(
        ScriptGroup buildScript,
        string workingCopySolutionFile,
        ToolingVersionInfo? toolingVersionInfo,
        NugetBuildDescriptor[] nugetBuildDescriptors)
        : this(ScriptGroup.NullScript, ScriptGroup.NullScript, toolingVersionInfo, buildScript, workingCopySolutionFile, nugetBuildDescriptors)
    { }

    // for deserialization
    private WorkingCopyBuildDescriptor()
    { }

    public WorkingCopyBuildDescriptor? CreateSinglePackageCopy(string packageName, string? libAssemblyName)
    {
        NugetBuildDescriptor? nugetBuildDescriptor = NugetBuildDescriptors.FirstOrDefault(d =>
            d.PackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))
            ?.CreateSingleLibAssetCopy(libAssemblyName);

        if (NugetBuildDescriptors.Length == 1 &&
            object.ReferenceEquals(NugetBuildDescriptors[0], nugetBuildDescriptor))
        {
            return this;
        }

        if (nugetBuildDescriptor == null)
        {
            return null;
        }

        return new WorkingCopyBuildDescriptor(WorkingCopyInitScript, PreBuildScript, ToolingVersionInfo, BuildScript,
            WorkingCopySolutionFile, new[] { nugetBuildDescriptor });
    }

    /// <summary>
    /// Optional - script to be used for one time initialization of working copy (after cloning).
    /// </summary>
    public ScriptGroup WorkingCopyInitScript { get; init; }
    /// <summary>
    /// Optional - script to be used for build initialization - to be run before each script (e.g. restore, if not part of the build).
    /// </summary>
    public ScriptGroup PreBuildScript { get; init; }
    /// <summary>
    /// Optional - information about tooling version used to build the package
    /// </summary>
    public ToolingVersionInfo? ToolingVersionInfo { get; init; }
    /// <summary>
    /// Optional - script that can be used for building of the whole working copy (as an alternative for the individual nuget builds)
    /// </summary>
    public ScriptGroup BuildScript { get; init; }
    /// <summary>
    /// Optional - path to solution file that can be used for building of the whole working copy (as an alternative for the individual nuget builds)
    /// </summary>
    public string WorkingCopySolutionFile { get; init; }
    /// <summary>
    /// Set of build descriptors for nuget packages buildable from the working copy.
    /// </summary>
    public NugetBuildDescriptor[] NugetBuildDescriptors { get; init; }
}
