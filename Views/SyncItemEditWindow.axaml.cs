using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using SyncBuddyLib;


namespace SyncBuddy.Views;

public partial class SyncItemEditWindow : Window
{
    private SyncItemExtended _itemCopy;
    private SyncItemExtended _originalItem;
    
    public SyncItemEditWindow()
    { 
        _itemCopy = new SyncItemExtended(0, "D:/TEST/FolderA", "D:/TEST/FolderB");
        if (Design.IsDesignMode)
            DataContext = _itemCopy;
        
        InitializeComponent();

        if (Design.IsDesignMode)
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        
        SetupValidators();
        Validate();
    }
    
    public SyncItemEditWindow(SyncItemExtended itemToEdit)
    {
        _originalItem = itemToEdit;
        _itemCopy = new SyncItemExtended(_originalItem);
        DataContext = _itemCopy;
        
        InitializeComponent();
        SetupValidators();
        Validate();

        DeleteButton.IsVisible = SyncManager.Items.Contains(itemToEdit);
    }

    private void SetupValidators()
    {
        _itemCopy.OnAnyPropertyChanged += (sender, args) =>
        {
            Dispatcher.UIThread.Invoke(Validate);
        };
    }

    private void Validate()
    {
        SaveButton.IsEnabled = IsValid();
    }

    private bool IsValid()
    {
        return Directory.Exists(_itemCopy.SourceDir) && Directory.Exists(_itemCopy.TargetDir) && SyncItem.AreDirectoriesIndependent(_itemCopy.SourceDir, _itemCopy.TargetDir) && _itemCopy.PeriodMinutes > 0;
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _originalItem.SourceDir = _itemCopy.SourceDir;
        _originalItem.TargetDir = _itemCopy.TargetDir;
        _originalItem.PeriodMinutes = _itemCopy.PeriodMinutes;
        _originalItem.Enabled = _itemCopy.Enabled;
        Close();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void ButtonBrowseSource_OnClick(object? sender, RoutedEventArgs e)
    {
        await _itemCopy.BrowseSourceDir();
        SourceDirTextBox.Text = _itemCopy.SourceDir;
        // hack: I do a circle loop to update this again, because
        //       for some reason the property update notification
        //       does not make it through to this text box
    }

    private async void ButtonBrowseTarget_OnClick(object? sender, RoutedEventArgs e)
    {
        await _itemCopy.BrowseTargetDir();
        TargetDirTextBox.Text = _itemCopy.TargetDir;
        // hack: I do a circle loop to update this again, because
        //       for some reason the property update notification
        //       does not make it through to this text box
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SyncManager.Items.Remove(_originalItem);
        Close();
    }
}