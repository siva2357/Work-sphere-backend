using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    /// <summary>
    /// 24. AttendanceLogs: Supports both individual employee "My Attendance / Mark Attendance"
    /// and Manager/Department "Attendance Management / Attendance Sheet" across reporting lines & departments.
    /// </summary>
    [Table("attendance_logs")]
    public class AttendanceLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [MaxLength(20)]
        [Column("day_of_week")]
        public string DayOfWeek { get; set; } = string.Empty;

        /// <summary>
        /// "Present", "Late", "Half Day", "Absent", "On Leave", "Weekend", "Holiday", "Pending"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Present";

        [MaxLength(25)]
        [Column("punch_in")]
        public string PunchIn { get; set; } = "--:--";

        [MaxLength(100)]
        [Column("login_location")]
        public string LoginLocation { get; set; } = "17.4482, 78.3742";

        [MaxLength(25)]
        [Column("punch_out")]
        public string PunchOut { get; set; } = "--:--";

        [MaxLength(100)]
        [Column("logout_location")]
        public string LogoutLocation { get; set; } = "17.4482, 78.3742";

        [MaxLength(25)]
        [Column("work_hours")]
        public string WorkHours { get; set; } = "9h 00m";

        [MaxLength(25)]
        [Column("overtime")]
        public string Overtime { get; set; } = "--";

        [Column("overtime_hours")]
        public decimal OvertimeHours { get; set; } = 0m;

        /// <summary>
        /// "On-site HQ", "Hybrid", "Remote"
        /// </summary>
        [MaxLength(30)]
        [Column("work_mode")]
        public string WorkMode { get; set; } = "On-site HQ";

        /// <summary>
        /// "Auto-Approved", "Approved", "Pending Regularization"
        /// </summary>
        [MaxLength(40)]
        [Column("approval_status")]
        public string ApprovalStatus { get; set; } = "Auto-Approved";

        [MaxLength(250)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("reminder_sent")]
        public bool ReminderSent { get; set; }

        [MaxLength(30)]
        [Column("last_reminder_time")]
        public string? LastReminderTime { get; set; }
    }

    /// <summary>
    /// 25. LeaveBalances: Tracks Casual Leave (CL), Sick Leave (SL), Earned Leave (EL), and Comp-Off (CO) per Employee
    /// </summary>
    [Table("leave_balances")]
    public class LeaveBalanceRecord
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("leave_type")]
        public string LeaveType { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        [Column("short_code")]
        public string ShortCode { get; set; } = string.Empty;

        [Column("total_allocated")]
        public int TotalAllocated { get; set; }

        [Column("used_days")]
        public int UsedDays { get; set; }

        [Column("available_days")]
        public int AvailableDays { get; set; }

        [MaxLength(40)]
        [Column("color_class")]
        public string ColorClass { get; set; } = "bg-emerald";
    }

    /// <summary>
    /// 26. LeaveProposals: Individual "My Leaves / Apply Leave" & Manager/Department "Leave Management"
    /// </summary>
    [Table("leave_proposals")]
    public class LeaveProposal
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("leave_code")]
        public string LeaveCode { get; set; } = string.Empty;

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        /// <summary>
        /// "Casual Leave", "Sick Leave", "Earned Leave", "Comp-Off", "Annual Vacation"
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("leave_type")]
        public string LeaveType { get; set; } = "Casual Leave";

        [Column("from_date")]
        public DateTime FromDate { get; set; }

        [Column("to_date")]
        public DateTime ToDate { get; set; }

        [Column("days")]
        public int Days { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// "Pending", "Approved", "Rejected", "Cancelled"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("approved_by_employee_id")]
        public Guid? ApprovedByEmployeeId { get; set; }

        [ForeignKey(nameof(ApprovedByEmployeeId))]
        public Employee? ApprovedByEmployee { get; set; }

        [MaxLength(400)]
        [Column("manager_comments")]
        public string? ManagerComments { get; set; }

        [Column("applied_at")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Column("acted_at")]
        public DateTime? ActedAt { get; set; }
    }

    /// <summary>
    /// 27. TaskItems: Supports Manager/Lead "Task Management / Assign Task to Team Member" and Individual "My Tasks"
    /// </summary>
    [Table("task_items")]
    public class TaskItem
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("task_code")]
        public string TaskCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        [MaxLength(150)]
        [Column("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [Column("assignee_employee_id")]
        public Guid AssigneeEmployeeId { get; set; }

        [ForeignKey(nameof(AssigneeEmployeeId))]
        public Employee? Assignee { get; set; }

        [Column("assigned_by_employee_id")]
        public Guid? AssignedByEmployeeId { get; set; }

        [ForeignKey(nameof(AssignedByEmployeeId))]
        public Employee? AssignedBy { get; set; }

        [Column("assigned_date")]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Column("due_date")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// "Pending", "In Progress", "Completed"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// "Urgent", "High", "Medium", "Low"
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("priority")]
        public string Priority { get; set; } = "Medium";

        [Column("estimated_hours")]
        public decimal EstimatedHours { get; set; } = 8m;

        [Column("logged_hours")]
        public decimal LoggedHours { get; set; } = 0m;

        [Column("progress_percent")]
        public int ProgressPercent { get; set; } = 0;

        [MaxLength(80)]
        [Column("category")]
        public string Category { get; set; } = "Engineering";

        /// <summary>
        /// Semicolon-separated or JSON array of subtasks
        /// </summary>
        [MaxLength(1000)]
        [Column("subtasks_json")]
        public string? SubtasksJson { get; set; }
    }

    /// <summary>
    /// 28. EmployeeDocumentRecords: Supports "My Documents" (Offer Letter, Hike Letter, Promotion Letter, Relieving Letter, Company Policies)
    /// </summary>
    [Table("employee_documents")]
    public class EmployeeDocumentRecord
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(40)]
        [Column("document_code")]
        public string DocumentCode { get; set; } = string.Empty;

        /// <summary>
        /// Null if it is an organization-wide Policy document visible to all employees
        /// </summary>
        [Column("employee_id")]
        public Guid? EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// "Employment", "Compensation", "Policy", "Separation"
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("category")]
        public string Category { get; set; } = "Employment";

        [Required]
        [MaxLength(60)]
        [Column("category_label")]
        public string CategoryLabel { get; set; } = "Employment Letter";

        [MaxLength(40)]
        [Column("issue_date")]
        public string IssueDate { get; set; } = string.Empty;

        [MaxLength(25)]
        [Column("file_size")]
        public string FileSize { get; set; } = "1.2 MB";

        [MaxLength(15)]
        [Column("file_format")]
        public string FileFormat { get; set; } = "PDF";

        [MaxLength(40)]
        [Column("badge")]
        public string? Badge { get; set; }

        [MaxLength(30)]
        [Column("badge_type")]
        public string? BadgeType { get; set; }

        [MaxLength(500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(300)]
        [Column("file_url")]
        public string? FileUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 29. MonthlyPayrollSlips: Supports "My Payroll" (Employee own payslips & tax slips) and HR Payroll Management
    /// </summary>
    [Table("monthly_payroll_slips")]
    public class MonthlyPayrollSlip
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("month")]
        public string Month { get; set; } = "October 2026";

        [Required]
        [MaxLength(20)]
        [Column("month_short")]
        public string MonthShort { get; set; } = "Oct 2026";

        [Required]
        [MaxLength(20)]
        [Column("financial_year")]
        public string FinancialYear { get; set; } = "FY 2026-27";

        [Required]
        [MaxLength(25)]
        [Column("pay_date")]
        public string PayDate { get; set; } = "31 Oct 2026";

        /// <summary>
        /// "Paid", "Processing", "Upcoming"
        /// </summary>
        [Required]
        [MaxLength(25)]
        [Column("status")]
        public string Status { get; set; } = "Paid";

        [Column("basic_salary")]
        public decimal BasicSalary { get; set; }

        [Column("hra")]
        public decimal Hra { get; set; }

        [Column("special_allowance")]
        public decimal SpecialAllowance { get; set; }

        [Column("conveyance")]
        public decimal Conveyance { get; set; }

        [Column("medical_allowance")]
        public decimal MedicalAllowance { get; set; }

        [Column("gross_earnings")]
        public decimal GrossEarnings { get; set; }

        [Column("epf_deduction")]
        public decimal EpfDeduction { get; set; }

        [Column("professional_tax")]
        public decimal ProfessionalTax { get; set; }

        [Column("tds_deduction")]
        public decimal TdsDeduction { get; set; }

        [Column("insurance_deduction")]
        public decimal InsuranceDeduction { get; set; }

        [Column("total_deductions")]
        public decimal TotalDeductions { get; set; }

        [Column("net_salary")]
        public decimal NetSalary { get; set; }
    }
}

