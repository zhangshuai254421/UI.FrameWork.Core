using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Domain
{
    public partial class SerilogHistory:Entity
    {
        public string? Timestamp { get; set; }

        [Column(TypeName = "VARCHAR(10)")]
        public string? Level { get; set; }

        public string? Exception { get; set; }

        public string? RenderedMessage { get; set; }

        public string? Properties { get; set; }
    }
}
