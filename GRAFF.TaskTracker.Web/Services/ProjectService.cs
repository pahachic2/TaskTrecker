using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GRAFF.TaskTracker.Web.Models; // Or equivalent path to your local DTOs

// Note: Authentication headers should be handled by the HttpClient instance
// configured in Program.cs with ApiAuthenticationStateProvider.

public interface IProjectService
{
    Task<List<ProjectViewModel>?> GetProjects();
    Task<ProjectViewModel?> GetProject(int projectId);
    Task<ProjectViewModel?> CreateProject(ProjectCreateDto project);
    Task<bool> UpdateProject(int projectId, ProjectUpdateDto project);
    Task<bool> DeleteProject(int projectId);
}

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProjectViewModel>?> GetProjects()
    {
        // The request URI is relative to the HttpClient's BaseAddress
        return await _httpClient.GetFromJsonAsync<List<ProjectViewModel>?>("api/projects");
    }

    public async Task<ProjectViewModel?> GetProject(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<ProjectViewModel?>($"api/projects/{projectId}");
    }

    public async Task<ProjectViewModel?> CreateProject(ProjectCreateDto project)
    {
        var response = await _httpClient.PostAsJsonAsync("api/projects", project);
        if (response.IsSuccessStatusCode)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ProjectViewModel?>();
            }
            catch (System.Text.Json.JsonException ex)
            {
                // Log or handle deserialization error
                System.Console.WriteLine($"Error deserializing project: {ex.Message}");
                return null;
            }
        }
        // Consider logging response.ReasonPhrase or response.Content here for more details
        return null;
    }

    public async Task<bool> UpdateProject(int projectId, ProjectUpdateDto project)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/projects/{projectId}", project);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProject(int projectId)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{projectId}");
        return response.IsSuccessStatusCode;
    }
}
