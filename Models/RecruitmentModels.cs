using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    public static class RecruitmentPhases
    {
        public const string Phase1Assessment = "Phase1_Assessment";
        public const string Phase2TechnicalRound = "Phase2_TechnicalRound";
        public const string Phase3OnlineInterview = "Phase3_OnlineInterview";
        public const string Phase4OfferRelease = "Phase4_OfferRelease";
        public const string Onboarding = "Onboarding";
        public const string Hired = "Hired";
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// 18. JobOpenings: Created from Hiring Requisitions (L1-L3) and published by HR/TA (H2-H5) to External Career Website
    /// </summary>
    [Table("job_openings")]
    public class JobOpening
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("job_code")]
        public string JobCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("department_id")]
        public Guid DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Column("designation_id")]
        public Guid? DesignationId { get; set; }

        [ForeignKey(nameof(DesignationId))]
        public Designation? Designation { get; set; }

        /// <summary>
        /// Employee Hierarchy Manager (L1 CTO, L2 Director, L3 Eng Manager) who requested the hire
        /// </summary>
        [Column("requested_by_employee_id")]
        public Guid? RequestedByEmployeeId { get; set; }

        [ForeignKey(nameof(RequestedByEmployeeId))]
        public Employee? RequestedByEmployee { get; set; }

        /// <summary>
        /// HR Hierarchy Recruiter / TA Manager (H4/H5) driving the opening
        /// </summary>
        [Column("assigned_recruiter_id")]
        public Guid? AssignedRecruiterId { get; set; }

        [ForeignKey(nameof(AssignedRecruiterId))]
        public Employee? AssignedRecruiter { get; set; }

        [MaxLength(120)]
        [Column("location")]
        public string Location { get; set; } = "Bangalore (Hybrid)";

        [MaxLength(40)]
        [Column("employment_type")]
        public string EmploymentType { get; set; } = "Full-time";

        [MaxLength(50)]
        [Column("experience_range")]
        public string ExperienceRange { get; set; } = "3-5 Years";

        [MaxLength(80)]
        [Column("salary_range")]
        public string? SalaryRange { get; set; }

        [Column("openings_count")]
        public int OpeningsCount { get; set; } = 1;

        [MaxLength(300)]
        [Column("external_posting_url")]
        public string? ExternalPostingUrl { get; set; }

        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    }

    /// <summary>
    /// 19. Candidates: Applicants ingested from External Career Website progressing through the 4 Phases into Onboarding
    /// </summary>
    [Table("candidates")]
    public class Candidate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        [Column("candidate_code")]
        public string CandidateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column("phone")]
        public string? Phone { get; set; }

        [MaxLength(50)]
        [Column("experience")]
        public string? Experience { get; set; }

        [MaxLength(300)]
        [Column("resume_url")]
        public string? ResumeUrl { get; set; }

        [MaxLength(60)]
        [Column("source")]
        public string Source { get; set; } = "ExternalCareerWebsite";

        [Column("job_opening_id")]
        public Guid JobOpeningId { get; set; }

        [ForeignKey(nameof(JobOpeningId))]
        public JobOpening? JobOpening { get; set; }

        /// <summary>
        /// Current phase in the 4-phase funnel:
        /// Phase1_Assessment -> Phase2_TechnicalRound -> Phase3_OnlineInterview -> Phase4_OfferRelease -> Onboarding -> Hired
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("current_phase")]
        public string CurrentPhase { get; set; } = RecruitmentPhases.Phase1Assessment;

        [Column("current_phase_number")]
        public int CurrentPhaseNumber { get; set; } = 1;

        [Column("match_score")]
        public int MatchScore { get; set; } = 85;

        [Column("overall_rating")]
        public decimal OverallRating { get; set; } = 0m;

        /// <summary>
        /// Populated when the candidate completes Phase 4 & Onboarding and is converted into an Employee record
        /// </summary>
        [Column("converted_employee_id")]
        public Guid? ConvertedEmployeeId { get; set; }

        [ForeignKey(nameof(ConvertedEmployeeId))]
        public Employee? ConvertedEmployee { get; set; }

        [Column("applied_at")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CandidatePhaseEvaluation> PhaseEvaluations { get; set; } = new List<CandidatePhaseEvaluation>();
    }

    /// <summary>
    /// 20. CandidatePhaseEvaluations: Tracks each of the 4 Phases:
    /// Phase 1: Assessment | Phase 2: Technical Round | Phase 3: Online Interview | Phase 4: Offer Release
    /// </summary>
    [Table("candidate_phase_evaluations")]
    public class CandidatePhaseEvaluation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [ForeignKey(nameof(CandidateId))]
        public Candidate? Candidate { get; set; }

        /// <summary>
        /// 1 = Assessment, 2 = Technical Round, 3 = Online Interview (Managerial + HR), 4 = Offer Release
        /// </summary>
        [Column("phase_number")]
        public int PhaseNumber { get; set; }

        [Required]
        [MaxLength(60)]
        [Column("phase_code")]
        public string PhaseCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("phase_title")]
        public string PhaseTitle { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether an Employee-track (L3-L7) or HR-track (H2-H6) evaluator owns this stage
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("owner_hierarchy_track")]
        public string OwnerHierarchyTrack { get; set; } = string.Empty;

        [Column("assigned_evaluator_id")]
        public Guid? AssignedEvaluatorId { get; set; }

        [ForeignKey(nameof(AssignedEvaluatorId))]
        public Employee? AssignedEvaluator { get; set; }

        [MaxLength(300)]
        [Column("meeting_or_test_link")]
        public string? MeetingOrTestLink { get; set; }

        [Column("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// Score out of 100 (for Assessment / Technical evaluation)
        /// </summary>
        [Column("score")]
        public decimal? Score { get; set; }

        /// <summary>
        /// Rating out of 5.0
        /// </summary>
        [Column("rating")]
        public decimal? Rating { get; set; }

        /// <summary>
        /// "Pending", "Scheduled", "InProgress", "Passed", "Failed"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [MaxLength(600)]
        [Column("feedback")]
        public string? Feedback { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// 21. CandidateOffers: Phase 4 Offer Release & Onboarding Handoff
    /// </summary>
    [Table("candidate_offers")]
    public class CandidateOffer
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [ForeignKey(nameof(CandidateId))]
        public Candidate? Candidate { get; set; }

        [Column("job_opening_id")]
        public Guid JobOpeningId { get; set; }

        [ForeignKey(nameof(JobOpeningId))]
        public JobOpening? JobOpening { get; set; }

        [Column("offered_designation_id")]
        public Guid? OfferedDesignationId { get; set; }

        [ForeignKey(nameof(OfferedDesignationId))]
        public Designation? OfferedDesignation { get; set; }

        [Column("department_id")]
        public Guid DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [MaxLength(80)]
        [Column("offered_ctc")]
        public string OfferedCtc { get; set; } = string.Empty;

        [Column("joining_date")]
        public DateTime JoiningDate { get; set; }

        [Column("prepared_by_employee_id")]
        public Guid? PreparedByEmployeeId { get; set; }

        [ForeignKey(nameof(PreparedByEmployeeId))]
        public Employee? PreparedByEmployee { get; set; }

        [Column("approved_by_employee_id")]
        public Guid? ApprovedByEmployeeId { get; set; }

        [ForeignKey(nameof(ApprovedByEmployeeId))]
        public Employee? ApprovedByEmployee { get; set; }

        /// <summary>
        /// "Draft", "Approved", "Released", "Accepted", "Declined"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("offer_status")]
        public string OfferStatus { get; set; } = "Draft";

        /// <summary>
        /// "Pending", "Initiated", "Completed_ConvertedToEmployee"
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("onboarding_status")]
        public string OnboardingStatus { get; set; } = "Pending";

        [Column("released_at")]
        public DateTime? ReleasedAt { get; set; }

        [Column("accepted_at")]
        public DateTime? AcceptedAt { get; set; }
    }
}

