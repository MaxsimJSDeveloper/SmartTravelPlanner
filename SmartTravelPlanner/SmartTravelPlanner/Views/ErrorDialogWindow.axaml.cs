using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SmartTravelPlanner.Views;

public partial class ErrorDialogWindow : Window
{
    public ErrorDialogWindow()
    {
        InitializeComponent();
    }

    public ErrorDialogWindow(string message, string title = "Error")
        : this()
    {
        Title = title;
        this.FindControl<TextBlock>("MessageTextBlock")!.Text = message;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
