using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using PDFtoImage;
using SkiaSharp;
using System.Threading;
using Avalonia.Threading;

namespace AIBookExtractor.Models {
    public class PdfFile {
        private string FilePath { get; }
        private string OutputPath { get; }
        public List<PageModel> Pages { get; }

        public PdfFile(string filePath, CancellationToken cancellationToken = default,
            Action<double> progressCallback = null) {
            Pages = new List<PageModel>();
            FilePath = string.IsNullOrEmpty(filePath) ? Path.Combine("assets", "sample.png") : filePath;

            if (string.IsNullOrEmpty(filePath)) {
                // Sample case: Simulate 5 pages with sample.jpg
                for (int i = 1; i <= 5; i++) {
                    Pages.Add(new PageModel {
                        PageNumber = i,
                        TextContent = $"Sample text for page {i}",
                        PageImage = new Bitmap(FilePath),
                        ProcessedByAI = false
                    });
                    Console.WriteLine($"Loaded sample page {i}");
                    progressCallback?.Invoke(i * 20.0);
                }

                Console.WriteLine("Loaded 5 sample pages.");
            }
            else {
                // Set output directory
                OutputPath = Path.Combine(Path.GetDirectoryName(FilePath),
                    Path.GetFileNameWithoutExtension(FilePath) + "_AIBE");
                if (!Directory.Exists(OutputPath)) {
                    Directory.CreateDirectory(OutputPath);
                }

                // Try to load existing processed data
                if (LoadExistingData(progressCallback)) {
                    Console.WriteLine($"Successfully loaded {Pages.Count} pages from existing data.");
                    // Ensure UI update on UI thread
                    Dispatcher.UIThread.InvokeAsync(() => progressCallback?.Invoke(100.0));
                    return; // Exit if existing data was loaded
                }

                // Process PDF if no existing data was loaded
                Console.WriteLine($"No valid existing data in {OutputPath}. Processing PDF.");
                ProcessPdfPages(cancellationToken, progressCallback);
            }
        }

        private bool LoadExistingData(Action<double> progressCallback) {
            var imageFiles = Directory.GetFiles(OutputPath, "page_*.png")
                .OrderBy(f => {
                    var fileName = Path.GetFileNameWithoutExtension(f);
                    return int.TryParse(fileName.Replace("page_", ""), out int pageNum) ? pageNum : int.MaxValue;
                })
                .ToList();

            if (!imageFiles.Any()) {
                Console.WriteLine($"No .png files found in {OutputPath}.");
                return false;
            }

            Console.WriteLine($"Found {imageFiles.Count} existing .png files in {OutputPath}.");

            // Load text content if available
            var pageData = new Dictionary<int, (string Text, bool ProcessedByAI)>();
            var textFile = Path.Combine(OutputPath, "extracted_text.txt");
            if (File.Exists(textFile)) {
                try {
                    var textContent = File.ReadAllText(textFile)
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    Console.WriteLine($"Read {textContent.Length} lines from {textFile}.");
                    int? currentPage = null;
                    string currentText = "";
                    bool isPageContent = false;

                    for (int i = 0; i < textContent.Length; i++) {
                        var line = textContent[i].Trim();
                        if (string.IsNullOrEmpty(line) && !isPageContent) {
                            continue; // Skip empty lines outside page content
                        }

                        if (line.StartsWith("--- Page ")) {
                            // Save previous page data
                            if (currentPage.HasValue && !string.IsNullOrEmpty(currentText)) {
                                pageData[currentPage.Value] = (currentText.Trim(),
                                    pageData.ContainsKey(currentPage.Value)
                                        ? pageData[currentPage.Value].ProcessedByAI
                                        : false);
                                Console.WriteLine(
                                    $"Stored text for page {currentPage.Value}: {currentText.Length} characters.");
                            }

                            // Parse new page header
                            var pageInfo = line.Replace("--- Page ", "").Split('|');
                            if (pageInfo.Length > 0 && int.TryParse(pageInfo[0].Trim(), out int pageNum)) {
                                currentPage = pageNum;
                                currentText = "";
                                isPageContent = true;
                                // Check for Processed by AI flag in header
                                bool processedByAI = pageInfo.Any(part => part.Contains("Processed by AI: True"));
                                if (pageData.ContainsKey(pageNum)) {
                                    pageData[pageNum] = (pageData[pageNum].Text, processedByAI);
                                }
                                else {
                                    pageData[pageNum] = ("", processedByAI);
                                }

                                Console.WriteLine(
                                    $"Detected page header: Page {pageNum}, ProcessedByAI={processedByAI}");
                            }
                            else {
                                Console.WriteLine($"Invalid page header at line {i + 1}: {line}");
                                isPageContent = false;
                            }

                            continue;
                        }

                        if (isPageContent) {
                            currentText += line + Environment.NewLine;
                        }
                    }

                    // Save the last page
                    if (currentPage.HasValue && !string.IsNullOrEmpty(currentText)) {
                        pageData[currentPage.Value] = (currentText.Trim(),
                            pageData.ContainsKey(currentPage.Value)
                                ? pageData[currentPage.Value].ProcessedByAI
                                : false);
                        Console.WriteLine(
                            $"Stored text for page {currentPage.Value}: {currentText.Length} characters.");
                    }

                    Console.WriteLine($"Parsed {pageData.Count} pages from {textFile}.");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error parsing {textFile}: {ex.Message}. Using default text.");
                }
            }
            else {
                Console.WriteLine($"No text file found at {textFile}. Using default text.");
            }

            // Load pages from image files
            bool anyPageLoaded = false;
            int processed = 0;
            foreach (var imageFile in imageFiles) {
                var fileName = Path.GetFileNameWithoutExtension(imageFile);
                if (!int.TryParse(fileName.Replace("page_", ""), out int pageNum)) {
                    Console.WriteLine($"Skipping invalid image file: {imageFile}");
                    continue;
                }

                try {
                    var pageModel = new PageModel {
                        PageNumber = pageNum,
                        PageImage = new Bitmap(imageFile),
                        ProcessedByAI = pageData.ContainsKey(pageNum) && pageData[pageNum].ProcessedByAI,
                        TextContent = pageData.ContainsKey(pageNum) && !string.IsNullOrEmpty(pageData[pageNum].Text)
                            ? pageData[pageNum].Text
                            : $"No text content loaded for page {pageNum}"
                    };

                    Pages.Add(pageModel);
                    anyPageLoaded = true;
                    Console.WriteLine(
                        $"Loaded page {pageNum}: Image={imageFile}, Text={(pageModel.TextContent.Length > 50 ? pageModel.TextContent.Substring(0, 50) + "..." : pageModel.TextContent)}, ProcessedByAI={pageModel.ProcessedByAI}");

                    // Update progress on UI thread
                    processed++;
                    double progressPercentage = (processed / (double)imageFiles.Count) * 100;
                    Dispatcher.UIThread.InvokeAsync(() => progressCallback?.Invoke(progressPercentage));
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error loading image {imageFile}: {ex.Message}");
                    continue;
                }
            }

            return anyPageLoaded;
        }

        private void ProcessPdfPages(CancellationToken cancellationToken, Action<double> progressCallback) {
            int totalPages = GetTotalPageCount(FilePath);
            Console.WriteLine($"Total pages detected: {totalPages}");

            if (totalPages == 0) {
                Console.WriteLine("No pages found in the PDF.");
                Dispatcher.UIThread.InvokeAsync(() => progressCallback?.Invoke(100.0));
                return;
            }

            for (int page = 0; page < totalPages; page++) {
                cancellationToken.ThrowIfCancellationRequested();

                try {
                    string outputImagePath = Path.Combine(OutputPath, $"page_{page + 1}.png");

                    // Skip if the image already exists
                    if (File.Exists(outputImagePath)) {
                        Console.WriteLine($"Skipping page {page + 1}: Image already exists at {outputImagePath}");
                        Pages.Add(new PageModel {
                            PageNumber = page + 1,
                            TextContent = $"Text for Page {page + 1}",
                            PageImage = new Bitmap(outputImagePath),
                            ProcessedByAI = false
                        });
                        double progressPercentage = ((page + 1) / (double)totalPages) * 100;
                        int roundedPercentage = (int)Math.Round(progressPercentage);
                        Console.WriteLine(
                            $"Loaded existing page {page + 1}/{totalPages}, Progress: {roundedPercentage}%");
                        Dispatcher.UIThread.InvokeAsync(() => progressCallback?.Invoke(roundedPercentage));
                        continue;
                    }

                    using (var pdfStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read)) {
                        var skBitmap = Conversion.ToImage(pdfStream, page);
                        if (skBitmap == null) {
                            Console.WriteLine(
                                $"Failed to convert page {page + 1} to image: Conversion.ToImage returned null.");
                            continue;
                        }

                        using (skBitmap) {
                            using (var wStream = new SKFileWStream(outputImagePath)) {
                                if (!skBitmap.Encode(wStream, SKEncodedImageFormat.Png, 100)) {
                                    Console.WriteLine($"Failed to encode page {page + 1} to PNG.");
                                    continue;
                                }
                            }

                            Pages.Add(new PageModel {
                                PageNumber = page + 1,
                                TextContent = $"Text for Page {page + 1}",
                                PageImage = new Bitmap(outputImagePath),
                                ProcessedByAI = false
                            });
                        }

                        double progressPercentage = ((page + 1) / (double)totalPages) * 100;
                        int roundedPercentage = (int)Math.Round(progressPercentage);
                        Console.WriteLine($"Processed page {page + 1}/{totalPages}, Progress: {roundedPercentage}%");
                        Dispatcher.UIThread.InvokeAsync(() => progressCallback?.Invoke(roundedPercentage));
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error processing page {page + 1}: {ex.Message}");
                    if (ex.InnerException != null) {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }

                    continue;
                }
            }
        }

        private int GetTotalPageCount(string filePath) {
            try {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var count = PDFtoImage.Conversion.GetPageCount(fileStream);
                return count;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error getting page count: {ex.Message}");
                return 0;
            }
        }

        public void Save() {
            string outputTextFile = Path.Combine(OutputPath, "extracted_text.txt");
            using (StreamWriter writer = new StreamWriter(outputTextFile)) {
                writer.WriteLine($"File: {FilePath}");
                writer.WriteLine($"Output Path: {OutputPath}");
                writer.WriteLine();

                foreach (var page in Pages) {
                    writer.WriteLine($"--- Page {page.PageNumber} | Processed by AI: {page.ProcessedByAI} ---");
                    writer.WriteLine(page.TextContent);
                    writer.WriteLine();
                }
            }
        }

        public string GetOutputPath() {
            return OutputPath;
        }
    }

    public class ProgressEventArgs : EventArgs {
        public double Percentage { get; }

        public ProgressEventArgs(double percentage) {
            Percentage = Math.Clamp(percentage, 0, 100);
        }
    }
}