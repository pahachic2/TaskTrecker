using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt; // Requires System.IdentityModel.Tokens.Jwt package
using System.Linq;
using System.Collections.Generic;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient; // To set default auth header

    public ApiAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt")));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
        _httpClient.DefaultRequestHeaders.Authorization = null; // Clear header on logout
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt))
            return new List<Claim>();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        
        // Extract claims. Add more as needed (e.g., roles, email)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty),
            new Claim(ClaimTypes.Name, token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty)
        };
        
        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").ToList();
        claims.AddRange(roleClaims);

        return claims;
    }
}
