using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GRAFF.TaskTracker.Models
{
    public class Task
    {
        public int TaskId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public int ColumnId { get; set; }
        [ForeignKey("ColumnId")]
        public Column? Column { get; set; }

        public int Order { get; set; } // To determine the order of tasks in a column
    }
}
