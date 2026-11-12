using System.ComponentModel.DataAnnotations;

namespace WorkSphere.API.DTOs
{
    public class CreateEmployeeRequestDto
    {
        [Required]
        public Guid EmployeeId { get; set; }

        /// <summary>
        /// "Leave Request", "Resignation Request", "Appraisal & Salary Review", "Promotion Request", etc.
        /// </summary>
        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Priority { get; set; } = "Medium";

        // Optional Leave fields
        public string? LeaveType { get; set; }
        public DateTime? LeaveStartDate { get; set; }
        public DateTime? LeaveEndDate { get; set; }
        public int? LeaveDays { get; set; }

        // Optional Promotion field
        public string? RequestedDesignationCode { get; set; }

        // Optional Hike / Salary Review fields
        public string? CurrentCtc { get; set; }
        public string? RequestedCtc { get; set; }
        public decimal? RequestedHikePercentage { get; set; }

        // Optional Resignation fields
        public DateTime? ProposedLastWorkingDay { get; set; }
        public int? NoticePeriodDays { get; set; }
    }

    public class RequestDecisionDto
    {
        public Guid? ApproverEmployeeId { get; set; }
        public string? Remarks { get; set; }
    }
}

