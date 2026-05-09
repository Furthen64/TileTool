using Avalonia.Controls;
using Avalonia.Input;
using TileTool.ViewModels;

namespace TileTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OwnerWindow = this;
            vm.SelectOutputFolderCommand.Execute(null);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && DataContext is MainWindowViewModel vm)
        {
            vm.SaveTile();
            e.Handled = true;
        }
    }
}
