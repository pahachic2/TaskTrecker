using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GRAFF.TaskTracker.Web.Models; // Local DTOs: ColumnViewModel, ColumnCreateDto, etc.

public interface IColumnService 
{
    Task<List<ColumnViewModel>?> GetColumns(int projectId);
    Task<ColumnViewModel?> CreateColumn(int projectId, ColumnCreateDto column);
    Task<bool> UpdateColumn(int projectId, int columnId, ColumnUpdateDto column);
    Task<bool> DeleteColumn(int projectId, int columnId);
    Task<bool> ReorderColumns(int projectId, ColumnOrderUpdateDto Dto);
}

public class ColumnService : IColumnService
{
    private readonly HttpClient _httpClient;
    public ColumnService(HttpClient httpClient) { _httpClient = httpClient; }

    public async Task<List<ColumnViewModel>?> GetColumns(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<List<ColumnViewModel>?>($"api/projects/{projectId}/columns");
    }
    public async Task<ColumnViewModel?> CreateColumn(int projectId, ColumnCreateDto column)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/projects/{projectId}/columns", column);
        if (response.IsSuccessStatusCode)
        {
            try 
            {
                return await response.Content.ReadFromJsonAsync<ColumnViewModel?>();
            }
            catch(System.Text.Json.JsonException ex)
            {
                System.Console.WriteLine($"Error deserializing column: {ex.Message}");
                return null;
            }
        }
        return null;
    }
    public async Task<bool> UpdateColumn(int projectId, int columnId, ColumnUpdateDto column)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/projects/{projectId}/columns/{columnId}", column);
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> DeleteColumn(int projectId, int columnId)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{projectId}/columns/{columnId}");
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> ReorderColumns(int projectId, ColumnOrderUpdateDto Dto)
    {
         var response = await _httpClient.PutAsJsonAsync($"api/projects/{projectId}/columns/reorder", Dto);
        return response.IsSuccessStatusCode;
    }
}
