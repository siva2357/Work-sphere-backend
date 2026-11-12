namespace WorkSphere.API.DTOs
{
    public class MarkAttendanceRequestDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        /// <summary>
        /// "PUNCH_IN" or "PUNCH_OUT"
        /// </summary>
        public string Action { get; set; } = "PUNCH_IN";
        public string? Time { get; set; }
        public string Location { get; set; } = "17.4482, 78.3742";
        public string WorkMode { get; set; } = "On-site HQ";
        public string? Remarks { get; set; }
    }

    public class BatchAttendanceReminderDto
    {
        public List<string> EmployeeCodes { get; set; } = new();
        public string? SentByManagerCode { get; set; }
    }

    public class CreateTaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProjectCode { get; set; }
        public string? ProjectName { get; set; }
        public string AssigneeEmployeeCode { get; set; } = string.Empty;
        public string? AssignedByEmployeeCode { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public decimal EstimatedHours { get; set; } = 8m;
        public string Category { get; set; } = "Engineering";
        public List<string>? Subtasks { get; set; }
    }

    public class UpdateTaskStatusRequestDto
    {
        /// <summary>
        /// "Pending", "In Progress", "Completed"
        /// </summary>
        public string Status { get; set; } = "In Progress";
        public int? ProgressPercent { get; set; }
        public decimal? LoggedHours { get; set; }
    }

    public class CreateLeaveProposalDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        /// <summary>
        /// "Casual Leave", "Sick Leave", "Earned Leave", "Comp-Off", "Annual Vacation"
        /// </summary>
        public string LeaveType { get; set; } = "Casual Leave";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? Days { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveDecisionDto
    {
        public string ManagerEmployeeCode { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class CreateProjectRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DepartmentCode { get; set; }
        public string? LeadEmployeeCode { get; set; }
        public List<string> TeamMemberCodes { get; set; } = new();
        public string Category { get; set; } = "Core Web Platform";
        public string Priority { get; set; } = "High";
        public string Status { get; set; } = "In Progress";
        public string Budget { get; set; } = "$75,000";
        public string Methodology { get; set; } = "Agile Scrum";
        public List<string>? Milestones { get; set; }
        public string? ExternalToolUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateEmployeeDocumentDto
    {
        public string? EmployeeCode { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// "Employment", "Compensation", "Policy", "Separation"
        /// </summary>
        public string Category { get; set; } = "Employment";
        public string CategoryLabel { get; set; } = "Employment Letter";
        public string IssueDate { get; set; } = string.Empty;
        public string FileSize { get; set; } = "1.2 MB";
        public string FileFormat { get; set; } = "PDF";
        public string? Badge { get; set; }
        public string? BadgeType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
    }

    public class ExternalCompanyRegistrationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = "Enterprise Software & IT";
        public string CompanySize { get; set; } = "100-250 Employees";
        public string Headquarters { get; set; } = "Bangalore, India";
        public string SubscriptionPlan { get; set; } = "Enterprise Suite";
        public string MainHrFullName { get; set; } = string.Empty;
        public string MainHrEmail { get; set; } = string.Empty;
        public string MainHrPassword { get; set; } = "MainHr@12345";
        public string? MainHrPhone { get; set; }
    }

    public class MainHrCreateEmployeeDto
    {
        /// <summary>
        /// Email or EmployeeCode of the Main HR administrator creating this employee in the Employee Directory.
        /// </summary>
        public string CreatedByHrEmailOrCode { get; set; } = "chro@worksphere.com";
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string InitialPassword { get; set; } = "Welcome@12345";
        public string? Phone { get; set; }
        /// <summary>
        /// Department code (e.g. "ENG", "ENG-FE", "ENG-BE", "PROD", "QA", "HR", "FIN", etc.)
        /// </summary>
        public string DepartmentCode { get; set; } = "ENG";
        /// <summary>
        /// Designation code (e.g. "DESIG_DEV", "DESIG_SR_DEV", "DESIG_TEAM_LEAD", "DESIG_PM", "DESIG_HR_EXEC")
        /// </summary>
        public string? DesignationCode { get; set; }
        /// <summary>
        /// System role code (e.g. "EMP_DEVELOPER", "EMP_SR_DEVELOPER", "EMP_TEAM_LEAD", "EMP_PROJECT_MANAGER", "HR_RECRUITER")
        /// </summary>
        public string? RoleCode { get; set; }
        /// <summary>
        /// "Full-time", "Contract", "Intern"
        /// </summary>
        public string EmploymentType { get; set; } = "Full-time";
        public string WorkLocation { get; set; } = "Bangalore, India";
        public string? ManagerEmployeeCode { get; set; }
    }
}

