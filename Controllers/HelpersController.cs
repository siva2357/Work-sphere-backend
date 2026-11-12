using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Helpers.Base)]
    public class HelpersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HelpersController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns all seeded lookup / dropdown lists (companies, organizational_levels, departments, designations, roles, permissions, role_permissions)
        /// for use in frontend forms and filters.
        /// </summary>
        [HttpGet]
        [HttpGet(ApiConstants.Helpers.Dropdowns)]
        public async Task<IActionResult> GetAllDropdowns()
        {
            var companies = await _db.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyCode)
                .Select(c => new
                {
                    id = c.Id,
                    code = c.CompanyCode,
                    name = c.Name,
                    domain = c.Domain,
                    industry = c.Industry,
                    headquarters = c.Headquarters
                })
                .ToListAsync();

            var organizationalLevels = await _db.OrganizationalLevels
                .OrderBy(l => l.HierarchyTrack)
                .ThenBy(l => l.RankOrder)
                .Select(l => new
                {
                    id = l.Id,
                    code = l.Code,
                    name = l.Name,
                    hierarchyTrack = l.HierarchyTrack,
                    rankOrder = l.RankOrder,
                    description = l.Description
                })
                .ToListAsync();

            var allDepts = await _db.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Code)
                .ToListAsync();

            var departments = allDepts
                .Where(d => d.ParentDepartmentId == null)
                .Select(d => new
                {
                    id = d.Id,
                    code = d.Code,
                    name = d.Name,
                    description = d.Description,
                    subDepartments = allDepts
                        .Where(sub => sub.ParentDepartmentId == d.Id)
                        .Select(sub => new
                        {
                            id = sub.Id,
                            code = sub.Code,
                            name = sub.Name,
                            description = sub.Description,
                            parentCode = d.Code
                        })
                        .ToList()
                })
                .ToList();

            var designations = await _db.Designations
                .Include(d => d.OrganizationalLevel)
                .OrderBy(d => d.HierarchyTrack)
                .ThenBy(d => d.OrganizationalLevel != null ? d.OrganizationalLevel.RankOrder : 99)
                .Select(d => new
                {
                    id = d.Id,
                    code = d.Code,
                    title = d.Title,
                    levelCode = d.OrganizationalLevel != null ? d.OrganizationalLevel.Code : "L8",
                    levelName = d.OrganizationalLevel != null ? d.OrganizationalLevel.Name : "",
                    hierarchyTrack = d.HierarchyTrack,
                    typicalReportingTo = d.TypicalReportingTo,
                    defaultRoleCode = d.DefaultRoleCode
                })
                .ToListAsync();

            var roles = await _db.Roles
                .OrderBy(r => r.HierarchyTrack)
                .ThenBy(r => r.Code)
                .Select(r => new
                {
                    id = r.Id,
                    code = r.Code,
                    name = r.Name,
                    hierarchyTrack = r.HierarchyTrack,
                    description = r.Description,
                    isSystemRole = r.IsSystemRole
                })
                .ToListAsync();

            var permissions = await _db.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .Select(p => new
                {
                    id = p.Id,
                    code = p.Code,
                    module = p.Module,
                    action = p.Action,
                    description = p.Description
                })
                .ToListAsync();

            var rolePermissions = await _db.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role != null && rp.Permission != null)
                .Select(rp => new
                {
                    roleId = rp.RoleId,
                    roleCode = rp.Role!.Code,
                    roleName = rp.Role.Name,
                    permissionId = rp.PermissionId,
                    permissionCode = rp.Permission!.Code,
                    module = rp.Permission.Module,
                    action = rp.Permission.Action,
                    scope = rp.Scope
                })
                .ToListAsync();

            return Ok(new
            {
                companies,
                organizationalLevels,
                departments,
                designations,
                roles,
                permissions,
                rolePermissions
            });
        }

        [HttpGet(ApiConstants.Helpers.Companies)]
        public async Task<IActionResult> GetCompaniesDropdown()
        {
            var companies = await _db.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyCode)
                .Select(c => new
                {
                    id = c.Id,
                    code = c.CompanyCode,
                    name = c.Name,
                    domain = c.Domain,
                    industry = c.Industry,
                    headquarters = c.Headquarters
                })
                .ToListAsync();

            return Ok(companies);
        }

        [HttpGet(ApiConstants.Helpers.OrganizationalLevels)]
        public async Task<IActionResult> GetOrganizationalLevelsDropdown([FromQuery] string? track = null)
        {
            var query = _db.OrganizationalLevels.AsQueryable();
            if (!string.IsNullOrWhiteSpace(track))
            {
                query = query.Where(l => l.HierarchyTrack.ToLower() == track.ToLower());
            }

            var levels = await query
                .OrderBy(l => l.HierarchyTrack)
                .ThenBy(l => l.RankOrder)
                .Select(l => new
                {
                    id = l.Id,
                    code = l.Code,
                    name = l.Name,
                    hierarchyTrack = l.HierarchyTrack,
                    rankOrder = l.RankOrder,
                    description = l.Description
                })
                .ToListAsync();

            return Ok(levels);
        }

        [HttpGet(ApiConstants.Helpers.Departments)]
        public async Task<IActionResult> GetDepartmentsDropdown([FromQuery] bool includeSubDepartments = true)
        {
            var allDepts = await _db.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Code)
                .ToListAsync();

            var topLevel = allDepts
                .Where(d => d.ParentDepartmentId == null)
                .Select(d => new
                {
                    id = d.Id,
                    code = d.Code,
                    name = d.Name,
                    description = d.Description,
                    subDepartments = includeSubDepartments
                        ? allDepts
                            .Where(sub => sub.ParentDepartmentId == d.Id)
                            .Select(sub => new
                            {
                                id = sub.Id,
                                code = sub.Code,
                                name = sub.Name,
                                description = sub.Description,
                                parentCode = d.Code
                            })
                            .ToList()
                        : null
                })
                .ToList();

            return Ok(topLevel);
        }

        [HttpGet(ApiConstants.Helpers.Designations)]
        public async Task<IActionResult> GetDesignationsDropdown([FromQuery] string? levelCode = null, [FromQuery] string? track = null)
        {
            var query = _db.Designations
                .Include(d => d.OrganizationalLevel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(levelCode))
            {
                var code = levelCode.ToUpper();
                query = query.Where(d => d.OrganizationalLevel != null && d.OrganizationalLevel.Code == code);
            }

            if (!string.IsNullOrWhiteSpace(track))
            {
                query = query.Where(d => d.HierarchyTrack.ToLower() == track.ToLower());
            }

            var designations = await query
                .OrderBy(d => d.HierarchyTrack)
                .ThenBy(d => d.OrganizationalLevel != null ? d.OrganizationalLevel.RankOrder : 99)
                .Select(d => new
                {
                    id = d.Id,
                    code = d.Code,
                    title = d.Title,
                    levelCode = d.OrganizationalLevel != null ? d.OrganizationalLevel.Code : "L8",
                    levelName = d.OrganizationalLevel != null ? d.OrganizationalLevel.Name : "",
                    hierarchyTrack = d.HierarchyTrack,
                    typicalReportingTo = d.TypicalReportingTo,
                    defaultRoleCode = d.DefaultRoleCode
                })
                .ToListAsync();

            return Ok(designations);
        }

        [HttpGet(ApiConstants.Helpers.Roles)]
        public async Task<IActionResult> GetRolesDropdown([FromQuery] string? track = null)
        {
            var query = _db.Roles.AsQueryable();
            if (!string.IsNullOrWhiteSpace(track))
            {
                query = query.Where(r => r.HierarchyTrack.ToLower() == track.ToLower());
            }

            var roles = await query
                .OrderBy(r => r.HierarchyTrack)
                .ThenBy(r => r.Code)
                .Select(r => new
                {
                    id = r.Id,
                    code = r.Code,
                    name = r.Name,
                    hierarchyTrack = r.HierarchyTrack,
                    description = r.Description,
                    isSystemRole = r.IsSystemRole
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpGet(ApiConstants.Helpers.Permissions)]
        public async Task<IActionResult> GetPermissionsDropdown([FromQuery] string? module = null)
        {
            var query = _db.Permissions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(p => p.Module.ToLower() == module.ToLower());
            }

            var permissions = await query
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .Select(p => new
                {
                    id = p.Id,
                    code = p.Code,
                    module = p.Module,
                    action = p.Action,
                    description = p.Description
                })
                .ToListAsync();

            return Ok(permissions);
        }

        [HttpGet(ApiConstants.Helpers.RolePermissions)]
        public async Task<IActionResult> GetRolePermissionsDropdown([FromQuery] string? roleCode = null)
        {
            var query = _db.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role != null && rp.Permission != null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roleCode))
            {
                var code = roleCode.ToUpper();
                query = query.Where(rp => rp.Role!.Code == code);
            }

            var rolePermissions = await query
                .Select(rp => new
                {
                    roleId = rp.RoleId,
                    roleCode = rp.Role!.Code,
                    roleName = rp.Role.Name,
                    permissionId = rp.PermissionId,
                    permissionCode = rp.Permission!.Code,
                    module = rp.Permission.Module,
                    action = rp.Permission.Action,
                    scope = rp.Scope
                })
                .ToListAsync();

            return Ok(rolePermissions);
        }
    }
}
