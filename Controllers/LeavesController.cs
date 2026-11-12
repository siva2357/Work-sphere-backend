using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Leaves.Base)]
    public class LeavesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LeavesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Individual Employee Leave Dashboard ("My Leaves" & "Apply Leave"):
        /// Returns CL, SL, EL, CO leave balances + personal leave records.
        /// </summary>
        [HttpGet(ApiConstants.Leaves.MyLeaves)]
        public async Task<IActionResult> GetMyLeaves(string employeeCode)
        {
            var employee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode.ToUpper());

            if (employee == null)
                return NotFound(new { message = $"Employee '{employeeCode}' not found." });

            var balances = await _db.LeaveBalances
                .Where(b => b.EmployeeId == employee.Id)
                .OrderBy(b => b.ShortCode)
                .ToListAsync();

            var proposals = await _db.LeaveProposals
                .Include(lp => lp.ApprovedByEmployee)
                .Where(lp => lp.EmployeeId == employee.Id)
                .OrderByDescending(lp => lp.AppliedAt)
                .ToListAsync();

            return Ok(new
            {
                employee = new
                {
                    employee.EmployeeCode,
                    employee.FullName,
                    department = employee.Department?.Name,
                    designation = employee.Designation?.Title
                },
                summary = new
                {
                    totalYearlyAllowed = balances.Sum(b => b.TotalAllocated),
                    totalUsed = balances.Sum(b => b.UsedDays),
                    totalAvailable = balances.Sum(b => b.AvailableDays),
                    pendingRequestsCount = proposals.Count(p => p.Status == "Pending")
                },
                leaveBalances = balances.Select(b => new
                {
                    type = b.LeaveType,
                    shortCode = b.ShortCode,
                    total = b.TotalAllocated,
                    used = b.UsedDays,
                    available = b.AvailableDays,
                    colorClass = b.ColorClass
                }),
                leaveRecords = proposals.Select(p => new
                {
                    id = p.LeaveCode,
                    leaveCode = p.LeaveCode,
                    leaveType = p.LeaveType,
                    fromDate = p.FromDate.ToString("dd MMM yyyy"),
                    toDate = p.ToDate.ToString("dd MMM yyyy"),
                    totalDays = p.Days,
                    reason = p.Reason,
                    appliedOn = p.AppliedAt.ToString("dd MMM yyyy"),
                    status = p.Status,
                    approvedBy = p.ApprovedByEmployee?.FullName ?? "Reporting Manager",
                    managerComments = p.ManagerComments
                })
            });
        }

        /// <summary>
        /// Manager / Department Leave Management ("Leave Management"):
        /// Lists leave proposals across team members reporting to a Manager or within a Department.
        /// </summary>
        [HttpGet(ApiConstants.Leaves.TeamLeaves)]
        public async Task<IActionResult> GetTeamLeaveProposals(
            [FromQuery] string? managerCode = null,
            [FromQuery] string? departmentCode = null,
            [FromQuery] string? status = null)
        {
            var query = _db.LeaveProposals
                .Include(lp => lp.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(lp => lp.Employee)
                    .ThenInclude(e => e!.Designation)
                .Include(lp => lp.ApprovedByEmployee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                var deptCode = departmentCode.ToUpper();
                query = query.Where(lp => lp.Employee != null && lp.Employee.Department != null && lp.Employee.Department.Code == deptCode);
            }
            else if (!string.IsNullOrWhiteSpace(managerCode))
            {
                var manager = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == managerCode.ToUpper());

                if (manager != null)
                {
                    var directReportIds = await _db.ReportingRelationships
                        .Where(r => r.ManagerEmployeeId == manager.Id && r.IsActive)
                        .Select(r => r.EmployeeId)
                        .ToListAsync();

                    if (directReportIds.Count > 0)
                    {
                        query = query.Where(lp => directReportIds.Contains(lp.EmployeeId) || (manager.DepartmentId != null && lp.Employee != null && lp.Employee.DepartmentId == manager.DepartmentId));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(lp => lp.Status.ToLower() == status.ToLower());
            }

            var proposals = await query
                .OrderByDescending(lp => lp.AppliedAt)
                .ToListAsync();

            var empIds = proposals.Select(p => p.EmployeeId).Distinct().ToList();
            var balances = await _db.LeaveBalances
                .Where(b => empIds.Contains(b.EmployeeId))
                .ToListAsync();

            var formatted = proposals.Select(p =>
            {
                var empName = p.Employee?.FullName ?? "Employee";
                var initials = string.Join("", empName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(s => char.ToUpperInvariant(s[0])));

                var empBalance = balances
                    .Where(b => b.EmployeeId == p.EmployeeId)
                    .Sum(b => b.AvailableDays);

                return new
                {
                    id = p.LeaveCode,
                    leaveCode = p.LeaveCode,
                    employeeName = empName,
                    employeeCode = p.Employee?.EmployeeCode,
                    role = p.Employee?.Designation?.Title ?? "Team Member",
                    department = p.Employee?.Department?.Name ?? "Engineering",
                    avatar = initials,
                    leaveType = p.LeaveType,
                    startDate = p.FromDate.ToString("MMM dd, yyyy"),
                    endDate = p.ToDate.ToString("MMM dd, yyyy"),
                    days = p.Days,
                    reason = p.Reason,
                    status = p.Status,
                    appliedOn = p.AppliedAt.ToString("MMM dd, yyyy"),
                    leaveBalanceRemaining = empBalance > 0 ? empBalance : 14,
                    approvedBy = p.ApprovedByEmployee?.FullName,
                    managerComments = p.ManagerComments
                };
            }).ToList();

            return Ok(new
            {
                summary = new
                {
                    totalProposals = formatted.Count,
                    pendingCount = formatted.Count(p => p.status == "Pending"),
                    approvedCount = formatted.Count(p => p.status == "Approved"),
                    rejectedCount = formatted.Count(p => p.status == "Rejected"),
                    totalLeaveDaysRequested = formatted.Sum(p => p.days)
                },
                proposals = formatted
            });
        }

        /// <summary>
        /// Submits a new Leave Proposal ("Apply Leave").
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ApplyLeave([FromBody] CreateLeaveProposalDto dto)
        {
            var employee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeCode == dto.EmployeeCode.ToUpper());

            if (employee == null)
                return NotFound(new { message = $"Employee '{dto.EmployeeCode}' not found." });

            var count = await _db.LeaveProposals.CountAsync();
            var leaveCode = $"LV-REQ-{501 + count}";

            var days = dto.Days.HasValue && dto.Days.Value > 0
                ? dto.Days.Value
                : Math.Max(1, (dto.ToDate.Date - dto.FromDate.Date).Days + 1);

            var proposal = new LeaveProposal
            {
                LeaveCode = leaveCode,
                EmployeeId = employee.Id,
                LeaveType = dto.LeaveType,
                FromDate = dto.FromDate.ToUniversalTime(),
                ToDate = dto.ToDate.ToUniversalTime(),
                Days = days,
                Reason = dto.Reason,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _db.LeaveProposals.Add(proposal);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Leave application '{proposal.LeaveCode}' submitted for {employee.FullName} ({days} day(s)).",
                proposal = new
                {
                    id = proposal.LeaveCode,
                    proposal.LeaveCode,
                    employeeCode = employee.EmployeeCode,
                    employeeName = employee.FullName,
                    proposal.LeaveType,
                    startDate = proposal.FromDate.ToString("MMM dd, yyyy"),
                    endDate = proposal.ToDate.ToString("MMM dd, yyyy"),
                    proposal.Days,
                    proposal.Reason,
                    proposal.Status
                }
            });
        }

        /// <summary>
        /// Manager approves a pending Leave Proposal and deducts used days from the employee's LeaveBalanceRecord.
        /// </summary>
        [HttpPost(ApiConstants.Leaves.Approve)]
        public async Task<IActionResult> ApproveLeave(string leaveCode, [FromBody] LeaveDecisionDto dto)
        {
            var proposal = await _db.LeaveProposals
                .Include(lp => lp.Employee)
                .FirstOrDefaultAsync(lp => lp.LeaveCode == leaveCode.ToUpper());

            if (proposal == null)
                return NotFound(new { message = $"Leave proposal '{leaveCode}' not found." });

            var manager = await _db.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == dto.ManagerEmployeeCode.ToUpper());

            proposal.Status = "Approved";
            proposal.ApprovedByEmployeeId = manager?.Id;
            proposal.ManagerComments = dto.Comments ?? "Approved by Manager.";
            proposal.ActedAt = DateTime.UtcNow;

            // Update LeaveBalanceRecord if matching leave type exists
            var shortCode = proposal.LeaveType switch
            {
                var s when s.Contains("Casual", StringComparison.OrdinalIgnoreCase) => "CL",
                var s when s.Contains("Sick", StringComparison.OrdinalIgnoreCase) => "SL",
                var s when s.Contains("Comp", StringComparison.OrdinalIgnoreCase) => "CO",
                _ => "EL"
            };

            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == proposal.EmployeeId && b.ShortCode == shortCode);

            if (balance != null)
            {
                balance.UsedDays += proposal.Days;
                balance.AvailableDays = Math.Max(0, balance.TotalAllocated - balance.UsedDays);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Leave proposal '{proposal.LeaveCode}' for {proposal.Employee?.FullName} approved.",
                leaveCode = proposal.LeaveCode,
                status = proposal.Status,
                approvedBy = manager?.FullName ?? dto.ManagerEmployeeCode,
                managerComments = proposal.ManagerComments
            });
        }

        /// <summary>
        /// Manager rejects a pending Leave Proposal.
        /// </summary>
        [HttpPost(ApiConstants.Leaves.Reject)]
        public async Task<IActionResult> RejectLeave(string leaveCode, [FromBody] LeaveDecisionDto dto)
        {
            var proposal = await _db.LeaveProposals
                .Include(lp => lp.Employee)
                .FirstOrDefaultAsync(lp => lp.LeaveCode == leaveCode.ToUpper());

            if (proposal == null)
                return NotFound(new { message = $"Leave proposal '{leaveCode}' not found." });

            var manager = await _db.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == dto.ManagerEmployeeCode.ToUpper());

            proposal.Status = "Rejected";
            proposal.ApprovedByEmployeeId = manager?.Id;
            proposal.ManagerComments = dto.Comments ?? "Declined due to sprint delivery commitments.";
            proposal.ActedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Leave proposal '{proposal.LeaveCode}' for {proposal.Employee?.FullName} rejected.",
                leaveCode = proposal.LeaveCode,
                status = proposal.Status,
                rejectedBy = manager?.FullName ?? dto.ManagerEmployeeCode,
                managerComments = proposal.ManagerComments
            });
        }
    }
}

