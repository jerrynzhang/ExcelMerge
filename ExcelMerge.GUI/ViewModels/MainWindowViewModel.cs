using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelMerge.GUI.Views;
using ExcelMerge.GUI.Settings;
using ExcelMerge.GUI.ValueConverters;

namespace ExcelMerge.GUI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private ContentControl content;
        public ContentControl Content
        {
            get { return content; }
            private set { SetProperty(ref content, value); }
        }

        private List<ExternalCommand> externalCommands;
        public List<ExternalCommand> ExternalCommands
        {
            get { return externalCommands; }
            private set { SetProperty(ref externalCommands, value); }
        }

        private List<FileSetting> fileSettings;
        public List<FileSetting> FileSettings
        {
            get { return fileSettings; }
            private set { SetProperty(ref fileSettings, value); }
        }

        private List<string> recentFiles;
        public List<string> RecentFiles
        {
            get { return recentFiles; }
            private set { SetProperty(ref recentFiles, value); }
        }

        private List<string> recentFileSets;
        public List<string> RecentFileSets
        {
            get { return recentFileSets; }
            private set { SetProperty(ref recentFileSets, value); }
        }

        private string srcPath;
        public string SrcPath
        {
            get { return srcPath; }
            set { SetProperty(ref srcPath, value); }
        }

        private string dstPath;
        public string DstPath
        {
            get { return dstPath; }
            set { SetProperty(ref dstPath, value); }
        }

        private string cultureName;
        public string CultureName
        {
            get { return cultureName; }
            private set { SetProperty(ref cultureName, value); }
        }

        public RelayCommand<ExternalCommand> ExecuteExternalCommandCommand { get; private set; }
        public RelayCommand OpenExternalCommandsWindowCommand { get; private set; }
        public RelayCommand OpenFileSettingsWindowCommand { get; private set; }
        public RelayCommand OpenDiffExtractionSettingsWindowCommand { get; private set; }
        public RelayCommand<FileDialogParameter> OpenFileDialogCommand { get; private set; }
        public RelayCommand<string> OpenAsSrcFileCommand { get; private set; }
        public RelayCommand<string> OpenAsDstFileCommand { get; private set; }
        public RelayCommand<string> OpenFileSetCommand { get; private set; }
        public RelayCommand<string> ChangeLanguageCommand{ get; private set; }

        public MainWindowViewModel(ContentControl content)
        {
            Content = content;

            Refresh();

            ExecuteExternalCommandCommand = new RelayCommand<ExternalCommand>((cmd) => cmd.Execute(false));
            OpenExternalCommandsWindowCommand = new RelayCommand(OpenExternalCommandsWindow);
            OpenFileSettingsWindowCommand = new RelayCommand(OpenFileSettingsWindow);
            OpenDiffExtractionSettingsWindowCommand = new RelayCommand(OpenDiffExtractionSettingWindow);
            OpenFileDialogCommand = new RelayCommand<FileDialogParameter>(OpenFileDialog);
            OpenAsSrcFileCommand = new RelayCommand<string>(OpenAsSrcFile);
            OpenAsDstFileCommand = new RelayCommand<string>(OpenAsDstFile);
            OpenFileSetCommand = new RelayCommand<string>(OpenFileSet);
            ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);

            App.Instance.Setting.PropertyChanged += Setting_PropertyChanged;
        }

        private void Setting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            RecentFiles = App.Instance.GetRecentFiles().ToList();
            ExternalCommands = App.Instance.Setting.ExternalCommands.ToList();
            FileSettings = App.Instance.Setting.FileSettings.ToList();
            RecentFileSets = App.Instance.GetRecentFileSets().Select(i => $"{i.Item1} | {i.Item2}").ToList();
            CultureName = App.Instance.Setting.Culture;
        }

        private void OpenExternalCommandsWindow()
        {
            var externalCommandsWindow = new ExternalCommandsWindow()
            {
                DataContext = new ExternalCommandsWindowViewModel()
            };

            externalCommandsWindow.ShowDialog();
        }

        private void OpenFileSettingsWindow()
        {
            var fileSettingsWindow = new FileSettingsWindow()
            {
                DataContext = new FileSettingsWindowViewModel()
            };

            fileSettingsWindow.ShowDialog();
        }

        private void OpenDiffExtractionSettingWindow()
        {
            var window = new DiffExtractionSettingWindow()
            {
                DataContext = new DiffExtractionSettingWindowViewModel(),
            };

            window.ShowDialog();
        }

        private void OpenFileDialog(FileDialogParameter parameter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = parameter.Title;
            if (dialog.ShowDialog().Value)
                parameter.PropertyInfo.SetValue(parameter.Obj, dialog.FileName);
        }

        private void OpenAsSrcFile(string file)
        {
            SrcPath = file;
        }

        private void OpenAsDstFile(string file)
        {
            DstPath = file;
        }

        private void OpenFileSet(string files)
        {
            var fs = files.Split('|');

            SrcPath = fs.ElementAtOrDefault(0).Trim();
            DstPath = fs.ElementAtOrDefault(1).Trim();
        }

        private void ChangeLanguage(string calture)
        {
            App.Instance.Setting.Culture = calture;
            App.Instance.Setting.Save();
            App.Instance.UpdateResourceCulture();
        }
    }
}
