namespace Gibbs2b.DtoGenerator;

public class AbstractGenerator
{
    private IList<string> _lines;
    private IEnumerable<string>? _path;
    private string _indent = "";
    public string IndentStep { get; set; } = "    ";

    public void StartFiles(IEnumerable<string> path)
    {
        if (_path != null)
            throw new InvalidOperationException();
        _path = path;
        _lines = new List<string>();
    }

    public void WriteLine()
    {
        _lines.Add("");
    }

    public void WriteLine(string line, bool autoIndent = true)
    {
        if (autoIndent && line.StartsWith('}'))
            DecreaseIndent();

        _lines.Add($"{_indent}{line}");

        if (autoIndent && line.EndsWith('{'))
            IncreaseIndent();
    }

    public void WriteLine(char line, bool autoIndent = true)
    {
        if (autoIndent && line == '}')
            DecreaseIndent();

        _lines.Add($"{_indent}{line}");

        if (autoIndent && line == '{')
            IncreaseIndent();
    }

    public void IncreaseIndent()
    {
        _indent += IndentStep;
    }

    public void DecreaseIndent()
    {
        _indent = _indent[..^IndentStep.Length];
    }

    public void CommitFiles()
    {
        if (_path == null)
            throw new InvalidOperationException();

        foreach (var path in _path)
        {
            using var stream = new StreamWriter(path);

            foreach (var line in _lines)
            {
                stream.WriteLine(line);
            }
        }

        _path = null;
    }
}