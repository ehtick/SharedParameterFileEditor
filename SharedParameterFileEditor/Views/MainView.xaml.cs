using Ookii.Dialogs.Wpf;
using SharedParametersFile;
using SharedParametersFile.Models;
using Syncfusion.UI.Xaml.Grid;
using System.Diagnostics;
using System.IO;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SharedParameterFileEditor.Views;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainView : Window
{
    private readonly ViewModels.MainViewModel _viewModel;

    public MainView()
    {
        InitializeComponent();

        _viewModel = (ViewModels.MainViewModel)this.DataContext;
        _viewModel.ClosingRequest += (sender, e) => this.Close();
    }

    private void menuItemAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutView = new AboutView()
        {
            Owner = this
        };
        aboutView.ShowDialog();
    }

    private void menuItemOpen_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaOpenFileDialog()
        {
            Filter = "Shared Parameter Definition File (*.txt)|*.txt",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Open Shared Parameter Definition File"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.FileInfo = new FileInfo(dialog.FileName);
            _viewModel.LoadDefinitionFile();

            this.menuItemSaveAs.IsEnabled = true;
        }
    }

    private void menuItemSaveAs_Click(object sender, RoutedEventArgs e)
    {
        SaveAs();
    }

    private void SaveAs()
    {
        var dialog = new VistaSaveFileDialog()
        {
            Filter = "Shared Parameter Definition File (*.txt)|*.txt",
            CheckFileExists = true,
            Title = "Save Shared Parameter Definition File",
        };

        if (dialog.ShowDialog() == true)
        {
            if(dialog.FileName.EndsWith(".txt") == false)
            {
                dialog.FileName += ".txt";
            }   

            _viewModel.NewFileName = dialog.FileName;

            _viewModel.SaveDefinitionFile();
        }
    }

    private void menuItemExit_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.UnsavedChanges)
        {
            var saveButton = new TaskDialogButton("&Save");
            var dontSaveButton = new TaskDialogButton("Do&n't save");
            var cancelButton = new TaskDialogButton(ButtonType.Cancel);

            var taskDialog = new TaskDialog()
            {
                WindowTitle = $"{_viewModel.WindowTitle} closing",
                MainInstruction = $"Do you want to save changes to {_viewModel.FileInfo.Name}?",
                ButtonStyle = Ookii.Dialogs.Wpf.TaskDialogButtonStyle.CommandLinks,
                Buttons = { saveButton, dontSaveButton, cancelButton }
            };

            var button = taskDialog.ShowDialog(this);

            if (button == saveButton)
            {
                if(_viewModel.Writable == true)
                {
                    _viewModel.SaveDefinitionFileCommand.Execute(null);
                }

                if(_viewModel.Writable == false)
                {
                    SaveAs();
                }
                
            }
        }

        this.Close();
    }

    private void menuItemMerge_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaOpenFileDialog()
        {
            Filter = "Shared Parameter Definition File (*.txt)|*.txt",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Open Shared Parameter Definition File"
        };

        if (dialog.ShowDialog() == true)
        {
            if (dialog.CheckFileExists)
            {

                var mergeSourceFile = new SharedParametersDefinitionFile(dialog.FileName);
                mergeSourceFile.LoadFile();

                var mergeParametersView = new MergeParametersView( _viewModel.DefFile.definitionFileModel, 
                    mergeSourceFile.definitionFileModel)
                {
                    Owner = this
                };
                mergeParametersView.ShowDialog();
            }
        }
    }

    private void MenuItem_MRU_Click(object sender, RoutedEventArgs e)
    {
        //get the header value from the clicked submenu item
        string filePath = ((System.Windows.Controls.MenuItem)e.OriginalSource).Header.ToString();

        OpenFile(filePath);

    }

    private void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            _viewModel.FileInfo = new FileInfo(filePath);
            _viewModel.LoadDefinitionFile();
        }
        else
        {
            MessageBox.Show("The file does not exist.  It may have been moved or deleted.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SfDataGridGroups_CurrentCellValidated(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellValidatedEventArgs e)
    {
        EnableSaveMenu(e);
    }

    private void SfDataGridParameters_CurrentCellValidated(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellValidatedEventArgs e)
    {
        EnableSaveMenu(e);
    }

    private void EnableSaveMenu(CurrentCellValidatedEventArgs e)
    {
        if (e.NewValue != e.OldValue)
        {
            this.menuItemSaveAs.IsEnabled = true;

            _viewModel.UnsavedChanges = true;
        }
    }

    private void sfDataGridGroups_RecordDeleting(object sender, RecordDeletingEventArgs e)
    {
        var group = e.Items.First() as GroupModel;

        var count = _viewModel.DefFile.definitionFileModel.Parameters.Count(x => x.Group == group.ID);

        if (count > 0)
        {
            e.Cancel = true;
        }
    }
}
