namespace TEMO.AI;

public partial class MainWindow
{
    private string _bannerLine = "  <Banner />";

    private const bool DevLayoutMode = true;

    public static Visibility DevEditVisibility =>
        DevLayoutMode ? Visibility.Visible : Visibility.Collapsed;

    private void ApplyDevLayoutMode()
    {
        ShellSectionWrap.Visibility = DevLayoutMode ? Visibility.Visible : Visibility.Collapsed;
        AddSectionBtn.Visibility = DevLayoutMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadLayoutComponents()
    {
        _selectedIndexForSwap = -1;
        _layoutComponents.Clear();
        LayoutList.Items.Clear();

        LoadShellComponents();

        if (LayoutStore.ReadIndex(_projectPath) is not { } content)
        {
            ShowMsg("⚠️ index.astro not found");
            return;
        }

        _bannerLine = LayoutStore.ParseBannerLine(content);

        foreach (var name in LayoutStore.ParseComponentNames(content))
        {
            if (SectionCatalog.FindByComponentName(name) is not { } definition)
                continue;

            var component = SectionCatalog.ToLayoutComponent(definition);
            _layoutComponents.Add(component);
            LayoutList.Items.Add(component);
        }

        UpdateAddSectionButton();
    }

    private void LoadShellComponents()
    {
        ShellList.Items.Clear();
        if (!HasOpenProject()) return;

        foreach (var slot in ShellSlot.All)
        {
            var variants = SectionCatalog.All.Where(d => d.Slot == slot).ToList();
            if (variants.Count == 0) continue;

            var current = ComponentStore.CurrentForSlot(_projectPath, slot);
            var comp = current is not null
                ? SectionCatalog.ToLayoutComponent(current)
                : new LayoutComponent
                {
                    Slot = slot,
                    Kind = variants[0].Kind,
                    DisplayName = SectionCatalog.DisplayName(variants[0].Kind),
                    Variant = "ไม่ทราบแบบ",
                };

            ShellList.Items.Add(comp);
        }
    }

    private void ChangeShellVariant_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Border { DataContext: LayoutComponent comp }) return;
        if (!HasOpenProject())
        {
            ShowMsg("เปิดโปรเจคก่อนจึงจะเปลี่ยนแบบได้");
            return;
        }

        SectionCatalog.Reload();
        var options = SectionCatalog.All.Where(d => d.Slot == comp.Slot).ToList();
        if (options.Count == 0)
        {
            ShowMsg("ยังไม่มี variant สำหรับส่วนนี้");
            return;
        }

        var picker = new SectionPickerDialog(options, $"เปลี่ยนแบบ {comp.DisplayName}") { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedSection is not { } selected) return;

        ComponentStore.CopyToProject(_projectPath, selected);
        LoadShellComponents();

        if (_devProcess is { HasExited: false })
            WebView.CoreWebView2?.Reload();

        ShowMsg($"เปลี่ยนเป็น {selected.DisplayName} แล้ว");
    }

    private void AddLayoutSection_Click(object sender, RoutedEventArgs e)
    {
        SectionCatalog.Reload();

        var existingKinds = _layoutComponents
            .Select(x => x.Kind)
            .ToHashSet(StringComparer.Ordinal);

        var options = SectionCatalog.All
            .Where(x => x.Slot == "body" && !existingKinds.Contains(x.Kind))
            .ToList();

        if (options.Count == 0)
        {
            ShowMsg("เพิ่มครบทุกชนิด section แล้ว");
            return;
        }

        var picker = new SectionPickerDialog(options, "เพิ่ม Section", chooseKindFirst: true) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedSection is not { } selected) return;

        if (_layoutComponents.Any(x => x.Kind.Equals(selected.Kind, StringComparison.Ordinal)))
        {
            ShowMsg($"มี {selected.DisplayName} ชนิดนี้อยู่แล้ว เพิ่มซ้ำไม่ได้");
            return;
        }

        _layoutComponents.Add(SectionCatalog.ToLayoutComponent(selected));
        MarkLayoutChanged($"เพิ่ม {selected.DisplayName} แล้ว", selected.Kind);
    }

    private void ChangeLayoutSection_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Border { DataContext: LayoutComponent component }) return;
        if (!component.CanChangeVariant)
        {
            ShowMsg("Section นี้ยังเปลี่ยนแบบไม่ได้");
            return;
        }

        var options = SectionCatalog.ForKind(component.Kind).ToList();
        if (options.Count == 0)
        {
            ShowMsg("ยังไม่มี variant สำหรับ section นี้");
            return;
        }

        var picker = new SectionPickerDialog(options, $"เปลี่ยนแบบ {component.DisplayName}") { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedSection is not { } selected) return;

        var index = _layoutComponents.IndexOf(component);
        if (index < 0) return;

        _layoutComponents[index] = SectionCatalog.ToLayoutComponent(selected);
        if (HasOpenProject())
            SwapVariantPreservingText(selected);
        MarkLayoutChanged($"เปลี่ยนเป็น {selected.DisplayName} แล้ว", selected.Kind);
    }

    private void DeleteLayoutSection_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Border { DataContext: LayoutComponent component }) return;
        if (!component.CanRemove)
        {
            ShowMsg("Section นี้ลบไม่ได้");
            return;
        }

        _layoutComponents.Remove(component);
        MarkLayoutChanged($"ลบ {component.DisplayName} แล้ว");
    }

    private void SwapIcon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (sender is not Border { DataContext: LayoutComponent clickedComp }) return;

        var currentIndex = _layoutComponents.IndexOf(clickedComp);
        if (currentIndex < 0) return;

        if (_selectedIndexForSwap < 0)
        {
            _selectedIndexForSwap = currentIndex;
            UpdateItemTag(currentIndex, "Selected");
            ShowMsg($"🔄 เลือกแล้ว: {clickedComp.DisplayName} — คลิกที่ ⇄ ของรายการอื่นเพื่อสลับ");
            return;
        }

        if (_selectedIndexForSwap == currentIndex)
        {
            UpdateItemTag(_selectedIndexForSwap, null);
            _selectedIndexForSwap = -1;
            ShowMsg("ยกเลิกการเลือก");
            return;
        }

        var sourceComp = _layoutComponents[_selectedIndexForSwap];
        var targetComp = _layoutComponents[currentIndex];
        _layoutComponents[_selectedIndexForSwap] = targetComp;
        _layoutComponents[currentIndex] = sourceComp;

        RefreshLayoutList();
        _selectedIndexForSwap = -1;

        MarkLayoutChanged($"สลับแล้ว: {sourceComp.DisplayName} ⇄ {targetComp.DisplayName}", rebuildContent: false);
    }

    private void UpdateItemTag(int index, object? tag)
    {
        if (index < 0 || index >= LayoutList.Items.Count) return;
        if (LayoutList.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem container)
            container.Tag = tag;
    }

    private void RefreshLayoutList()
    {
        LayoutList.Items.Clear();
        foreach (var comp in _layoutComponents)
            LayoutList.Items.Add(comp);
        UpdateAddSectionButton();
    }

    private void UpdateAddSectionButton()
    {
        var activeKinds = _layoutComponents
            .Select(x => x.Kind)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var canAdd = SectionCatalog.All.Any(x => x.Slot == "body" && !activeKinds.Contains(x.Kind));
        AddSectionBtn.IsEnabled = canAdd;
        AddSectionBtn.ToolTip = canAdd ? "เพิ่ม section" : "เพิ่มครบทุกชนิด section แล้ว";
    }

    private void MarkLayoutChanged(string message, string? refreshKind = null, bool rebuildContent = true)
    {
        using var session = Io.Session();
        var unsavedBeforeLayoutSave = CaptureUnsavedKeysExceptLayout();
        if (SectionCatalog.ContentLabel(refreshKind) is { } refreshLabel)
            unsavedBeforeLayoutSave.ExceptWith(_fields
                .Where(f => string.Equals(f.Section, refreshLabel, StringComparison.Ordinal))
                .Select(f => f.Id));
        _selectedIndexForSwap = -1;
        RefreshLayoutList();
        if (HasOpenProject())
            SaveLayoutToFile(showMessage: false);
        if (rebuildContent)
        {
            RebuildContentForLayout(refreshKind);
            if (HasOpenProject())
            {
                ImagesStore.SyncStandard(_projectPath, src => Io.DeleteFile(PublicPath(src)));
                BuildImagesPanel();
                PullImages();
            }
        }
        MarkLayoutAutosaveSnapshot(unsavedBeforeLayoutSave);
        if (!_suppressSaveTracking)
            UpdateSaveAllUi();
        ShowMsg(message);
    }

    private void SaveLayoutToFile(bool showMessage = true)
    {
        if (LayoutStore.ReadIndex(_projectPath) is not { } content)
        {
            ShowMsg("⚠️ Cannot save: index.astro not found");
            return;
        }

        var active = ProjectComponentSync.EnsureComponents(_projectPath, _layoutComponents);

        if (LayoutStore.BuildIndex(content, _bannerLine, _layoutComponents) is not { } built)
        {
            ShowMsg("⚠️ Cannot parse BaseLayout structure");
            return;
        }

        LayoutStore.WriteIndex(_projectPath, built);

        if (active is not null)
            ProjectComponentSync.RemoveOrphans(_projectPath, active);
        if (showMessage)
            ShowMsg($"Layout saved · {_layoutComponents.Count + 1} components");
    }

    private string CaptureLayoutState() =>
        string.Join("\n", _layoutComponents.Select(x => string.Join("|",
            EscapeLayoutToken(x.Name),
            EscapeLayoutToken(x.DisplayName),
            EscapeLayoutToken(x.Kind),
            EscapeLayoutToken(x.Variant),
            EscapeLayoutToken(x.ImportPath),
            EscapeLayoutToken(x.CssImportPath),
            EscapeLayoutToken(x.StoreDirectory),
            EscapeLayoutToken(x.AstroFile),
            EscapeLayoutToken(x.CssFile),
            x.HasExternalLink ? "1" : "0",
            x.CanRemove ? "1" : "0",
            x.CanChangeVariant ? "1" : "0")));

    private void RestoreLayoutState(string state)
    {
        _layoutComponents.Clear();

        foreach (var line in state.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = line.Split('|');
            if (p.Length < 12) continue;

            _layoutComponents.Add(new LayoutComponent
            {
                Name = UnescapeLayoutToken(p[0]),
                DisplayName = UnescapeLayoutToken(p[1]),
                Kind = UnescapeLayoutToken(p[2]),
                Variant = UnescapeLayoutToken(p[3]),
                ImportPath = UnescapeLayoutToken(p[4]),
                CssImportPath = UnescapeLayoutToken(p[5]),
                StoreDirectory = UnescapeLayoutToken(p[6]),
                AstroFile = UnescapeLayoutToken(p[7]),
                CssFile = UnescapeLayoutToken(p[8]),
                HasExternalLink = p[9] == "1",
                CanRemove = p[10] == "1",
                CanChangeVariant = p[11] == "1",
            });
        }

        RefreshLayoutList();
    }

    private static string EscapeLayoutToken(string value) =>
        value.Replace("%", "%25").Replace("|", "%7C").Replace("\n", "%0A").Replace("\r", "%0D");

    private static string UnescapeLayoutToken(string value) =>
        value.Replace("%0D", "\r").Replace("%0A", "\n").Replace("%7C", "|").Replace("%25", "%");
}
