namespace ReactivePipelineEditor.Domain.Common
{
    /// <summary>
    /// Представляет идентификатор конвейера
    /// </summary>
    /// <param name="Value">GUID, представляющий идентификатор конвейера</param>
    public readonly record struct PipelineId(Guid Value)
    {
        /// <summary>
        /// Создает новый уникальный идентификатор конвейера
        /// </summary>
        public static PipelineId New() => new(Guid.NewGuid());

        /// <summary>
        /// Создает идентификатор конвейера из существующего GUID. Если переданный GUID является пустым, выбрасывается исключение ArgumentException.
        /// </summary>
        /// <param name="value">GUID, представляющий идентификатор конвейера</param>
        public static PipelineId From(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("PipelineId не может быть пустым", nameof(value));
            return new(value);
        }

        /// <summary>
        /// Возвращает строковое представление идентификатора конвейера, используя метод ToString() для внутреннего значения GUID
        /// </summary>
        public override string ToString() => Value.ToString();
    }
}
