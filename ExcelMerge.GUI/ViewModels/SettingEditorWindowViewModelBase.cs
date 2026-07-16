using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelMerge.GUI.Settings;

namespace ExcelMerge.GUI.ViewModels
{
    public class SettingEditorWindowViewModelBase<T> : ObservableObject where T : Setting<T>
    {
        public delegate bool ValidateSettingDelegate(T setting, ref string error);

        public ValidateSettingDelegate ValidateSettingCallback { get; set; }

        private T setting;
        public T Setting
        {
            get { return setting; }
            private set { SetProperty(ref setting, value); }
        }

        public bool IsCancelled { get; private set; } = true;

        public RelayCommand<Window> CancelCommand { get; private set; }
        public RelayCommand<Window> DoneCommand { get; private set; }

        public SettingEditorWindowViewModelBase(T setting)
        {
            Setting = setting.DeepClone();
            Setting.Clean();

            Setting = setting;

            CancelCommand = new RelayCommand<Window>((w) =>
            {
                IsCancelled = true;

                w.Close();
            });

            DoneCommand = new RelayCommand<Window>((w) =>
            {
                string error = string.Empty;
                if (ValidateSettingCallback != null && !ValidateSettingCallback(Setting, ref error))
                {
                    MessageBox.Show(error);
                    return;
                }

                IsCancelled = false;

                w.Close();
            });
        }
    }
}
