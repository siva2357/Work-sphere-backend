using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    /// <summary>
    /// 0. Companies: Registered tenant companies fetched/synced from the External Main Website.
    /// Individual users cannot sign up directly; a company registers on the external main website,
    /// receives Main HR access, and Main HR creates employees inside the Employee Directory.
    /// </summary>
    [Table("companies")]
    public class Company
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("company_code")]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        [Column("external_registration_id")]
        public string ExternalRegistrationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("domain")]
        public string Domain { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("industry")]
        public string Industry { get; set; } = "Enterprise Software & IT";

        [MaxLength(50)]
        [Column("company_size")]
        public string CompanySize { get; set; } = "250-500 Employees";

        [MaxLength(150)]
        [Column("headquarters")]
        public string Headquarters { get; set; } = "Hyderabad / Bangalore, India";

        [MaxLength(250)]
        [Column("external_website_url")]
        public string ExternalWebsiteUrl { get; set; } = "https://worksphere.com/companies";

        [MaxLength(50)]
        [Column("registration_source")]
        public string RegistrationSource { get; set; } = "EXTERNAL_MAIN_WEBSITE";

        [MaxLength(50)]
        [Column("subscription_plan")]
        public string SubscriptionPlan { get; set; } = "Enterprise Suite";

        [Required]
        [MaxLength(150)]
        [Column("main_hr_email")]
        public string MainHrEmail { get; set; } = string.Empty;

        [MaxLength(120)]
        [Column("main_hr_name")]
        public string MainHrName { get; set; } = string.Empty;

        [Column("main_hr_employee_id")]
        public Guid? MainHrEmployeeId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("registered_at")]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    /// <summary>
    /// 1. Company Departments (Engineering, Product, Human Resources, Finance, etc.)
    /// </summary>
    [Table("departments")]
    public class Department
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(300)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("head_employee_id")]
        public Guid? HeadEmployeeId { get; set; }

        [Column("parent_department_id")]
        public Guid? ParentDepartmentId { get; set; }

        [ForeignKey(nameof(ParentDepartmentId))]
        public Department? ParentDepartment { get; set; }

        public ICollection<Department> SubDepartments { get; set; } = new List<Department>();

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 2. Organizational Levels: L1-L10 (Employee track) and H1-H6 (HR track)
    /// </summary>
    [Table("organizational_levels")]
    public class OrganizationalLevel
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// e.g. "L1".."L10" or "H1".."H6"
        /// </summary>
        [Required]
        [MaxLength(15)]
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

        /// <summary>
        /// Numeric seniority rank within track (1 = top executive, 10 = entry level)
        /// </summary>
        [Column("rank_order")]
        public int RankOrder { get; set; }

        [MaxLength(300)]
        [Column("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 3. Designations: What position the employee holds (CTO, Developer, HR Manager, etc.)
    /// Separated from OrganizationalLevel and System Role.
    /// </summary>
    [Table("designations")]
    public class Designation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("organizational_level_id")]
        public Guid OrganizationalLevelId { get; set; }

        [ForeignKey(nameof(OrganizationalLevelId))]
        public OrganizationalLevel? OrganizationalLevel { get; set; }

        /// <summary>
        /// "Employee" or "HR"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("hierarchy_track")]
        public string HierarchyTrack { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("typical_reporting_to")]
        public string? TypicalReportingTo { get; set; }

        [MaxLength(60)]
        [Column("default_role_code")]
        public string? DefaultRoleCode { get; set; }

        [MaxLength(500)]
        [Column("responsibilities")]
        public string? Responsibilities { get; set; }
    }

    /// <summary>
    /// 4. Employees: Core identity and organizational placement
    /// </summary>
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("employee_code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Main portal access: "hr" or "employee"
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("portal_type")]
        public string PortalType { get; set; } = "employee";

        [Column("company_id")]
        public Guid? CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }

        /// <summary>
        /// True if this account is the Primary/Main HR Administrator provisioned when the Company registered.
        /// Only Main HR / authorized HR users can create new employees in the Employee Directory.
        /// </summary>
        [Column("is_main_hr")]
        public bool IsMainHr { get; set; } = false;

        [Column("department_id")]
        public Guid? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Column("designation_id")]
        public Guid? DesignationId { get; set; }

        [ForeignKey(nameof(DesignationId))]
        public Designation? Designation { get; set; }

        [Column("organizational_level_id")]
        public Guid? OrganizationalLevelId { get; set; }

        [ForeignKey(nameof(OrganizationalLevelId))]
        public OrganizationalLevel? OrganizationalLevel { get; set; }

        [MaxLength(30)]
        [Column("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// "Full-time", "Contract", "Intern" (used in HR Employee Directory)
        /// </summary>
        [MaxLength(30)]
        [Column("employment_type")]
        public string EmploymentType { get; set; } = "Full-time";

        /// <summary>
        /// "Hyderabad HQ", "Bangalore, India", "Remote", etc.
        /// </summary>
        [MaxLength(100)]
        [Column("work_location")]
        public string WorkLocation { get; set; } = "Bangalore, India";

        /// <summary>
        /// "Active", "On Leave", "Remote"
        /// </summary>
        [MaxLength(30)]
        [Column("work_status")]
        public string WorkStatus { get; set; } = "Active";

        [Column("created_by_employee_id")]
        public Guid? CreatedByEmployeeId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();
        public ICollection<EmployeePermissionOverride> PermissionOverrides { get; set; } = new List<EmployeePermissionOverride>();
    }

    /// <summary>
    /// 5. ReportingRelationships: Who the employee reports to (Primary, Project, Technical, Mentor)
    /// </summary>
    [Table("reporting_relationships")]
    public class ReportingRelationship
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Column("manager_employee_id")]
        public Guid ManagerEmployeeId { get; set; }

        [ForeignKey(nameof(ManagerEmployeeId))]
        public Employee? Manager { get; set; }

        /// <summary>
        /// "Primary", "Project", "Technical", "Mentor"
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("relationship_type")]
        public string RelationshipType { get; set; } = "Primary";

        [Column("is_primary")]
        public bool IsPrimary { get; set; } = true;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("effective_from")]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    }
}

