using System;
using System.IO;
using System.Threading.Tasks;
using AIBookExtractor.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Data;
using Avalonia.Controls.Shapes;
using System.Linq;
using System.Threading;
using Avalonia.Layout;
using Avalonia.Media;

namespace AIBookExtractor {
    public partial class MainWindow : Window {
        private PdfFile _currentPdf;
        private Button? _selectedThumbnailButton;

        public MainWindow() {
            InitializeComponent();
            ExtractedTextBox.TextChanged += ExtractedTextBox_TextChanged;
        }

        private async void LoadPdfButton_Click(object? sender, RoutedEventArgs e) {
            // Disable buttons during processing
            LoadPdfButton.IsEnabled = false;
            SaveTextButton.IsEnabled = false;
            SendToAiButton.IsEnabled = false;

            // Show file selection dialog
            var dialog = new OpenFileDialog {
                Title = "Select PDF File",
                Filters = new() {
                    new FileDialogFilter { Name = "PDF Files", Extensions = { "pdf" } },
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(this);
            if (result == null || result.Length == 0) {
                // Re-enable buttons
                LoadPdfButton.IsEnabled = true;
                SaveTextButton.IsEnabled = true;
                SendToAiButton.IsEnabled = true;
                return; // User canceled the dialog
            }

            string filePath = result[0];

            // Show progress dialog
            var progressDialog = new ProgressDialog();
            progressDialog.Show(this); // Show as a modal dialog

            try {
                // Process PDF in a background thread with progress callback
                _currentPdf = await Task.Run(() => {
                    var pdfFile = new PdfFile(filePath, progressDialog.CancellationToken, (percentage) => {
                        Console.WriteLine($"Event raised: {(int)percentage}%"); // Debug event raise
                        Dispatcher.UIThread.InvokeAsync(() => {
                                if (progressDialog != null && progressDialog.IsVisible) // Check visibility
                                {
                                    Console.WriteLine(
                                        $"UI Thread: Setting ProgressValue to {(int)percentage}%"); // Debug before set
                                    progressDialog.ProgressValue = percentage; // Update bound property
                                    progressDialog.ProgressText.Text = $"Processing PDF... {(int)percentage}%";
                                    Console.WriteLine(
                                        $"UI Thread: Set ProgressValue to {(int)percentage}% via SetValue"); // Debug after set
                                }
                                else {
                                    Console.WriteLine("Error: ProgressDialog is null or not visible");
                                }
                            }, DispatcherPriority.Render)
                            .Wait(); // Temporary sync for debugging
                    });
                    return pdfFile;
                });

                // Close progress dialog
                progressDialog.Close();

                // Clear existing thumbnails after progress dialog
                PdfPagesPanel.Children.Clear();
                _selectedThumbnailButton = null;

                // Add thumbnails to PdfPagesPanel (deferred to avoid UI thread blocking)
                await Dispatcher.UIThread.InvokeAsync(() => {
                    foreach (var page in _currentPdf.Pages) {
                        var stackPanel = new StackPanel {
                            Orientation = Orientation.Vertical,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(5),
                            Width = 260
                        };

                        var image = new Image {
                            Source = page.PageImage, Width = 250, Height = 300, Stretch = Avalonia.Media.Stretch.Uniform
                        };

                        var pageNumberLabel = new TextBlock {
                            Text = $"Page {page.PageNumber}",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 5, 0, 0)
                        };

                        stackPanel.Children.Add(image);
                        stackPanel.Children.Add(pageNumberLabel);

                        var button = new Button {
                            Content = stackPanel,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Tag = page,
                            Background = Avalonia.Media.Brushes.Transparent,
                            BorderThickness = new Thickness(0)
                        };

                        button.Click += (s, e) => {
                            if (_selectedThumbnailButton != null) {
                                _selectedThumbnailButton.BorderThickness = new Thickness(0);
                                _selectedThumbnailButton.Background = Avalonia.Media.Brushes.Transparent;
                            }

                            button.BorderThickness = new Thickness(2);
                            button.BorderBrush = Avalonia.Media.Brushes.Blue;
                            button.Background = Avalonia.Media.Brushes.LightGray;
                            _selectedThumbnailButton = button;
                            PageThumbnail_Click(s, e);
                        };

                        if (page.PageImage == null) {
                            button.Content = new StackPanel {
                                Orientation = Orientation.Vertical,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(5),
                                Children = {
                                    new TextBlock {
                                        Text = $"Page {page.PageNumber} (No Image)",
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center
                                    },
                                    pageNumberLabel
                                }
                            };
                        }

                        PdfPagesPanel.Children.Add(button);
                    }

                    // Select first page
                    if (_currentPdf.Pages.Count > 0) {
                        var firstButton = PdfPagesPanel.Children.OfType<Button>().FirstOrDefault();
                        if (firstButton != null) {
                            firstButton.BorderThickness = new Thickness(2);
                            firstButton.BorderBrush = Avalonia.Media.Brushes.Blue;
                            firstButton.Background = Avalonia.Media.Brushes.LightGray;
                            _selectedThumbnailButton = firstButton;
                            UpdateExtractedText(_currentPdf.Pages[0]);
                        }
                    }
                });
            }
            catch (OperationCanceledException) {
                progressDialog.Close();
                await new MessageBox { Title = "Cancelled", Message = "PDF processing was cancelled by the user." }
                    .ShowDialog(this);
            }
            catch (Exception ex) {
                progressDialog.Close();
                await new MessageBox { Title = "Error", Message = $"Failed to process PDF: {ex.Message}" }
                    .ShowDialog(this);
            }
            finally {
                // Re-enable buttons
                LoadPdfButton.IsEnabled = true;
                SaveTextButton.IsEnabled = _currentPdf != null;
                SendToAiButton.IsEnabled = _currentPdf != null;
            }
        }

        private void PageThumbnail_Click(object? sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is PageModel page) {
                UpdateExtractedText(page);
            }
        }

        private void UpdateExtractedText(PageModel page) {
            ExtractedTextBox.Text = page.TextContent;
            PageNumberHeader.Text = $"Page {page.PageNumber}";
        }

        private void SaveTextButton_Click(object? sender, RoutedEventArgs e) {
            if (_currentPdf == null) {
                new MessageBox { Title = "Error", Message = "No PDF loaded." }.ShowDialog(this);
                return;
            }

            try {
                _currentPdf.Save();
                new MessageBox {
                    Title = "Success", Message = $"Text saved to {_currentPdf.GetOutputPath()}/extracted_text.txt"
                }.ShowDialog(this);
            }
            catch (Exception ex) {
                new MessageBox { Title = "Error", Message = $"Failed to save text: {ex.Message}" }.ShowDialog(this);
            }
        }

        private async void SendToAiButton_Click(object? sender, RoutedEventArgs e) {
            if (_currentPdf == null || ExtractedTextBox.Text == null) {
                await new MessageBox { Title = "Error", Message = "No PDF or text selected." }.ShowDialog(this);
                return;
            }

            try {
                var selectedPage = _currentPdf.Pages.FirstOrDefault(p =>
                    p.PageNumber.ToString() == PageNumberHeader.Text.Replace("Page ", ""));
                if (selectedPage == null) {
                    await new MessageBox { Title = "Error", Message = "No page selected." }.ShowDialog(this);
                    return;
                }

                // Simulate AI processing
                await Task.Delay(1000);
                selectedPage.TextContent = $"AI Processed: {selectedPage.TextContent}";
                selectedPage.ProcessedByAI = true;
                UpdateExtractedText(selectedPage);

                await new MessageBox { Title = "Success", Message = "Text sent to AI and updated." }.ShowDialog(this);
            }
            catch (Exception ex) {
                await new MessageBox { Title = "Error", Message = $"Failed to process with AI: {ex.Message}" }
                    .ShowDialog(this);
            }
        }

        private void ExtractedTextBox_TextChanged(object? sender, EventArgs e) {
            if (_selectedThumbnailButton?.Tag is PageModel currentPage) {
                currentPage.TextContent = ExtractedTextBox.Text;
            }
        }
    }

    public class MessageBox : Window {
        public string Title { get; set; }
        public string Message { get; set; }

        public MessageBox() {
            Width = 300;
            Height = 150;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock { Text = Message, TextWrapping = TextWrapping.Wrap });
            var okButton = new Button {
                Content = "OK", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (s, e) => Close();
            stackPanel.Children.Add(okButton);

            Content = stackPanel;
        }

        public Task ShowDialog(Window owner) {
            return ShowDialog<object>(owner);
        }
    }
}