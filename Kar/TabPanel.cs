using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;  
namespace Kar
{
    public class TabPanel: System.Windows.Controls.Panel
    {
        private const double MaxTabWidth = 150;
        private const double MinTabWidth = 40.0;
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            double maxHeight = 0;

            if (InternalChildren.Count == 0) return new System.Windows.Size(0, 0);

            UIElement addBtn = InternalChildren[InternalChildren.Count - 1];
            addBtn.Measure(availableSize);
            double addBtnWidth = addBtn.DesiredSize.Width;

            double availableForTabs = availableSize.Width - addBtnWidth;
            int tabCount = InternalChildren.Count - 1;

            double childWidth = tabCount > 0 ? availableForTabs / tabCount : MaxTabWidth;

           if(childWidth>MaxTabWidth) childWidth = MaxTabWidth;
           if(childWidth<MinTabWidth) childWidth = MinTabWidth;

            for (int i = 0; i < tabCount; i++)
            {
                InternalChildren[i].Measure(new System.Windows.Size(childWidth, availableSize.Height));
                maxHeight = Math.Max(maxHeight, InternalChildren[i].DesiredSize.Height);
            }
            return new System.Windows.Size(availableSize.Width, maxHeight);
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            if(InternalChildren.Count == 0) return finalSize;

            UIElement addBtn = InternalChildren[InternalChildren.Count - 1];
            int tabCount = InternalChildren.Count - 1;

            double availableForTabs = finalSize.Width - addBtn.DesiredSize.Width;
            double childWidth = tabCount >0 ? availableForTabs / tabCount: MaxTabWidth;

            if (childWidth > MaxTabWidth) childWidth = MaxTabWidth;
            if (childWidth < MinTabWidth) childWidth = MinTabWidth;

            if (tabCount > 0)
            {
                childWidth = Math.Min(MaxTabWidth, availableForTabs / tabCount);
            }

            double x = 0;
            for (int i = 0; i < tabCount; i++)
            {
                InternalChildren[i].Arrange(new Rect(x, 0, childWidth, finalSize.Height));
                x += childWidth;
            }

            addBtn.Arrange(new Rect(x, (finalSize.Height - addBtn.DesiredSize.Height) / 2,
                                        addBtn.DesiredSize.Width, addBtn.DesiredSize.Height));

            return finalSize;
        }

    }
}
