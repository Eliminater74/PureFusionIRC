namespace PureFusionIRC.Core.Scripting;

public interface IPureFusionPlugin
{
    string Name { get; }
    string Version { get; }
    void Start(IPluginHost host);
    void Stop();
}

public interface IPluginHost
{
    string AppVersion { get; }
    void Print(string text);
    Task RunCommandAsync(string command);
}

/// <summary>Ensures the plugin drop folder exists. Assembly loading is a later milestone.</summary>
public sealed class PluginFolder
{
    public PluginFolder(string directory) => DirectoryPath = directory;

    public string DirectoryPath { get; }

    public void Ensure() => Directory.CreateDirectory(DirectoryPath);
}
