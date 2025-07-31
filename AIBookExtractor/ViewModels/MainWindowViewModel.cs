using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace AIBookExtractor.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private MainTabViewModel _mainTabViewModel;
        private SettingsTabViewModel _settingsTabViewModel;

        public MainWindowViewModel()
        {
            SettingsTabViewModel = new SettingsTabViewModel();
            MainTabViewModel = new MainTabViewModel(SettingsTabViewModel);
        }

        public MainTabViewModel MainTabViewModel
        {
            get => _mainTabViewModel;
            set => this.RaiseAndSetIfChanged(ref _mainTabViewModel, value);
        }

        public SettingsTabViewModel SettingsTabViewModel
        {
            get => _settingsTabViewModel;
            set => this.RaiseAndSetIfChanged(ref _settingsTabViewModel, value);
        }
    }
}