using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    /// <summary>
    /// 15. ApprovalWorkflows: Configurable approval workflows independent of designation levels
    /// </summary>
    [Table("approval_workflows")]
    public class ApprovalWorkflow
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// e.g. "Leave", "WorkLogs", "Offers", "Payroll"
        /// </summary>
        [Required]
        [MaxLength(60)]
        [Column("module")]
        public string Module { get; set; } = string.Empty;

        [MaxLength(300)]
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional rule filter, e.g. {"leaveType":"Annual","minDays":3}
        /// </summary>
        [Column("condition_json")]
        public string? ConditionJson { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    }

    /// <summary>
    /// 16. ApprovalSteps: Individual approval stages in an ApprovalWorkflow
    /// </summary>
    [Table("approval_steps")]
    public class ApprovalStep
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("workflow_id")]
        public Guid WorkflowId { get; set; }

        [ForeignKey(nameof(WorkflowId))]
        public ApprovalWorkflow? Workflow { get; set; }

        [Column("step_order")]
        public int StepOrder { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("step_name")]
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// "TeamLead", "ReportingManager", "ProjectManager", "DepartmentManager", "SpecificRole"
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("approver_type")]
        public string ApproverType { get; set; } = string.Empty;

        [Column("required_role_id")]
        public Guid? RequiredRoleId { get; set; }

        [ForeignKey(nameof(RequiredRoleId))]
        public Role? RequiredRole { get; set; }

        /// <summary>
        /// "Recommend", "Process", "Approve"
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("action_type")]
        public string ActionType { get; set; } = "Approve";

        [Column("allow_delegation")]
        public bool AllowDelegation { get; set; } = true;

        [Column("is_mandatory")]
        public bool IsMandatory { get; set; } = true;
    }

    /// <summary>
    /// 17. ApprovalHistory: Audit trail of recommendations, reviews, and approvals
    /// </summary>
    [Table("approval_history")]
    public class ApprovalHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("workflow_id")]
        public Guid WorkflowId { get; set; }

        [ForeignKey(nameof(WorkflowId))]
        public ApprovalWorkflow? Workflow { get; set; }

        [Required]
        [MaxLength(60)]
        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [Column("entity_id")]
        public Guid EntityId { get; set; }

        [Column("step_order")]
        public int StepOrder { get; set; }

        [Column("acted_by_employee_id")]
        public Guid? ActedByEmployeeId { get; set; }

        [ForeignKey(nameof(ActedByEmployeeId))]
        public Employee? ActedByEmployee { get; set; }

        /// <summary>
        /// "Pending", "Recommended", "Approved", "Rejected"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        [Column("comments")]
        public string? Comments { get; set; }

        [Column("acted_at")]
        public DateTime ActedAt { get; set; } = DateTime.UtcNow;
    }
}

