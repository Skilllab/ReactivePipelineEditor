namespace ReactivePipelineEditor.Domain.Common
{
    /// <summary>
    /// Представляет идентификатор узла в конвейере
    /// </summary>
    /// <param name="Value">GUID, представляющий идентификатор узла</param>
    public readonly record struct NodeId(Guid Value)
    {
        /// <summary>
        /// Создает новый уникальный идентификатор узла
        /// </summary>
        public static NodeId New() => new(Guid.NewGuid());

        /// <summary>
        /// Создает идентификатор узла из существующего GUID. Если переданный GUID является пустым, выбрасывается исключение ArgumentException.
        /// </summary>
        /// <param name="value">GUID, представляющий идентификатор узла</param>
        public static NodeId From(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("NodeId не может быть пустым", nameof(value));
            return new(value);
        }

        /// <summary>
        /// Возвращает строковое представление идентификатора узла, используя метод ToString() для внутреннего значения GUID
        /// </summary>
        public override string ToString() => Value.ToString();
    }
}
