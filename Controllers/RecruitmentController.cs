using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Recruitment.Base)]
    public class RecruitmentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<Employee> _passwordHasher = new();

        public RecruitmentController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Explains and returns the 4-Phase Recruitment & Onboarding Matrix across Employee (L1-L7) & HR (H1-H6) hierarchies.
        /// </summary>
        [HttpGet(ApiConstants.Recruitment.WorkflowOverview)]
        public IActionResult GetRecruitmentWorkflowOverview()
        {
            var phases = new[]
            {
                new
                {
                    PhaseNumber = 0,
                    PhaseCode = "PrePhase_JobRequisitionAndExternalPost",
                    Title = "Job Requisition & External Career Website Sync",
                    EmployeeHierarchyRole = "L1 CTO / L2 Director / L3 Engineering Manager requests headcount & technical requirements.",
                    HrHierarchyRole = "H4 Talent Acquisition Manager / H2 HR Manager approves & publishes JobOpening to the external career portal; H5 Recruiter is assigned."
                },
                new
                {
                    PhaseNumber = 1,
                    PhaseCode = RecruitmentPhases.Phase1Assessment,
                    Title = "Phase 1: Online Assessment",
                    EmployeeHierarchyRole = "L5 Solution Architect / Technical Lead defines standard assessment benchmarks.",
                    HrHierarchyRole = "H5 Recruiter / H6 Recruitment Coordinator dispatches assessment link, tracks completion score, and shortlists passing candidates."
                },
                new
                {
                    PhaseNumber = 2,
                    PhaseCode = RecruitmentPhases.Phase2TechnicalRound,
                    Title = "Phase 2: Technical Round",
                    EmployeeHierarchyRole = "L5 Technical Lead / Solution Architect, L6 Team Lead, or L7 Senior Developer conducts technical evaluation, scores coding/architecture, and submits Recommend/Reject.",
                    HrHierarchyRole = "H6 Recruitment Coordinator / H5 Recruiter schedules the technical panel and coordinates candidate feedback."
                },
                new
                {
                    PhaseNumber = 3,
                    PhaseCode = RecruitmentPhases.Phase3OnlineInterview,
                    Title = "Phase 3: Online Interview (Managerial + HR)",
                    EmployeeHierarchyRole = "L3 Engineering Manager / L4 Project Manager evaluates team fit, delivery ownership, and approves selection.",
                    HrHierarchyRole = "H4 TA Manager / H5 Recruiter conducts HR & compensation discussion and aligns joining timeline."
                },
                new
                {
                    PhaseNumber = 4,
                    PhaseCode = RecruitmentPhases.Phase4OfferRelease,
                    Title = "Phase 4: Offer Release & Onboarding Handoff",
                    EmployeeHierarchyRole = "L3 Engineering Manager / L6 Team Lead is assigned as Primary Reporting Manager & Team Lead for the incoming hire.",
                    HrHierarchyRole = "H5 Recruiter prepares offer -> H4 TA Manager / H2 HR Manager approves & releases Offer -> H3 HR Operations Manager / H5 HR Executive completes Onboarding & converts Candidate into an active Employee in PostgreSQL."
                }
            };

            return Ok(phases);
        }

        /// <summary>
        /// Lists all Job Openings synced with the external career portal.
        /// </summary>
        [HttpGet(ApiConstants.Recruitment.Jobs)]
        public async Task<IActionResult> GetJobOpenings()
        {
            var jobs = await _db.JobOpenings
                .Include(j => j.Department)
                .Include(j => j.Designation)
                    .ThenInclude(d => d!.OrganizationalLevel)
                .Include(j => j.RequestedByEmployee)
                .Include(j => j.AssignedRecruiter)
                .Include(j => j.Candidates)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new
                {
                    j.Id,
                    j.JobCode,
                    j.Title,
                    Department = j.Department != null ? j.Department.Name : null,
                    TargetDesignation = j.Designation != null ? j.Designation.Title : null,
                    TargetLevel = j.Designation != null && j.Designation.OrganizationalLevel != null
                        ? j.Designation.OrganizationalLevel.Code
                        : null,
                    HiringManager = j.RequestedByEmployee != null ? j.RequestedByEmployee.FullName : null,
                    AssignedRecruiter = j.AssignedRecruiter != null ? j.AssignedRecruiter.FullName : null,
                    j.Location,
                    j.EmploymentType,
                    j.ExperienceRange,
                    j.SalaryRange,
                    j.OpeningsCount,
                    j.ExternalPostingUrl,
                    j.Status,
                    ApplicantsCount = j.Candidates.Count
                })
                .ToListAsync();

            return Ok(jobs);
        }

        /// <summary>
        /// Webhook / Public endpoint for the External Career Website to submit a candidate application and start Phase 1 (Assessment).
        /// </summary>
        [HttpPost(ApiConstants.Recruitment.ExternalApply)]
        public async Task<IActionResult> ApplyFromExternalWebsite([FromBody] ExternalCandidateApplicationDto request)
        {
            var job = await _db.JobOpenings
                .FirstOrDefaultAsync(j => j.JobCode.ToLower() == request.JobCode.ToLower());

            if (job == null)
            {
                return NotFound(new { message = $"Job opening '{request.JobCode}' not found." });
            }

            var count = await _db.Candidates.CountAsync();
            var candidateCode = $"CAND-{(count + 1):D2}";

            var candidate = new Candidate
            {
                CandidateCode = candidateCode,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Experience = request.Experience,
                ResumeUrl = request.ResumeUrl,
                Source = "ExternalCareerWebsite",
                JobOpeningId = job.Id,
                CurrentPhase = RecruitmentPhases.Phase1Assessment,
                CurrentPhaseNumber = 1,
                MatchScore = 88
            };

            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync();

            // Initialize all 4 Phase Evaluation records
            var phases = new List<CandidatePhaseEvaluation>
            {
                new()
                {
                    CandidateId = candidate.Id,
                    PhaseNumber = 1,
                    PhaseCode = RecruitmentPhases.Phase1Assessment,
                    PhaseTitle = "Phase 1: Online Assessment",
                    OwnerHierarchyTrack = HierarchyTracks.Hr,
                    AssignedEvaluatorId = job.AssignedRecruiterId,
                    MeetingOrTestLink = $"https://assess.worksphere.com/test/{candidate.CandidateCode}",
                    Status = "Scheduled"
                },
                new()
                {
                    CandidateId = candidate.Id,
                    PhaseNumber = 2,
                    PhaseCode = RecruitmentPhases.Phase2TechnicalRound,
                    PhaseTitle = "Phase 2: Technical Round (Employee Track L5-L7)",
                    OwnerHierarchyTrack = HierarchyTracks.Employee,
                    Status = "Pending"
                },
                new()
                {
                    CandidateId = candidate.Id,
                    PhaseNumber = 3,
                    PhaseCode = RecruitmentPhases.Phase3OnlineInterview,
                    PhaseTitle = "Phase 3: Online Interview (Managerial L3 + HR H4/H5)",
                    OwnerHierarchyTrack = "Employee & HR Joint",
                    AssignedEvaluatorId = job.RequestedByEmployeeId,
                    Status = "Pending"
                },
                new()
                {
                    CandidateId = candidate.Id,
                    PhaseNumber = 4,
                    PhaseCode = RecruitmentPhases.Phase4OfferRelease,
                    PhaseTitle = "Phase 4: Offer Release & Onboarding Handoff",
                    OwnerHierarchyTrack = HierarchyTracks.Hr,
                    Status = "Pending"
                }
            };

            _db.CandidatePhaseEvaluations.AddRange(phases);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Application received from external website and enrolled in Phase 1 (Assessment).",
                candidate.Id,
                candidate.CandidateCode,
                candidate.CurrentPhase
            });
        }

        /// <summary>
        /// Lists all Candidates with their 4-Phase Evaluations and Offer status.
        /// </summary>
        [HttpGet(ApiConstants.Recruitment.Candidates)]
        public async Task<IActionResult> GetCandidates()
        {
            var candidates = await _db.Candidates
                .Include(c => c.JobOpening)
                    .ThenInclude(j => j!.Department)
                .Include(c => c.PhaseEvaluations)
                    .ThenInclude(pe => pe.AssignedEvaluator)
                .OrderByDescending(c => c.AppliedAt)
                .ToListAsync();

            var offers = await _db.CandidateOffers.ToListAsync();

            var result = candidates.Select(c => new
            {
                c.Id,
                c.CandidateCode,
                c.FullName,
                c.Email,
                c.Phone,
                c.Experience,
                c.Source,
                JobCode = c.JobOpening?.JobCode,
                JobTitle = c.JobOpening?.Title,
                Department = c.JobOpening?.Department?.Name,
                c.CurrentPhase,
                c.CurrentPhaseNumber,
                c.MatchScore,
                c.OverallRating,
                c.ConvertedEmployeeId,
                Phases = c.PhaseEvaluations
                    .OrderBy(pe => pe.PhaseNumber)
                    .Select(pe => new
                    {
                        pe.PhaseNumber,
                        pe.PhaseCode,
                        pe.PhaseTitle,
                        pe.OwnerHierarchyTrack,
                        EvaluatorName = pe.AssignedEvaluator?.FullName,
                        pe.MeetingOrTestLink,
                        pe.Score,
                        pe.Rating,
                        pe.Status,
                        pe.Feedback,
                        pe.CompletedAt
                    }),
                Offer = offers
                    .Where(o => o.CandidateId == c.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.OfferedCtc,
                        o.JoiningDate,
                        o.OfferStatus,
                        o.OnboardingStatus,
                        o.ReleasedAt
                    })
                    .FirstOrDefault()
            });

            return Ok(result);
        }

        /// <summary>
        /// Evaluates a candidate's current phase (1: Assessment, 2: Technical Round, 3: Online Interview, 4: Offer Release)
        /// and automatically transitions them to the next phase.
        /// </summary>
        [HttpPost(ApiConstants.Recruitment.EvaluatePhase)]
        public async Task<IActionResult> EvaluateCandidatePhase(
            Guid candidateId,
            [FromBody] EvaluateRecruitmentPhaseDto request)
        {
            var candidate = await _db.Candidates
                .Include(c => c.JobOpening)
                .Include(c => c.PhaseEvaluations)
                .FirstOrDefaultAsync(c => c.Id == candidateId);

            if (candidate == null)
            {
                return NotFound(new { message = "Candidate not found." });
            }

            var phaseEval = candidate.PhaseEvaluations
                .FirstOrDefault(p => p.PhaseNumber == request.PhaseNumber);

            if (phaseEval == null)
            {
                return BadRequest(new { message = $"Phase {request.PhaseNumber} evaluation record not found." });
            }

            if (request.EvaluatorEmployeeId.HasValue)
            {
                phaseEval.AssignedEvaluatorId = request.EvaluatorEmployeeId;
            }

            phaseEval.Score = request.Score;
            phaseEval.Rating = request.Rating;
            phaseEval.Feedback = request.Feedback;
            phaseEval.Status = request.Passed ? "Passed" : "Failed";
            phaseEval.CompletedAt = DateTime.UtcNow;

            if (!request.Passed)
            {
                candidate.CurrentPhase = RecruitmentPhases.Rejected;
                await _db.SaveChangesAsync();
                return Ok(new { message = $"Candidate rejected at Phase {request.PhaseNumber}.", candidate.CurrentPhase });
            }

            // Advance to next phase
            if (request.PhaseNumber < 4)
            {
                var nextPhaseNum = request.PhaseNumber + 1;
                var nextPhase = candidate.PhaseEvaluations.FirstOrDefault(p => p.PhaseNumber == nextPhaseNum);
                if (nextPhase != null)
                {
                    nextPhase.Status = nextPhaseNum == 4 ? "InProgress" : "Scheduled";
                    if (!string.IsNullOrWhiteSpace(request.NextPhaseMeetingLink))
                        nextPhase.MeetingOrTestLink = request.NextPhaseMeetingLink;
                    if (request.NextPhaseEvaluatorId.HasValue)
                        nextPhase.AssignedEvaluatorId = request.NextPhaseEvaluatorId;

                    candidate.CurrentPhase = nextPhase.PhaseCode;
                    candidate.CurrentPhaseNumber = nextPhaseNum;
                }
            }
            else if (request.PhaseNumber == 4)
            {
                // Phase 4 completed -> Offer Released & Ready for Onboarding
                candidate.CurrentPhase = RecruitmentPhases.Onboarding;

                if (candidate.JobOpening != null && !await _db.CandidateOffers.AnyAsync(o => o.CandidateId == candidate.Id))
                {
                    _db.CandidateOffers.Add(new CandidateOffer
                    {
                        CandidateId = candidate.Id,
                        JobOpeningId = candidate.JobOpeningId,
                        OfferedDesignationId = candidate.JobOpening.DesignationId,
                        DepartmentId = candidate.JobOpening.DepartmentId,
                        OfferedCtc = request.OfferedCtc ?? candidate.JobOpening.SalaryRange ?? "$85,000 / year",
                        JoiningDate = DateTime.UtcNow.AddDays(14),
                        ApprovedByEmployeeId = request.EvaluatorEmployeeId,
                        OfferStatus = "Accepted",
                        OnboardingStatus = "Initiated",
                        ReleasedAt = DateTime.UtcNow,
                        AcceptedAt = DateTime.UtcNow
                    });
                }
            }

            var ratedPhases = candidate.PhaseEvaluations.Where(p => p.Rating.HasValue).ToList();
            if (ratedPhases.Count > 0)
            {
                candidate.OverallRating = Math.Round(ratedPhases.Average(p => p.Rating!.Value), 2);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Phase {request.PhaseNumber} passed. Candidate moved to '{candidate.CurrentPhase}'.",
                candidate.CandidateCode,
                candidate.CurrentPhase,
                candidate.CurrentPhaseNumber,
                candidate.OverallRating
            });
        }

        /// <summary>
        /// Final Onboarding Step (H3 HR Operations Manager / H5 HR Executive):
        /// Converts a Candidate into an official Employee in the PostgreSQL 'employees' table with their Level, Designation, Role & Reporting Manager.
        /// </summary>
        [HttpPost(ApiConstants.Recruitment.OnboardCandidate)]
        public async Task<IActionResult> OnboardCandidateAsEmployee(
            Guid candidateId,
            [FromBody] OnboardCandidateRequestDto request)
        {
            var candidate = await _db.Candidates
                .Include(c => c.JobOpening)
                    .ThenInclude(j => j!.Designation)
                .FirstOrDefaultAsync(c => c.Id == candidateId);

            if (candidate == null)
            {
                return NotFound(new { message = "Candidate not found." });
            }

            if (candidate.ConvertedEmployeeId.HasValue)
            {
                return BadRequest(new { message = "Candidate has already been onboarded as an Employee." });
            }

            var job = candidate.JobOpening;
            if (job == null)
            {
                return BadRequest(new { message = "Candidate is not linked to a valid JobOpening." });
            }

            var empCount = await _db.Employees.CountAsync();
            var employeeCode = $"EMP-{(empCount + 101):D3}";

            var designation = job.Designation;
            var newEmployee = new Employee
            {
                EmployeeCode = employeeCode,
                FullName = candidate.FullName,
                Email = request.WorkEmail.Trim().ToLowerInvariant(),
                Phone = candidate.Phone,
                PortalType = designation?.HierarchyTrack == HierarchyTracks.Hr ? PortalTypes.Hr : PortalTypes.Employee,
                DepartmentId = job.DepartmentId,
                DesignationId = job.DesignationId,
                OrganizationalLevelId = designation?.OrganizationalLevelId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            newEmployee.PasswordHash = _passwordHasher.HashPassword(newEmployee, request.TemporaryPassword);

            _db.Employees.Add(newEmployee);
            await _db.SaveChangesAsync();

            // Assign default System Role from Designation
            if (!string.IsNullOrWhiteSpace(designation?.DefaultRoleCode))
            {
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == designation.DefaultRoleCode);
                if (defaultRole != null)
                {
                    _db.EmployeeRoles.Add(new EmployeeRole
                    {
                        EmployeeId = newEmployee.Id,
                        RoleId = defaultRole.Id,
                        AssignedDepartmentId = job.DepartmentId,
                        IsPrimary = true
                    });
                }
            }

            // Assign Primary Reporting Manager (either specified or Job's Hiring Manager)
            var managerId = request.ReportingManagerEmployeeId ?? job.RequestedByEmployeeId;
            if (managerId.HasValue)
            {
                _db.ReportingRelationships.Add(new ReportingRelationship
                {
                    EmployeeId = newEmployee.Id,
                    ManagerEmployeeId = managerId.Value,
                    RelationshipType = "Primary",
                    IsPrimary = true,
                    IsActive = true
                });
            }

            // Mark Candidate & Offer as Hired / Onboarded
            candidate.CurrentPhase = RecruitmentPhases.Hired;
            candidate.ConvertedEmployeeId = newEmployee.Id;

            var offer = await _db.CandidateOffers.FirstOrDefaultAsync(o => o.CandidateId == candidate.Id);
            if (offer != null)
            {
                offer.OfferStatus = "Accepted";
                offer.OnboardingStatus = "Completed_ConvertedToEmployee";
                offer.AcceptedAt ??= DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Candidate successfully onboarded and converted into an active Employee!",
                EmployeeId = newEmployee.Id,
                newEmployee.EmployeeCode,
                newEmployee.FullName,
                newEmployee.Email,
                Designation = designation?.Title,
                DefaultRole = designation?.DefaultRoleCode
            });
        }
    }
}

