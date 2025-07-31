using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AIBookExtractor.Models;
using AIBookExtractor.Views.Dialogs;
using PdfFile = AIBookExtractor.Models.PdfFile;

namespace AIBookExtractor.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string?> SelectPdfFile()
        {
            var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (window == null)
                return null;

            var dialog = new FilePickerOpenOptions
            {
                Title = "Select PDF File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                },
                AllowMultiple = false
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(dialog);
            return result.FirstOrDefault()?.Path.LocalPath;
        }

        public async Task<PdfFile> LoadPdf(string filePath, IProgress<double>? progress = null)
        {
            var tcs = new TaskCompletionSource<PdfFile>();
            
            await Task.Run(() =>
            {
                try
                {
                    var pdfFile = new PdfFile(filePath, CancellationToken.None, percentage =>
                    {
                        // Report progress on the UI thread
                        progress?.Report(percentage);
                    });
                    tcs.SetResult(pdfFile);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            return await tcs.Task;
        }

        public async Task SaveText(PdfFile pdfFile)
        {
            await Task.Run(() => pdfFile.Save());
        }
    }
}