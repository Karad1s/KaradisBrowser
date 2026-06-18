using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;  
namespace Kar
{
    /// <summary>
    /// Кастомная панель для размещения вкладок (в стиле Chrome).
    /// Автоматически масштабирует ширину вкладок в зависимости от свободного пространства в окне.
    /// </summary>
    public class TabPanel: System.Windows.Controls.Panel
    {
        private const double MaxTabWidth = 150;
        private const double MinTabWidth = 40.0;

        /// <summary>
        /// Выполняет предварительный расчет размеров всех дочерних элементов панели.
        /// Вычисляет оптимальную ширину вкладок, чтобы они уместились на экране.
        /// </summary>
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            double maxHeight = 0;

            if (InternalChildren.Count == 0) return new System.Windows.Size(0, 0);

            // Кнопка добавления новой вкладки (всегда последний элемент коллекции)
            UIElement addBtn = InternalChildren[InternalChildren.Count - 1];
            addBtn.Measure(availableSize);
            double addBtnWidth = addBtn.DesiredSize.Width;

            // Вычисляем доступную ширину для размещения самих вкладок
            double availableForTabs = availableSize.Width - addBtnWidth;
            int tabCount = InternalChildren.Count - 1;

            // Распределяем ширину поровну между всеми вкладками
            double childWidth = tabCount > 0 ? availableForTabs / tabCount : MaxTabWidth;

            // Ограничиваем размеры вкладок максимальной и минимальной шириной
            if(childWidth>MaxTabWidth) childWidth = MaxTabWidth;
            if(childWidth<MinTabWidth) childWidth = MinTabWidth;

            for (int i = 0; i < tabCount; i++)
            {
                InternalChildren[i].Measure(new System.Windows.Size(childWidth, availableSize.Height));
                maxHeight = Math.Max(maxHeight, InternalChildren[i].DesiredSize.Height);
            }
            return new System.Windows.Size(availableSize.Width, maxHeight);
        }

        /// <summary>
        /// Позиционирует дочерние элементы панели (вкладки и кнопку "+") в соответствии с рассчитанными размерами.
        /// </summary>
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
            // Размещаем каждую вкладку одну за другой
            for (int i = 0; i < tabCount; i++)
            {
                InternalChildren[i].Arrange(new Rect(x, 0, childWidth, finalSize.Height));
                x += childWidth;
            }

            // Размещаем кнопку добавления новой вкладки после последней вкладки
            addBtn.Arrange(new Rect(x, (finalSize.Height - addBtn.DesiredSize.Height) / 2,
                                        addBtn.DesiredSize.Width, addBtn.DesiredSize.Height));

            return finalSize;
        }

    }
}
