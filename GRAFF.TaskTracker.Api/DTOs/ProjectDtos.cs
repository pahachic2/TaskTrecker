using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GRAFF.TaskTracker.Api.DTOs
{
    public class ProjectCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class ProjectUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class ProjectDto // For returning project details
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OwnerUserId { get; set; }
        public List<ColumnDto> Columns { get; set; } = new List<ColumnDto>(); // Include columns
    }
}
