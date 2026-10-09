using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SmartTravelPlanner.Views;

public partial class PlannerView : UserControl
{
    public PlannerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
