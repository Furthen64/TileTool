using Avalonia.Controls;
using Avalonia.Input;
using System;
using TileTool.ViewModels;

namespace TileTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OwnerWindow = this;
            vm.SelectOutputFolderCommand.Execute(null);
        }
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && DataContext is MainWindowViewModel vm)
        {
            await vm.SaveTileAsync();
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Dispose();

        base.OnClosed(e);
    }
}
