using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SyncBuddy.Views;

public partial class MainWindow : Window
{
    private PixelPoint _cachedPos;
    
    public MainWindow()
    {
        InitializeComponent();
        
        PositionWindow();
        
        if (Design.IsDesignMode)
            Background = Brushes.Black;
    }
    
    private void PositionWindow()
    {
        var workingArea = Screens.Primary.WorkingArea;
        _cachedPos = new PixelPoint(workingArea.Width - (int)Width - 20, workingArea.Height - (int)Height - 50);
        Position = _cachedPos;
    }

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        Activate();
        Focus();
    }

    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        Hide();
    }
}