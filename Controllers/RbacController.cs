using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;
using WorkSphere.API.Services;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Rbac.Base)]
    public class RbacController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPermissionService _permissionService;

        public RbacController(AppDbContext db, IPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        // ====================================================================
        // 1. DEPARTMENTS & SUB-DEPARTMENTS
        // ====================================================================

        /// <summary>
        /// Lists all 22 Top-Level Company Departments and their nested Subdepartments hierarchy.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Departments)]
        public async Task<IActionResult> GetDepartmentsHierarchy()
        {
            var allDepartments = await _db.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Code)
                .ToListAsync();

            var topLevel = allDepartments
                .Where(d => d.ParentDepartmentId == null)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    d.Name,
                    d.Description,
                    d.HeadEmployeeId,
                    d.IsActive,
                    SubDepartments = allDepartments
                        .Where(sub => sub.ParentDepartmentId == d.Id)
                        .Select(sub => new
                        {
                            sub.Id,
                            sub.Code,
                            sub.Name,
                            sub.Description,
                            sub.HeadEmployeeId,
                            sub.IsActive
                        })
                        .ToList()
                })
                .ToList();

            return Ok(topLevel);
        }

        /// <summary>
        /// Creates a new department or subdepartment.
        /// </summary>
        [HttpPost(ApiConstants.Rbac.Departments)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Department Name and Code are required." });
            }

            var codeExists = await _db.Departments.AnyAsync(d => d.Code.ToLower() == dto.Code.Trim().ToLower());
            if (codeExists)
            {
                return BadRequest(new { message = $"Department with code '{dto.Code}' already exists." });
            }

            var dept = new Department
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim().ToUpper(),
                Description = dto.Description?.Trim(),
                ParentDepartmentId = dto.ParentDepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Departments.Add(dept);
            await _db.SaveChangesAsync();

            return Ok(dept);
        }

        /// <summary>
        /// Updates department details.
        /// </summary>
        [HttpPut(ApiConstants.Rbac.DepartmentById)]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentDto dto)
        {
            var dept = await _db.Departments.FindAsync(id);
            if (dept == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            dept.Name = dto.Name.Trim();
            dept.Description = dto.Description?.Trim();
            dept.HeadEmployeeId = dto.HeadEmployeeId;
            dept.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return Ok(dept);
        }

        /// <summary>
        /// Deactivates or removes a department.
        /// </summary>
        [HttpDelete(ApiConstants.Rbac.DepartmentById)]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var dept = await _db.Departments.Include(d => d.SubDepartments).FirstOrDefaultAsync(d => d.Id == id);
            if (dept == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            var hasEmployees = await _db.Employees.AnyAsync(e => e.DepartmentId == id);
            if (hasEmployees)
            {
                dept.IsActive = false;
                await _db.SaveChangesAsync();
                return Ok(new { message = $"Department '{dept.Name}' contains active employees and was deactivated instead of deleted." });
            }

            _db.Departments.Remove(dept);
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Department '{dept.Name}' deleted successfully." });
        }

        // ====================================================================
        // 2. ORGANIZATIONAL LEVELS & DESIGNATIONS (L1-L10 & H1-H6)
        // ====================================================================

        /// <summary>
        /// Lists all Organizational Levels (L1-L10 & H1-H6) along with their Designations.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Levels)]
        public async Task<IActionResult> GetLevelsAndDesignations()
        {
            var levels = await _db.OrganizationalLevels
                .OrderBy(l => l.HierarchyTrack)
                .ThenBy(l => l.RankOrder)
                .ToListAsync();

            var designations = await _db.Designations.ToListAsync();

            var result = levels.Select(l => new
            {
                l.Id,
                l.Code,
                l.Name,
                l.HierarchyTrack,
                l.RankOrder,
                l.Description,
                Designations = designations
                    .Where(d => d.OrganizationalLevelId == l.Id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Code,
                        d.Title,
                        d.HierarchyTrack,
                        d.TypicalReportingTo,
                        d.DefaultRoleCode,
                        d.Responsibilities
                    })
                    .ToList()
            });

            return Ok(result);
        }

        /// <summary>
        /// Lists all Designations across levels.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Designations)]
        public async Task<IActionResult> GetDesignations()
        {
            var designations = await _db.Designations
                .Include(d => d.OrganizationalLevel)
                .OrderBy(d => d.HierarchyTrack)
                .ThenBy(d => d.Code)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    d.Title,
                    d.OrganizationalLevelId,
                    LevelCode = d.OrganizationalLevel != null ? d.OrganizationalLevel.Code : string.Empty,
                    LevelName = d.OrganizationalLevel != null ? d.OrganizationalLevel.Name : string.Empty,
                    d.HierarchyTrack,
                    d.TypicalReportingTo,
                    d.DefaultRoleCode,
                    d.Responsibilities
                })
                .ToListAsync();

            return Ok(designations);
        }

        /// <summary>
        /// Creates a new Designation mapped to an Organizational Level.
        /// </summary>
        [HttpPost(ApiConstants.Rbac.Designations)]
        public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Designation Title and Code are required." });
            }

            var codeExists = await _db.Designations.AnyAsync(d => d.Code.ToLower() == dto.Code.Trim().ToLower());
            if (codeExists)
            {
                return BadRequest(new { message = $"Designation with code '{dto.Code}' already exists." });
            }

            var level = await _db.OrganizationalLevels.FindAsync(dto.OrganizationalLevelId);
            if (level == null)
            {
                return BadRequest(new { message = "Selected Organizational Level does not exist." });
            }

            var designation = new Designation
            {
                Title = dto.Title.Trim(),
                Code = dto.Code.Trim().ToUpper(),
                OrganizationalLevelId = dto.OrganizationalLevelId,
                HierarchyTrack = dto.HierarchyTrack,
                TypicalReportingTo = dto.TypicalReportingTo?.Trim(),
                DefaultRoleCode = dto.DefaultRoleCode?.Trim(),
                Responsibilities = dto.Responsibilities?.Trim()
            };

            _db.Designations.Add(designation);
            await _db.SaveChangesAsync();

            return Ok(designation);
        }

        /// <summary>
        /// Updates a Designation.
        /// </summary>
        [HttpPut(ApiConstants.Rbac.DesignationById)]
        public async Task<IActionResult> UpdateDesignation(Guid id, [FromBody] UpdateDesignationDto dto)
        {
            var desig = await _db.Designations.FindAsync(id);
            if (desig == null)
            {
                return NotFound(new { message = "Designation not found." });
            }

            desig.Title = dto.Title.Trim();
            desig.TypicalReportingTo = dto.TypicalReportingTo?.Trim();
            desig.DefaultRoleCode = dto.DefaultRoleCode?.Trim();
            desig.Responsibilities = dto.Responsibilities?.Trim();

            await _db.SaveChangesAsync();
            return Ok(desig);
        }

        /// <summary>
        /// Deletes a Designation if not currently assigned to employees.
        /// </summary>
        [HttpDelete(ApiConstants.Rbac.DesignationById)]
        public async Task<IActionResult> DeleteDesignation(Guid id)
        {
            var desig = await _db.Designations.FindAsync(id);
            if (desig == null)
            {
                return NotFound(new { message = "Designation not found." });
            }

            var inUse = await _db.Employees.AnyAsync(e => e.DesignationId == id);
            if (inUse)
            {
                return BadRequest(new { message = "Cannot delete designation because it is currently assigned to one or more employees." });
            }

            _db.Designations.Remove(desig);
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Designation '{desig.Title}' deleted successfully." });
        }

        // ====================================================================
        // 3. PERMISSIONS CATALOG & ROLES
        // ====================================================================

        /// <summary>
        /// Lists all system permissions catalog grouped by module.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Permissions)]
        public async Task<IActionResult> GetAllPermissions()
        {
            var perms = await _db.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();

            var grouped = perms
                .GroupBy(p => p.Module)
                .Select(g => new
                {
                    Module = g.Key,
                    Permissions = g.Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Module,
                        p.Action,
                        p.Description
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }

        /// <summary>
        /// Lists all 23 System Roles across Employee & HR hierarchies.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Roles)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _db.Roles
                .Include(r => r.RolePermissions)
                .OrderBy(r => r.HierarchyTrack)
                .ThenBy(r => r.Code)
                .Select(r => new
                {
                    r.Id,
                    r.Code,
                    r.Name,
                    r.HierarchyTrack,
                    r.Description,
                    r.IsSystemRole,
                    PermissionCount = r.RolePermissions.Count
                })
                .ToListAsync();

            return Ok(roles);
        }

        /// <summary>
        /// Creates a new custom role.
        /// </summary>
        [HttpPost(ApiConstants.Rbac.Roles)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Role Name and Code are required." });
            }

            var exists = await _db.Roles.AnyAsync(r => r.Code.ToLower() == dto.Code.Trim().ToLower());
            if (exists)
            {
                return BadRequest(new { message = $"Role with code '{dto.Code}' already exists." });
            }

            var role = new Role
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim().ToUpper(),
                HierarchyTrack = dto.HierarchyTrack,
                Description = dto.Description?.Trim(),
                IsSystemRole = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            return Ok(role);
        }

        /// <summary>
        /// Returns the complete module & action permission matrix for a given Role code (e.g. EMP_CTO, EMP_DEVELOPER, HR_MANAGER).
        /// </summary>
        [HttpGet(ApiConstants.Rbac.RolePermissions)]
        public async Task<IActionResult> GetRolePermissions(string roleCode)
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Code.ToLower() == roleCode.ToLower());

            if (role == null)
            {
                return NotFound(new { message = $"Role '{roleCode}' not found." });
            }

            var grouped = role.RolePermissions
                .Where(rp => rp.Permission != null)
                .GroupBy(rp => rp.Permission!.Module)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Module = g.Key,
                    Permissions = g.Select(rp => new
                    {
                        rp.Permission!.Id,
                        rp.Permission.Code,
                        rp.Permission.Action,
                        rp.Scope
                    }).OrderBy(p => p.Action).ToList()
                })
                .ToList();

            return Ok(new
            {
                role.Id,
                role.Code,
                role.Name,
                role.HierarchyTrack,
                role.Description,
                role.IsSystemRole,
                Modules = grouped
            });
        }

        /// <summary>
        /// Dynamically updates permissions & scopes for a Role in PostgreSQL.
        /// </summary>
        [HttpPut(ApiConstants.Rbac.RolePermissions)]
        public async Task<IActionResult> UpdateRolePermissions(
            string roleCode,
            [FromBody] UpdateRolePermissionsRequestDto request)
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Code.ToLower() == roleCode.ToLower());

            if (role == null)
            {
                return NotFound(new { message = $"Role '{roleCode}' not found." });
            }

            var allPermissions = await _db.Permissions.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);

            _db.RolePermissions.RemoveRange(role.RolePermissions);

            foreach (var item in request.Permissions)
            {
                if (allPermissions.TryGetValue(item.PermissionCode, out var perm))
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = perm.Id,
                        Scope = string.IsNullOrWhiteSpace(item.Scope) ? "Own" : item.Scope
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = $"Updated {request.Permissions.Count} permissions for role '{role.Code}'." });
        }

        // ====================================================================
        // 4. EMPLOYEES HIERARCHY & PERMISSION OVERRIDES
        // ====================================================================

        /// <summary>
        /// Lists all employees with their Organizational Level, Designation, Roles, and Reporting relationships.
        /// </summary>
        [HttpGet(ApiConstants.Rbac.Employees)]
        public async Task<IActionResult> GetEmployeesHierarchy()
        {
            var employees = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.OrganizationalLevel)
                .Include(e => e.Designation)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            var reporting = await _db.ReportingRelationships
                .Include(r => r.Manager)
                .Where(r => r.IsActive)
                .ToListAsync();

            var result = employees.Select(e => new
            {
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.PortalType,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name,
                DepartmentCode = e.Department?.Code,
                LevelId = e.OrganizationalLevelId,
                LevelCode = e.OrganizationalLevel?.Code,
                HierarchyTrack = e.OrganizationalLevel?.HierarchyTrack,
                DesignationId = e.DesignationId,
                DesignationTitle = e.Designation?.Title,
                Roles = e.EmployeeRoles.Where(er => er.Role != null).Select(er => er.Role!.Code).ToList(),
                ReportingTo = reporting
                    .Where(r => r.EmployeeId == e.Id)
                    .Select(r => new
                    {
                        ManagerName = r.Manager?.FullName,
                        ManagerCode = r.Manager?.EmployeeCode,
                        r.RelationshipType,
                        r.IsPrimary
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Grants or revokes an exceptional permission override for a specific employee.
        /// </summary>
        [HttpPost(ApiConstants.Rbac.EmployeeOverrides)]
        public async Task<IActionResult> UpsertEmployeeOverride(
            Guid employeeId,
            [FromBody] UpsertPermissionOverrideRequestDto request)
        {
            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            var permission = await _db.Permissions
                .FirstOrDefaultAsync(p => p.Code.ToLower() == request.PermissionCode.ToLower());

            if (permission == null)
            {
                return BadRequest(new { message = $"Permission '{request.PermissionCode}' does not exist." });
            }

            var existing = await _db.EmployeePermissionOverrides
                .FirstOrDefaultAsync(o => o.EmployeeId == employeeId && o.PermissionId == permission.Id);

            if (existing != null)
            {
                existing.IsGranted = request.IsGranted;
                existing.Scope = request.Scope;
                existing.Reason = request.Reason;
                existing.ExpiresAt = request.ExpiresAt;
            }
            else
            {
                _db.EmployeePermissionOverrides.Add(new EmployeePermissionOverride
                {
                    EmployeeId = employeeId,
                    PermissionId = permission.Id,
                    IsGranted = request.IsGranted,
                    Scope = request.Scope,
                    Reason = request.Reason,
                    ExpiresAt = request.ExpiresAt
                });
            }

            await _db.SaveChangesAsync();

            var updatedEffective = await _permissionService.GetEffectivePermissionsAsync(employeeId);
            return Ok(new
            {
                message = "Employee permission override saved.",
                EffectivePermissions = updatedEffective
            });
        }

        /// <summary>
        /// Lists configured multi-stage Approval Workflows (independent of designation levels).
        /// </summary>
        [HttpGet(ApiConstants.Rbac.ApprovalWorkflows)]
        public async Task<IActionResult> GetApprovalWorkflows()
        {
            var workflows = await _db.ApprovalWorkflows
                .Include(w => w.Steps)
                    .ThenInclude(s => s.RequiredRole)
                .ToListAsync();

            var result = workflows.Select(w => new
            {
                w.Id,
                w.Name,
                w.Module,
                w.Description,
                w.ConditionJson,
                w.IsActive,
                Steps = w.Steps
                    .OrderBy(s => s.StepOrder)
                    .Select(s => new
                    {
                        s.StepOrder,
                        s.StepName,
                        s.ApproverType,
                        RequiredRoleCode = s.RequiredRole?.Code,
                        s.ActionType,
                        s.AllowDelegation,
                        s.IsMandatory
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }
    }
}
