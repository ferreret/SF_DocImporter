using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil.Models
{
    public class DocumentDefinition
    {
        public string? Name { get; set; }
        public int MinIdentifiers { get; set; }
        public List<SearchRectangle>? Identifiers { get; set; }
        public List<SearchRectangle>? Fields { get; set; }
    }
}
