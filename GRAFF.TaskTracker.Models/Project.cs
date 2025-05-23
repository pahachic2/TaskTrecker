using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GRAFF.TaskTracker.Models
{
    public class Project
    {
        public int ProjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int OwnerUserId { get; set; }
        [ForeignKey("OwnerUserId")]
        public User? Owner { get; set; }

        public ICollection<Column> Columns { get; set; } = new List<Column>();
    }
}
