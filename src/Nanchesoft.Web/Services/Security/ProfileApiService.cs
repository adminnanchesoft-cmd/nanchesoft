using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Security;

public sealed class ProfileApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;
    private readonly AuthService _authService;

    public ProfileApiService(IHttpClientFactory httpClientFactory, AuthState authState, AuthService authService)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
        _authService = authService;
    }

    public async Task<ProfileDto?> GetMyProfileAsync()
    {
        if (!_authState.UserId.HasValue) return null;

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.GetAsync($"/api/security/users/{_authState.UserId.Value:D}/profile");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProfileDto>();
    }

    public async Task<ProfileUpdateResult> UpdateAsync(UpdateProfileRequest request)
    {
        if (!_authState.UserId.HasValue)
            return new ProfileUpdateResult { Success = false, ErrorMessage = "Sesión no encontrada." };

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync(
            $"/api/security/users/{_authState.UserId.Value:D}/profile", request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await TryReadErrorMessageAsync(response);
            return new ProfileUpdateResult
            {
                Success = false,
                ErrorMessage = err ?? "No fue posible guardar los cambios."
            };
        }

        var payload = await response.Content.ReadFromJsonAsync<ProfileDto>();
        if (payload is null)
            return new ProfileUpdateResult { Success = false, ErrorMessage = "Respuesta inválida del servidor." };

        _authState.FirstName = payload.FirstName ?? string.Empty;
        _authState.LastName = payload.LastName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(payload.DisplayName))
            _authState.DisplayName = payload.DisplayName;
        _authState.NotifyStateChanged();
        await _authService.RefreshPersistedSessionAsync();

        return new ProfileUpdateResult
        {
            Success = true,
            Profile = payload
        };
    }

    public async Task<AvatarUploadResult> UploadAvatarAsync(Stream stream, string fileName, string contentType)
    {
        if (!_authState.UserId.HasValue)
            return new AvatarUploadResult { Success = false, ErrorMessage = "Sesión no encontrada." };

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await client.PostAsync(
            $"/api/security/users/{_authState.UserId.Value:D}/avatar", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await TryReadErrorMessageAsync(response);
            return new AvatarUploadResult
            {
                Success = false,
                ErrorMessage = err ?? "No fue posible subir la imagen."
            };
        }

        var payload = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>();
        if (payload is null)
            return new AvatarUploadResult { Success = false, ErrorMessage = "Respuesta inválida del servidor." };

        _authState.AvatarUrl = payload.AvatarUrl ?? string.Empty;
        _authState.NotifyStateChanged();
        await _authService.RefreshPersistedSessionAsync();

        return new AvatarUploadResult
        {
            Success = true,
            AvatarUrl = payload.AvatarUrl ?? string.Empty
        };
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return null;
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch
        {
        }
        return null;
    }
}

public sealed class ProfileDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
}

public sealed class ProfileUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ProfileDto? Profile { get; set; }
}

public sealed class AvatarUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

internal sealed class AvatarUploadResponse
{
    public string AvatarUrl { get; set; } = string.Empty;
}
