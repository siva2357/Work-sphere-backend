using System.ComponentModel.DataAnnotations;

namespace WorkSphere.API.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional CompanyCode (e.g. "CMP-001" or "CMP-002"). If specified, verifies the user belongs to that company.
        /// </summary>
        public string? CompanyCode { get; set; }
    }

    public class EffectiveModulePermissionDto
    {
        public string Module { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public Dictionary<string, string> ActionScopes { get; set; } = new();
        public string HighestScope { get; set; } = "Own";
    }

    public class EmployeeInfoDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyDomain { get; set; }
        public bool IsMainHr { get; set; }
        public string PortalType { get; set; } = string.Empty;
        public string? HierarchyTrack { get; set; }
        public string? LevelCode { get; set; }
        public string? LevelName { get; set; }
        public string? DesignationTitle { get; set; }
        public string? DepartmentName { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<EffectiveModulePermissionDto> Permissions { get; set; } = new();
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public EmployeeInfoDto User { get; set; } = new();
    }

    public class UpdateRolePermissionsRequestDto
    {
        public List<RolePermissionItemDto> Permissions { get; set; } = new();
    }

    public class RolePermissionItemDto
    {
        [Required]
        public string PermissionCode { get; set; } = string.Empty;

        [Required]
        public string Scope { get; set; } = "Own";
    }

    public class UpsertPermissionOverrideRequestDto
    {
        [Required]
        public string PermissionCode { get; set; } = string.Empty;

        public bool IsGranted { get; set; } = true;

        [Required]
        public string Scope { get; set; } = "Own";

        public string? Reason { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
