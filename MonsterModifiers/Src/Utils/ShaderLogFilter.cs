using System.Collections.Generic;
using BepInEx.Logging;

namespace MonsterModifiers;

public class ShaderLogFilter : ILogListener
{
    private readonly ILogListener _inner;

    private ShaderLogFilter(ILogListener inner)
    {
        _inner = inner;
    }

    public static void Install()
    {
        var listeners = BepInEx.Logging.Logger.Listeners;

        // 이미 설치되어 있으면 스킵
        foreach (var l in listeners)
            if (l is ShaderLogFilter)
                return;

        var snapshot = new List<ILogListener>(listeners);
        listeners.Clear();
        foreach (var listener in snapshot)
            listeners.Add(new ShaderLogFilter(listener));
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        if (eventArgs?.Data?.ToString()?.Contains("Failed to find expected binary shader data") == true)
            return;

        _inner?.LogEvent(sender, eventArgs);
    }

    public void Dispose()
    {
        _inner?.Dispose();
    }
}
