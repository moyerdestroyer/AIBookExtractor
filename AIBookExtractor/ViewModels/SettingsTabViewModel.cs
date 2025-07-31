using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using AIBookExtractor.Services;
using AIBookExtractor.Services.Configuration;

namespace AIBookExtractor.ViewModels
{
    public class SettingsTabViewModel : ReactiveObject
    {
        private readonly IAiServiceConfiguration _serviceConfiguration;
        private readonly IAiServiceFactory _serviceFactory;
        
        private AiServiceDefinition? _selectedService;
        private AiModelDefinition? _selectedModel;
        private string? _apiKey;
        private string? _prompt;
        private IAiService? _currentAiService;

        public SettingsTabViewModel()
        {
            _serviceConfiguration = new AiServiceConfiguration();
            _serviceFactory = new AiServiceFactory(_serviceConfiguration);
            
            AvailableServices = new ObservableCollection<AiServiceDefinition>(_serviceConfiguration.GetAvailableServices());
            AvailableModels = new ObservableCollection<AiModelDefinition>();
            
            // Set default prompt
            Prompt = "Please extract and clean up the text from this page, preserving the original meaning and structure.";
            
            // Watch for service selection changes
            this.WhenAnyValue(x => x.SelectedService)
                .Subscribe(service =>
                {
                    if (service != null)
                    {
                        UpdateAvailableModels(service);
                        UpdateApiKeyVisibility(service);
                    }
                });
            
            // Watch for configuration changes
            this.WhenAnyValue(
                x => x.SelectedService,
                x => x.SelectedModel,
                x => x.ApiKey,
                x => x.Prompt)
                .Subscribe(_ => UpdateAiServiceConfiguration());
        }

        public ObservableCollection<AiServiceDefinition> AvailableServices { get; }
        public ObservableCollection<AiModelDefinition> AvailableModels { get; }

        public AiServiceDefinition? SelectedService
        {
            get => _selectedService;
            set => this.RaiseAndSetIfChanged(ref _selectedService, value);
        }

        public AiModelDefinition? SelectedModel
        {
            get => _selectedModel;
            set => this.RaiseAndSetIfChanged(ref _selectedModel, value);
        }

        public string? ApiKey
        {
            get => _apiKey;
            set => this.RaiseAndSetIfChanged(ref _apiKey, value);
        }

        public string? Prompt
        {
            get => _prompt;
            set => this.RaiseAndSetIfChanged(ref _prompt, value);
        }

        private bool _isApiKeyRequired = true;
        public bool IsApiKeyRequired
        {
            get => _isApiKeyRequired;
            private set => this.RaiseAndSetIfChanged(ref _isApiKeyRequired, value);
        }

        private string? _apiKeyHelpText;
        public string? ApiKeyHelpText
        {
            get => _apiKeyHelpText;
            private set => this.RaiseAndSetIfChanged(ref _apiKeyHelpText, value);
        }

        public IAiService? GetConfiguredAiService()
        {
            return _currentAiService;
        }

        private void UpdateAvailableModels(AiServiceDefinition service)
        {
            AvailableModels.Clear();
            foreach (var model in service.Models)
            {
                AvailableModels.Add(model);
            }
            
            // Select first model by default
            if (AvailableModels.Count > 0)
            {
                SelectedModel = AvailableModels[0];
            }
        }

        private void UpdateApiKeyVisibility(AiServiceDefinition service)
        {
            IsApiKeyRequired = service.RequiresApiKey;
            ApiKeyHelpText = service.ApiKeyHelpText;
        }

        private void UpdateAiServiceConfiguration()
        {
            if (SelectedService == null || SelectedModel == null || string.IsNullOrEmpty(Prompt))
                return;
            
            if (SelectedService.RequiresApiKey && string.IsNullOrEmpty(ApiKey))
                return;

            try
            {
                _currentAiService = _serviceFactory.CreateService(SelectedService.Name);
                _currentAiService.Configure(
                    SelectedService.Name, 
                    SelectedModel.Name, 
                    ApiKey ?? "", 
                    Prompt);
            }
            catch (Exception ex)
            {
                // Log error or notify user
                Console.WriteLine($"Error configuring AI service: {ex.Message}");
                _currentAiService = null;
            }
        }
    }
}