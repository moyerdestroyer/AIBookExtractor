using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AIBookExtractor.Models {
    public class PageModel {
        public int PageNumber { get; set; }
        public required Bitmap PageImage { get; set; }
        public required string TextContent { get; set; }
        public bool ProcessedByAI { get; set; }
    }
}
