using Microsoft.EntityFrameworkCore;
using Nanchesoft.Api.Auth;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Domain.Enums;
using Nanchesoft.Persistence.Context;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Nanchesoft.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/users").WithTags("Users");

        group.MapGet("/", GetUsersAsync);
        group.MapGet("/tenants", GetTenantsAsync);
        group.MapGet("/roles", GetRolesAsync);
        group.MapPost("/", CreateUserAsync);
        group.MapPost("/{id:guid}/avatar", UploadAvatarAsync).DisableAntiforgery();
        group.MapGet("/{id:guid}/profile", GetOwnProfileAsync);
        group.MapPut("/{id:guid}", UpdateUserAsync);
        group.MapPut("/{id:guid}/profile", UpdateOwnProfileAsync);
        group.MapDelete("/{id:guid}", DeleteUserAsync);

        return app;
    }

    private static async Task<IResult> GetUsersAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Users
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var users = await query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync();
        var result = users.Select(x =>
        {
            var selectedRole = x.UserRoles
                .OrderByDescending(y => y.AssignedAt)
                .Select(y => y.Role)
                .FirstOrDefault(y => y is not null);

            return new UserListItemDto
            {
                UserId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant?.Name ?? string.Empty,
                Username = x.Username,
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                FullName = x.GetDisplayName(),
                Phone = x.Phone ?? string.Empty,
                RoleId = selectedRole?.Id,
                RoleName = selectedRole?.Name ?? string.Empty,
                MustChangePassword = x.MustChangePassword,
                IsLocked = x.IsLocked,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt
            };
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetTenantsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Tenants.AsNoTracking().Where(x => x.IsActive).AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.Id == tenantId.Value);

        var tenants = await query.OrderBy(x => x.Name).Select(x => new UserTenantLookupDto
        {
            TenantId = x.Id,
            TenantName = x.Name
        }).ToListAsync();

        return Results.Ok(tenants);
    }

    private static async Task<IResult> GetRolesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Roles.AsNoTracking().Include(x => x.Tenant).Where(x => x.IsActive).AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var roles = await query.OrderBy(x => x.Name).Select(x => new UserRoleLookupDto
        {
            RoleId = x.Id,
            RoleName = x.Name,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty
        }).ToListAsync();

        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateUserAsync(HttpContext httpContext, CreateOrUpdateUserRequest request, NanchesoftDbContext db, IPasswordHasher passwordHasher)
    {
        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue)
            request.TenantId = tenantScopeId;

        if (!request.TenantId.HasValue || request.TenantId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "El tenant es obligatorio." });

        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId.Value);
        if (tenant is null)
            return Results.BadRequest(new { message = "No se encontró el tenant enviado." });

        var username = (request.Username ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Results.BadRequest(new { message = "Usuario, correo, nombre y apellidos son obligatorios." });

        var duplicate = await db.Users.AnyAsync(x => x.TenantId == tenant.Id && (x.Username == username || x.Email == email));
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe un usuario con el mismo username o correo dentro del tenant." });

        Role? selectedRole = null;
        if (request.RoleId.HasValue && request.RoleId.Value != Guid.Empty)
        {
            selectedRole = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId.Value && x.TenantId == tenant.Id);
            if (selectedRole is null)
                return Results.BadRequest(new { message = "No se encontró el rol enviado para el tenant seleccionado." });
        }

        var user = new User
        {
            TenantId = tenant.Id,
            Username = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            PasswordHash = passwordHasher.Hash("Admin123*"),
            MustChangePassword = request.MustChangePassword,
            IsLocked = request.IsLocked,
            IsActive = request.IsActive,
            Status = ResolveStatus(request.IsActive, request.IsLocked),
            CreatedBy = "web-api"
        };

        db.Users.Add(user);
        if (selectedRole is not null)
        {
            db.UserRoles.Add(new UserRole
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                RoleId = selectedRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "web-api",
                CreatedBy = "web-api"
            });
        }

        await db.SaveChangesAsync();

        return Results.Ok(new UserListItemDto
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.GetDisplayName(),
            Phone = user.Phone ?? string.Empty,
            RoleId = selectedRole?.Id,
            RoleName = selectedRole?.Name ?? string.Empty,
            MustChangePassword = user.MustChangePassword,
            IsLocked = user.IsLocked,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        });
    }

    private static async Task<IResult> UpdateUserAsync(HttpContext httpContext, Guid id, CreateOrUpdateUserRequest request, NanchesoftDbContext db)
    {
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return Results.NotFound(new { message = "No se encontró el usuario." });

        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue)
        {
            if (user.TenantId != tenantScopeId.Value)
                return Results.StatusCode(403);
            request.TenantId = tenantScopeId;
        }

        var tenantId = request.TenantId.HasValue && request.TenantId.Value != Guid.Empty ? request.TenantId.Value : user.TenantId;
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null)
            return Results.BadRequest(new { message = "No se encontró el tenant del usuario." });

        var username = string.IsNullOrWhiteSpace(request.Username) ? user.Username : request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? user.Email : request.Email.Trim().ToLowerInvariant();
        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? user.FirstName : request.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(request.LastName) ? user.LastName : request.LastName.Trim();
        var phone = request.Phone is null ? user.Phone : (string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim());

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Results.BadRequest(new { message = "Usuario, correo, nombre y apellidos son obligatorios." });

        var duplicate = await db.Users.AnyAsync(x => x.Id != id && x.TenantId == tenant.Id && (x.Username == username || x.Email == email));
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe otro usuario con el mismo username o correo dentro del tenant." });

        Role? selectedRole = null;
        if (request.RoleId.HasValue && request.RoleId.Value != Guid.Empty)
        {
            selectedRole = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId.Value && x.TenantId == tenant.Id);
            if (selectedRole is null)
                return Results.BadRequest(new { message = "No se encontró el rol enviado para el tenant seleccionado." });
        }

        user.TenantId = tenant.Id;
        user.Username = username;
        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.Phone = phone;
        user.MustChangePassword = request.MustChangePassword;
        user.IsLocked = request.IsLocked;
        user.IsActive = request.IsActive;
        user.Status = ResolveStatus(request.IsActive, request.IsLocked);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = "web-api";

        db.UserRoles.RemoveRange(user.UserRoles);
        if (selectedRole is not null)
        {
            db.UserRoles.Add(new UserRole
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                RoleId = selectedRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "web-api",
                CreatedBy = "web-api"
            });
        }

        await db.SaveChangesAsync();

        return Results.Ok(new UserListItemDto
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.GetDisplayName(),
            Phone = user.Phone ?? string.Empty,
            RoleId = selectedRole?.Id,
            RoleName = selectedRole?.Name ?? string.Empty,
            MustChangePassword = user.MustChangePassword,
            IsLocked = user.IsLocked,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        });
    }

    private static async Task<IResult> DeleteUserAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var user = await db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return Results.NotFound(new { message = "No se encontró el usuario." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue && user.TenantId != tenantId.Value)
            return Results.StatusCode(403);

        var sessions = await db.UserSessions.Where(x => x.UserId == user.Id).ToListAsync();
        var accessLogs = await db.AccessLogs.Where(x => x.UserId == user.Id).ToListAsync();
        db.UserRoles.RemoveRange(user.UserRoles);
        db.UserSessions.RemoveRange(sessions);
        db.AccessLogs.RemoveRange(accessLogs);
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetOwnProfileAsync(
        Guid id,
        HttpContext httpContext,
        NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return Results.NotFound(new { message = "Usuario no encontrado." });
        if (!isPlatformOwner && tenantId.HasValue && user.TenantId != tenantId.Value)
            return Results.NotFound(new { message = "Usuario no encontrado." });

        return Results.Ok(new
        {
            userId = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            displayName = user.GetDisplayName(),
            email = user.Email,
            phone = user.Phone ?? string.Empty,
            birthDate = user.BirthDate,
            avatarUrl = user.AvatarUrl ?? string.Empty
        });
    }

    private static async Task<IResult> UpdateOwnProfileAsync(
        Guid id,
        UpdateOwnProfileRequest request,
        HttpContext httpContext,
        NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return Results.NotFound(new { message = "Usuario no encontrado." });
        if (!isPlatformOwner && tenantId.HasValue && user.TenantId != tenantId.Value)
            return Results.NotFound(new { message = "Usuario no encontrado." });

        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Results.BadRequest(new { message = "Nombre y apellido son obligatorios." });

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.BirthDate = request.BirthDate;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            firstName = user.FirstName,
            lastName = user.LastName,
            phone = user.Phone ?? string.Empty,
            birthDate = user.BirthDate,
            displayName = user.GetDisplayName()
        });
    }

    private static async Task<IResult> UploadAvatarAsync(
        Guid id,
        IFormFile file,
        HttpContext httpContext,
        NanchesoftDbContext db,
        IConfiguration configuration)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "No se recibió ninguna imagen." });

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return Results.BadRequest(new { message = "La imagen no debe superar 5 MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return Results.BadRequest(new { message = "Formato no permitido. Usa JPG, PNG o WEBP." });

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return Results.NotFound(new { message = "Usuario no encontrado." });
        if (!isPlatformOwner && tenantId.HasValue && user.TenantId != tenantId.Value)
            return Results.NotFound(new { message = "Usuario no encontrado." });

        var root = configuration["Uploads:RootPath"] ?? "/opt/nanchesoft/uploads";
        var usersDir = Path.Combine(root, "users");
        Directory.CreateDirectory(usersDir);
        var filePath = Path.Combine(usersDir, $"{id}.jpg");

        using (var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream()))
        {
            var size = Math.Min(image.Width, image.Height);
            var x = (image.Width - size) / 2;
            var y = (image.Height - size) / 2;
            image.Mutate(ctx => ctx
                .Crop(new SixLabors.ImageSharp.Rectangle(x, y, size, size))
                .Resize(256, 256));
            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 };
            await image.SaveAsJpegAsync(filePath, encoder);
        }

        user.AvatarUrl = $"/uploads/users/{id}.jpg";
        await db.SaveChangesAsync();

        return Results.Ok(new { avatarUrl = user.AvatarUrl });
    }

    private static UserStatus ResolveStatus(bool isActive, bool isLocked)
    {
        if (!isActive)
            return UserStatus.Inactive;
        return isLocked ? UserStatus.Locked : UserStatus.Active;
    }
}

public sealed class UserListItemDto
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class UserTenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class UserRoleLookupDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateUserRequest
{
    public Guid? TenantId { get; set; }
    public Guid? RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateOwnProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
}
