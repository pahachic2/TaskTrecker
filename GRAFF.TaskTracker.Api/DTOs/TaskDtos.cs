using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GRAFF.TaskTracker.Api.DTOs
{
    public class TaskCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        // ColumnId will be taken from the route or payload
    }

    public class TaskUpdateDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public int ColumnId { get; set; } // Allow moving task to a different column
        public int Order { get; set; } // Allow updating order within a column
    }
    
    public class TaskOrderUpdateDto // For reordering tasks within a column
    {
        [Required]
        public List<int>? TaskIdsInOrder { get; set; }
    }

    public class TaskMoveDto // For moving a task to a different column and optionally reordering
    {
        [Required]
        public int TargetColumnId { get; set; }
        public int NewOrderInColumn { get; set; } // The new index/order within the target column
    }

    public class TaskDto // For returning task details
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public int ColumnId { get; set; }
        public int Order { get; set; }
    }
}
