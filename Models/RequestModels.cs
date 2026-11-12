using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    public static class RequestCategories
    {
        public const string LeaveRequest = "Leave Request";
        public const string ResignationRequest = "Resignation Request";
        public const string HikeRequest = "Appraisal & Salary Review";
        public const string PromotionRequest = "Promotion Request";
        public const string DepartmentTransfer = "Department Transfer";
        public const string HardwareAsset = "Hardware / Asset";
        public const string ItSystemAccess = "IT & System Access";
        public const string WorkFromHome = "Work From Home";
        public const string ExpenseReimbursement = "Expense Reimbursement";

        public static readonly string[] All =
        [
            LeaveRequest,
            ResignationRequest,
            HikeRequest,
            PromotionRequest,
            DepartmentTransfer,
            HardwareAsset,
            ItSystemAccess,
            WorkFromHome,
            ExpenseReimbursement
        ];
    }

    /// <summary>
    /// 22. EmployeeRequests: Unified Request Management table for Leave, Resignation, Salary Hike, Promotion, etc.
    /// Linked directly to ApprovalWorkflows.
    /// </summary>
    [Table("employee_requests")]
    public class EmployeeRequest
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Human-readable request ID, e.g. "REQ-2026-101"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("request_code")]
        public string RequestCode { get; set; } = string.Empty;

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        /// <summary>
        /// "Leave Request", "Resignation Request", "Appraisal & Salary Review", "Promotion Request", etc.
        /// </summary>
        [Required]
        [MaxLength(60)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(1500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// "Low", "Medium", "High", "Urgent"
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("priority")]
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// "Pending", "Under Review", "Approved", "Rejected"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("overall_status")]
        public string OverallStatus { get; set; } = "Pending";

        [Column("approval_workflow_id")]
        public Guid ApprovalWorkflowId { get; set; }

        [ForeignKey(nameof(ApprovalWorkflowId))]
        public ApprovalWorkflow? ApprovalWorkflow { get; set; }

        [Column("current_level_number")]
        public int CurrentLevelNumber { get; set; } = 1;

        [Column("total_levels")]
        public int TotalLevels { get; set; } = 1;

        // --- Optional Category-Specific Metadata ---

        // 1. Leave Request Fields
        [MaxLength(40)]
        [Column("leave_type")]
        public string? LeaveType { get; set; }

        [Column("leave_start_date")]
        public DateTime? LeaveStartDate { get; set; }

        [Column("leave_end_date")]
        public DateTime? LeaveEndDate { get; set; }

        [Column("leave_days")]
        public int? LeaveDays { get; set; }

        // 2. Promotion Request Fields
        [Column("requested_designation_id")]
        public Guid? RequestedDesignationId { get; set; }

        [ForeignKey(nameof(RequestedDesignationId))]
        public Designation? RequestedDesignation { get; set; }

        // 3. Hike / Salary Review Fields
        [MaxLength(60)]
        [Column("current_ctc")]
        public string? CurrentCtc { get; set; }

        [MaxLength(60)]
        [Column("requested_ctc")]
        public string? RequestedCtc { get; set; }

        [Column("requested_hike_percentage")]
        public decimal? RequestedHikePercentage { get; set; }

        // 4. Resignation Request Fields
        [Column("proposed_last_working_day")]
        public DateTime? ProposedLastWorkingDay { get; set; }

        [Column("notice_period_days")]
        public int? NoticePeriodDays { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        public ICollection<RequestApprovalChainStep> ApprovalChain { get; set; } = new List<RequestApprovalChainStep>();
    }

    /// <summary>
    /// 23. RequestApprovalChainSteps: Live multi-stage approval chain generated from ApprovalWorkflow.Steps for each EmployeeRequest
    /// </summary>
    [Table("request_approval_chain_steps")]
    public class RequestApprovalChainStep
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_request_id")]
        public Guid EmployeeRequestId { get; set; }

        [ForeignKey(nameof(EmployeeRequestId))]
        public EmployeeRequest? EmployeeRequest { get; set; }

        [Column("level_number")]
        public int LevelNumber { get; set; }

        [Required]
        [MaxLength(120)]
        [Column("level_title")]
        public string LevelTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        [Column("approver_role_code")]
        public string ApproverRoleCode { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("approver_role_display")]
        public string ApproverRoleDisplay { get; set; } = string.Empty;

        [Column("approver_employee_id")]
        public Guid? ApproverEmployeeId { get; set; }

        [ForeignKey(nameof(ApproverEmployeeId))]
        public Employee? ApproverEmployee { get; set; }

        /// <summary>
        /// "Pending", "Approved", "Rejected"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("is_current")]
        public bool IsCurrent { get; set; }

        [Column("is_final_hr")]
        public bool IsFinalHr { get; set; }

        [MaxLength(500)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("acted_at")]
        public DateTime? ActedAt { get; set; }
    }
}

