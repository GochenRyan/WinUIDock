using System;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ToolControlPartName, Type = typeof(ToolControl))]
    [TemplatePart(Name = ToolChromeControlPartName, Type = typeof(ToolChromeControl))]
    public sealed class ToolDockControl : Control
    {
        public const string ToolControlPartName = "PART_ToolControl";
        public const string ToolChromeControlPartName = "PART_ToolChromeControl";
        public ToolDockControl()
        {
            this.DefaultStyleKey = typeof(ToolDockControl);
            Loaded += ToolDockControl_Loaded;
            Unloaded += ToolDockControl_Unloaded;
            AddHandler(PointerPressedEvent, new PointerEventHandler(Dockable_PointerPressed), true);
        }

        private void ToolDockControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock toolDock)
            {
                toolDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }
        }

        private void ToolDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolDockControl_DataContextChanged;
        }

        private void ToolDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolChromeControl = GetTemplateChild(ToolChromeControlPartName) as ToolChromeControl;
            _toolControl = GetTemplateChild(ToolControlPartName) as ToolControl;
            _toolChromeControl.RegisterPropertyChangedCallback(ToolChromeControl.VisibilityProperty, OnChildVisibilityChanged);

            BindData();
        }

        private void OnChildVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == ToolChromeControl.VisibilityProperty)
            {
                var visibility = (Visibility)sender.GetValue(dp);
                Visibility = visibility;
            }
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ToolDock toolDock)
            {
                _toolChromeControl.ClearValue(ToolChromeControl.IsActiveProperty);
                _toolChromeControl.SetBinding(ToolChromeControl.IsActiveProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsActive"),
                    Mode = BindingMode.OneWay
                });

                _toolControl.ClearValue(ToolControl.DataContextProperty);
                _toolControl.SetBinding(ToolControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                toolDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
                toolDock.VisibleDockables.CollectionChanged += VisibleDockables_CollectionChanged;
            }
        }

        private void VisibleDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                sender is not ObservableCollection<IDockable> visibleDockables)
            {
                return;
            }

            if (DataContext is ToolDock toolDock)
            {
                // A dockable can arrive with no active selection (observed on
                // the root-edge redock path: the tab shows up but ActiveDockable
                // stays null, so the content pipeline never runs and the pane
                // renders empty). Activate the newcomer.
                if (toolDock.ActiveDockable is null
                    && e.NewItems is { Count: > 0 }
                    && e.NewItems[0] is IDockable added)
                {
                    Internal.DockDiag.Log($"ToolDockControl: ActiveDockable null after add — activating '{added.Title}'");
                    toolDock.ActiveDockable = added;
                }
            }

            if (visibleDockables.Count == 1)
            {
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null)
                {
                    if (parent is ProportionalStackPanel proportionalStackPanel)
                    {
                        proportionalStackPanel.InvalidateMeasure();
                        break;
                    }
                    else
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                return base.MeasureOverride(availableSize);
            }
            catch (Exception ex) when (Internal.DockDiag.IsTransientLayoutError(ex))
            {
                // See ToolChromeControl.MeasureOverride — transient cross-window
                // migration race; self-heal instead of tearing the app down.
                Internal.DockDiag.Log($"ToolDockControl.MeasureOverride transient failure on {Internal.DockDiag.Describe(this)}: {ex.Message} — retrying inline");
                try
                {
                    // The pinpoint probe proved an immediate re-measure of the
                    // same subtree succeeds — retry inline so the pass still
                    // materializes templates/content (skipping it left panes
                    // empty after redock).
                    return base.MeasureOverride(availableSize);
                }
                catch (Exception retryEx) when (Internal.DockDiag.IsTransientLayoutError(retryEx))
                {
                    Internal.DockDiag.Log("ToolDockControl.MeasureOverride retry also failed — deferring");
                    DispatcherQueue?.TryEnqueue(InvalidateMeasure);
                    return DesiredSize;
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            try
            {
                return base.ArrangeOverride(finalSize);
            }
            catch (Exception ex) when (Internal.DockDiag.IsTransientLayoutError(ex))
            {
                Internal.DockDiag.Log($"ToolDockControl.ArrangeOverride transient failure on {Internal.DockDiag.Describe(this)}: {ex.Message} — retrying inline");
                try
                {
                    return base.ArrangeOverride(finalSize);
                }
                catch (Exception retryEx) when (Internal.DockDiag.IsTransientLayoutError(retryEx))
                {
                    Internal.DockDiag.Log("ToolDockControl.ArrangeOverride retry also failed — deferring");
                    DispatcherQueue?.TryEnqueue(InvalidateArrange);
                    return finalSize;
                }
            }
        }

        private void Dockable_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext is ToolDock dock)
            {
                if (dock.ActiveDockable != null)
                {
                    dock.Owner.Factory.SetActiveDockable(dock.ActiveDockable);
                }
            }
        }

        private ToolChromeControl _toolChromeControl;
        private ToolControl _toolControl;
    }
}
