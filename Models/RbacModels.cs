using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    /// <summary>
    /// 6. Roles: System access roles (decoupled from job titles so an employee can hold multiple roles)
    /// </summary>
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(60)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// "Employee" or "HR"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("hierarchy_track")]
        public string HierarchyTrack { get; set; } = string.Empty;

        [MaxLength(300)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_system_role")]
        public bool IsSystemRole { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// 7. Permissions: Atomic Module + Action definitions (View, Create, Edit, Delete, Assign, Approve, Reject, Review, Export, Manage)
    /// </summary>
    [Table("permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Unique code, e.g. "Tasks.Assign", "Payroll.Approve"
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        [Column("module")]
        public string Module { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [MaxLength(250)]
        [Column("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 8. RolePermissions: Permissions assigned to a Role along with DataScope (Own, Team, Project, Department, Organization, Delegated)
    /// </summary>
    [Table("role_permissions")]
    public class RolePermission
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("role_id")]
        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        [Column("permission_id")]
        public Guid PermissionId { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission? Permission { get; set; }

        /// <summary>
        /// Visibility / operation scope: "Own", "Delegated", "Team", "Project", "Department", "Organization"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("scope")]
        public string Scope { get; set; } = "Own";
    }

    /// <summary>
    /// 9. EmployeeRoles: Many-to-many mapping of Employees to Roles
    /// </summary>
    [Table("employee_roles")]
    public class EmployeeRole
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Column("role_id")]
        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        /// <summary>
        /// Optional department scope for roles like Director or HR Business Partner
        /// </summary>
        [Column("assigned_department_id")]
        public Guid? AssignedDepartmentId { get; set; }

        [Column("is_primary")]
        public bool IsPrimary { get; set; } = true;

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 10. EmployeePermissionOverrides: Exceptional employee-specific permission grants or revocations
    /// </summary>
    [Table("employee_permission_overrides")]
    public class EmployeePermissionOverride
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Column("permission_id")]
        public Guid PermissionId { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission? Permission { get; set; }

        /// <summary>
        /// true = explicitly granted; false = explicitly revoked
        /// </summary>
        [Column("is_granted")]
        public bool IsGranted { get; set; } = true;

        [Required]
        [MaxLength(30)]
        [Column("scope")]
        public string Scope { get; set; } = "Own";

        [MaxLength(300)]
        [Column("reason")]
        public string? Reason { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

