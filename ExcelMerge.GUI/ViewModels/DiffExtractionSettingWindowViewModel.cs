using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelMerge.GUI.Settings;

namespace ExcelMerge.GUI.ViewModels
{
    public class DiffExtractionSettingWindowViewModel : ObservableObject
    {
        private ApplicationSetting originalSetting;

        private ApplicationSetting setting;
        public ApplicationSetting Setting
        {
            get { return setting; }
            private set { SetProperty(ref setting, value); }
        }

        private bool isDirty;
        public bool IsDirty
        {
            get { return isDirty; }
            private set { SetProperty(ref isDirty, value); }
        }

        private bool canRemoveAlternationColor;
        public bool CanRemoveAlternationColor
        {
            get { return canRemoveAlternationColor; }
            private set { SetProperty(ref canRemoveAlternationColor, value); }
        }

        public List<string> FontNames
        {
            get { return System.Drawing.FontFamily.Families.Select(f => f.Name).ToList(); }
        }

        public RelayCommand<Window> DoneCommand { get; private set; }
        public RelayCommand ResetCommand { get; private set; }
        public RelayCommand ApplyCommand { get; private set; }
        public RelayCommand<object> EditAlternationColorCommand { get; private set; }
        public RelayCommand<int?> RemoveAlternationColorCommand { get; private set; }
        public RelayCommand<Color?> AddAlternationColorCommand { get; private set; }

        public DiffExtractionSettingWindowViewModel()
        {
            originalSetting = App.Instance.Setting;
            Setting = originalSetting.DeepClone();

            CanRemoveAlternationColor = Setting.AlternatingColorStrings.Count > 1;

            Setting.PropertyChanged += Setting_PropertyChanged;

            DoneCommand = new RelayCommand<Window>(Done);
            ResetCommand = new RelayCommand(Reset);
            ApplyCommand = new RelayCommand(Apply);

            EditAlternationColorCommand = new RelayCommand<object>(EditAlternationColor);
            RemoveAlternationColorCommand = new RelayCommand<int?>(RemoveAlternationColor);
            AddAlternationColorCommand = new RelayCommand<Color?>(AddAlternationColor);
        }

        private void Setting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateDirtyFlag();
        }

        private void UpdateDirtyFlag()
        {
            IsDirty = !Setting.Equals(originalSetting);
        }

        private void Done(Window window)
        {
            if (IsDirty)
                Apply();

            window.Close();
        }

        private void Reset()
        {
            Setting.PropertyChanged -= Setting_PropertyChanged;
            Setting = originalSetting.DeepClone();
            Setting.PropertyChanged += Setting_PropertyChanged;

            IsDirty = false;
        }

        private void Apply()
        {
            App.Instance.UpdateSetting(Setting);
            App.Instance.Setting.Save();

            originalSetting = App.Instance.Setting;

            IsDirty = false;
        }

        private void EditAlternationColor(object parameter)
        {
            var parameters = parameter as List<object>;

            if (parameters?.Count < 2)
                return;

            var index = Convert.ToInt32(parameters[0]);
            var color = parameters[1].ToString();

            Setting.AlternatingColorStrings[index] = color;

            UpdateDirtyFlag();
        }

        private void RemoveAlternationColor(int? index)
        {
            if (!index.HasValue)
                return;

            Setting.AlternatingColorStrings.RemoveAt(index.Value);
            CanRemoveAlternationColor = Setting.AlternatingColorStrings.Count > 1;

            UpdateDirtyFlag();
        }

        private void AddAlternationColor(Color? color)
        {
            if (!color.HasValue)
                return;

            Setting.AlternatingColorStrings.Add(color.ToString());
            CanRemoveAlternationColor = Setting.AlternatingColorStrings.Count > 1;

            UpdateDirtyFlag();
        }
    }
}
