using System.Runtime.InteropServices;

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
