namespace PdfUtil.Components
{
    public class DocumentDefinition
    {
        public string? Name { get; set; }
        public int MinIdentifiers { get; set; }
        public List<SearchRectangle>? Identifiers { get; set; }
        public List<SearchRectangle>? Fields { get; set; }
    }
}