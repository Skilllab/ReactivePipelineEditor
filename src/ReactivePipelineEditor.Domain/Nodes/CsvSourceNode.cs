using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Источник данных: читает CSV-файл построчно и эмитит записи
/// в формате RawRecord (сырая, не типизированная запись).
/// </summary>
public sealed class CsvSourceNode : Node
{
    /// <summary>
    /// Имя выходного порта
    /// </summary>
    public static readonly PortName OutPortName = PortName.From("Out");

    /// <summary>
    /// Путь к CSV-файлу
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Размер батча при чтении. Сколько строк читать за одну итерацию
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Кодировка файла. UTF-8 по умолчанию — самая распространённая
    /// и безопасная. Runtime откроет StreamReader с этой кодировкой.
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// Разделитель колонок. Запятая — стандарт CSV. Но CSV бывает
    /// с точкой с запятой (европейский формат), табуляцией (TSV),
    /// вертикальной чертой. Поэтому параметр, а не хардкод.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Есть ли в файле строка заголовка. Если true — первая строка
    /// считается заголовком, поля получают имена. Если false —
    /// поля называются Field0, Field1, ...
    /// </summary>
    public bool HasHeader { get; set; } = true;

    public CsvSourceNode(NodeId id, Point2D position, string filePath)
        : base(id, "Csv Source", position)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Входных портов нет — источник является точкой входа данных.
    /// Возвращаем пустой массив, а не null: вызывающий код не должен
    /// проверять на null и писать foreach с null-check.
    /// </summary>
    public override IReadOnlyList<Port> Inputs => Array.Empty<Port>();

    /// <summary>
    /// Один выходной порт: Out типа RawRecord.
    /// RawRecord — потому что CSV дает сырые строки без типизации.
    /// Типизация — задача MapNode.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => new[]
    {
        new Port(OutPortName, PortDirection.Output, DataType.RawRecord)
    };
}
