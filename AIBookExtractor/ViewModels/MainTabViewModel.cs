using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;
using AIBookExtractor.models;
using AIBookExtractor.Models;
using AIBookExtractor.Services;

namespace AIBookExtractor.ViewModels
{
    public class MainTabViewModel : ReactiveObject
    {
        private readonly IPdfService _pdfService;
        private readonly SettingsTabViewModel _settingsViewModel;
        
        private PdfFile? _currentPdf;
        private PageModel? _selectedPage;
        private string _extractedText = "";
        private string _pageNumberHeader = "No Page Selected";
        private bool _isLoading;
        private bool _canSave;
        private bool _canSendToAi;

        public MainTabViewModel(SettingsTabViewModel settingsViewModel)
        {
            _pdfService = new PdfService();
            _settingsViewModel = settingsViewModel;
            
            Pages = new ObservableCollection<PageViewModel>();
            
            LoadPdfCommand = ReactiveCommand.CreateFromTask(LoadPdf);
            SaveTextCommand = ReactiveCommand.CreateFromTask(SaveText, this.WhenAnyValue(x => x.CanSave));
            SendToAiCommand = ReactiveCommand.CreateFromTask(SendToAi, this.WhenAnyValue(x => x.CanSendToAi));
        }

        public ObservableCollection<PageViewModel> Pages { get; }

        public ReactiveCommand<Unit, Unit> LoadPdfCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveTextCommand { get; }
        public ReactiveCommand<Unit, Unit> SendToAiCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool CanSave
        {
            get => _canSave;
            set => this.RaiseAndSetIfChanged(ref _canSave, value);
        }

        public bool CanSendToAi
        {
            get => _canSendToAi;
            set => this.RaiseAndSetIfChanged(ref _canSendToAi, value);
        }

        public string ExtractedText
        {
            get => _extractedText;
            set
            {
                this.RaiseAndSetIfChanged(ref _extractedText, value);
                if (_selectedPage != null)
                {
                    _selectedPage.TextContent = value;
                }
            }
        }

        public string PageNumberHeader
        {
            get => _pageNumberHeader;
            set => this.RaiseAndSetIfChanged(ref _pageNumberHeader, value);
        }

        public PageModel? SelectedPage
        {
            get => _selectedPage;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPage, value);
                if (value != null)
                {
                    ExtractedText = value.TextContent;
                    PageNumberHeader = $"Page {value.PageNumber}";
                }
            }
        }

        public void SetLoadedPdf(PdfFile pdfFile)
        {
            _currentPdf = pdfFile;
            
            Pages.Clear();
            foreach (var page in pdfFile.Pages)
            {
                Pages.Add(new PageViewModel(page));
            }

            if (Pages.Count > 0)
            {
                SelectedPage = Pages[0].Page;
            }

            CanSave = true;
            CanSendToAi = true;
        }

        private async Task LoadPdf()
        {
            try
            {
                IsLoading = true;
                
                var filePath = await _pdfService.SelectPdfFile();
                if (string.IsNullOrEmpty(filePath))
                    return;

                _currentPdf = await _pdfService.LoadPdf(filePath);
                
                Pages.Clear();
                foreach (var page in _currentPdf.Pages)
                {
                    Pages.Add(new PageViewModel(page));
                }

                if (Pages.Count > 0)
                {
                    SelectedPage = Pages[0].Page;
                }

                CanSave = true;
                CanSendToAi = true;
            }
            catch (Exception ex)
            {
                // Handle error - will be shown via the view
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveText()
        {
            if (_currentPdf == null)
                return;

            await _pdfService.SaveText(_currentPdf);
        }

        private async Task SendToAi()
        {
            if (_selectedPage == null || string.IsNullOrEmpty(ExtractedText))
                return;

            var aiService = _settingsViewModel.GetConfiguredAiService();
            if (aiService == null)
            {
                // TODO: Show error message to user
                throw new InvalidOperationException("AI service is not configured. Please configure it in the Settings tab.");
            }

            var processedText = await aiService.ProcessText(ExtractedText);
            ExtractedText = processedText;
            _selectedPage.ProcessedByAi = true;
        }
    }

    public class PageViewModel : ReactiveObject
    {
        private bool _isSelected;

        public PageViewModel(PageModel page)
        {
            Page = page;
        }

        public PageModel Page { get; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public Bitmap? Thumbnail => Page.PageImage;
        public int PageNumber => Page.PageNumber;
        public bool ProcessedByAi { get; set; } = false;
    }
}