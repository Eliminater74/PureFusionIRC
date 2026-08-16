using Jint;
using Jint.Native;
using PureFusionIRC.Core.Irc;

namespace PureFusionIRC.Core.Scripting;

public sealed class ScriptErrorEventArgs : EventArgs
{
    public ScriptErrorEventArgs(string file, string message)
    {
        File = file;
        Message = message;
    }

    public string File { get; }
    public string Message { get; }
}

/// <summary>
/// Loads %.pf.js files. PureFusion's own script surface — not mIRC script, not HexChat Python.
/// </summary>
public sealed class JavascriptScriptHost
{
    private readonly List<(string File, Engine Engine)> _scripts = new();
    private IrcSession? _session;
    private Func<string, Task>? _runCommand;
    private Action<string>? _print;

    public event EventHandler<ScriptErrorEventArgs>? Error;

    public int LoadedCount => _scripts.Count;

    public void Attach(IrcSession session, Func<string, Task> runCommand, Action<string> print)
    {
        _session = session;
        _runCommand = runCommand;
        _print = print;
    }

    public void LoadDirectory(string directory)
    {
        Unload();
        Directory.CreateDirectory(directory);
        SeedExample(directory);
        foreach (var file in Directory.GetFiles(directory, "*.pf.js"))
        {
            TryLoad(file);
        }
    }

    public void Unload() => _scripts.Clear();

    public void Emit(string eventName, IReadOnlyDictionary<string, object?> payload)
    {
        foreach (var (file, engine) in _scripts.ToArray())
        {
            try
            {
                var js = JsValue.FromObject(engine, payload);
                engine.Invoke("__pureEmit", eventName, js);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ScriptErrorEventArgs(file, ex.Message));
            }
        }
    }

    private void TryLoad(string file)
    {
        try
        {
            var engine = new Engine(options =>
            {
                options.TimeoutInterval(TimeSpan.FromSeconds(2));
                options.LimitRecursion(64);
                options.MaxStatements(10_000);
                options.Strict();
            });

            engine.SetValue("__pureCommand", new Action<string>(line => _runCommand?.Invoke(line)));
            engine.SetValue("__purePrint", new Action<string>(text => _print?.Invoke(text)));
            engine.SetValue("__pureNick", new Func<string>(() => _session?.CurrentNick ?? string.Empty));
            engine.Execute("""
                var __pureHandlers = Object.create(null);
                function __pureEmit(name, payload) {
                  var list = __pureHandlers[name];
                  if (!list) { return; }
                  for (var i = 0; i < list.length; i++) { list[i](payload); }
                }
                var irc = {
                  on: function (name, fn) {
                    if (!__pureHandlers[name]) { __pureHandlers[name] = []; }
                    __pureHandlers[name].push(fn);
                  },
                  command: function (line) { __pureCommand(String(line)); },
                  print: function (text) { __purePrint(String(text)); }
                };
                Object.defineProperty(irc, "nick", { get: function () { return __pureNick(); } });
                """);
            engine.Execute(File.ReadAllText(file));
            _scripts.Add((file, engine));
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new ScriptErrorEventArgs(file, ex.Message));
        }
    }

    private static void SeedExample(string directory)
    {
        var example = Path.Combine(directory, "example.pf.js");
        if (File.Exists(example))
        {
            return;
        }

        File.WriteAllText(example, """
            // PureFusionIRC script example (JavaScript, not mIRC script).
            // Rename or copy this file; it is loaded from %AppData%\PureFusionIRC\scripts\
            irc.on("connect", function () {
              irc.print("example.pf.js loaded. Current nick: " + irc.nick);
            });

            irc.on("message", function (e) {
              // e.target, e.nick, e.text
            });
            """);
    }
}
