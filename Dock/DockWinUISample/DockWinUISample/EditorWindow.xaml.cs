using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI;
using WinUIEx;
using Path = System.IO.Path;

namespace DockWinUISample
{
    public enum EditorKind
    {
        Animation,
        ShaderGraph
    }

    /// <summary>
    /// A context of its own: a top-level window carrying its own DockControl,
    /// Factory and RootDock, in the shape of an asset editor.
    ///
    /// Two rules make it a context rather than just another window:
    /// its factory instance is private to it, which is what keeps its panels from
    /// docking into the main window's; and its layout is persisted per editor
    /// KIND, so every Animation editor opens the way the last one was left while
    /// the Shader Graph keeps its own arrangement.
    /// </summary>
    public sealed partial class EditorWindow : WindowEx
    {
        public EditorWindow(EditorKind kind, Window owner)
        {
            InitializeComponent();

            _kind = kind;

            // Window geometry persists per editor KIND (the Unreal model), matching
            // the per-kind layout templates below. Set before activation so WinUIEx
            // restores the placement while the window is being set up.
            PersistenceId = kind == EditorKind.Animation ? "AnimationEditor" : "ShaderGraphEditor";

            // Enters the dock registry so coordinate transforms can resolve this
            // window, and — through the owner — so it and everything torn off it
            // close when the application's main window does.
            HostWindow.Register(this, owner);

            DockThemeManager.RegisterWindow(this);

            Title = kind == EditorKind.Animation ? "Animation Editor" : "Shader Graph";
            EditorTitleBar.Title = Title;
            AssetsText.Text = kind == EditorKind.Animation
                ? "Clips, skeletons, curves."
                : "Materials, textures, functions.";
            PropertiesText.Text = kind == EditorKind.Animation
                ? "Selected key: no selection."
                : "Selected node: no selection.";
            StageHost.Content = kind == EditorKind.Animation ? BuildTimeline() : BuildNodeGraph();
            Stage.Title = kind == EditorKind.Animation ? "Walk.anim" : "M_Water.shader";

            _serializer = new DockSerializer(typeof(List<>));

            // The XAML sets these too, but x:Bind's first pass runs after the
            // constructor body — and the snapshot below happens here. Left to the
            // bindings, "the built-in layout" would be captured with no
            // DefaultDockable and no active tab anywhere: restoring it would
            // render an empty window (the former) or empty panes (the latter).
            Root.DefaultDockable = Body;
            LeftPane.ActiveDockable = Assets;
            EditorPane.ActiveDockable = Stage;
            BottomPane.ActiveDockable = LogTool;
            RightPane.ActiveDockable = Properties;

            if (Dock?.Layout is { } layout)
            {
                _declaredRoot = layout;
                IndexDeclared(layout);
                _defaultLayout = _serializer.Serialize(layout);
            }

            LoadTemplate();

            // Saving on Closed rather than on every rearrangement keeps the file
            // writes down to one per session; the window is still alive here, so
            // its layout is readable.
            Closed += (_, _) => SaveTemplate();

            if (Environment.GetEnvironmentVariable("DOCKSAMPLE_EDGEREPRO") is { Length: > 0 } repro)
            {
                Dock.Loaded += (_, _) => ReproRootTopDock(repro == "1");
            }
        }

        /// <summary>
        /// Replays "dock the right pane onto the top edge of the window" against
        /// the real, XAML-built tree and reports what the root is left pointing at.
        /// </summary>
        private void ReproRootTopDock(bool performDock)
        {
            void Report(string when)
            {
                var root = Dock.Layout as IRootDock;
                App.Log($"EdgeRepro[{_kind}] {when}: default={DockDiagnostics.Describe(root?.DefaultDockable)} "
                        + $"| children={root?.VisibleDockables?.Count} "
                        + $"| tree={DockDiagnostics.DescribeTree(Dock.Layout)}");
            }

            Report("before");

            if (!performDock)
            {
                return;
            }

            var ok = Dock.DockManager.ValidateDockable(
                RightPane, LeftPane, DragAction.Move, DockOperation.RootTop, true);

            App.Log($"EdgeRepro[{_kind}] ValidateDockable(RootTop) -> {ok}");
            Report("after");
        }

        public DockControl DockControl => Dock;

        public EditorKind Kind => _kind;

        // ---- per-kind layout template ----

        /// <summary>
        /// One file per editor KIND, not per window: opening a second Animation
        /// editor gets the arrangement the first one was left in. LocalAppData,
        /// not TEMP — a layout template is a setting, and TEMP gets cleaned.
        /// </summary>
        private static string TemplatePath(EditorKind kind)
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "winuidock-sample", $"layout-{kind}.json");

        public static void ResetTemplates()
        {
            foreach (EditorKind kind in Enum.GetValues(typeof(EditorKind)))
            {
                try
                {
                    File.Delete(TemplatePath(kind));
                }
                catch
                {
                    // Never held open — a failure here means it was already gone.
                }
            }
        }

        private void LoadTemplate()
        {
            var path = TemplatePath(_kind);
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                using var stream = File.OpenRead(path);
                if (_serializer.Load<IDock>(stream, ResolveDockable) is { } layout)
                {
                    Dock.Layout = layout;
                    Log($"loaded the {_kind} layout template");
                }
            }
            catch (Exception e)
            {
                Log($"template load failed: {e.Message}");
            }
        }

        private void SaveTemplate()
        {
            if (Dock?.Layout is not { } layout)
            {
                return;
            }

            try
            {
                var path = TemplatePath(_kind);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var stream = File.Create(path);
                _serializer.Save(stream, layout);
            }
            catch
            {
                // A template that cannot be written is not worth a dialog; the
                // next session simply opens the built-in layout.
            }
        }

        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            SaveTemplate();
            Log($"saved the {_kind} layout template");
        }

        private void ResetTemplate_Click(object sender, RoutedEventArgs e) => ResetToBuiltIn();

        public void ResetToBuiltIn()
        {
            if (_defaultLayout is null)
            {
                return;
            }

            var layout = _serializer.Deserialize<IDock>(_defaultLayout);
            if (DockableResolution.Apply(layout, ResolveDockable) is IDock restored)
            {
                Dock.Layout = restored;
                Log("layout reset to the built-in default");
            }
        }

        // Same adoption rule as the main window: structure from the file,
        // identity from the XAML-declared instances, so [JsonIgnore] Content
        // survives a round trip.
        private void IndexDeclared(IDockable dockable)
        {
            if (!string.IsNullOrEmpty(dockable.Id))
            {
                _declared[dockable.Id] = dockable;
            }

            if (dockable is IDock dock && dock.VisibleDockables is { } children)
            {
                foreach (var child in children)
                {
                    IndexDeclared(child);
                }
            }
        }

        private IDockable ResolveDockable(IDockable deserialized)
        {
            if (string.IsNullOrEmpty(deserialized.Id)
                || !_declared.TryGetValue(deserialized.Id, out var live)
                || ReferenceEquals(live, deserialized))
            {
                return deserialized;
            }

            if (ReferenceEquals(live, _declaredRoot))
            {
                return deserialized;
            }

            if (deserialized is IDock loaded && live is IDock target)
            {
                DockableResolution.TransplantStructure(loaded, target);
            }

            return live;
        }

        private void Log(string message)
        {
            LogText.Text = message + Environment.NewLine + LogText.Text;
        }

        // ---- mock content, just enough for the two editors to look different ----

        private static UIElement BuildTimeline()
        {
            var rows = new StackPanel { Padding = new Thickness(12), Spacing = 6 };

            foreach (var (track, start, length) in new[]
            {
                ("Root", 0, 26), ("Spine", 3, 18), ("Arm.L", 6, 12), ("Arm.R", 8, 12), ("Leg.L", 1, 22)
            })
            {
                var row = new StackPanel
                {
                    Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                    Spacing = 8
                };
                row.Children.Add(new TextBlock
                {
                    Text = track,
                    Width = 60,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                });

                var lane = new Grid { Width = 320, Height = 14, Background = new SolidColorBrush(Colors.Black) };
                lane.Children.Add(new Rectangle
                {
                    Width = length * 10,
                    Height = 14,
                    Margin = new Thickness(start * 10, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x4E, 0xC9, 0xB0))
                });

                row.Children.Add(lane);
                rows.Children.Add(row);
            }

            return rows;
        }

        private static UIElement BuildNodeGraph()
        {
            var canvas = new Canvas { Margin = new Thickness(12) };

            void Node(string title, double x, double y)
            {
                var border = new Border
                {
                    Width = 130,
                    Padding = new Thickness(8, 6, 8, 6),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x2D, 0x2D, 0x30)),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x56, 0x9C, 0xD6)),
                    Child = new TextBlock { Text = title, FontSize = 12 }
                };

                Microsoft.UI.Xaml.Controls.Canvas.SetLeft(border, x);
                Microsoft.UI.Xaml.Controls.Canvas.SetTop(border, y);
                canvas.Children.Add(border);
            }

            void Wire(double x1, double y1, double x2, double y2)
            {
                canvas.Children.Add(new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    StrokeThickness = 1.5,
                    Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x86, 0x86, 0x86))
                });
            }

            Node("Texture Sample", 0, 20);
            Node("Panner", 0, 90);
            Node("Lerp", 170, 55);
            Node("Output", 340, 55);
            Wire(130, 36, 170, 71);
            Wire(130, 106, 170, 87);
            Wire(300, 71, 340, 71);

            return canvas;
        }

        private readonly EditorKind _kind;
        private readonly IDockSerializer _serializer;
        private readonly Dictionary<string, IDockable> _declared = new();
        private IDockable _declaredRoot;
        private string _defaultLayout;
    }
}
