using System;
using System.IO;
using System.Text;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace Dock.Model;

/// <summary>
/// Opt-in tracing for the layout mutations that are hard to reproduce by hand.
/// Disabled by default and free when off — the message is a lambda, so nothing is
/// formatted unless tracing is on.
///
/// Enable with <c>WINUIDOCK_DIAG=1</c>; output goes to
/// <c>%TEMP%\winuidock-diag.log</c>, the same file the WinUI3 layer writes to, so
/// model-level and input-level lines interleave in one timeline. Kept separate from
/// <c>Dock.WinUI3.Internal.DockDiag</c> only because Dock.Model must not depend on
/// the UI assembly.
/// </summary>
public static class DockDiagnostics
{
    private static readonly bool s_enabled =
        Environment.GetEnvironmentVariable("WINUIDOCK_DIAG") == "1";

    private static readonly string s_logPath =
        Path.Combine(Path.GetTempPath(), "winuidock-diag.log");

    public static bool IsEnabled => s_enabled;

    public static void Log(Func<string> message)
    {
        if (!s_enabled)
        {
            return;
        }

        try
        {
            File.AppendAllText(s_logPath, $"[{DateTimeOffset.Now:O}] model: {message()}\r\n");
        }
        catch
        {
        }
    }

    public static string Describe(IDockable? dockable)
    {
        if (dockable is null)
        {
            return "<null>";
        }

        var id = string.IsNullOrEmpty(dockable.Id) ? "-" : dockable.Id;
        return $"{dockable.GetType().Name}#{dockable.GetHashCode():x8}({id})";
    }

    /// <summary>
    /// One-line rendering of the whole layout under <paramref name="dockable"/>,
    /// including the bits that are easy to forget: a root's DefaultDockable, its
    /// float windows, and its parked HiddenDockables.
    /// </summary>
    public static string DescribeTree(IDockable? dockable)
    {
        var builder = new StringBuilder();
        Append(builder, dockable, 0);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, IDockable? dockable, int depth)
    {
        if (depth > 12)
        {
            builder.Append("…");
            return;
        }

        if (dockable is null)
        {
            builder.Append("<null>");
            return;
        }

        builder.Append(Describe(dockable));

        if (dockable is IProportionalDock proportional)
        {
            builder.Append(proportional.Orientation == Orientation.Horizontal ? "[H]" : "[V]");
        }

        if (dockable is IRootDock root)
        {
            builder.Append("{default=").Append(Describe(root.DefaultDockable));
            builder.Append(",active=").Append(Describe(root.ActiveDockable));

            if (root.Windows is { Count: > 0 } windows)
            {
                builder.Append(",windows=").Append(windows.Count);
            }

            if (root.HiddenDockables is { Count: > 0 } hidden)
            {
                builder.Append(",hidden=[");
                for (var i = 0; i < hidden.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(Describe(hidden[i]));
                }

                builder.Append(']');
            }

            builder.Append('}');
        }

        if (dockable is IDock dock && dock.VisibleDockables is { Count: > 0 } children)
        {
            builder.Append(" (");
            for (var i = 0; i < children.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" , ");
                }

                Append(builder, children[i], depth + 1);
            }

            builder.Append(')');
        }
    }
}
