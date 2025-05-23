using System;
using System.ComponentModel.DataAnnotations;

namespace GRAFF.TaskTracker.Web.Models // Changed from Api.DTOs to Web.Models
{
    public class LoginModel 
    { 
        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; } 

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; } 
    }

    public class RegisterModel 
    { 
        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; } 

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string? Email { get; set; } 

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; } 
    }

    public class AuthResponseModel 
    { 
        public string? Token { get; set; } 
        public DateTime Expiration { get; set; } 
        public string? Username { get; set; } 
        public string? Email { get; set; } 
        public int UserId {get; set;} 
    }
}
