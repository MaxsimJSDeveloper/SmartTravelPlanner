using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SmartTravelPlanner.Views;

public partial class MapEditorWindow : Window
{
    public MapEditorWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
