using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    /// <summary>
    /// 11. Teams: Team information within a Department
    /// </summary>
    [Table("teams")]
    public class Team
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column("code")]
        public string? Code { get; set; }

        [Column("department_id")]
        public Guid? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Column("lead_employee_id")]
        public Guid? LeadEmployeeId { get; set; }

        [ForeignKey(nameof(LeadEmployeeId))]
        public Employee? LeadEmployee { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    }

    /// <summary>
    /// 12. TeamMembers: Employee-team relationships
    /// </summary>
    [Table("team_members")]
    public class TeamMember
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("team_id")]
        public Guid TeamId { get; set; }

        [ForeignKey(nameof(TeamId))]
        public Team? Team { get; set; }

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [MaxLength(60)]
        [Column("role_in_team")]
        public string? RoleInTeam { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 13. Projects: Project information
    /// </summary>
    [Table("projects")]
    public class Project
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(120)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("department_id")]
        public Guid? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Column("project_manager_id")]
        public Guid? ProjectManagerId { get; set; }

        [Column("product_manager_id")]
        public Guid? ProductManagerId { get; set; }

        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "In Progress";

        [MaxLength(80)]
        [Column("category")]
        public string Category { get; set; } = "Core Web Platform";

        [MaxLength(20)]
        [Column("priority")]
        public string Priority { get; set; } = "High";

        [Column("progress_percent")]
        public int ProgressPercent { get; set; } = 25;

        [MaxLength(50)]
        [Column("budget")]
        public string Budget { get; set; } = "$75,000";

        [MaxLength(50)]
        [Column("methodology")]
        public string Methodology { get; set; } = "Agile Scrum";

        [Column("milestones_json")]
        public string? MilestonesJson { get; set; }

        [MaxLength(300)]
        [Column("external_tool_url")]
        public string? ExternalToolUrl { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    }

    /// <summary>
    /// 14. ProjectMembers: Project membership (an employee can belong to multiple projects without changing designation)
    /// </summary>
    [Table("project_members")]
    public class ProjectMember
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [MaxLength(60)]
        [Column("role_in_project")]
        public string? RoleInProject { get; set; }

        [Column("allocation_percentage")]
        public int AllocationPercentage { get; set; } = 100;

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}

