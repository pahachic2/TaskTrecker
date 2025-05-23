using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Blazored.LocalStorage;
// Assuming DTOs will be created locally in GRAFF.TaskTracker.Web/Models
using GRAFF.TaskTracker.Web.Models; 
using Microsoft.AspNetCore.Components.Authorization;
using System.Collections.Generic; 
using System.Text.Json; 
using System; // Required for DateTime


public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient httpClient,
                       AuthenticationStateProvider authenticationStateProvider,
                       ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResult> Register(RegisterModel registerModel)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerModel);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            // Deserialize error to provide more details if possible
            return new AuthResult { Successful = false, ErrorMessage = $"Registration failed: {response.ReasonPhrase}. Details: {errorContent}" };
        }
        return new AuthResult { Successful = true };
    }

    public async Task<AuthResult> Login(LoginModel loginModel)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);
        if (!response.IsSuccessStatusCode)
        {
             var errorContent = await response.Content.ReadAsStringAsync();
            return new AuthResult { Successful = false, ErrorMessage = $"Login failed: {response.ReasonPhrase}. Details: {errorContent}" };
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
        if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
        {
             return new AuthResult { Successful = false, ErrorMessage = "Invalid token response." };
        }

        await _localStorage.SetItemAsync("authToken", authResponse.Token);
        ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(authResponse.Token);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return new AuthResult { Successful = true };
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}

public interface IAuthService
{
    Task<AuthResult> Register(RegisterModel registerModel);
    Task<AuthResult> Login(LoginModel loginModel);
    Task Logout();
}

public class AuthResult
{
    public bool Successful { get; set; }
    public string? ErrorMessage { get; set; }
    // public AuthResponseModel? Data {get; set;} // Optionally return data
}
