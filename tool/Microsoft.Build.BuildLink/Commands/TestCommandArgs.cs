namespace Microsoft.Build.BuildLink.Commands;

public sealed class TestCommandArgs
{
    public TestCommandArgs(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; init; }
    public int Value { get; init; }
}