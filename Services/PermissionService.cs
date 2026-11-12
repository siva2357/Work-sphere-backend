using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;

namespace WorkSphere.API.Services
{
    public interface IPermissionService
    {
        Task<List<EffectiveModulePermissionDto>> GetEffectivePermissionsAsync(Guid employeeId);
        Task<(bool IsAllowed, string? Scope)> CheckPermissionAsync(Guid employeeId, string module, string action);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;

        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<EffectiveModulePermissionDto>> GetEffectivePermissionsAsync(Guid employeeId)
        {
            // 1. Load all role-based permissions for the employee
            var rolePermissions = await _db.EmployeeRoles
                .Where(er => er.EmployeeId == employeeId)
                .Join(_db.RolePermissions,
                    er => er.RoleId,
                    rp => rp.RoleId,
                    (er, rp) => rp)
                .Include(rp => rp.Permission)
                .ToListAsync();

            // Map of PermissionCode -> (Module, Action, Scope)
            var effectiveMap = new Dictionary<string, (string Module, string Action, string Scope)>(StringComparer.OrdinalIgnoreCase);

            foreach (var rp in rolePermissions)
            {
                if (rp.Permission == null) continue;
                var code = rp.Permission.Code;

                if (!effectiveMap.TryGetValue(code, out var existing) ||
                    DataScopes.GetRank(rp.Scope) > DataScopes.GetRank(existing.Scope))
                {
                    effectiveMap[code] = (rp.Permission.Module, rp.Permission.Action, rp.Scope);
                }
            }

            // 2. Apply employee-specific permission overrides (non-expired)
            var now = DateTime.UtcNow;
            var overrides = await _db.EmployeePermissionOverrides
                .Where(o => o.EmployeeId == employeeId && (o.ExpiresAt == null || o.ExpiresAt > now))
                .Include(o => o.Permission)
                .ToListAsync();

            foreach (var ov in overrides)
            {
                if (ov.Permission == null) continue;
                var code = ov.Permission.Code;

                if (ov.IsGranted)
                {
                    effectiveMap[code] = (ov.Permission.Module, ov.Permission.Action, ov.Scope);
                }
                else
                {
                    effectiveMap.Remove(code);
                }
            }

            // 3. Group by Module for clean frontend & API consumption
            return effectiveMap.Values
                .GroupBy(x => x.Module)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var actionScopes = g.ToDictionary(x => x.Action, x => x.Scope);
                    var highestScope = g
                        .OrderByDescending(x => DataScopes.GetRank(x.Scope))
                        .Select(x => x.Scope)
                        .FirstOrDefault() ?? DataScopes.Own;

                    return new EffectiveModulePermissionDto
                    {
                        Module = g.Key,
                        Actions = g.Select(x => x.Action).OrderBy(a => a).ToList(),
                        ActionScopes = actionScopes,
                        HighestScope = highestScope
                    };
                })
                .ToList();
        }

        public async Task<(bool IsAllowed, string? Scope)> CheckPermissionAsync(Guid employeeId, string module, string action)
        {
            var permissions = await GetEffectivePermissionsAsync(employeeId);
            var modPerm = permissions.FirstOrDefault(p =>
                string.Equals(p.Module, module, StringComparison.OrdinalIgnoreCase));

            if (modPerm == null)
                return (false, null);

            if (modPerm.ActionScopes.TryGetValue(action, out var scope))
                return (true, scope);

            var matchedKey = modPerm.ActionScopes.Keys
                .FirstOrDefault(k => string.Equals(k, action, StringComparison.OrdinalIgnoreCase));

            return matchedKey != null
                ? (true, modPerm.ActionScopes[matchedKey])
                : (false, null);
        }
    }
}

