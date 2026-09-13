using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ReactivePipelineEditor.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<NodeViewModel> Nodes { get; } = new();

        public MainViewModel()
        {
            var nvm = new NodeViewModel("n1", "Csv Source", 200, 150, hasOutput: true);
            nvm.HasOutput = true;
            Nodes.Add(nvm);
        }
    }
}
