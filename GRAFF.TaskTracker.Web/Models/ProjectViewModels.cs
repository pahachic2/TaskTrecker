using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GRAFF.TaskTracker.Web.Models
{
    // For displaying project details, including its columns and tasks
    public class ProjectViewModel
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OwnerUserId { get; set; }
        public List<ColumnViewModel> Columns { get; set; } = new List<ColumnViewModel>();
    }

    // For creating a new project
    public class ProjectCreateDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Project name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    // For updating an existing project
    public class ProjectUpdateDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Project name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
