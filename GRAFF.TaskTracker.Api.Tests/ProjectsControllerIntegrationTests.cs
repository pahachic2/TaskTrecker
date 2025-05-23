using System.Net;
using System.Net.Http;
using System.Net.Http.Headers; // For AuthenticationHeaderValue
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GRAFF.TaskTracker.Api.DTOs; // Your DTOs
// using GRAFF.TaskTracker.Models; // Not directly used here, ProjectDto is from Api.DTOs
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
// using FluentAssertions; // Not using for now
using System.Collections.Generic; // For List

// If Program is in the global namespace for GRAFF.TaskTracker.Api
// and CustomWebApplicationFactory is in GRAFF.TaskTracker.Api.Tests
// using GRAFF.TaskTracker.Api; // This might be needed if Program is not found otherwise


namespace GRAFF.TaskTracker.Api.Tests
{
    public class ProjectsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>> 
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ProjectsControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private async Task<string> GetValidTestToken()
        {
            // Register a test user
            var registerDto = new RegisterModel 
            { 
                Username = $"testuser_{Guid.NewGuid()}", 
                Email = $"test_{Guid.NewGuid()}@example.com", 
                Password = "Password123!" 
            };
            
            // Use a fresh client for auth operations to avoid interference or ensure clean state
            var authClient = _factory.CreateClient();
            var regResponse = await authClient.PostAsJsonAsync("/api/auth/register", registerDto);
            
            if (!regResponse.IsSuccessStatusCode)
            {
                var errorContent = await regResponse.Content.ReadAsStringAsync();
                throw new Exception($"Registration failed: {regResponse.StatusCode} - {errorContent}");
            }
            regResponse.EnsureSuccessStatusCode();

            // Login to get token
            var loginDto = new LoginModel { Username = registerDto.Username, Password = registerDto.Password };
            var response = await authClient.PostAsJsonAsync("/api/auth/login", loginDto);
            
            if (!response.IsSuccessStatusCode)
            {
                 var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {response.StatusCode} - {errorContent}");
            }
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
            {
                throw new Exception("Failed to retrieve a valid token from login response.");
            }
            return authResponse.Token;
        }

        [Fact]
        public async Task PostProject_WithValidData_ShouldCreateProject()
        {
            // Arrange
            var token = await GetValidTestToken();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var projectCreateDto = new ProjectCreateDto { Name = "Test Project 1" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/projects", projectCreateDto);

            // Assert
            response.EnsureSuccessStatusCode(); 
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var createdProject = await response.Content.ReadFromJsonAsync<ProjectDto>();
            Assert.NotNull(createdProject);
            Assert.Equal(projectCreateDto.Name, createdProject.Name);
            Assert.True(createdProject.ProjectId > 0);

            // Optional: Verify it's in the database via a GET request
            var getResponse = await _client.GetAsync($"/api/projects/{createdProject.ProjectId}");
            getResponse.EnsureSuccessStatusCode();
            var fetchedProject = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
            Assert.NotNull(fetchedProject);
            Assert.Equal(projectCreateDto.Name, fetchedProject.Name);
        }

        [Fact]
        public async Task GetProjects_WhenNoProjects_ShouldReturnEmptyList()
        {
            // Arrange
            // Each test runs with a new in-memory database due to Guid.NewGuid() in factory
            // and GetValidTestToken creates a new user each time.
            var token = await GetValidTestToken(); 
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/projects");

            // Assert
            response.EnsureSuccessStatusCode();
            var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
            Assert.NotNull(projects);
            Assert.Empty(projects);
        }
    }
}
