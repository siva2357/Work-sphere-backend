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
                    SubDepartments = allDepartments
                        .Where(sub => sub.ParentDepartmentId == d.Id)
                        .Select(sub => new
                        {
                            sub.Id,
                            sub.Code,
                            sub.Name,
                            sub.Description,
                            sub.HeadEmployeeId
                        })
                        .ToList()
                })
                .ToList();

            return Ok(topLevel);
        }

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
                Designations = designations
                    .Where(d => d.OrganizationalLevelId == l.Id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Code,
                        d.Title,
                        d.TypicalReportingTo,
                        d.DefaultRoleCode
                    })
            });

            return Ok(result);
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
                        rp.Permission!.Code,
                        rp.Permission.Action,
                        rp.Scope
                    }).OrderBy(p => p.Action)
                });

            return Ok(new
            {
                role.Id,
                role.Code,
                role.Name,
                role.HierarchyTrack,
                role.Description,
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
                        Scope = item.Scope
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = $"Updated permissions for role '{role.Code}'." });
        }

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
                Department = e.Department?.Name,
                LevelCode = e.OrganizationalLevel?.Code,
                HierarchyTrack = e.OrganizationalLevel?.HierarchyTrack,
                Designation = e.Designation?.Title,
                Roles = e.EmployeeRoles.Where(er => er.Role != null).Select(er => er.Role!.Code),
                ReportingTo = reporting
                    .Where(r => r.EmployeeId == e.Id)
                    .Select(r => new
                    {
                        ManagerName = r.Manager?.FullName,
                        r.RelationshipType,
                        r.IsPrimary
                    })
            });

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
            });

            return Ok(result);
        }
    }
}

