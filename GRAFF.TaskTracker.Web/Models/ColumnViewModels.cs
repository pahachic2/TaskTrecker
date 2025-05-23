using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GRAFF.TaskTracker.Web.Models
{
    public class ColumnViewModel
    {
        public int ColumnId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int Order { get; set; }
        public List<TaskViewModel> Tasks { get; set; } = new List<TaskViewModel>();
    }

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
        public int Order { get; set; } // Allow updating order, though reorder endpoint is preferred for list changes
    }
    
    public class ColumnOrderUpdateDto
    {
        [Required]
        public List<int>? ColumnIdsInOrder { get; set; } 
    }
}
