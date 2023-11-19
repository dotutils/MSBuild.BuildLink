namespace DotUtils.MsBuild.BuildLink.SourceCodes;

internal class Script
{
    public static readonly Script NullScript = new Script();

    private Script()
    {
        ScriptFilePath = string.Empty;
    }

    public static Script FromPath(string path)
    {
        return string.IsNullOrEmpty(path) ? NullScript : new Script(path);
    }

    public Script(string scriptFilePath)
    {
        ScriptFilePath = scriptFilePath;
        ScriptType = scriptFilePath.ToScriptType();
    }

    public Script(string scriptFilePath, ScriptType scriptType)
    {
        ScriptFilePath = scriptFilePath;
        ScriptType = scriptType;
    }

    public string ScriptFilePath { get; init; }
    public ScriptType ScriptType { get; init; }

}
