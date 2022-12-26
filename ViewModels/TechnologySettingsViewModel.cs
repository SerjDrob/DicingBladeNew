using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.Technology;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Properties;
using DicingBlade.Utility;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using PropertyChanged;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace DicingBlade.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class TechnologySettingsViewModel : ITechnology, IDataErrorInfo
    {
        public TechnologySettingsViewModel()
        {
            _validator = new TechnologySettingsValidator();
            FileName = Settings.Default.TechnologyLastFile;
            if (FileName == null | !File.Exists(FileName))
            {
                SpindleFreq = 25000;
                FeedSpeed = 2;
                WaferBladeGap = 1;
                FilmThickness = 0.08;
                UnterCut = 0;
                PassCount = 1;
                PassType = Directions.Direct;
                StartControlNum = 3;
                ControlPeriod = 3;
                PassType = Directions.Direct;
            }
            else
            {
                ((ITechnology)StatMethods.DeSerializeObjectJson<Technology>(FileName)).CopyPropertiesTo(this);
            }

        }
        public string FileName { get; set; }
        public int SpindleFreq { get; set; }
        public double FeedSpeed { get; set; }
        public double WaferBladeGap { get; set; }
        public double FilmThickness { get; set; }
        public double UnterCut { get; set; }
        public int PassCount { get; set; }
        public Directions PassType { get; set; }
        public int StartControlNum { get; set; }
        public int ControlPeriod { get; set; }

        [ICommand]
        private void ClosingWnd()
        {
            PropContainer.Technology = this;
            new Technology(PropContainer.Technology).SerializeObjectJson(PropContainer.Technology.FileName);
            Settings.Default.TechnologyLastFile = PropContainer.Technology.FileName;
            Settings.Default.Save();
        }
        [ICommand]
        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Файлы технологии (*.json)|*.json",
            };

            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileName = dialog.FileName;
                ((ITechnology)StatMethods.DeSerializeObjectJson<Technology>(FileName)).CopyPropertiesTo(this);
            }
        }
        [ICommand]
        private void SaveFileAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Файлы технологии (*.json)|*.json",
            };

            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileName = dialog.FileName;
                ClosingWnd();
            }
        }
        public string Error => string.Empty;

        private readonly TechnologySettingsValidator _validator;
        public string this[string columnName]
        {
            get
            {
                var firstOrDefault = _validator.Validate(this).Errors.FirstOrDefault(lol => lol.PropertyName == columnName);
                if (firstOrDefault != null)
                    return _validator != null ? firstOrDefault.ErrorMessage : string.Empty;
                return string.Empty;
            }
        }

    }
}
