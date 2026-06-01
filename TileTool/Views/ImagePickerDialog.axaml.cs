using Avalonia.Controls;
using Avalonia.Input;
using TileTool.ViewModels;

namespace TileTool.Views;

public partial class ImagePickerDialog : Window
{
    public ImagePickerDialog()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var listBox = this.FindControl<ListBox>("ItemsList");
        if (listBox != null)
            listBox.DoubleTapped += OnItemDoubleTapped;
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ImagePickerViewModel vm && vm.SelectedItem != null)
            vm.OpenItemCommand.Execute(vm.SelectedItem);
    }

    public static async System.Threading.Tasks.Task<string?> ShowAsync(Window owner, string initialFolder)
    {
        var vm = new ImagePickerViewModel(initialFolder);
        var dialog = new ImagePickerDialog
        {
            DataContext = vm
        };

        var tcs = new System.Threading.Tasks.TaskCompletionSource<string?>();
        vm.CloseRequested += accepted =>
        {
            tcs.TrySetResult(accepted ? vm.ResultPath : null);
            dialog.Close();
        };

        await dialog.ShowDialog(owner);

        // If the window was closed without going through CloseRequested (e.g. Alt+F4)
        tcs.TrySetResult(null);
        return await tcs.Task;
    }
}

