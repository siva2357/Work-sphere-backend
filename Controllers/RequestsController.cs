using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Requests.Base)]
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RequestsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns all Request Categories (Leave, Resignation, Hike/Appraisal, Promotion) and their linked Multi-Stage Approval Workflows.
        /// </summary>
        [HttpGet(ApiConstants.Requests.Workflows)]
        public async Task<IActionResult> GetRequestApprovalWorkflows()
        {
            var workflows = await _db.ApprovalWorkflows
                .Include(w => w.Steps)
                    .ThenInclude(s => s.RequiredRole)
                .Where(w => w.Module != PermissionModules.Recruitment)
                .OrderBy(w => w.Name)
                .ToListAsync();

            var result = workflows.Select(w => new
            {
                w.Id,
                w.Name,
                RequestCategory = w.Module == PermissionModules.Leave ? RequestCategories.LeaveRequest : w.Module,
                w.Description,
                TotalStages = w.Steps.Count,
                ApprovalStages = w.Steps
                    .OrderBy(s => s.StepOrder)
                    .Select(s => new
                    {
                        s.StepOrder,
                        s.StepName,
                        s.ApproverType,
                        RequiredRoleCode = s.RequiredRole?.Code,
                        RequiredRoleName = s.RequiredRole?.Name,
                        HierarchyTrack = s.RequiredRole?.HierarchyTrack,
                        s.ActionType,
                        IsFinalHrStage = s.StepOrder == w.Steps.Count
                    })
            });

            return Ok(result);
        }

        /// <summary>
        /// Lists all Employee Requests (Leave, Resignation, Hike/Appraisal, Promotion, etc.) with their live Multi-Stage Approval Chain.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRequests(
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? employeeId = null)
        {
            var query = _db.EmployeeRequests
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Designation)
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.OrganizationalLevel)
                .Include(r => r.RequestedDesignation)
                    .ThenInclude(d => d!.OrganizationalLevel)
                .Include(r => r.ApprovalWorkflow)
                .Include(r => r.ApprovalChain)
                    .ThenInclude(s => s.ApproverEmployee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.OverallStatus.ToLower() == status.ToLower());
            }

            if (employeeId.HasValue)
            {
                query = query.Where(r => r.EmployeeId == employeeId.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var result = requests.Select(r => new
            {
                id = r.RequestCode,
                requestGuid = r.Id,
                employeeId = r.Employee?.EmployeeCode,
                employeeGuid = r.EmployeeId,
                employeeName = r.Employee?.FullName,
                employeeEmail = r.Employee?.Email,
                employeeRole = r.Employee?.Designation?.Title,
                employeeLevel = r.Employee?.OrganizationalLevel?.Code,
                department = r.Employee?.Department?.Name,
                r.Category,
                r.Subject,
                r.Description,
                submittedDate = r.SubmittedAt.ToString("dd MMM yyyy"),
                r.Priority,
                r.OverallStatus,
                workflowName = r.ApprovalWorkflow?.Name,
                r.CurrentLevelNumber,
                r.TotalLevels,
                // Category-specific metadata
                details = new
                {
                    r.LeaveType,
                    r.LeaveStartDate,
                    r.LeaveEndDate,
                    r.LeaveDays,
                    RequestedPromotionTo = r.RequestedDesignation?.Title,
                    RequestedPromotionLevel = r.RequestedDesignation?.OrganizationalLevel?.Code,
                    r.CurrentCtc,
                    r.RequestedCtc,
                    r.RequestedHikePercentage,
                    r.ProposedLastWorkingDay,
                    r.NoticePeriodDays
                },
                approvalChain = r.ApprovalChain
                    .OrderBy(s => s.LevelNumber)
                    .Select(s => new
                    {
                        levelNumber = s.LevelNumber,
                        levelTitle = s.LevelTitle,
                        role = s.ApproverRoleDisplay,
                        roleCode = s.ApproverRoleCode,
                        approverName = s.ApproverEmployee?.FullName ?? s.ApproverRoleDisplay,
                        approverId = s.ApproverEmployee?.EmployeeCode,
                        status = s.Status,
                        actionDate = s.ActedAt?.ToString("dd MMM yyyy, hh:mm tt"),
                        remarks = s.Remarks,
                        isCurrent = s.IsCurrent,
                        isFinalHR = s.IsFinalHr
                    })
            });

            return Ok(result);
        }

        /// <summary>
        /// Submits a new Employee Request (Leave, Resignation, Hike/Appraisal, Promotion) and automatically generates
        /// its Multi-Stage Approval Chain from the linked ApprovalWorkflow.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateEmployeeRequestDto dto)
        {
            var employee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            // Find matching ApprovalWorkflow for this request category
            var workflowModuleKey = dto.Category.Equals(RequestCategories.LeaveRequest, StringComparison.OrdinalIgnoreCase)
                ? PermissionModules.Leave
                : dto.Category;

            var workflow = await _db.ApprovalWorkflows
                .Include(w => w.Steps)
                    .ThenInclude(s => s.RequiredRole)
                .FirstOrDefaultAsync(w => w.Module.ToLower() == workflowModuleKey.ToLower() && w.IsActive)
                ?? await _db.ApprovalWorkflows
                    .Include(w => w.Steps)
                        .ThenInclude(s => s.RequiredRole)
                    .FirstAsync(w => w.Module == PermissionModules.Leave);

            Designation? targetDesignation = null;
            if (!string.IsNullOrWhiteSpace(dto.RequestedDesignationCode))
            {
                targetDesignation = await _db.Designations
                    .FirstOrDefaultAsync(d => d.Code.ToLower() == dto.RequestedDesignationCode.ToLower());
            }

            var count = await _db.EmployeeRequests.CountAsync();
            var requestCode = $"REQ-2026-{(101 + count)}";
            var orderedSteps = workflow.Steps.OrderBy(s => s.StepOrder).ToList();

            var newRequest = new EmployeeRequest
            {
                RequestCode = requestCode,
                EmployeeId = employee.Id,
                Category = dto.Category,
                Subject = dto.Subject,
                Description = dto.Description,
                Priority = dto.Priority,
                OverallStatus = "Pending",
                ApprovalWorkflowId = workflow.Id,
                CurrentLevelNumber = 1,
                TotalLevels = orderedSteps.Count,
                LeaveType = dto.LeaveType,
                LeaveStartDate = dto.LeaveStartDate,
                LeaveEndDate = dto.LeaveEndDate,
                LeaveDays = dto.LeaveDays,
                RequestedDesignationId = targetDesignation?.Id,
                CurrentCtc = dto.CurrentCtc,
                RequestedCtc = dto.RequestedCtc,
                RequestedHikePercentage = dto.RequestedHikePercentage,
                ProposedLastWorkingDay = dto.ProposedLastWorkingDay,
                NoticePeriodDays = dto.NoticePeriodDays
            };

            _db.EmployeeRequests.Add(newRequest);
            await _db.SaveChangesAsync();

            // Resolve approvers from EmployeeRoles for each step in the workflow
            var employeeRoles = await _db.EmployeeRoles
                .Include(er => er.Employee)
                .Include(er => er.Role)
                .ToListAsync();

            for (var i = 0; i < orderedSteps.Count; i++)
            {
                var step = orderedSteps[i];
                var roleCode = step.RequiredRole?.Code ?? SystemRoleCodes.HrManager;
                var roleName = step.RequiredRole?.Name ?? "Approver";

                var matchedApprover = employeeRoles
                    .FirstOrDefault(er => er.Role != null && er.Role.Code == roleCode)?.Employee;

                _db.RequestApprovalChainSteps.Add(new RequestApprovalChainStep
                {
                    EmployeeRequestId = newRequest.Id,
                    LevelNumber = step.StepOrder,
                    LevelTitle = step.StepName,
                    ApproverRoleCode = roleCode,
                    ApproverRoleDisplay = roleName,
                    ApproverEmployeeId = matchedApprover?.Id,
                    Status = "Pending",
                    IsCurrent = i == 0,
                    IsFinalHr = i == orderedSteps.Count - 1
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Request '{newRequest.RequestCode}' created and linked to workflow '{workflow.Name}'.",
                newRequest.RequestCode,
                newRequest.Category,
                Workflow = workflow.Name,
                newRequest.TotalLevels
            });
        }

        /// <summary>
        /// Approves the current active stage of a request, logs to ApprovalHistory, and advances to the next level in the hierarchy
        /// (or completes Final HR Sign-Off and applies Promotion/Request changes).
        /// </summary>
        [HttpPost(ApiConstants.Requests.Approve)]
        public async Task<IActionResult> ApproveCurrentStage(
            string requestCode,
            [FromBody] RequestDecisionDto decision)
        {
            var req = await _db.EmployeeRequests
                .Include(r => r.Employee)
                .Include(r => r.RequestedDesignation)
                .Include(r => r.ApprovalChain)
                .FirstOrDefaultAsync(r => r.RequestCode.ToLower() == requestCode.ToLower());

            if (req == null)
            {
                return NotFound(new { message = $"Request '{requestCode}' not found." });
            }

            var currentStep = req.ApprovalChain
                .OrderBy(s => s.LevelNumber)
                .FirstOrDefault(s => s.IsCurrent);

            if (currentStep == null)
            {
                return BadRequest(new { message = "No pending approval stage found for this request." });
            }

            currentStep.Status = "Approved";
            currentStep.IsCurrent = false;
            currentStep.Remarks = string.IsNullOrWhiteSpace(decision.Remarks)
                ? $"Approved at {currentStep.LevelTitle}."
                : decision.Remarks;
            currentStep.ActedAt = DateTime.UtcNow;

            if (decision.ApproverEmployeeId.HasValue)
            {
                currentStep.ApproverEmployeeId = decision.ApproverEmployeeId;
            }

            // Record immutable audit trail in approval_history
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                WorkflowId = req.ApprovalWorkflowId,
                EntityType = req.Category,
                EntityId = req.Id,
                StepOrder = currentStep.LevelNumber,
                ActedByEmployeeId = currentStep.ApproverEmployeeId,
                Status = "Approved",
                Comments = currentStep.Remarks,
                ActedAt = DateTime.UtcNow
            });

            var isFinalStage = currentStep.IsFinalHr || req.CurrentLevelNumber >= req.TotalLevels;

            if (isFinalStage)
            {
                req.OverallStatus = "Approved";
                req.CompletedAt = DateTime.UtcNow;

                // Automatic Business Effect on Final HR Approval:
                // If Promotion Request -> Elevate Employee's Designation, OrganizationalLevel, and System Role!
                if (req.Category == RequestCategories.PromotionRequest &&
                    req.Employee != null &&
                    req.RequestedDesignation != null)
                {
                    req.Employee.DesignationId = req.RequestedDesignation.Id;
                    req.Employee.OrganizationalLevelId = req.RequestedDesignation.OrganizationalLevelId;

                    if (!string.IsNullOrWhiteSpace(req.RequestedDesignation.DefaultRoleCode))
                    {
                        var newRole = await _db.Roles
                            .FirstOrDefaultAsync(r => r.Code == req.RequestedDesignation.DefaultRoleCode);
                        if (newRole != null)
                        {
                            var currentRoles = await _db.EmployeeRoles
                                .Where(er => er.EmployeeId == req.EmployeeId && er.IsPrimary)
                                .ToListAsync();
                            _db.EmployeeRoles.RemoveRange(currentRoles);

                            _db.EmployeeRoles.Add(new EmployeeRole
                            {
                                EmployeeId = req.EmployeeId,
                                RoleId = newRole.Id,
                                AssignedDepartmentId = req.Employee.DepartmentId,
                                IsPrimary = true
                            });
                        }
                    }
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    isFinal = true,
                    requestCode = req.RequestCode,
                    overallStatus = req.OverallStatus,
                    message = $"Final HR sign-off completed for {req.RequestCode} ({req.Category})! Request is now fully Approved."
                });
            }

            // Advance to next level in the approval hierarchy
            req.CurrentLevelNumber++;
            req.OverallStatus = "Under Review";

            var nextStep = req.ApprovalChain
                .FirstOrDefault(s => s.LevelNumber == req.CurrentLevelNumber);

            if (nextStep != null)
            {
                nextStep.IsCurrent = true;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                isFinal = false,
                requestCode = req.RequestCode,
                overallStatus = req.OverallStatus,
                currentLevelNumber = req.CurrentLevelNumber,
                nextStage = nextStep?.LevelTitle,
                message = $"Approved at {currentStep.LevelTitle}. Forwarded to {nextStep?.LevelTitle}."
            });
        }

        /// <summary>
        /// Rejects the request at the current stage and logs an audit entry in ApprovalHistory.
        /// </summary>
        [HttpPost(ApiConstants.Requests.Reject)]
        public async Task<IActionResult> RejectCurrentStage(
            string requestCode,
            [FromBody] RequestDecisionDto decision)
        {
            var req = await _db.EmployeeRequests
                .Include(r => r.ApprovalChain)
                .FirstOrDefaultAsync(r => r.RequestCode.ToLower() == requestCode.ToLower());

            if (req == null)
            {
                return NotFound(new { message = $"Request '{requestCode}' not found." });
            }

            var currentStep = req.ApprovalChain.FirstOrDefault(s => s.IsCurrent);
            if (currentStep != null)
            {
                currentStep.Status = "Rejected";
                currentStep.IsCurrent = false;
                currentStep.Remarks = string.IsNullOrWhiteSpace(decision.Remarks)
                    ? "Declined during review stage."
                    : decision.Remarks;
                currentStep.ActedAt = DateTime.UtcNow;

                _db.ApprovalHistories.Add(new ApprovalHistory
                {
                    WorkflowId = req.ApprovalWorkflowId,
                    EntityType = req.Category,
                    EntityId = req.Id,
                    StepOrder = currentStep.LevelNumber,
                    ActedByEmployeeId = decision.ApproverEmployeeId ?? currentStep.ApproverEmployeeId,
                    Status = "Rejected",
                    Comments = currentStep.Remarks,
                    ActedAt = DateTime.UtcNow
                });
            }

            req.OverallStatus = "Rejected";
            req.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                requestCode = req.RequestCode,
                overallStatus = req.OverallStatus,
                message = $"Request {req.RequestCode} has been rejected at {currentStep?.LevelTitle}."
            });
        }
    }
}

