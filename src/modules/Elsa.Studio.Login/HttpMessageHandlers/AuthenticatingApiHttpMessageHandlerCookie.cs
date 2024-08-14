using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Elsa.Studio.Login.HttpMessageHandlers;

public class AuthenticatingApiHttpMessageHandlerCookie(
    IRemoteBackendAccessor remoteBackendAccessor,
    IBlazorServiceAccessor blazorServiceAccessor)
    : DelegatingHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var jsRuntime = sp.GetRequiredService<IJSRuntime>();

        // Retrieve the authentication cookie from the browser
        try
        {
            var authCookie = await jsRuntime.InvokeAsync<string>("blazorExtensions.getCookie", cancellationToken,
                ".AspNet.SharedCookie");
            var idservCookie = await jsRuntime.InvokeAsync<string>("blazorExtensions.getCookie", cancellationToken,
                "idsrv.session");
            Console.WriteLine(authCookie);
            if (!string.IsNullOrEmpty(authCookie))
            {
                request.Headers.Add("Cookie", $".AspNet.SharedCookie={authCookie}");
            }

            if (!string.IsNullOrEmpty(idservCookie))
            {
                request.Headers.Add("Cookie", $"idsrv.session={authCookie}");
            }

            var cookieContainer = new System.Net.CookieContainer();

            cookieContainer.Add(new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority)),
                new System.Net.Cookie("idsrv.session", idservCookie));
            cookieContainer.Add(new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority)),
                new System.Net.Cookie(".AspNet.SharedCookie", authCookie));

            if (InnerHandler is HttpClientHandler httpClientHandler)
            {
                httpClientHandler.CookieContainer = cookieContainer;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        request.Headers.Add("CT", "somevalue");
        Console.WriteLine($"Added cookie to request headers: {request.Headers}");
        var response = await base.SendAsync(request, cancellationToken);

        // Handle unauthorized response
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Optionally handle token refresh or re-authentication logic here
            // For example, you can redirect to the login page or refresh the cookie

            // Retry the request once if necessary
            // authCookie = await RefreshAuthCookieAsync();
            // request.Headers.Remove("Cookie");
            // request.Headers.Add("Cookie", $".AspNetCore.Identity.Application={authCookie}");
            // response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}