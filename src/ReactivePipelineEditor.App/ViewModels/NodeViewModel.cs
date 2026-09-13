using CommunityToolkit.Mvvm.ComponentModel;

namespace ReactivePipelineEditor.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    public string Id { get; }
    [ObservableProperty] private string _title;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;

    public NodeViewModel(string id, string title, double x, double y)
    {
        Id = id;
        _title = title;
        _x = x;
        _y = y;
    }
}
