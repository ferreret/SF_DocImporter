using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfProcessingService.Models
{
    public class SearchRectangle
    {
        public string? Name { get; set; }
        public float Top { get; set; }
        public float Left { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string? Expression { get; set; }
    }
}
