using System.ComponentModel;

namespace TEMO.AI;

public partial class MainWindow
{
    private const string LayoutKey = "layout:items";
    private const string FaqCountKey = "faq:count";
    private const string CssPrefix = "css:";
    private const string SitePrefix = "site:";
    private const string KwPrefix = "kw:";

    private Dictionary<string, string> _savedSnapshot = new(StringComparer.Ordinal);
    private readonly Stack<Dictionary<string, string>> _undoHistory = new();
    private bool _isSavingFromUndo;
    private bool _suppressSaveTracking;

    private void TakeSavedSnapshot() => _savedSnapshot = CollectEditorState();

    private Dictionary<string, string> CollectEditorState()
    {
        var state = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (id, box) in _boxes)
            state[id] = box.Text;

        foreach (var (id, box) in _cssBoxes)
            state[$"{CssPrefix}{id}"] = box.Text;

        foreach (var (id, box) in _siteBoxes)
            state[$"{SitePrefix}{id}"] = box.Text;

        foreach (var (pageId, boxes) in _kwBoxes)
            for (int i = 0; i < boxes.Count; i++)
                state[$"{KwPrefix}{pageId}:{i}"] = boxes[i].Text;

        state[LayoutKey] = CaptureLayoutState();
        state[FaqCountKey] = _fields.Count(f => f.Id.StartsWith("faq-q-")).ToString();
        return state;
    }

    private HashSet<string> CaptureUnsavedKeysExceptLayout()
    {
        if (!HasOpenProject())
            return new HashSet<string>(StringComparer.Ordinal);

        var current = CollectEditorState();
        return current
            .Where(x => x.Key != LayoutKey
                && (!_savedSnapshot.TryGetValue(x.Key, out var saved) || saved != x.Value))
            .Select(x => x.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void MarkLayoutAutosaveSnapshot(HashSet<string> preserveUnsavedKeys)
    {
        if (!HasOpenProject())
            return;

        var current = CollectEditorState();
        var next = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in current)
        {
            if (preserveUnsavedKeys.Contains(key)
                && _savedSnapshot.TryGetValue(key, out var previousSaved))
            {
                next[key] = previousSaved;
            }
            else
            {
                next[key] = value;
            }
        }

        _savedSnapshot = next;
    }

    private void PushUndoHistory() =>
        _undoHistory.Push(new Dictionary<string, string>(_savedSnapshot, StringComparer.Ordinal));

    private void RestoreEditorState(Dictionary<string, string> state)
    {
        if (state.TryGetValue(FaqCountKey, out var countStr)
            && int.TryParse(countStr, out var targetCount))
        {
            var currentCount = _fields.Count(f => f.Id.StartsWith("faq-q-"));
            if (targetCount != currentCount)
            {
                var snapshot = CaptureAllBoxValues();
                _fields.RemoveAll(f => f.Id.StartsWith("faq-"));
                for (int i = 1; i <= targetCount; i++)
                    AddFaqFields(i);
                BuildContentPanel();
                RestoreBoxValues(snapshot);
            }
        }

        foreach (var (key, value) in state)
        {
            if (key == LayoutKey)
            {
                RestoreLayoutState(value);
            }
            else if (key.StartsWith(CssPrefix))
            {
                if (_cssBoxes.TryGetValue(key[CssPrefix.Length..], out var box)) box.Text = value;
            }
            else if (key.StartsWith(SitePrefix))
            {
                if (_siteBoxes.TryGetValue(key[SitePrefix.Length..], out var box)) box.Text = value;
            }
            else if (key.StartsWith(KwPrefix))
            {
                var parts = key.Split(':', 3);
                if (parts.Length == 3
                    && _kwBoxes.TryGetValue(parts[1], out var kwBoxes)
                    && int.TryParse(parts[2], out var idx)
                    && idx < kwBoxes.Count)
                    kwBoxes[idx].Text = value;
            }
            else if (!key.StartsWith("faq:"))
            {
                if (_boxes.TryGetValue(key, out var box)) box.Text = value;
            }
        }
    }

    private void UpdateUndoButtons()
    {
        var hasHistory = _undoHistory.Count > 0;
        ContentUndoBtn.IsEnabled = hasHistory;
        CssUndoBtn.IsEnabled = hasHistory;
        UpdateSaveAllUi();
    }

    private DispatcherTimer? _saveUiTimer;

    private void WireEditorTracking(TextBox box) =>
        box.TextChanged += (_, _) => { if (!_suppressSaveTracking) ScheduleSaveAllUi(); };

    private void ScheduleSaveAllUi()
    {
        _saveUiTimer ??= CreateSaveUiTimer();
        _saveUiTimer.Stop();
        _saveUiTimer.Start();
    }

    private DispatcherTimer CreateSaveUiTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (_, _) => { timer.Stop(); UpdateSaveAllUi(); };
        return timer;
    }

    private void UpdateSaveAllUi()
    {
        _saveUiTimer?.Stop();
        var canSave = ContentScroll.IsEnabled && HasUnsavedChanges();
        SaveAllBtn.IsEnabled = canSave;
        SaveAllBtn.Style = (Style)FindResource(canSave ? "BtnAccent" : "Btn");
    }

    private void UndoLastSave_Click(object sender, RoutedEventArgs e)
    {
        if (_undoHistory.Count == 0) return;

        var previousState = _undoHistory.Pop();
        _suppressSaveTracking = true;
        RestoreEditorState(previousState);
        _suppressSaveTracking = false;

        _isSavingFromUndo = true;
        try { SaveAll_Click(null!, null!); }
        finally { _isSavingFromUndo = false; }

        var remaining = _undoHistory.Count;
        ShowMsg(remaining > 0
            ? $"↩  Undo สำเร็จ — ย้อนได้อีก {remaining} ครั้ง"
            : "↩  Undo สำเร็จ — ไม่มี save ให้ย้อนอีกแล้ว");
    }

    private bool HasUnsavedChanges()
    {
        if (!HasOpenProject()) return false;

        var current = CollectEditorState();
        if (current.Count != _savedSnapshot.Count) return true;

        foreach (var (key, value) in current)
            if (!_savedSnapshot.TryGetValue(key, out var saved) || saved != value)
                return true;

        return false;
    }

    private bool ConfirmUnsavedChanges()
    {
        if (!HasOpenProject() || !HasUnsavedChanges()) return true;

        SaveAll_Click(this, new RoutedEventArgs());

        return true;

    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
            e.Cancel = true;
    }
}
