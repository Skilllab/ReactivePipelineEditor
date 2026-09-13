namespace ReactivePipelineEditor.Domain;

public sealed class Node
{
    public string Id { get; }
    public string Title { get; }

    public Node(string id, string title)
    {
        Id = id;
        Title = title;
    }
}
