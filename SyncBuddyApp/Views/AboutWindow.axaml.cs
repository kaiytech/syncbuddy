using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SyncBuddyApp.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/kaiytech", UseShellExecute = true });
    }
}