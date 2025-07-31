using System;
using System.Threading.Tasks;
using AIBookExtractor.models;
using AIBookExtractor.Models;

namespace AIBookExtractor.Services
{
    public interface IPdfService
    {
        Task<string?> SelectPdfFile();
        Task<PdfFile> LoadPdf(string filePath, IProgress<double>? progress = null);
        Task SaveText(PdfFile pdfFile);
    }
}