using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = "InternalThumb", Type = typeof(Thumb))]
    public class ProportionalStackPanelSplitter : Control
    {
        public ProportionalStackPanelSplitter()
        {
            this.DefaultStyleKey = typeof(ProportionalStackPanelSplitter);
            Loaded += ProportionalStackPanelSplitter_Loaded;
        }

        private void ProportionalStackPanelSplitter_Loaded(object sender, RoutedEventArgs e)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            var panel = GetPanel();
            if (panel is null)
            {
                return;
            }

            UpdateHeightOrWidth();
        }

        protected override void OnApplyTemplate()
        {
            if (_thumb != null)
            {
                _thumb.DragCompleted -= Thumb_DragCompleted;
                _thumb.DragDelta -= Thumb_DragDelta;
                _thumb.DragStarted -= Thumb_DragStarted;
                _thumb.KeyDown -= Thumb_KeyDown;
            }

            _thumb = GetTemplateChild("InternalThumb") as Thumb;

            if (_thumb != null)
            {
                _thumb.DragCompleted += Thumb_DragCompleted;
                _thumb.DragDelta += Thumb_DragDelta;
                _thumb.DragStarted += Thumb_DragStarted;
                _thumb.KeyDown += Thumb_KeyDown;
            }
        }

        private void Thumb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            ThumbDragStarted?.Invoke(sender, e);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ThumbDragDelta?.Invoke(sender, e);
            if (GetPanel() is { } panel)
            {
                SetTargetProportion(panel.Orientation == Orientation.Vertical ? e.VerticalChange : e.HorizontalChange);
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ThumbDragCompleted?.Invoke(sender, e);
        }

        public static double GetMinimumProportionSize(UIElement obj)
        {
            return (double)obj.GetValue(MinimumProportionSizeProperty);
        }
        public static void SetMinimumProportionSize(UIElement obj, double value)
        {
            obj.SetValue(MinimumProportionSizeProperty, value);
        }

        private void UpdateHeightOrWidth()
        {
            if (GetPanel() is { } panel)
            {
                if (panel.Orientation == Orientation.Vertical)
                {
                    Height = Thickness;
                    Width = double.NaN;
                    ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
                }
                else
                {
                    Width = Thickness;
                    Height = double.NaN;
                    ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
                }
            }
        }

        public static bool IsSplitter(UIElement element)
        {
            if (element is ContentPresenter contentPresenter)
            {
                var control = VisualTreeHelper.GetChild(contentPresenter, 0);

                if (control == null && contentPresenter.Content != null && contentPresenter.Content is ProportionalDockSplitter)
                {
                    return true;
                }

                if (control is ProportionalStackPanelSplitter)
                {
                    return true;
                }
            }

            return element is ProportionalStackPanelSplitter;
        }

        private ProportionalStackPanel GetPanel()
        {
            var parentElement = VisualTreeHelper.GetParent(this);
            if (parentElement is ContentPresenter presenter)
            {
                if (VisualTreeHelper.GetParent(presenter) is ProportionalStackPanel panel)
                {
                    return panel;
                }
            }
            else if (parentElement is ProportionalStackPanel panel)
            {
                return panel;
            }

            return null;
        }

        private UIElement GetSiblingInDirection(ProportionalStackPanel panel, int direction)
        {
            Debug.Assert(direction == -1 || direction == 1);

            var children = panel.Children;
            int siblingIndex;

            if (VisualTreeHelper.GetParent(this) is ContentPresenter parentContentPresenter)
            {
                siblingIndex = children.IndexOf(parentContentPresenter) + direction;
            }
            else
            {
                siblingIndex = children.IndexOf(this) + direction;
            }

            while (siblingIndex >= 0 && siblingIndex < children.Count &&
                   (ProportionalStackPanel.GetIsCollapsed(children[siblingIndex]) || IsSplitter(children[siblingIndex])))
            {
                siblingIndex += direction;
            }

            return siblingIndex >= 0 && siblingIndex < children.Count ? children[siblingIndex] : null;
        }

        private UIElement GetTargetElement(ProportionalStackPanel panel)
        {
            return GetSiblingInDirection(panel, -1);
        }

        private UIElement FindNextChild(ProportionalStackPanel panel)
        {
            return GetSiblingInDirection(panel, 1);
        }

        private void SetTargetProportion(double dragDelta)
        {
            var panel = GetPanel();
            if (panel == null)
            {
                return;
            }

            var target = GetTargetElement(panel);
            if (target is null)
            {
                return;
            }

            var child = FindNextChild(panel);

            var targetElementProportion = ProportionalStackPanel.GetProportion(target);
            var neighbourProportion = child is not null ? ProportionalStackPanel.GetProportion(child) : double.NaN;

            var dProportion = dragDelta / (panel.Orientation == Orientation.Vertical ? panel.ActualHeight : panel.ActualWidth);

            if (targetElementProportion + dProportion < 0)
            {
                dProportion = -targetElementProportion;
            }

            if (neighbourProportion - dProportion < 0)
            {
                dProportion = +neighbourProportion;
            }

            targetElementProportion += dProportion;
            neighbourProportion -= dProportion;

            var minProportion = GetMinimumProportionSize(this) / (panel.Orientation == Orientation.Vertical ? panel.ActualHeight : panel.ActualWidth);

            if (targetElementProportion < minProportion)
            {
                dProportion = targetElementProportion - minProportion;
                neighbourProportion += dProportion;
                targetElementProportion -= dProportion;

            }
            else if (neighbourProportion < minProportion)
            {
                dProportion = neighbourProportion - minProportion;
                neighbourProportion -= dProportion;
                targetElementProportion += dProportion;
            }

            ProportionalStackPanel.SetProportion(target, targetElementProportion);

            if (child is not null)
            {
                ProportionalStackPanel.SetProportion(child, neighbourProportion);
            }

            panel.InvalidateMeasure();
        }

        public event DragStartedEventHandler ThumbDragStarted;
        public event DragCompletedEventHandler ThumbDragCompleted;
        public event DragDeltaEventHandler ThumbDragDelta;

        private Thumb _thumb;

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness),
            typeof(double),
            typeof(ProportionalStackPanelSplitter),
            new PropertyMetadata(4.0));

        public static readonly DependencyProperty MinimumProportionSizeProperty = DependencyProperty.RegisterAttached(
            "MinimumProportionSize",
            typeof(double),
            typeof(ProportionalStackPanelSplitter),
            new PropertyMetadata(75.0));

        /// <summary>
        /// Gets or sets the thickness (height or width, depending on orientation).
        /// </summary>
        /// <value>The thickness.</value>
        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }
    }
}
