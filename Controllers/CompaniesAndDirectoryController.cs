using System.IO;
using System.Text;
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
    [Route(ApiConstants.Companies.Base)]
    public class CompaniesAndDirectoryController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<Employee> _passwordHasher = new();

        public CompaniesAndDirectoryController(AppDbContext db)
        {
            _db = db;
        }

        // =========================================================================
        // 1. COMPANIES (External Main Website Company Fetch & Registration)
        // =========================================================================

        /// <summary>
        /// Fetches all companies registered on the platform (simulating the External Main Website company feed).
        /// Returns the 2 dummy companies (CMP-001: WorkSphere Technologies, CMP-002: NexaCloud Enterprise)
        /// and their Main HR Administrator accounts.
        /// </summary>
        [HttpGet(ApiConstants.Companies.GetAll)]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _db.Companies
                .Include(c => c.Employees)
                .OrderBy(c => c.CompanyCode)
                .ToListAsync();

            return Ok(new
            {
                platformAccessFlow = new
                {
                    step1 = "Company registers on the External Main Website (e.g., CMP-001 or CMP-002).",
                    step2 = "Platform automatically provisions the Company's Main HR Administrator account (H1 - CHRO / Main HR).",
                    step3 = "Only Main HR logs in and adds employees inside the HR Employee Directory (/api/companies/{companyCode}/directory/employees) with their Department, Level (L1-L10 / H1-H6), and System Role.",
                    step4 = "Individual employees can then log into Work-Sphere (/api/auth/login) with their assigned company access and permissions."
                },
                totalCompanies = companies.Count,
                companies = companies.Select(c =>
                {
                    var mainHr = c.Employees.FirstOrDefault(e => e.IsMainHr);
                    return new
                    {
                        c.Id,
                        c.CompanyCode,
                        c.ExternalRegistrationId,
                        c.Name,
                        c.Domain,
                        c.Industry,
                        c.CompanySize,
                        c.Headquarters,
                        c.ExternalWebsiteUrl,
                        c.RegistrationSource,
                        c.SubscriptionPlan,
                        c.IsActive,
                        registeredAt = c.RegisteredAt.ToString("dd MMM yyyy"),
                        mainHrAccess = new
                        {
                            employeeId = mainHr?.Id ?? c.MainHrEmployeeId,
                            employeeCode = mainHr?.EmployeeCode ?? "EMP-006",
                            fullName = mainHr?.FullName ?? c.MainHrName,
                            email = mainHr?.Email ?? c.MainHrEmail,
                            portalType = "hr",
                            level = "H1 (Main HR / CHRO)",
                            canCreateEmployeesInDirectory = true
                        },
                        stats = new
                        {
                            totalEmployees = c.Employees.Count,
                            hrPortalUsers = c.Employees.Count(e => e.PortalType == PortalTypes.Hr),
                            employeePortalUsers = c.Employees.Count(e => e.PortalType == PortalTypes.Employee)
                        }
                    };
                })
            });
        }

        /// <summary>
        /// Fetches a single company by companyCode (e.g. CMP-001 or CMP-002) with its Main HR details.
        /// </summary>
        [HttpGet(ApiConstants.Companies.GetByCode)]
        public async Task<IActionResult> GetCompanyByCode(string companyCode)
        {
            var company = await _db.Companies
                .Include(c => c.Employees)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employees)
                    .ThenInclude(e => e.Designation)
                .Include(c => c.Employees)
                    .ThenInclude(e => e.OrganizationalLevel)
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode.ToUpper());

            if (company == null)
                return NotFound(new { message = $"Company '{companyCode}' not found." });

            var mainHrUsers = company.Employees
                .Where(e => e.IsMainHr)
                .Select(e => new
                {
                    e.EmployeeCode,
                    e.FullName,
                    e.Email,
                    designation = e.Designation?.Title,
                    level = e.OrganizationalLevel?.Code,
                    e.IsMainHr
                });

            return Ok(new
            {
                company.Id,
                company.CompanyCode,
                company.ExternalRegistrationId,
                company.Name,
                company.Domain,
                company.Industry,
                company.CompanySize,
                company.Headquarters,
                company.ExternalWebsiteUrl,
                company.RegistrationSource,
                company.SubscriptionPlan,
                mainHrUsers,
                totalEmployees = company.Employees.Count
            });
        }

        /// <summary>
        /// Simulates the External Main Website registering a new company and provisioning its Main HR Account.
        /// </summary>
        [HttpPost(ApiConstants.Companies.ExternalRegister)]
        public async Task<IActionResult> RegisterCompanyFromExternalWebsite([FromBody] ExternalCompanyRegistrationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Domain) || string.IsNullOrWhiteSpace(dto.MainHrEmail))
            {
                return BadRequest(new { message = "Company Name, Domain, and MainHrEmail are required." });
            }

            var normalizedDomain = dto.Domain.Trim().ToLowerInvariant();
            if (await _db.Companies.AnyAsync(c => c.Domain.ToLower() == normalizedDomain))
            {
                return Conflict(new { message = $"A company with domain '{normalizedDomain}' is already registered." });
            }

            var normalizedHrEmail = dto.MainHrEmail.Trim().ToLowerInvariant();
            if (await _db.Employees.AnyAsync(e => e.Email.ToLower() == normalizedHrEmail))
            {
                return Conflict(new { message = $"An account with email '{normalizedHrEmail}' already exists." });
            }

            var count = await _db.Companies.CountAsync();
            var companyCode = $"CMP-{(count + 1):D3}";
            var extRegId = $"EXT-REG-2026-{(count + 1):D3}";

            var company = new Company
            {
                CompanyCode = companyCode,
                ExternalRegistrationId = extRegId,
                Name = dto.Name.Trim(),
                Domain = normalizedDomain,
                Industry = dto.Industry,
                CompanySize = dto.CompanySize,
                Headquarters = dto.Headquarters,
                ExternalWebsiteUrl = $"https://worksphere.com/companies/{normalizedDomain.Replace('.', '-')}",
                RegistrationSource = "EXTERNAL_MAIN_WEBSITE",
                SubscriptionPlan = dto.SubscriptionPlan,
                MainHrEmail = normalizedHrEmail,
                MainHrName = dto.MainHrFullName.Trim(),
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var hrDept = await _db.Departments.FirstOrDefaultAsync(d => d.Code == "HR");
            var h1Level = await _db.OrganizationalLevels.FirstOrDefaultAsync(l => l.Code == "H1");
            var chroDesig = await _db.Designations.FirstOrDefaultAsync(d => d.Code == "DESIG_CHRO");
            var chroRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == SystemRoleCodes.Chro);
            var hrMgrRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == SystemRoleCodes.HrManager);

            var empCount = await _db.Employees.CountAsync();
            var mainHrEmployee = new Employee
            {
                EmployeeCode = $"{companyCode.Replace("CMP-", "HR-")}-{(empCount + 1):D2}",
                FullName = dto.MainHrFullName.Trim(),
                Email = normalizedHrEmail,
                PortalType = PortalTypes.Hr,
                CompanyId = company.Id,
                IsMainHr = true,
                DepartmentId = hrDept?.Id,
                DesignationId = chroDesig?.Id,
                OrganizationalLevelId = h1Level?.Id,
                Phone = dto.MainHrPhone ?? "+91 98000 00000",
                EmploymentType = "Full-time",
                WorkLocation = dto.Headquarters,
                WorkStatus = "Active"
            };
            mainHrEmployee.PasswordHash = _passwordHasher.HashPassword(mainHrEmployee, dto.MainHrPassword);

            _db.Employees.Add(mainHrEmployee);
            await _db.SaveChangesAsync();

            company.MainHrEmployeeId = mainHrEmployee.Id;

            if (chroRole != null)
            {
                _db.EmployeeRoles.Add(new EmployeeRole
                {
                    EmployeeId = mainHrEmployee.Id,
                    RoleId = chroRole.Id,
                    IsPrimary = true
                });
            }

            if (hrMgrRole != null)
            {
                _db.EmployeeRoles.Add(new EmployeeRole
                {
                    EmployeeId = mainHrEmployee.Id,
                    RoleId = hrMgrRole.Id,
                    IsPrimary = false
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Company '{company.Name}' ({company.CompanyCode}) registered from External Main Website and Main HR account provisioned.",
                company = new
                {
                    company.Id,
                    company.CompanyCode,
                    company.ExternalRegistrationId,
                    company.Name,
                    company.Domain,
                    company.Industry,
                    company.Headquarters,
                    company.SubscriptionPlan
                },
                mainHrAccount = new
                {
                    mainHrEmployee.Id,
                    mainHrEmployee.EmployeeCode,
                    mainHrEmployee.FullName,
                    mainHrEmployee.Email,
                    initialPassword = dto.MainHrPassword,
                    portalType = mainHrEmployee.PortalType,
                    isMainHr = mainHrEmployee.IsMainHr,
                    level = "H1 (CHRO / Main HR)"
                }
            });
        }

        // =========================================================================
        // 2. EMPLOYEE DIRECTORY (Main HR Creates & Manages Company Employees)
        // =========================================================================

        /// <summary>
        /// Lists all employees in the Company's Employee Directory (/hr/employee-directory).
        /// Supports filtering by companyCode (defaults to CMP-001), departmentCode, and status.
        /// </summary>
        [HttpGet(ApiConstants.Companies.CompanyDirectory)]
        [HttpGet(ApiConstants.Companies.EmployeeDirectory)]
        public async Task<IActionResult> GetEmployeeDirectory(
            string? companyCode = null,
            [FromQuery] string? departmentCode = null,
            [FromQuery] string? status = null)
        {
            var claimCompanyCode = User.FindFirst("company_code")?.Value;
            var targetCompanyCode = (!string.IsNullOrWhiteSpace(companyCode) ? companyCode : (claimCompanyCode ?? "CMP-001")).ToUpper();

            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.CompanyCode == targetCompanyCode);

            if (company == null)
                return NotFound(new { message = $"Company '{targetCompanyCode}' not found." });

            var query = _db.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.OrganizationalLevel)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .Where(e => e.CompanyId == company.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentCode) && !departmentCode.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var deptCode = departmentCode.ToUpper();
                query = query.Where(e => e.Department != null && (e.Department.Code == deptCode || e.Department.Name.ToLower() == departmentCode.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.WorkStatus.ToLower() == status.ToLower());
            }

            var employees = await query
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            var empIds = employees.Select(e => e.Id).ToList();
            var reportingLinks = await _db.ReportingRelationships
                .Include(r => r.Manager)
                .Where(r => empIds.Contains(r.EmployeeId) && r.IsPrimary && r.IsActive)
                .ToListAsync();

            var directoryRecords = employees.Select(e =>
            {
                var managerLink = reportingLinks.FirstOrDefault(r => r.EmployeeId == e.Id);
                var initials = string.Join("", e.FullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(s => char.ToUpperInvariant(s[0])));

                return new
                {
                    id = e.EmployeeCode,
                    employeeCode = e.EmployeeCode,
                    name = e.FullName,
                    role = e.Designation?.Title ?? "Specialist",
                    level = e.OrganizationalLevel?.Code ?? "L8",
                    hierarchyTrack = e.OrganizationalLevel?.HierarchyTrack ?? "Employee",
                    department = e.Department?.Name ?? "Engineering",
                    departmentCode = e.Department?.Code ?? "ENG",
                    email = e.Email,
                    phone = e.Phone ?? "+91 98450 12345",
                    location = e.WorkLocation,
                    status = e.WorkStatus,
                    joiningDate = e.JoinedAt.ToString("MMM dd, yyyy"),
                    avatar = initials,
                    type = e.EmploymentType,
                    manager = managerLink?.Manager?.FullName ?? (e.IsMainHr ? "CEO / Executive Board" : "Department Head"),
                    portalType = e.PortalType,
                    isMainHr = e.IsMainHr,
                    companyCode = company.CompanyCode,
                    companyName = company.Name,
                    systemRoles = e.EmployeeRoles
                        .Where(er => er.Role != null)
                        .Select(er => er.Role!.Code)
                        .ToList()
                };
            }).ToList();

            return Ok(new
            {
                company = new
                {
                    company.Id,
                    company.CompanyCode,
                    company.Name,
                    company.Domain,
                    company.MainHrEmail,
                    company.MainHrName
                },
                totalEmployees = directoryRecords.Count,
                employees = directoryRecords
            });
        }

        /// <summary>
        /// Main HR Only Action: Creates a new employee inside the Company's Employee Directory (/hr/employee-directory)
        /// and assigns their Department, Organizational Level (L1-L10 or H1-H6), Designation, System Role, and Login Credentials
        /// so the individual employee can log into the platform (/api/auth/login) with their role permissions.
        /// </summary>
        [HttpPost(ApiConstants.Companies.CompanyCreateEmployee)]
        [HttpPost(ApiConstants.Companies.DirectoryCreateEmployee)]
        public async Task<IActionResult> CreateEmployeeByMainHr(
            [FromBody] MainHrCreateEmployeeDto dto,
            string? companyCode = null)
        {
            var claimCompanyCode = User.FindFirst("company_code")?.Value;
            var targetCompanyCode = (!string.IsNullOrWhiteSpace(companyCode) ? companyCode : (claimCompanyCode ?? "CMP-001")).ToUpper();

            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.CompanyCode == targetCompanyCode);

            if (company == null)
                return NotFound(new { message = $"Company '{targetCompanyCode}' not found." });

            // Verify that the creator is an authorized Main HR / HR user of this company
            var hrIdentifier = dto.CreatedByHrEmailOrCode.Trim().ToLowerInvariant();
            var creatorHr = await _db.Employees
                .FirstOrDefaultAsync(e =>
                    e.CompanyId == company.Id &&
                    (e.Email.ToLower() == hrIdentifier || e.EmployeeCode.ToLower() == hrIdentifier));

            if (creatorHr == null || (!creatorHr.IsMainHr && creatorHr.PortalType != PortalTypes.Hr))
            {
                return StatusCode(403, new
                {
                    message = $"Access Denied: Only the Company's Main HR or authorized HR Administrator of '{company.Name}' ({company.CompanyCode}) can add employees to the Employee Directory."
                });
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            if (await _db.Employees.AnyAsync(e => e.Email.ToLower() == normalizedEmail))
            {
                return Conflict(new { message = $"An employee with email '{normalizedEmail}' already exists." });
            }

            // Resolve Department
            var dept = await _db.Departments
                .FirstOrDefaultAsync(d => d.Code == dto.DepartmentCode.ToUpper() || d.Name.ToLower() == dto.DepartmentCode.ToLower())
                ?? await _db.Departments.FirstAsync(d => d.Code == "ENG");

            // Resolve Designation & Level
            Designation? desig = null;
            if (!string.IsNullOrWhiteSpace(dto.DesignationCode))
            {
                desig = await _db.Designations
                    .Include(d => d.OrganizationalLevel)
                    .FirstOrDefaultAsync(d => d.Code == dto.DesignationCode.ToUpper() || d.Title.ToLower() == dto.DesignationCode.ToLower());
            }
            desig ??= await _db.Designations
                .Include(d => d.OrganizationalLevel)
                .FirstAsync(d => d.Code == "DESIG_DEV");

            // Resolve System Role
            var targetRoleCode = !string.IsNullOrWhiteSpace(dto.RoleCode)
                ? dto.RoleCode.ToUpper()
                : (desig.DefaultRoleCode ?? SystemRoleCodes.Developer);

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == targetRoleCode)
                ?? await _db.Roles.FirstAsync(r => r.Code == SystemRoleCodes.Developer);

            var portalType = desig.HierarchyTrack == HierarchyTracks.Hr ? PortalTypes.Hr : PortalTypes.Employee;

            var companyEmpCount = await _db.Employees.CountAsync(e => e.CompanyId == company.Id);
            var prefix = company.CompanyCode == "CMP-002" ? "NX" : "EMP";
            var employeeCode = $"{prefix}-{(companyEmpCount + 1):D3}";

            // Ensure unique employeeCode
            while (await _db.Employees.AnyAsync(e => e.EmployeeCode == employeeCode))
            {
                companyEmpCount++;
                employeeCode = $"{prefix}-{(companyEmpCount + 1):D3}";
            }

            var newEmployee = new Employee
            {
                EmployeeCode = employeeCode,
                FullName = dto.FullName.Trim(),
                Email = normalizedEmail,
                PortalType = portalType,
                CompanyId = company.Id,
                IsMainHr = false,
                DepartmentId = dept.Id,
                DesignationId = desig.Id,
                OrganizationalLevelId = desig.OrganizationalLevelId,
                Phone = dto.Phone ?? "+91 98450 55667",
                EmploymentType = dto.EmploymentType,
                WorkLocation = dto.WorkLocation,
                WorkStatus = "Active",
                CreatedByEmployeeId = creatorHr.Id,
                JoinedAt = DateTime.UtcNow
            };
            newEmployee.PasswordHash = _passwordHasher.HashPassword(newEmployee, dto.InitialPassword);

            _db.Employees.Add(newEmployee);
            await _db.SaveChangesAsync();

            // Assign System Role so RBAC permissions are active immediately on login
            _db.EmployeeRoles.Add(new EmployeeRole
            {
                EmployeeId = newEmployee.Id,
                RoleId = role.Id,
                IsPrimary = true
            });

            // Assign Reporting Manager if provided
            Employee? manager = null;
            if (!string.IsNullOrWhiteSpace(dto.ManagerEmployeeCode))
            {
                manager = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == dto.ManagerEmployeeCode.ToUpper() || e.FullName.ToLower() == dto.ManagerEmployeeCode.ToLower());

                if (manager != null)
                {
                    _db.ReportingRelationships.Add(new ReportingRelationship
                    {
                        EmployeeId = newEmployee.Id,
                        ManagerEmployeeId = manager.Id,
                        RelationshipType = "Primary",
                        IsPrimary = true,
                        IsActive = true
                    });
                }
            }

            // Provision initial Leave Balances (CL, SL, EL, CO)
            _db.LeaveBalances.AddRange(
                new LeaveBalanceRecord { EmployeeId = newEmployee.Id, LeaveType = "Casual Leave", ShortCode = "CL", TotalAllocated = 12, UsedDays = 0, AvailableDays = 12, ColorClass = "bg-emerald" },
                new LeaveBalanceRecord { EmployeeId = newEmployee.Id, LeaveType = "Sick Leave", ShortCode = "SL", TotalAllocated = 8, UsedDays = 0, AvailableDays = 8, ColorClass = "bg-info" },
                new LeaveBalanceRecord { EmployeeId = newEmployee.Id, LeaveType = "Earned Leave", ShortCode = "EL", TotalAllocated = 15, UsedDays = 0, AvailableDays = 15, ColorClass = "bg-primary" },
                new LeaveBalanceRecord { EmployeeId = newEmployee.Id, LeaveType = "Comp-Off", ShortCode = "CO", TotalAllocated = 2, UsedDays = 0, AvailableDays = 2, ColorClass = "bg-warning" }
            );

            // Provision Offer & Appointment Letter in Employee Documents
            var docCount = await _db.EmployeeDocuments.CountAsync();
            _db.EmployeeDocuments.Add(new EmployeeDocumentRecord
            {
                DocumentCode = $"DOC-OFFER-{(docCount + 1):D3}",
                EmployeeId = newEmployee.Id,
                Title = "Official Offer & Appointment Letter",
                Category = "Employment",
                CategoryLabel = "Employment Letter",
                IssueDate = DateTime.UtcNow.ToString("dd MMM yyyy"),
                FileSize = "1.2 MB",
                FileFormat = "PDF",
                Badge = "Verified",
                BadgeType = "emerald",
                Description = $"Issued by {creatorHr.FullName} (Main HR - {company.Name}) upon Employee Directory onboarding."
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Employee '{newEmployee.FullName}' ({newEmployee.EmployeeCode}) created in '{company.Name}' Employee Directory by Main HR ({creatorHr.FullName}). They can now log in via POST /api/auth/login.",
                employee = new
                {
                    id = newEmployee.EmployeeCode,
                    newEmployee.EmployeeCode,
                    name = newEmployee.FullName,
                    email = newEmployee.Email,
                    initialPassword = dto.InitialPassword,
                    companyCode = company.CompanyCode,
                    companyName = company.Name,
                    portalType = newEmployee.PortalType,
                    level = desig.OrganizationalLevel?.Code,
                    role = desig.Title,
                    systemRoleCode = role.Code,
                    department = dept.Name,
                    departmentCode = dept.Code,
                    type = newEmployee.EmploymentType,
                    location = newEmployee.WorkLocation,
                    status = newEmployee.WorkStatus,
                    manager = manager?.FullName ?? "Department Head",
                    createdByMainHr = creatorHr.FullName
                }
            });
        }

        // =========================================================================
        // 3. BULK IMPORT & CSV UPLOAD (/hr/employee-directory)
        // =========================================================================

        /// <summary>
        /// Main HR Action: Bulk imports multiple employee records into the Company's Employee Directory (/hr/employee-directory).
        /// </summary>
        [HttpPost(ApiConstants.Companies.CompanyBulkImport)]
        [HttpPost(ApiConstants.Companies.DirectoryBulkImport)]
        public async Task<IActionResult> BulkImportEmployees(
            [FromBody] BulkImportEmployeesDto dto,
            string? companyCode = null)
        {
            var claimCompanyCode = User.FindFirst("company_code")?.Value;
            var targetCompanyCode = (!string.IsNullOrWhiteSpace(companyCode) ? companyCode : (claimCompanyCode ?? "CMP-001")).ToUpper();

            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.CompanyCode == targetCompanyCode);

            if (company == null)
                return NotFound(new { message = $"Company '{targetCompanyCode}' not found." });

            var hrIdentifier = (dto.CreatedByHrEmailOrCode ?? company.MainHrEmail).Trim().ToLowerInvariant();
            var creatorHr = await _db.Employees
                .FirstOrDefaultAsync(e =>
                    e.CompanyId == company.Id &&
                    (e.Email.ToLower() == hrIdentifier || e.EmployeeCode.ToLower() == hrIdentifier))
                ?? await _db.Employees.FirstOrDefaultAsync(e => e.CompanyId == company.Id && e.IsMainHr);

            if (creatorHr == null)
            {
                return StatusCode(403, new
                {
                    message = $"Access Denied: Main HR Administrator of '{company.Name}' ({company.CompanyCode}) must be active to import employees."
                });
            }

            var result = await ProcessBulkImportAsync(dto.Employees, company, creatorHr);
            return Ok(result);
        }

        /// <summary>
        /// Main HR Action: Uploads and imports a CSV file containing employee records into the Employee Directory.
        /// Exact CSV Headers: FullName,Email,DepartmentCode,DesignationCode,RoleCode,EmploymentType,WorkLocation,Phone,InitialPassword,ManagerEmployeeCode
        /// </summary>
        [HttpPost(ApiConstants.Companies.CompanyImportCsv)]
        [HttpPost(ApiConstants.Companies.DirectoryImportCsv)]
        public async Task<IActionResult> ImportEmployeesFromCsv(
            IFormFile file,
            [FromForm] string? createdByHrEmailOrCode = null,
            string? companyCode = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please upload a valid CSV file." });
            }

            var claimCompanyCode = User.FindFirst("company_code")?.Value;
            var targetCompanyCode = (!string.IsNullOrWhiteSpace(companyCode) ? companyCode : (claimCompanyCode ?? "CMP-001")).ToUpper();

            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.CompanyCode == targetCompanyCode);

            if (company == null)
                return NotFound(new { message = $"Company '{targetCompanyCode}' not found." });

            var hrIdentifier = (createdByHrEmailOrCode ?? company.MainHrEmail).Trim().ToLowerInvariant();
            var creatorHr = await _db.Employees
                .FirstOrDefaultAsync(e =>
                    e.CompanyId == company.Id &&
                    (e.Email.ToLower() == hrIdentifier || e.EmployeeCode.ToLower() == hrIdentifier))
                ?? await _db.Employees.FirstOrDefaultAsync(e => e.CompanyId == company.Id && e.IsMainHr);

            if (creatorHr == null)
            {
                return StatusCode(403, new { message = "Main HR Administrator not found for this company." });
            }

            var parsedEmployees = new List<MainHrCreateEmployeeDto>();
            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    return BadRequest(new { message = "The CSV file is empty." });
                }

                var headers = ParseCsvLine(headerLine)
                    .Select(h => h.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", ""))
                    .ToList();

                int idxFullName = headers.FindIndex(h => h is "fullname" or "name");
                int idxEmail = headers.FindIndex(h => h is "email" or "workemail");
                int idxDept = headers.FindIndex(h => h is "departmentcode" or "department" or "dept");
                int idxDesig = headers.FindIndex(h => h is "designationcode" or "designation" or "role");
                int idxRole = headers.FindIndex(h => h is "rolecode" or "systemrole" or "systemrolecode");
                int idxType = headers.FindIndex(h => h is "employmenttype" or "type");
                int idxLoc = headers.FindIndex(h => h is "worklocation" or "location");
                int idxPhone = headers.FindIndex(h => h is "phone" or "phonenumber");
                int idxPassword = headers.FindIndex(h => h is "initialpassword" or "password");
                int idxManager = headers.FindIndex(h => h is "manageremployeecode" or "manager" or "managercode");

                if (idxFullName == -1 || idxEmail == -1)
                {
                    return BadRequest(new
                    {
                        message = "Invalid CSV format. Header line must contain at least 'FullName' and 'Email' columns. Example: FullName,Email,DepartmentCode,DesignationCode,RoleCode,EmploymentType,WorkLocation,Phone,InitialPassword,ManagerEmployeeCode"
                    });
                }

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = ParseCsvLine(line);
                    if (cols.Count <= Math.Max(idxFullName, idxEmail)) continue;

                    var name = cols[idxFullName].Trim();
                    var email = cols[idxEmail].Trim();
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email)) continue;

                    parsedEmployees.Add(new MainHrCreateEmployeeDto
                    {
                        CreatedByHrEmailOrCode = creatorHr.Email,
                        FullName = name,
                        Email = email,
                        DepartmentCode = idxDept >= 0 && idxDept < cols.Count && !string.IsNullOrWhiteSpace(cols[idxDept]) ? cols[idxDept].Trim() : "ENG",
                        DesignationCode = idxDesig >= 0 && idxDesig < cols.Count && !string.IsNullOrWhiteSpace(cols[idxDesig]) ? cols[idxDesig].Trim() : "DESIG_DEV",
                        RoleCode = idxRole >= 0 && idxRole < cols.Count && !string.IsNullOrWhiteSpace(cols[idxRole]) ? cols[idxRole].Trim() : null,
                        EmploymentType = idxType >= 0 && idxType < cols.Count && !string.IsNullOrWhiteSpace(cols[idxType]) ? cols[idxType].Trim() : "Full-time",
                        WorkLocation = idxLoc >= 0 && idxLoc < cols.Count && !string.IsNullOrWhiteSpace(cols[idxLoc]) ? cols[idxLoc].Trim() : "Bangalore, India",
                        Phone = idxPhone >= 0 && idxPhone < cols.Count && !string.IsNullOrWhiteSpace(cols[idxPhone]) ? cols[idxPhone].Trim() : "+91 98450 55667",
                        InitialPassword = idxPassword >= 0 && idxPassword < cols.Count && !string.IsNullOrWhiteSpace(cols[idxPassword]) ? cols[idxPassword].Trim() : "Welcome@12345",
                        ManagerEmployeeCode = idxManager >= 0 && idxManager < cols.Count && !string.IsNullOrWhiteSpace(cols[idxManager]) ? cols[idxManager].Trim() : null
                    });
                }
            }

            var result = await ProcessBulkImportAsync(parsedEmployees, company, creatorHr);
            return Ok(result);
        }

        private async Task<BulkImportResultDto> ProcessBulkImportAsync(
            List<MainHrCreateEmployeeDto> items,
            Company company,
            Employee creatorHr)
        {
            var result = new BulkImportResultDto
            {
                TotalSubmitted = items.Count
            };

            if (items.Count == 0)
            {
                result.Messages.Add("No employee records provided.");
                return result;
            }

            // In-memory cache for ultra-fast lookup
            var allDepts = await _db.Departments.ToListAsync();
            var allDesigs = await _db.Designations.Include(d => d.OrganizationalLevel).ToListAsync();
            var allRoles = await _db.Roles.ToListAsync();
            var existingEmails = (await _db.Employees.Select(e => e.Email.ToLower()).ToListAsync()).ToHashSet();

            var defaultDept = allDepts.FirstOrDefault(d => d.Code == "ENG") ?? allDepts.First();
            var defaultDesig = allDesigs.FirstOrDefault(d => d.Code == "DESIG_DEV") ?? allDesigs.First();
            var defaultRole = allRoles.FirstOrDefault(r => r.Code == SystemRoleCodes.Developer) ?? allRoles.First();

            var prefix = company.CompanyCode == "CMP-002" ? "NX" : "EMP";
            var companyEmpCount = await _db.Employees.CountAsync(e => e.CompanyId == company.Id);
            var initialDocCount = await _db.EmployeeDocuments.CountAsync();

            var batchEmployees = new List<Employee>();
            var batchRoles = new List<EmployeeRole>();
            var batchLeaves = new List<LeaveBalanceRecord>();
            var batchDocs = new List<EmployeeDocumentRecord>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.FullName) || string.IsNullOrWhiteSpace(item.Email))
                {
                    result.TotalSkipped++;
                    result.Messages.Add("Skipped record: Missing Name or Email.");
                    continue;
                }

                var normalizedEmail = item.Email.Trim().ToLowerInvariant();
                if (existingEmails.Contains(normalizedEmail))
                {
                    result.TotalSkipped++;
                    result.Messages.Add($"Skipped '{normalizedEmail}': Email already exists.");
                    continue;
                }

                // Resolve Dept
                var deptCode = (item.DepartmentCode ?? "ENG").Trim().ToUpper();
                var dept = allDepts.FirstOrDefault(d => d.Code.Equals(deptCode, StringComparison.OrdinalIgnoreCase) || d.Name.Equals(deptCode, StringComparison.OrdinalIgnoreCase))
                    ?? defaultDept;

                // Resolve Designation & Level
                var desigCode = (item.DesignationCode ?? "DESIG_DEV").Trim().ToUpper();
                var desig = allDesigs.FirstOrDefault(d => d.Code.Equals(desigCode, StringComparison.OrdinalIgnoreCase) || d.Title.Equals(desigCode, StringComparison.OrdinalIgnoreCase))
                    ?? defaultDesig;

                // Resolve Role
                var targetRoleCode = !string.IsNullOrWhiteSpace(item.RoleCode)
                    ? item.RoleCode.Trim().ToUpper()
                    : (desig.DefaultRoleCode ?? SystemRoleCodes.Developer);

                var role = allRoles.FirstOrDefault(r => r.Code.Equals(targetRoleCode, StringComparison.OrdinalIgnoreCase))
                    ?? defaultRole;

                var portalType = desig.HierarchyTrack == HierarchyTracks.Hr ? PortalTypes.Hr : PortalTypes.Employee;

                companyEmpCount++;
                var employeeCode = $"{prefix}-{companyEmpCount:D3}";
                while (existingEmails.Contains(employeeCode.ToLower()))
                {
                    companyEmpCount++;
                    employeeCode = $"{prefix}-{companyEmpCount:D3}";
                }

                var newEmp = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = employeeCode,
                    FullName = item.FullName.Trim(),
                    Email = normalizedEmail,
                    PortalType = portalType,
                    CompanyId = company.Id,
                    IsMainHr = false,
                    DepartmentId = dept.Id,
                    DesignationId = desig.Id,
                    OrganizationalLevelId = desig.OrganizationalLevelId,
                    Phone = !string.IsNullOrWhiteSpace(item.Phone) ? item.Phone.Trim() : "+91 98450 55667",
                    EmploymentType = !string.IsNullOrWhiteSpace(item.EmploymentType) ? item.EmploymentType.Trim() : "Full-time",
                    WorkLocation = !string.IsNullOrWhiteSpace(item.WorkLocation) ? item.WorkLocation.Trim() : "Bangalore, India",
                    WorkStatus = "Active",
                    CreatedByEmployeeId = creatorHr.Id,
                    JoinedAt = DateTime.UtcNow
                };
                newEmp.PasswordHash = _passwordHasher.HashPassword(newEmp, !string.IsNullOrWhiteSpace(item.InitialPassword) ? item.InitialPassword : "Welcome@12345");

                batchEmployees.Add(newEmp);
                existingEmails.Add(normalizedEmail);

                batchRoles.Add(new EmployeeRole
                {
                    EmployeeId = newEmp.Id,
                    RoleId = role.Id,
                    IsPrimary = true
                });

                // Leave Balances
                batchLeaves.AddRange(new[]
                {
                    new LeaveBalanceRecord { EmployeeId = newEmp.Id, LeaveType = "Casual Leave", ShortCode = "CL", TotalAllocated = 12, UsedDays = 0, AvailableDays = 12, ColorClass = "bg-emerald" },
                    new LeaveBalanceRecord { EmployeeId = newEmp.Id, LeaveType = "Sick Leave", ShortCode = "SL", TotalAllocated = 8, UsedDays = 0, AvailableDays = 8, ColorClass = "bg-info" },
                    new LeaveBalanceRecord { EmployeeId = newEmp.Id, LeaveType = "Earned Leave", ShortCode = "EL", TotalAllocated = 15, UsedDays = 0, AvailableDays = 15, ColorClass = "bg-primary" },
                    new LeaveBalanceRecord { EmployeeId = newEmp.Id, LeaveType = "Comp-Off", ShortCode = "CO", TotalAllocated = 2, UsedDays = 0, AvailableDays = 2, ColorClass = "bg-warning" }
                });

                // Offer Letter Document
                initialDocCount++;
                batchDocs.Add(new EmployeeDocumentRecord
                {
                    DocumentCode = $"DOC-OFFER-{initialDocCount:D3}",
                    EmployeeId = newEmp.Id,
                    Title = "Official Offer & Appointment Letter",
                    Category = "Employment",
                    CategoryLabel = "Employment Letter",
                    IssueDate = DateTime.UtcNow.ToString("dd MMM yyyy"),
                    FileSize = "1.2 MB",
                    FileFormat = "PDF",
                    Badge = "Verified",
                    BadgeType = "emerald",
                    Description = $"Issued by {creatorHr.FullName} upon Employee Directory bulk import."
                });

                result.TotalImported++;
                result.ImportedEmployees.Add(new
                {
                    employeeCode = newEmp.EmployeeCode,
                    name = newEmp.FullName,
                    email = newEmp.Email,
                    department = dept.Name,
                    role = desig.Title,
                    level = desig.OrganizationalLevel?.Code,
                    portalType = newEmp.PortalType
                });
            }

            if (batchEmployees.Count > 0)
            {
                await _db.Employees.AddRangeAsync(batchEmployees);
                await _db.EmployeeRoles.AddRangeAsync(batchRoles);
                await _db.LeaveBalances.AddRangeAsync(batchLeaves);
                await _db.EmployeeDocuments.AddRangeAsync(batchDocs);
                await _db.SaveChangesAsync();
            }

            result.Messages.Add($"Successfully imported {result.TotalImported} employees into {company.Name} ({company.CompanyCode}).");
            return result;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            values.Add(sb.ToString().Trim());
            return values;
        }
    }
}
