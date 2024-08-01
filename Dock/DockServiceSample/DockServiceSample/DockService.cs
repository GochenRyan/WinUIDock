using CommunityToolkit.WinUI.UI;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DockServiceSample
{
    public class DockService
    {
        public DockService()
        {
            DockControl = HostWindow.MainWindow.Content.FindDescendant<DockControl>();

            RegisterDockableControls();

            m_serializer = new DockSerializer(typeof(List<>));

            m_dockState = new DockState();
        }

        public void LoadDefault()
        {
            try
            {
                using (var stream = new FileStream(DefaultPath, FileMode.Create, FileAccess.Write))
                {
                    SaveLayout(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                using (var stream = new FileStream(DefaultPath, FileMode.Open, FileAccess.Read))
                {
                    LoadLayout(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SaveLayout(Stream stream)
        {
            var layout = DockControl.Layout;
            if (layout is { })
            {
                m_serializer.Save(stream, layout);
            }
        }

        public void LoadLayout(Stream stream)
        {
            var layout = m_serializer.Load<IDock>(stream);
            if (layout is { })
            {
                DockControl.Layout = layout;
                var map = HostWindow.windowMap;
                m_loadingCnt = map.Count;
                foreach (var kv in map)
                {
                    var windowContent = kv.Value.Content as FrameworkElement;
                    if (windowContent != null)
                    {
                        var hostWindowControl = windowContent.FindChild<HostWindowControl>();
                        hostWindowControl.Loaded += HostWindowControl_Loaded;
                    }
                }
                LinkRegisterControls();
            }
        }

        private void LinkRegisterControls()
        {
            if (m_loadingCnt == 0)
            {
                Link();
                m_dockState.Save(DockControl.Layout);
                m_dockState.Restore(DockControl.Layout);
            }
        }

        private void HostWindowControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            m_loadingCnt--;
            LinkRegisterControls();
        }

        private void Link()
        {
            foreach (var pair in m_controlInfoDict)
            {
                var dockables = WinUIDockManager.FindDockableByID(pair.Key);
                if (dockables.Count() > 0)
                {
                    if (dockables.First() is IToolContent toolContent)
                    {
                        toolContent.Content = pair.Value.Control;
                    }
                    else if (dockables.First() is IDocumentContent documentContent)
                    {
                        documentContent.Content = pair.Value.Control;
                    }
                }
                else
                {
                    ShowUnlinkedDockableControls(pair.Key, pair.Value);
                }
            }
        }

        private void ShowUnlinkedDockableControls(string id, ControlInfo info)
        {
            switch (info.Group)
            {
                case StandardControlGroup.Top:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Bottom:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(BottomPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Left:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(LeftPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Right:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(RightPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Center:
                    {
                        var document = WinUIDockManager.CreateDockable(DockableType.Document, id, info.Name, info.Control) as IDocument;
                        var dock = WinUIDockManager.FindDockByID(DocumentPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(document, dock);
                    }
                    break;
                case StandardControlGroup.CenterPermanent:
                    {
                        var document = WinUIDockManager.CreateDockable(DockableType.Document, id, info.Name, info.Control) as IDocument;
                        document.CanClose = false;
                        var dock = WinUIDockManager.FindDockByID(DocumentPaneName).First();
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(document, dock);
                    }
                    break;
                case StandardControlGroup.LeftHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(LeftPaneName).First();
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.RightHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(RightPaneName).First();
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.TopHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName).First();
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.BottomHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName).First();
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.Floating:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as IDocument;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName).First();
                        if (dock != null)
                        {
                            WinUIDockManager.SplitToWindow(dock, tool, 0, 0, 800, 600);
                        }
                    }
                    break;
            }
        }

        private void RegisterDockableControls()
        {
            var documentControl = new DocumentSampleControl1
            {
                DocumentText = "May the force be with you."
            };
            var info = new ControlInfo("document1", StandardControlGroup.CenterPermanent)
            {
                Control = documentControl
            };
            var id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var leftToolControl = new ToolSampleControl1
            {
                ToolText = "You must unlearn what you have learned."
            };
            info = new ControlInfo("left_tool", StandardControlGroup.Left)
            {
                Control = leftToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var rightToolControl = new ToolSampleControl1
            {
                ToolText = "If you will not be turned, you will be destroyed."
            };
            info = new ControlInfo("right_tool", StandardControlGroup.Right)
            {
                Control = rightToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var bottomToolControl = new ToolSampleControl1
            {
                ToolText = "I am your Father!"
            };
            info = new ControlInfo("bottom_tool", StandardControlGroup.Bottom)
            {
                Control = bottomToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var toolWindowControl = new ToolSampleControl1
            {
                ToolText = "Do or do not. there’s no try."
            };
            info = new ControlInfo("window_tool", StandardControlGroup.Floating)
            {
                Control = toolWindowControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;
        }

        public void Hide(string name)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == name)
                {
                    var dockables = WinUIDockManager.FindDockableByID(pair.Key);
                    if (dockables.Count() > 0)
                    {
                        var dockable = dockables.First();
                        WinUIDockManager.CloseDockable(dockable);
                    }
                }
            }
        }

        public void Show(string name)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == name)
                {
                    var dockables = WinUIDockManager.FindDockableByID(pair.Key);
                    if (dockables.Count() > 0)
                        return;
                    ShowUnlinkedDockableControls(pair.Key, pair.Value);
                }
            }
        }

        private string GetPersistenceId(ControlInfo info)
        {
            // first, try Name            
            string name = info.Name;

            // don't use name as a part of id if it is too long 
            bool usedefault
                = string.IsNullOrEmpty(name)
                || name.Length > 64
                || name.IndexOfAny(s_pathDelimiters) > 0
                || name.Contains(".");

            if (usedefault)
                name = "document_panel";

            string id = info.Control.GetType().Name + "_" + name;
            id = m_idNamer.Name(id);
            return id;
        }

        public readonly string DefaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.json");

        private readonly Dictionary<string, ControlInfo> m_controlInfoDict = new();

        public DockControl DockControl { get; set; }

        private readonly IDockSerializer m_serializer;
        private readonly IDockState m_dockState;
        private readonly UniqueNamer m_idNamer = new();
        private static readonly char[] s_pathDelimiters = new[] { '/', '\\' };

        private const string TopPaneName = "TopPane";
        private const string BottomPaneName = "BottomPane";
        private const string LeftPaneName = "LeftPane";
        private const string RightPaneName = "RightPane";
        private const string DocumentPaneName = "DocumentPane";

        private int m_loadingCnt = 0;
    }
}
