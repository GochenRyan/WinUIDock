using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    internal static class DockHelpers
    {
        private static bool IsHitTestVisible(UIElement element)
        {
            return element != null &&
                   element.Visibility == Visibility.Visible &&
                   element.IsHitTestVisible;
        }

        public static IEnumerable<UIElement> GetVisualsAt(Point point, UIElement element)
        {
            List<UIElement> elements = new();
            var hits = VisualTreeHelper.FindElementsInHostCoordinates(point, element, true);
            foreach (var hit in hits)
            {
                if (IsHitTestVisible(hit) && hit is UIElement uiElement)
                {
                    elements.Add(uiElement);
                }
            }
            return elements;
        }

        public static Control GetControl(UIElement input, Point point, DependencyProperty property)
        {
            IEnumerable<UIElement> inputElements = GetVisualsAt(point, input);

            var panels = inputElements?.OfType<Panel>().ToList();
            Panel selectedPanel = null;
            if (panels is { })
            {
                foreach (var panel in panels)
                {
                    if ((bool)panel.GetValue(property))
                    {
                        selectedPanel = panel;
                        break;
                    }
                }
            }

            var controls = inputElements?.OfType<Control>().ToList();
            if (controls is { })
            {
                foreach (var control in controls)
                {
                    if (selectedPanel != null && ContainsPanel(control, selectedPanel))
                    {
                        return control;
                    }

                    if ((bool)control.GetValue(property))
                    {
                        return control;
                    }
                }
            }
            return null;
        }

        private static bool ContainsPanel(Control parentControl, Panel panelToFind)
        {
            return FindChildPanel(parentControl, panelToFind) != null;
        }

        private static Panel FindChildPanel(DependencyObject parent, Panel panelToFind)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child == panelToFind)
                {
                    return panelToFind;
                }

                Panel foundPanel = FindChildPanel(child, panelToFind);

                if (foundPanel != null)
                {
                    return foundPanel;
                }
            }

            return null;
        }

        public static DockPoint ToDockPoint(Point point)
        {
            return new(point.X, point.Y);
        }

        public static void ShowWindows(IDockable dockable)
        {
            if (dockable.Owner is IDock { Factory: { } factory } dock)
            {
                if (factory.FindRoot(dock, _ => true) is { ActiveDockable: IRootDock activeRootDockable })
                {
                    if (activeRootDockable.ShowWindows.CanExecute(null))
                    {
                        activeRootDockable.ShowWindows.Execute(null);
                    }
                }
            }
        }
    }
}
