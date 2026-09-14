namespace ReactivePipelineEditor.Domain.Common
{
    /// <summary>
    /// Представляет идентификатор соединения
    /// </summary>
    public readonly record struct ConnectionId
    {
        /// <summary>
        /// GUID, представляющий идентификатор соединения
        /// </summary>
        public Guid Value { get; }

        private ConnectionId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ConnectionId не может быть пустым", nameof(value));
            Value = value;
        }

        /// <summary>
        /// Создает новый уникальный идентификатор соединения
        /// </summary>        
        public static ConnectionId New() => new(Guid.NewGuid());

        /// <summary>
        /// Создает идентификатор соединения из существующего GUID
        /// </summary>
        /// <param name="value">GUID, представляющий идентификатор соединения; не может быть пустым</param>
        public static ConnectionId From(Guid value) => new(value);

        /// <summary>
        /// Возвращает строковое представление идентификатора соединения
        /// </summary>
        public override string ToString() => Value.ToString();
    }
}
