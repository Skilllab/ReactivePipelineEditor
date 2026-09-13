using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ReactivePipelineEditor.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<NodeViewModel> Nodes { get; } = new();

        public MainViewModel()
        {
            Nodes.Add(new NodeViewModel("n1", "Csv Source", 200, 150));
        }
    }
}
