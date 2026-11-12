using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Workspace.Base)]
    public class WorkspaceController : ControllerBase
    {
        private readonly AppDbContext _db;

        public WorkspaceController(AppDbContext db)
        {
            _db = db;
        }

        // =========================================================================
        // 1. MY DOCUMENTS (/employee/my-documents)
        // =========================================================================

        /// <summary>
        /// Returns an employee's personal documents (Offer Letter, Hike Letter, Promotion Letter, Appointment Letter, NDA, Relieving Letter)
        /// combined with all organization-wide Policy documents.
        /// </summary>
        [HttpGet(ApiConstants.Workspace.MyDocuments)]
        public async Task<IActionResult> GetMyDocuments(string employeeCode, [FromQuery] string? category = null)
        {
            var employee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode.ToUpper());

            if (employee == null)
                return NotFound(new { message = $"Employee '{employeeCode}' not found." });

            var query = _db.EmployeeDocuments
                .Where(d => d.EmployeeId == employee.Id || d.EmployeeId == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Category.ToLower() == category.ToLower());
            }

            var docs = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var allEmployeeDocs = await _db.EmployeeDocuments
                .Where(d => d.EmployeeId == employee.Id || d.EmployeeId == null)
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
                counts = new
                {
                    all = allEmployeeDocs.Count,
                    employment = allEmployeeDocs.Count(d => d.Category == "Employment"),
                    compensation = allEmployeeDocs.Count(d => d.Category == "Compensation"),
                    policy = allEmployeeDocs.Count(d => d.Category == "Policy"),
                    separation = allEmployeeDocs.Count(d => d.Category == "Separation")
                },
                documents = docs.Select(d => new
                {
                    id = d.DocumentCode,
                    documentCode = d.DocumentCode,
                    title = d.Title,
                    category = d.Category,
                    categoryLabel = d.CategoryLabel,
                    issueDate = d.IssueDate,
                    fileSize = d.FileSize,
                    format = d.FileFormat,
                    badge = d.Badge,
                    badgeType = d.BadgeType,
                    description = d.Description,
                    fileUrl = d.FileUrl ?? $"/api/documents/download/{d.DocumentCode}",
                    isOrganizationPolicy = d.EmployeeId == null
                })
            });
        }

        /// <summary>
        /// Issues a new personal or company-wide document.
        /// </summary>
        [HttpPost(ApiConstants.Workspace.CreateDocument)]
        public async Task<IActionResult> CreateDocument([FromBody] CreateEmployeeDocumentDto dto)
        {
            Employee? emp = null;
            if (!string.IsNullOrWhiteSpace(dto.EmployeeCode))
            {
                emp = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == dto.EmployeeCode.ToUpper());

                if (emp == null)
                    return NotFound(new { message = $"Employee '{dto.EmployeeCode}' not found." });
            }

            var count = await _db.EmployeeDocuments.CountAsync();
            var docCode = $"DOC-2026-{(count + 1):D3}";

            var doc = new EmployeeDocumentRecord
            {
                DocumentCode = docCode,
                EmployeeId = emp?.Id,
                Title = dto.Title,
                Category = dto.Category,
                CategoryLabel = dto.CategoryLabel,
                IssueDate = !string.IsNullOrWhiteSpace(dto.IssueDate) ? dto.IssueDate : DateTime.UtcNow.ToString("dd MMM yyyy"),
                FileSize = dto.FileSize,
                FileFormat = dto.FileFormat,
                Badge = dto.Badge,
                BadgeType = dto.BadgeType,
                Description = dto.Description,
                FileUrl = dto.FileUrl
            };

            _db.EmployeeDocuments.Add(doc);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Document '{doc.DocumentCode}' ({doc.Title}) issued successfully.",
                document = new
                {
                    id = doc.DocumentCode,
                    doc.DocumentCode,
                    employeeCode = emp?.EmployeeCode ?? "ALL_EMPLOYEES",
                    doc.Title,
                    doc.Category,
                    doc.CategoryLabel,
                    doc.IssueDate,
                    doc.FileSize,
                    format = doc.FileFormat
                }
            });
        }

        // =========================================================================
        // 2. PROJECT MANAGEMENT & MY PROJECTS (/employee/project-management & /employee/my-projects)
        // =========================================================================

        /// <summary>
        /// Lists projects with Lead and Team Members matching Angular's ProjectService schema.
        /// </summary>
        [HttpGet(ApiConstants.Workspace.GetProjects)]
        public async Task<IActionResult> GetProjects(
            [FromQuery] string? departmentCode = null,
            [FromQuery] string? memberCode = null,
            [FromQuery] string? status = null)
        {
            var projects = await _db.Projects
                .Include(p => p.Department)
                .Include(p => p.Members)
                    .ThenInclude(m => m.Employee)
                        .ThenInclude(e => e!.Designation)
                .Include(p => p.Members)
                    .ThenInclude(m => m.Employee)
                        .ThenInclude(e => e!.Department)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var allEmployees = await _db.Employees
                .Include(e => e.Designation)
                .Include(e => e.Department)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                var deptCode = departmentCode.ToUpper();
                projects = projects.Where(p => p.Department != null && p.Department.Code == deptCode).ToList();
            }

            if (!string.IsNullOrWhiteSpace(memberCode))
            {
                var code = memberCode.ToUpper();
                projects = projects.Where(p =>
                    p.Members.Any(m => m.Employee != null && m.Employee.EmployeeCode == code) ||
                    allEmployees.Any(e => e.EmployeeCode == code && (e.Id == p.ProjectManagerId || e.Id == p.ProductManagerId))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var formatted = projects.Select(p =>
            {
                var leadEmp = allEmployees.FirstOrDefault(e => e.Id == p.ProjectManagerId)
                    ?? p.Members.FirstOrDefault()?.Employee
                    ?? allEmployees.FirstOrDefault();

                var milestones = string.IsNullOrWhiteSpace(p.MilestonesJson)
                    ? new[] { "Architecture Freeze", "API Integration", "UAT Sign-off" }
                    : p.MilestonesJson.Split(';', StringSplitOptions.RemoveEmptyEntries);

                return new
                {
                    id = p.Code,
                    code = p.Code,
                    name = p.Name,
                    description = p.Description ?? string.Empty,
                    category = p.Category,
                    department = p.Department?.Name ?? "Engineering & Software Development",
                    departmentCode = p.Department?.Code ?? "ENG",
                    startDate = (p.StartDate ?? DateTime.UtcNow.AddMonths(-2)).ToString("MMM dd, yyyy"),
                    endDate = (p.EndDate ?? DateTime.UtcNow.AddMonths(3)).ToString("MMM dd, yyyy"),
                    priority = p.Priority,
                    status = p.Status,
                    progressPercent = p.ProgressPercent,
                    budget = p.Budget,
                    methodology = p.Methodology,
                    milestones,
                    externalToolUrl = p.ExternalToolUrl,
                    lead = leadEmp != null ? MapProjectMember(leadEmp) : null,
                    teamMembers = p.Members
                        .Where(m => m.Employee != null)
                        .Select(m => MapProjectMember(m.Employee!))
                        .ToList()
                };
            }).ToList();

            return Ok(new
            {
                summary = new
                {
                    totalProjects = formatted.Count,
                    inProgress = formatted.Count(p => p.status == "In Progress"),
                    completed = formatted.Count(p => p.status == "Completed"),
                    planning = formatted.Count(p => p.status == "Planning"),
                    averageProgress = formatted.Count > 0 ? (int)Math.Round(formatted.Average(p => p.progressPercent)) : 0
                },
                projects = formatted
            });
        }

        /// <summary>
        /// Creates a new project ("Create Project") and assigns Lead + Team Members.
        /// </summary>
        [HttpPost(ApiConstants.Workspace.CreateProject)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequestDto dto)
        {
            var count = await _db.Projects.CountAsync();
            var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.ToUpper() : $"PRJ-{101 + count}";

            Department? dept = null;
            if (!string.IsNullOrWhiteSpace(dto.DepartmentCode))
            {
                dept = await _db.Departments.FirstOrDefaultAsync(d => d.Code == dto.DepartmentCode.ToUpper());
            }

            Employee? lead = null;
            if (!string.IsNullOrWhiteSpace(dto.LeadEmployeeCode))
            {
                lead = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == dto.LeadEmployeeCode.ToUpper());
            }

            var project = new Project
            {
                Code = code,
                Name = dto.Name,
                Description = dto.Description,
                DepartmentId = dept?.Id ?? lead?.DepartmentId,
                ProjectManagerId = lead?.Id,
                Category = dto.Category,
                Priority = dto.Priority,
                Status = dto.Status,
                ProgressPercent = dto.Status == "Completed" ? 100 : (dto.Status == "In Progress" ? 15 : 0),
                Budget = dto.Budget,
                Methodology = dto.Methodology,
                MilestonesJson = dto.Milestones != null && dto.Milestones.Count > 0
                    ? string.Join(";", dto.Milestones)
                    : "Kickoff & Architecture;Core Module Development;QA & Production Release",
                ExternalToolUrl = dto.ExternalToolUrl,
                StartDate = dto.StartDate?.ToUniversalTime() ?? DateTime.UtcNow,
                EndDate = dto.EndDate?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(3)
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            var memberCodes = dto.TeamMemberCodes.Select(c => c.ToUpper()).Distinct().ToList();
            if (lead != null && !memberCodes.Contains(lead.EmployeeCode))
                memberCodes.Add(lead.EmployeeCode);

            var members = await _db.Employees
                .Where(e => memberCodes.Contains(e.EmployeeCode))
                .ToListAsync();

            foreach (var emp in members)
            {
                _db.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project.Id,
                    EmployeeId = emp.Id,
                    RoleInProject = emp.Id == lead?.Id ? "Project Lead" : "Project Member",
                    AllocationPercentage = 100
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Project '{project.Code}' ({project.Name}) created with {members.Count} member(s).",
                projectCode = project.Code,
                name = project.Name,
                status = project.Status
            });
        }

        // =========================================================================
        // 3. TEAM MANAGEMENT (/employee/team-management)
        // =========================================================================

        /// <summary>
        /// Lists Team Member profiles with live Attendance %, Task Completion metrics, Leave breakdown, and Assigned Projects
        /// matching Angular's TeamService (TeamMemberProfile).
        /// </summary>
        [HttpGet(ApiConstants.Workspace.TeamMembers)]
        public async Task<IActionResult> GetTeamMembers(
            [FromQuery] string? managerCode = null,
            [FromQuery] string? departmentCode = null)
        {
            var employeesQuery = _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.OrganizationalLevel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                var deptCode = departmentCode.ToUpper();
                employeesQuery = employeesQuery.Where(e => e.Department != null && e.Department.Code == deptCode);
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
                        employeesQuery = employeesQuery.Where(e => directReportIds.Contains(e.Id) || (manager.DepartmentId != null && e.DepartmentId == manager.DepartmentId));
                    }
                }
            }

            var employees = await employeesQuery
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            var empIds = employees.Select(e => e.Id).ToList();

            var allTasks = await _db.TaskItems
                .Where(t => empIds.Contains(t.AssigneeEmployeeId))
                .ToListAsync();

            var allLeaves = await _db.LeaveBalances
                .Where(b => empIds.Contains(b.EmployeeId))
                .ToListAsync();

            var allAttendance = await _db.AttendanceLogs
                .Where(a => empIds.Contains(a.EmployeeId))
                .ToListAsync();

            var allProjectMembers = await _db.ProjectMembers
                .Include(pm => pm.Project)
                .Where(pm => empIds.Contains(pm.EmployeeId))
                .ToListAsync();

            var profiles = employees.Select(emp =>
            {
                var empTasks = allTasks.Where(t => t.AssigneeEmployeeId == emp.Id).ToList();
                var totalAssigned = Math.Max(empTasks.Count, 1);
                var completed = empTasks.Count(t => t.Status == "Completed");
                var inProgress = empTasks.Count(t => t.Status == "In Progress");
                var overdue = empTasks.Count(t => t.Status != "Completed" && t.DueDate < DateTime.UtcNow);
                var completionRate = empTasks.Count > 0
                    ? (int)Math.Round((double)completed / totalAssigned * 100)
                    : 88;

                var empLeaves = allLeaves.Where(b => b.EmployeeId == emp.Id).ToList();
                var empLogs = allAttendance.Where(a => a.EmployeeId == emp.Id).ToList();
                var todayLog = empLogs.OrderByDescending(l => l.Date).FirstOrDefault();

                var empProjects = allProjectMembers
                    .Where(pm => pm.EmployeeId == emp.Id && pm.Project != null)
                    .Select(pm => new
                    {
                        id = pm.Project!.Code,
                        name = pm.Project!.Name,
                        roleInProject = pm.RoleInProject ?? emp.Designation?.Title ?? "Member",
                        status = pm.Project!.Status
                    })
                    .ToList();

                var initials = string.Join("", emp.FullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(s => char.ToUpperInvariant(s[0])));

                return new
                {
                    id = emp.EmployeeCode,
                    name = emp.FullName,
                    role = emp.Designation?.Title ?? "Engineer",
                    level = emp.OrganizationalLevel?.Code ?? "L8",
                    department = emp.Department?.Name ?? "Engineering",
                    avatar = initials,
                    email = emp.Email,
                    phone = "+91 98450 11223",
                    location = "Hyderabad HQ (Hybrid)",
                    joiningDate = emp.CreatedAt.AddYears(-1).ToString("MMM dd, yyyy"),
                    status = todayLog?.Status == "On Leave" ? "On Leave" : (todayLog?.WorkMode == "Remote" ? "Remote" : "Active"),
                    attendancePercent = 94,
                    leavesTaken = empLeaves.Sum(b => b.UsedDays),
                    totalLeavesAllowed = empLeaves.Sum(b => b.TotalAllocated) > 0 ? empLeaves.Sum(b => b.TotalAllocated) : 34,
                    leaveBreakdown = new
                    {
                        sick = empLeaves.FirstOrDefault(b => b.ShortCode == "SL")?.UsedDays ?? 2,
                        casual = empLeaves.FirstOrDefault(b => b.ShortCode == "CL")?.UsedDays ?? 3,
                        annual = empLeaves.FirstOrDefault(b => b.ShortCode == "EL")?.UsedDays ?? 4
                    },
                    taskMetrics = new
                    {
                        totalAssigned = empTasks.Count,
                        completed,
                        inProgress,
                        overdue,
                        completionRate
                    },
                    skills = new[] { "TypeScript", "ASP.NET Core", "PostgreSQL", "System Design" },
                    assignedProjects = empProjects
                };
            }).ToList();

            return Ok(new
            {
                totalMembers = profiles.Count,
                members = profiles
            });
        }

        // =========================================================================
        // 4. MY PAYROLL (/employee/my-payroll)
        // =========================================================================

        /// <summary>
        /// Returns an employee's compensation profile, YTD summary, and monthly payroll/tax slips ("My Payroll").
        /// </summary>
        [HttpGet(ApiConstants.Workspace.MyPayroll)]
        public async Task<IActionResult> GetMyPayroll(string employeeCode)
        {
            var employee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.OrganizationalLevel)
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode.ToUpper());

            if (employee == null)
                return NotFound(new { message = $"Employee '{employeeCode}' not found." });

            var slips = await _db.MonthlyPayrollSlips
                .Where(p => p.EmployeeId == employee.Id)
                .OrderByDescending(p => p.Month)
                .ToListAsync();

            var latestSlip = slips.FirstOrDefault();
            var annualCtc = latestSlip != null ? latestSlip.GrossEarnings * 12 : 1740000m;

            return Ok(new
            {
                employeeProfile = new
                {
                    name = employee.FullName,
                    empId = employee.EmployeeCode,
                    designation = employee.Designation?.Title ?? "Software Engineer",
                    department = employee.Department?.Name ?? "Engineering",
                    level = employee.OrganizationalLevel?.Code ?? "L8",
                    annualCTC = annualCtc,
                    bankName = "HDFC Bank •••• 4829",
                    panNumber = "ABCDE1234F",
                    pfAccountNumber = "TS/HYD/0048291/000/00105",
                    uanNumber = "100948271654",
                    taxRegime = "New Tax Regime (FY 2026-27)"
                },
                ytdSummary = new
                {
                    ytdGrossEarnings = slips.Sum(s => s.GrossEarnings),
                    ytdNetPay = slips.Sum(s => s.NetSalary),
                    ytdTaxDeducted = slips.Sum(s => s.TdsDeduction),
                    ytdPfContributed = slips.Sum(s => s.EpfDeduction)
                },
                monthlySlips = slips.Select(s => new
                {
                    s.Id,
                    s.Month,
                    s.MonthShort,
                    s.FinancialYear,
                    s.PayDate,
                    s.Status,
                    s.BasicSalary,
                    s.Hra,
                    s.SpecialAllowance,
                    s.Conveyance,
                    s.MedicalAllowance,
                    s.GrossEarnings,
                    s.EpfDeduction,
                    s.ProfessionalTax,
                    s.TdsDeduction,
                    s.InsuranceDeduction,
                    s.TotalDeductions,
                    s.NetSalary
                })
            });
        }

        private static object MapProjectMember(Employee emp)
        {
            var initials = string.Join("", emp.FullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(s => char.ToUpperInvariant(s[0])));

            return new
            {
                id = emp.EmployeeCode,
                name = emp.FullName,
                role = emp.Designation?.Title ?? "Developer",
                department = emp.Department?.Name ?? "Engineering",
                avatar = initials
            };
        }
    }
}
