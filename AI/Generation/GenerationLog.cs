using System.Text;

namespace TEMO.AI.Ai;

internal sealed class GenerationLog
{
    private readonly object _sync = new();
    private readonly StringBuilder _sb = new();
    private readonly string _projectPath;
    private readonly string _brand;
    private DateTime _start;

    public GenerationLog(string projectPath, string brand)
    {
        _projectPath = projectPath;
        _brand = brand;
        _start = DateTime.Now;
        Section($"เริ่มสร้างเว็บ แบรนด์: {brand}");
        Line($"เวลาเริ่ม: {_start:yyyy-MM-dd HH:mm:ss}");
    }

    public void Section(string title)
    {
        lock (_sync)
        {
            _sb.AppendLine();
            _sb.AppendLine($"========== {title} ==========");
        }
    }

    public void Line(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        lock (_sync) _sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] {text}");
    }

    public void Prompt(string label, string prompt)
    {
        lock (_sync)
        {
            _sb.AppendLine();
            _sb.AppendLine($"--- PROMPT: {label} ---");
            _sb.AppendLine(prompt);
            _sb.AppendLine("--- สิ้นสุด PROMPT ---");
        }
    }

    public void Block(string label, string content)
    {
        lock (_sync)
        {
            _sb.AppendLine();
            _sb.AppendLine($"--- {label} ---");
            _sb.AppendLine(content);
            _sb.AppendLine($"--- สิ้นสุด {label} ---");
        }
    }

    public void Error(string text)
    {
        lock (_sync) _sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: {text}");
    }

    public void Warn(string text)
    {
        lock (_sync) _sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ⚠️ WARN: {text}");
    }

    public void Finish(bool ok, string message)
    {
        var end = DateTime.Now;
        lock (_sync)
        {
            Section("สรุปการทำงาน");
            Line($"ผลลัพธ์: {(ok ? "สำเร็จ" : "ล้มเหลว")}");
            Line($"ข้อความ: {message}");
            Line($"เวลาจบ: {end:yyyy-MM-dd HH:mm:ss}");
            Line($"ใช้เวลาทั้งหมด: {(end - _start):hh\\:mm\\:ss}");
        }
        Save();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_projectPath);
            var path = Path.Combine(_projectPath, $"gen-{_start:yyyyMMdd-HHmmss}.log");
            lock (_sync) File.WriteAllText(path, _sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }
}
