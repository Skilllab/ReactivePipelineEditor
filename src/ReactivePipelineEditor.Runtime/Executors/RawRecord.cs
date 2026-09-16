namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Сырая запись из источника: строка CSV, строка JSONL и т.д.
/// Содержит номер строки в исходном файле, массив полей и (опционально) заголовки.
///
/// LineNumber — 1-based, для диагностики: можно сказать пользователю
/// "ошибка в строке 42 файла input.csv".
///
/// Headers — если в файле была строка заголовка. null, если заголовков нет.
/// </summary>
public sealed record RawRecord(int LineNumber, string[] Fields, string[]? Headers)
{
    /// <summary>
    /// Возвращает значение поля по имени (если есть заголовки)
    /// или по индексу (если заголовков нет).
    /// </summary>
    public string? GetField(string nameOrIndex)
    {
        if (Headers is not null)
        {
            var index = Array.IndexOf(Headers, nameOrIndex);
            if (index >= 0 && index < Fields.Length)
                return Fields[index];
        }

        if (int.TryParse(nameOrIndex, out var i) && i >= 0 && i < Fields.Length)
            return Fields[i];

        return null;
    }
}
