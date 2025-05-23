using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GRAFF.TaskTracker.Web.Models
{
    public class TaskViewModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public int ColumnId { get; set; }
        public int Order { get; set; }
    }

    public class TaskCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        // ColumnId will be supplied separately
    }

    public class TaskUpdateDto
    {
        public int TaskId { get; set; } // Important to identify the task

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public int ColumnId { get; set; } // To allow moving task to a different column
        public int Order { get; set; } 
    }
    
    public class TaskOrderUpdateDto 
    {
        [Required]
        public List<int>? TaskIdsInOrder { get; set; }
    }

    public class TaskMoveDto
    {
        [Required]
        public int TargetColumnId { get; set; }
        public int NewOrderInColumn { get; set; } 
    }
}
