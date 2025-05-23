using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GRAFF.TaskTracker.Api.DTOs
{
    public class ColumnCreateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        // ProjectId will be taken from the route
    }

    public class ColumnUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; } // Allow updating order
    }
    
    public class ColumnOrderUpdateDto // For reordering columns
    {
        [Required]
        public List<int>? ColumnIdsInOrder { get; set; } // List of Column IDs in the new desired order
    }

    public class ColumnDto // For returning column details
    {
        public int ColumnId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int Order { get; set; }
        public List<TaskDto> Tasks { get; set; } = new List<TaskDto>(); // Include tasks
    }
}
