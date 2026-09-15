namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Режим записи в выходной файл.
/// </summary>
public enum WriteMode
{
    /// <summary>
    /// Перезаписать файл. Если файл существует — он будет очищен
    /// перед записью новых данных.
    /// </summary>
    Overwrite,

    /// <summary>
    /// Дописать в конец файла. Если файла нет — создать.
    /// Полезно при повторных запусках pipeline, чтобы не терять
    /// предыдущие результаты.
    /// </summary>
    Append
}
