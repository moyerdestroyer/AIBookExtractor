using Avalonia.Media.Imaging;

namespace AIBookExtractor.models {
    public class PageModel {
        public int PageNumber { get; set; }
        public required Bitmap PageImage { get; set; }
        public required string TextContent { get; set; }
        public bool ProcessedByAi { get; set; }
    }
}
