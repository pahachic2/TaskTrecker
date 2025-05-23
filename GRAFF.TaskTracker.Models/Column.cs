using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GRAFF.TaskTracker.Models
{
    public class Column
    {
        public int ColumnId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public int Order { get; set; } // To determine the order of columns in a project

        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
