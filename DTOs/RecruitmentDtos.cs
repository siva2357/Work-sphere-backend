using System.ComponentModel.DataAnnotations;

namespace WorkSphere.API.DTOs
{
    public class ExternalCandidateApplicationDto
    {
        [Required]
        public string JobCode { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Experience { get; set; }
        public string? ResumeUrl { get; set; }
    }

    public class EvaluateRecruitmentPhaseDto
    {
        [Range(1, 4)]
        public int PhaseNumber { get; set; }

        public Guid? EvaluatorEmployeeId { get; set; }
        public decimal? Score { get; set; }
        public decimal? Rating { get; set; }
        public bool Passed { get; set; } = true;
        public string? Feedback { get; set; }
        public string? NextPhaseMeetingLink { get; set; }
        public Guid? NextPhaseEvaluatorId { get; set; }
        public string? OfferedCtc { get; set; }
    }

    public class OnboardCandidateRequestDto
    {
        [Required]
        public string WorkEmail { get; set; } = string.Empty;

        [Required]
        public string TemporaryPassword { get; set; } = "Welcome@123";

        public Guid? ReportingManagerEmployeeId { get; set; }
    }
}

