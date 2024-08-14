using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Provides the authentication state for the current user based on an access token.
/// </summary>
public class AccessTokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public AccessTokenAuthenticationStateProvider(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await GetCurrentUserAsync();
        
        if (user == null || !user.Identity.IsAuthenticated)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        return new AuthenticationState(user);
    }

    private async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<UserInfo>("https://local.zentsocloud.com:5002/api/account/user");

            if (result == null || !result.IsAuthenticated)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var claims = result.Claims.Select(c => new Claim(c.Type, c.Value));
            var identity = new ClaimsIdentity(claims, "Identity.Application");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }


    /// <summary>
    /// Notifies the authentication state has changed.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

public class UserInfo
{
    public bool IsAuthenticated { get; set; }
    public IEnumerable<ClaimDto> Claims { get; set; }
}

public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}