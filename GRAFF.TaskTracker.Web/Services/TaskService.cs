using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GRAFF.TaskTracker.Web.Models; // Local DTOs: TaskViewModel, TaskCreateDto, etc.

public interface ITaskService 
{
    Task<List<TaskViewModel>?> GetTasksForColumn(int columnId);
    Task<TaskViewModel?> GetTask(int taskId);
    Task<TaskViewModel?> CreateTask(int columnId, TaskCreateDto task);
    Task<bool> UpdateTask(int taskId, TaskUpdateDto task);
    Task<bool> DeleteTask(int taskId);
    Task<bool> MoveTask(int taskId, TaskMoveDto moveDto);
    Task<bool> ReorderTasks(int columnId, TaskOrderUpdateDto orderDto);
}
public class TaskService : ITaskService
{
    private readonly HttpClient _httpClient;
    public TaskService(HttpClient httpClient) { _httpClient = httpClient; }

    public async Task<List<TaskViewModel>?> GetTasksForColumn(int columnId)
    {
        return await _httpClient.GetFromJsonAsync<List<TaskViewModel>?>($"api/columns/{columnId}/tasks");
    }
    public async Task<TaskViewModel?> GetTask(int taskId)
    {
        return await _httpClient.GetFromJsonAsync<TaskViewModel?>($"api/tasks/{taskId}");
    }
    public async Task<TaskViewModel?> CreateTask(int columnId, TaskCreateDto task)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/columns/{columnId}/tasks", task);
         if (response.IsSuccessStatusCode)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<TaskViewModel?>();
            }
            catch(System.Text.Json.JsonException ex)
            {
                System.Console.WriteLine($"Error deserializing task: {ex.Message}");
                return null;
            }
        }
        return null;
    }
    public async Task<bool> UpdateTask(int taskId, TaskUpdateDto task)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tasks/{taskId}", task);
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> DeleteTask(int taskId)
    {
        var response = await _httpClient.DeleteAsync($"api/tasks/{taskId}");
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> MoveTask(int taskId, TaskMoveDto moveDto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tasks/{taskId}/move", moveDto);
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> ReorderTasks(int columnId, TaskOrderUpdateDto orderDto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/columns/{columnId}/tasks/reorder", orderDto);
        return response.IsSuccessStatusCode;
    }
}
