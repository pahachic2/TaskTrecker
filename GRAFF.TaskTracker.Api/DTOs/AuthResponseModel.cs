using System;

namespace GRAFF.TaskTracker.Api.DTOs
{
    public class AuthResponseModel
    {
        public string? Token { get; set; }
        public DateTime Expiration { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public int UserId { get; set; }
    }
}
