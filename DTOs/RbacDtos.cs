using System.ComponentModel.DataAnnotations;

namespace WorkSphere.API.DTOs
{
    public class CreateDepartmentDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public Guid? ParentDepartmentId { get; set; }
    }

    public class UpdateDepartmentDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public Guid? HeadEmployeeId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CreateDesignationDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid OrganizationalLevelId { get; set; }

        [Required]
        [MaxLength(30)]
        public string HierarchyTrack { get; set; } = "Employee";

        [MaxLength(150)]
        public string? TypicalReportingTo { get; set; }

        [MaxLength(60)]
        public string? DefaultRoleCode { get; set; }

        [MaxLength(500)]
        public string? Responsibilities { get; set; }
    }

    public class UpdateDesignationDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? TypicalReportingTo { get; set; }

        [MaxLength(60)]
        public string? DefaultRoleCode { get; set; }

        [MaxLength(500)]
        public string? Responsibilities { get; set; }
    }

    public class CreateRoleDto
    {
        [Required]
        [MaxLength(60)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string HierarchyTrack { get; set; } = "Employee";

        [MaxLength(300)]
        public string? Description { get; set; }
    }

    public class UpdateRoleDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }
    }
}

