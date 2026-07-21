using System.ComponentModel;

namespace TEMO.AI.Ai;

internal sealed class GenQueueItem : INotifyPropertyChanged
{
    private string _status = "รอคิว";
    private string _message = "";
    private string? _projectPath;
    private int _stageIndex;
    private TimeSpan? _duration;
    private double? _costThb;

    public event PropertyChangedEventHandler? PropertyChanged;

    public GenQueueItem(GenerationOptions options)
    {
        Options = options;
    }

    public GenerationOptions Options { get; }

    public string Brand => Options.Brand;

    public string ContentTypeText => Options.ContentType switch
    {
        AiPromptType.Lottery => "หวย",
        AiPromptType.Slot => "สล็อต",
        _ => "คาสิโนออนไลน์",
    };

    public string Summary => $"{Brand}  ประเภทเนื้อหา{ContentTypeText}";

    public string Meta => ContentTypeText;

    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnChanged(nameof(Status)); OnChanged(nameof(Detail)); } }
    }

    public string Message
    {
        get => _message;
        set { if (_message != value) { _message = value; OnChanged(nameof(Message)); } }
    }

    public string? ProjectPath
    {
        get => _projectPath;
        set { if (_projectPath != value) { _projectPath = value; OnChanged(nameof(ProjectPath)); } }
    }

    public int StageIndex
    {
        get => _stageIndex;
        set { if (_stageIndex != value) { _stageIndex = value; OnChanged(nameof(StageIndex)); } }
    }

    public TimeSpan? Duration
    {
        get => _duration;
        set { if (_duration != value) { _duration = value; OnChanged(nameof(Duration)); OnChanged(nameof(Detail)); OnChanged(nameof(HasDetail)); OnChanged(nameof(DurationText)); } }
    }

    public double? CostThb
    {
        get => _costThb;
        set { if (_costThb != value) { _costThb = value; OnChanged(nameof(CostThb)); OnChanged(nameof(Detail)); OnChanged(nameof(HasDetail)); OnChanged(nameof(CostText)); } }
    }

    public string Detail
    {
        get
        {
            if (_duration is null && _costThb is null) return "";
            var dur = _duration is { } d ? FormatDuration(d) : "—";
            var cost = _costThb is { } c ? $"{c:N2}฿" : "—";
            return $"ใช้เวลา {dur} • ต้นทุน {cost}";
        }
    }

    public bool HasDetail => _duration is not null || _costThb is not null;

    public string DurationText => _duration is { } d ? FormatDuration(d) : "xx นาที xx วินาที";

    public string CostText => _costThb is { } c ? $"{c:N2}฿" : "xx.xx฿";

    private static string FormatDuration(TimeSpan d)
    {
        var total = (int)d.TotalSeconds;
        if (total < 60) return $"{total} วินาที";
        return $"{total / 60} นาที {total % 60} วินาที";
    }

    private void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

