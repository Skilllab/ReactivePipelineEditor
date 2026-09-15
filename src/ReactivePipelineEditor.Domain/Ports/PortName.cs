namespace ReactivePipelineEditor.Domain.Ports;

/// <summary>
/// Имя порта   
/// </summary>
public readonly record struct PortName
{
    public string Value { get; }

    private PortName(string value) => Value = value;

    /// <summary>
    /// Создаёт экземпляр PortName из заданной строки.
    /// </summary>
    /// <param name="value">Имя порта; не может быть пустым или состоять только из пробелов, будет обрезано по краям, максимум 64 символа, должно соответствовать шаблону [A-Za-z][A-Za-z0-9_]*.</param>
    public static PortName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Имя порта не может быть пустым", nameof(value));

        value = value.Trim();

        if (value.Length > 64)
            throw new ArgumentException("Имя порта слишком длинное (максимум 64 символа)", nameof(value));

        if (!IsValidIdentifier(value))
            throw new ArgumentException(
                $"Имя порта '{value}' должно соответствовать шаблону [A-Za-z][A-Za-z0-9_]*", nameof(value));

        return new PortName(value);
    }

    private static bool IsValidIdentifier(string s)
    {
        if (!char.IsLetter(s[0])) return false;
        for (var i = 1; i < s.Length; i++)
            if (!char.IsLetterOrDigit(s[i]) && s[i] != '_')
                return false;
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(PortName name) => name.Value;
}
