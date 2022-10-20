using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Notifications.Wpf.Core.Controls
{
    /// <summary>
    /// Stack panel implementation that has the ability to reverse the contained elements order
    /// </summary>
    public class ReversibleStackPanel : StackPanel
    {
        /// <summary>
        /// True if the order should be reversed, false otherwise
        /// </summary>
        public bool ReverseOrder
        {
            get
            {
                return (bool)GetValue(ReverseOrderProperty);
            }
            set
            {
                SetValue(ReverseOrderProperty, value);
            }
        }

        /// <summary>
        /// Dependency property of ReverseOrder
        /// </summary>
        public static readonly DependencyProperty ReverseOrderProperty =
            DependencyProperty.Register("ReverseOrder", typeof(bool), typeof(ReversibleStackPanel), new PropertyMetadata(false));

        /// <summary>
        /// Adjust the ordering of the children
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double x = 0;
            double y = 0;

            IEnumerable<UIElement> children = ReverseOrder ? InternalChildren.Cast<UIElement>().Reverse() : InternalChildren.Cast<UIElement>();

            foreach (UIElement child in children)
            {
                Size size;

                if (Orientation == Orientation.Horizontal)
                {
                    size = new Size(child.DesiredSize.Width, Math.Max(arrangeSize.Height, child.DesiredSize.Height));
                    child.Arrange(new Rect(new Point(x, y), size));
                    x += size.Width;
                }
                else
                {
                    size = new Size(Math.Max(arrangeSize.Width, child.DesiredSize.Width), child.DesiredSize.Height);
                    child.Arrange(new Rect(new Point(x, y), size));
                    y += size.Height;
                }
            }

            return Orientation == Orientation.Horizontal ? new Size(x, arrangeSize.Height) : new Size(arrangeSize.Width, y);
        }
    }
}