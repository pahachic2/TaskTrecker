using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GRAFF.TaskTracker.Models
{
    public class User : IdentityUser<int> // Inherit from IdentityUser<int>
    {
        // Properties like UserId (as Id), UserName, Email, PasswordHash are inherited.
        // UserId is inherited as Id. We'll rely on this.
        // Username, Email, PasswordHash are inherited.

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
