using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AIBookExtractor.Models;
using AIBookExtractor.Services;
using AIBookExtractor.ViewModels;
using AIBookExtractor.Views.Controls;
using AIBookExtractor.Views.Dialogs;
using Avalonia;

namespace AIBookExtractor.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
        private Button? _selectedThumbnailButton;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            ExtractedTextBox.TextChanged += ExtractedTextBox_TextChanged;
            
            _viewModel.MainTabViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(MainTabViewModel.Pages))
                {
                    await UpdateThumbnails();
                }
            };
        }

        private async void LoadPdfButton_Click(object? sender, RoutedEventArgs e)
        {
            var progressDialog = new ProgressDialog();
            
            try
            {
                progressDialog.Show(this);
                
                // Create a progress reporter that updates on the UI thread
                var progress = new Progress<double>(percentage =>
                {
                    // This is already on the UI thread when using Progress<T>
                    if (progressDialog.IsVisible)
                    {
                        progressDialog.ProgressValue = percentage;
                        progressDialog.ProgressText.Text = $"Processing PDF... {(int)percentage}%";
                    }
                });

                // Pass the progress to the service
                var pdfService = new PdfService();
                var filePath = await pdfService.SelectPdfFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    progressDialog.Close();
                    return;
                }

                var pdf = await pdfService.LoadPdf(filePath, progress);
                
                // Update the view model with the loaded PDF
                _viewModel.MainTabViewModel.SetLoadedPdf(pdf);
                await UpdateThumbnails();
            }
            catch (OperationCanceledException)
            {
                await MessageBox.Show(this, "Cancelled", "PDF processing was cancelled by the user.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, "Error", $"Failed to process PDF: {ex.Message}");
            }
            finally
            {
                progressDialog.Close();
            }
        }

        private async Task UpdateThumbnails()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PdfPagesPanel.Children.Clear();
                _selectedThumbnailButton = null;

                foreach (var pageVm in _viewModel.MainTabViewModel.Pages)
                {
                    var button = CreateThumbnailButton(pageVm);
                    PdfPagesPanel.Children.Add(button);
                    
                    if (pageVm.Page == _viewModel.MainTabViewModel.SelectedPage)
                    {
                        SelectThumbnailButton(button);
                    }
                }
            });
        }

        private Button CreateThumbnailButton(PageViewModel pageViewModel)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5),
                Width = 260
            };

            if (pageViewModel.Thumbnail != null)
            {
                var image = new Image
                {
                    Source = pageViewModel.Thumbnail,
                    Width = 250,
                    Height = 300,
                    Stretch = Stretch.Uniform
                };
                stackPanel.Children.Add(image);
            }
            else
            {
                var noImageText = new TextBlock
                {
                    Text = $"Page {pageViewModel.PageNumber} (No Image)",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(noImageText);
            }

            var pageNumberLabel = new TextBlock
            {
                Text = $"Page {pageViewModel.PageNumber}",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(pageNumberLabel);

            var processedIcon = new TextBlock
            {
                Text = pageViewModel.Page.ProcessedByAi ? "✓" : "",
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Green,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stackPanel.Children.Add(processedIcon);
            

            var button = new Button
            {
                Content = stackPanel,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = pageViewModel,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            button.Click += (s, e) =>
            {
                if (_selectedThumbnailButton != null)
                {
                    DeselectThumbnailButton(_selectedThumbnailButton);
                }
                
                SelectThumbnailButton(button);
                _viewModel.MainTabViewModel.SelectedPage = pageViewModel.Page;
            };

            return button;
        }

        private void SelectThumbnailButton(Button button)
        {
            button.BorderThickness = new Thickness(2);
            button.BorderBrush = Brushes.Blue;
            button.Background = Brushes.LightGray;
            _selectedThumbnailButton = button;
        }

        private void DeselectThumbnailButton(Button button)
        {
            button.BorderThickness = new Thickness(0);
            button.Background = Brushes.Transparent;
        }

        private async void SaveTextButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.MainTabViewModel.SaveTextCommand.Execute();
                await MessageBox.Show(this, "Success", "Text saved successfully.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, "Error", $"Failed to save text: {ex.Message}");
            }
        }

        private async void SendToAiButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.MainTabViewModel.SendToAiCommand.Execute();
                await MessageBox.Show(this, "Success", "Text sent to AI and updated.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, "Error", $"Failed to process with AI: {ex.Message}");
            }
			//update thumbnails
			await UpdateThumbnails();
        }

        private void ExtractedTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (_viewModel.MainTabViewModel != null && ExtractedTextBox.Text != null)
            {
                _viewModel.MainTabViewModel.ExtractedText = ExtractedTextBox.Text;
            }
        }
    }
}