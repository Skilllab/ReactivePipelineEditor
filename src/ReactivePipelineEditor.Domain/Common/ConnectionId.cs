namespace ReactivePipelineEditor.Domain.Common
{
    /// <summary>
    /// Представляет идентификатор соединения между узлами в конвейере
    /// </summary>
    /// <param name="Value">GUID, представляющий идентификатор соединения</param>
    public readonly record struct ConnectionId(Guid Value)
    {
        /// <summary>
        /// Создает новый уникальный идентификатор соединения
        /// </summary>
        public static ConnectionId New() => new(Guid.NewGuid());

        /// <summary>
        /// Создает идентификатор соединения из существующего GUID. Если переданный GUID является пустым, выбрасывается исключение ArgumentException.
        /// </summary>
        /// <param name="value">GUID, представляющий идентификатор соединения</param>
        public static ConnectionId From(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ConnectionId не может быть пустым", nameof(value));
            return new(value);
        }

        /// <summary>
        /// Возвращает строковое представление идентификатора соединения, используя метод ToString() для внутреннего значения GUID.
        /// </summary>
        public override string ToString() => Value.ToString();
    }
}
