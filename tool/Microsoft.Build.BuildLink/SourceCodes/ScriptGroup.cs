using System.Runtime.InteropServices;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.Utils;

namespace Microsoft.Build.BuildLink.SourceCodes;

internal class ScriptGroup : Dictionary<OSPlatform, Script>
{
    public static readonly ScriptGroup NullScript = new ScriptGroup();

    // for deserialization
    private ScriptGroup()
    { }

    public static ScriptGroup FromPath(string path)
    {
        return string.IsNullOrEmpty(path) ? NullScript : new ScriptGroup(path);
    }

    public static ScriptGroup FromPaths(params string[] paths)
        => FromPaths((IEnumerable<string>)paths);

    public static ScriptGroup FromPaths(IEnumerable<string> paths)
    {
        Dictionary<OSPlatform, Script> scripts = new Dictionary<OSPlatform, Script>();
        foreach (string path in paths)
        {
            Script script = Script.FromPath(path);
            if (!scripts.TryAdd(script.ScriptType.ToDefaultOsPlatform(), script))
            {
                throw new BuildLinkException($"Attempt to add [{path}], while ScriptGroup already contains script for that platform ({scripts.Values.ToCsvString()})", BuildLinkErrorCode.InternalError);
            }
        }

        return scripts.Any() ? new ScriptGroup(scripts.Values) : NullScript;
    }

    public ScriptGroup(string scriptFilePath)
        :this(new Script(scriptFilePath, scriptFilePath.ToScriptType()))
    { }

    public ScriptGroup(Script script)
    {
        this[script.ScriptType.ToDefaultOsPlatform()] = script;
    }

    public ScriptGroup(params Script[] scripts)
        :this((IEnumerable<Script>)scripts)
    { }

    public ScriptGroup(IEnumerable<Script> scripts)
    {
        foreach (Script script in scripts.Where(s => !s.IsNull()))
        {
            this[script.ScriptType.ToDefaultOsPlatform()] = script;
        }
    }
}
