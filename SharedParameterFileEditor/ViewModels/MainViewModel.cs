using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using Serilog.Context;
using SharedParameterFileEditor.Messages;
using SharedParameterFileEditor.Models;
using SharedParameterFileEditor.Views;
using SharedParametersFile;
using SharedParametersFile.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharedParameterFileEditor.ViewModels;
internal partial class MainViewModel : BaseViewModel
{
	public string WindowTitle { get; private set; }

    public bool SaveEnabled => Writable & UnsavedChanges;

    [ObservableProperty]
    private System.Windows.Visibility _groupsVisible = System.Windows.Visibility.Visible;

    [ObservableProperty]
    private string _toggleGroupMenuText = "Hide groups";

    [ObservableProperty]
    private FileInfo _fileInfo;

    [ObservableProperty]
    private SharedParametersDefinitionFile _defFile;

    [ObservableProperty]
    private string _newFileName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveEnabled))]
    private bool _writable = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveEnabled))]
    private bool _unsavedChanges = false;

    [ObservableProperty]
    private bool _mergeEnabled = false;

    [ObservableProperty]
    private bool _editGuid = false;

    private SharedParametersDefinitionFile _mergeSourceFile;

    [ObservableProperty]
    private List<ParameterType> _types = Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>().ToList();

    [ObservableProperty]
    private List<string> _mostRecentlyUsedFiles;

    public MainViewModel()
	{
        var informationVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        WindowTitle = $"Shared Parameter View Editor {informationVersion}";

        MostRecentlyUsedFiles = GetMostRecentlyUsedFiles();

        //WeakReferenceMessenger.Default.Register<ListOfParametersMessage>(this, (r, m) =>
        //{
        //    MergeDefinitionFile((List<ParameterModel>)m.Value);
        //});

        WeakReferenceMessenger.Default.Register<ListsOfGroupsAndParametersMessage>(this, (r, m) =>
        {
            MergeDefinitionFile((GroupsAndParametersModel)m.Value);
        });
    }

    [RelayCommand]
    private void ToggleGroupVisibility()
    {
        if(GroupsVisible == System.Windows.Visibility.Visible)
        {
            GroupsVisible = System.Windows.Visibility.Hidden;
            ToggleGroupMenuText = "Show groups";
            return;
        }

        if(GroupsVisible == System.Windows.Visibility.Hidden)
        {
            GroupsVisible = System.Windows.Visibility.Visible;
            ToggleGroupMenuText = "Hide groups";
            return;
        }
    }

    [RelayCommand]
    public void LoadDefinitionFile()
    {
        using (LogContext.PushProperty("UsageTracking", true))
        {
            Log.Information("{command}", nameof(LoadDefinitionFile));
        }


        DefFile = new SharedParametersDefinitionFile(FileInfo.FullName);
        DefFile.LoadFile();

        UpdateMRU();

        DefFile.definitionFileModel.Parameters.CollectionChanged += Parameters_CollectionChanged;
        DefFile.definitionFileModel.Groups.CollectionChanged += Groups_CollectionChanged;

        MergeEnabled = true;

        //check if the file is writable or readonly
        Writable = true;

        if (FileInfo.IsReadOnly)
        {
            Writable = false;
            return;
        }
    }


    [RelayCommand]
    public void SaveDefinitionFile()
    {
        using (LogContext.PushProperty("UsageTracking", true))
        {
            Log.Information("{command}", nameof(SaveDefinitionFile));
        }

        if (NewFileName != null)
        {
            DefFile?.SaveFile(NewFileName, false);
            return;
        }

        DefFile?.SaveFile();
        UnsavedChanges = false;
    }

    [RelayCommand]
    private void MergeDefinitionFile(GroupsAndParametersModel itemsToMerge)
    {
        using (LogContext.PushProperty("UsageTracking", true))
        {
            Log.Information("{command}", nameof(MergeDefinitionFile));
        }

        if (itemsToMerge.ParameterModels.Count > 0)
        {
            var newGroup = new GroupModel
            {
                Name = "Merged Parameters"
            };

            DefFile.definitionFileModel.Groups.Add(newGroup);

            //for each parameter.  
            foreach (var parameter in itemsToMerge.ParameterModels)
            {
                parameter.Group = newGroup.ID;
                DefFile.definitionFileModel.Parameters.Add(parameter);
            }

            UnsavedChanges = true;
        }
    }

    private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var ID = DefFile.definitionFileModel.Groups.Max(x => x.ID);
            ID++;

            var newGroup = e.NewItems[0] as GroupModel;
            newGroup.ID = ID;
        }

        UnsavedChanges = true;
    }

    private void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var param = e.NewItems[0] as ParameterModel;

            if(param.Group <= 1)
            {
                param.Group = DefFile.definitionFileModel.Groups.Min(x => x.ID);
            }

        }

        UnsavedChanges = true;
    }


    private void UpdateMRU()
    {
        MostRecentlyUsedFiles.Clear();

        MostRecentlyUsedFiles = GetMostRecentlyUsedFiles();
    }

    private List<string> GetMostRecentlyUsedFiles()
    {
        var recentFiles = new List<string>();

        var path = Environment.GetFolderPath(Environment.SpecialFolder.Recent);

        var directory = new DirectoryInfo(path);
        var shortcutFiles = directory.GetFiles("*.txt.lnk")
            .Where(f => f.Name.Contains("shared", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(10)
            .ToList();

        if (shortcutFiles.Count < 1)
        {
            return recentFiles;
        }

        dynamic script = CreateComInstance("Wscript.Shell");

        foreach (var file in shortcutFiles)
        {
            dynamic sc = script.CreateShortcut(file.FullName);
            recentFiles.Add(sc.TargetPath);
            Marshal.FinalReleaseComObject(sc);
        }
        Marshal.FinalReleaseComObject(script);

        return recentFiles;
    }

    private object CreateComInstance(string progId)
    {
        Type type = Type.GetTypeFromProgID(progId);
        if (type == null)
        {
            return null;
        }

        return Activator.CreateInstance(type);
    }
}