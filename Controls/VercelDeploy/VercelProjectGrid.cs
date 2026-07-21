using Binding = System.Windows.Data.Binding;
using Control = System.Windows.Controls.Control;
using DataGridColumnHeader = System.Windows.Controls.Primitives.DataGridColumnHeader;

namespace TEMO.AI;

internal static class VercelProjectGrid
{
    private const uint HoverBg = 0x1E1E1E;
    private const uint SelectedBg = 0x2C2C2C;

    public static DataGrid Create(Action<VercelProjectOption>? onManageDomains = null)
    {
        var menu = onManageDomains is not null ? BuildRowContextMenu(onManageDomains) : null;
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = Ui.Brush(0x1F1F1F),
            Background = Brushes.Transparent,
            Foreground = Ui.Brush(0xEDEDED),
            BorderThickness = new Thickness(0),
            ClipToBounds = true,
            RowBackground = Brushes.Transparent,
            AlternatingRowBackground = Brushes.Transparent,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            CanUserResizeRows = false,
            CanUserReorderColumns = false,
            CanUserResizeColumns = false,
            EnableRowVirtualization = true,
            RowHeight = 46,
            ColumnHeaderHeight = 40,
            MinHeight = 360,
            MaxHeight = 460,
            ColumnHeaderStyle = HeaderStyle(),
            CellStyle = CellStyle(),
            RowStyle = RowStyle(menu),
        };

        dg.Columns.Add(new DataGridTemplateColumn
        {
            Header = "ชื่อโปรเจค",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CanUserResize = false,
            CellStyle = LeftCellStyle(),
            CellTemplate = CellTemplate(nameof(VercelProjectOption.DisplayName), 0xDEDEDE, 18, FontWeights.SemiBold,
                System.Windows.HorizontalAlignment.Stretch, TextAlignment.Left, new Thickness(18, 0, 10, 0)),
        });
        dg.Columns.Add(new DataGridTemplateColumn
        {
            Header = "โดเมน",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CanUserResize = false,
            CellTemplate = DomainLinkTemplate(),
        });

        return dg;
    }

    private static Style HeaderStyle()
    {
        var s = new Style(typeof(DataGridColumnHeader));
        s.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        s.Setters.Add(new Setter(Control.ForegroundProperty, Ui.Brush(0x888888)));
        s.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
        s.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10)));
        s.Setters.Add(new Setter(Control.BorderBrushProperty, Ui.Brush(0x282828)));
        s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
        s.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
        return s;
    }

    private static Style LeftCellStyle()
    {
        var s = CellStyle();
        s.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Stretch));
        return s;
    }

    private static Style CellStyle()
    {
        var s = new Style(typeof(DataGridCell));
        s.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent));
        s.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
        s.Setters.Add(new Setter(DataGridCell.FocusVisualStyleProperty, null));
        s.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(8, 0, 8, 0)));
        s.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
        s.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));

        var selected = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Ui.Brush(SelectedBg)));
        selected.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.White));
        selected.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
        s.Triggers.Add(selected);
        return s;
    }

    private static Style RowStyle(ContextMenu? menu)
    {
        var s = new Style(typeof(DataGridRow));
        s.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Transparent));
        s.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Ui.Brush(0xEDEDED)));
        s.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0)));
        s.Setters.Add(new Setter(DataGridRow.CursorProperty, Cursors.Hand));

        if (menu is not null)
        {
            s.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, menu));
            var isNew = new DataTrigger
            {
                Binding = new Binding(nameof(VercelProjectOption.IsNew)),
                Value = true,
            };
            isNew.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, null));
            s.Triggers.Add(isNew);
        }

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Ui.Brush(HoverBg)));
        var selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Ui.Brush(SelectedBg)));
        selected.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
        s.Triggers.Add(hover);
        s.Triggers.Add(selected);
        return s;
    }

    private static ContextMenu BuildRowContextMenu(Action<VercelProjectOption> onManageDomains)
    {
        var menu = new ContextMenu
        {
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6),
            Foreground = Ui.Brush(0xEDEDED),
            Template = RoundedContextMenuTemplate(),
        };

        var item = new MenuItem
        {
            Header = "จัดการโดเมน",
            Style = RoundedMenuItemStyle(),
            Foreground = Ui.Brush(0xEDEDED),
            FontSize = 13,
            Padding = new Thickness(16, 8, 16, 8),
            Background = Ui.Brush(0x202020),
            BorderBrush = Ui.Brush(0x3A3A3A),
            BorderThickness = new Thickness(1),
        };
        item.Click += (s, _) =>
        {
            if (s is MenuItem { DataContext: VercelProjectOption opt } && !opt.IsNew)
                onManageDomains(opt);
        };
        menu.Items.Add(item);
        return menu;
    }

    private static ControlTemplate RoundedContextMenuTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var items = new FrameworkElementFactory(typeof(ItemsPresenter));
        border.AppendChild(items);
        return new ControlTemplate(typeof(ContextMenu)) { VisualTree = border };
    }

    private static Style RoundedMenuItemStyle()
    {
        var border = new FrameworkElementFactory(typeof(Border)) { Name = "bd" };
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(content);

        var template = new ControlTemplate(typeof(MenuItem)) { VisualTree = border };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, Ui.Brush(0x2B2B2B), "bd"));
        hover.Setters.Add(new Setter(Border.BorderBrushProperty, Ui.Brush(0x5A5A5A), "bd"));
        template.Triggers.Add(hover);

        var style = new Style(typeof(MenuItem));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
        return style;
    }

    private static DataTemplate DomainLinkTemplate()
    {
        var text = new FrameworkElementFactory(typeof(TextBlock)) { Name = "txt" };
        text.SetBinding(TextBlock.TextProperty, new Binding(nameof(VercelProjectOption.Domain)));
        text.SetValue(TextBlock.ForegroundProperty, Ui.Brush(0x888888));
        text.SetValue(TextBlock.FontSizeProperty, 14.0);
        text.SetValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        text.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

        var link = new FrameworkElementFactory(typeof(Border)) { Name = "link" };
        link.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        link.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        link.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        link.SetValue(Border.PaddingProperty, new Thickness(0));
        link.AppendChild(text);
        link.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler((s, _) =>
        {
            if (s is not Border bd || bd.DataContext is not VercelProjectOption { Url: { Length: > 0 } url })
                return;
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }));

        var root = new FrameworkElementFactory(typeof(Grid));
        root.AppendChild(link);

        var dt = new DataTemplate { VisualTree = root };

        var trigger = new DataTrigger
        {
            Binding = new Binding(nameof(VercelProjectOption.HasUrl)),
            Value = true,
        };
        trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Ui.Brush(0x5599FF), "txt"));
        trigger.Setters.Add(new Setter(TextBlock.TextDecorationsProperty, TextDecorations.Underline, "txt"));
        trigger.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand, "link"));
        dt.Triggers.Add(trigger);

        return dt;
    }

    private static DataTemplate CellTemplate(
        string property,
        uint color,
        double fontSize,
        FontWeight fontWeight,
        System.Windows.HorizontalAlignment horizontalAlignment = System.Windows.HorizontalAlignment.Center,
        TextAlignment textAlignment = TextAlignment.Center,
        Thickness? padding = null)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(property));
        text.SetValue(TextBlock.ForegroundProperty, Ui.Brush(color));
        text.SetValue(TextBlock.FontSizeProperty, fontSize);
        text.SetValue(TextBlock.FontWeightProperty, fontWeight);
        text.SetValue(FrameworkElement.HorizontalAlignmentProperty, horizontalAlignment);
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        text.SetValue(TextBlock.TextAlignmentProperty, textAlignment);
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

        if (padding is null)
            return new DataTemplate { VisualTree = text };

        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);
        border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        border.SetValue(Border.PaddingProperty, padding);
        border.AppendChild(text);
        return new DataTemplate { VisualTree = border };
    }
}
