using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Log.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Log.Domain 程序集中的某个文件（如 AssemblyInfo.cs）
[assembly: DefaultDbContext(typeof(DataContext))]
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
