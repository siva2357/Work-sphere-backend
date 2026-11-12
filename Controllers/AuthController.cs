using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;
using WorkSphere.API.Services;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Auth.Base)]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IPermissionService _permissionService;
        private readonly PasswordHasher<Employee> _passwordHasher = new();

        public AuthController(AppDbContext db, IConfiguration config, IPermissionService permissionService)
        {
            _db = db;
            _config = config;
            _permissionService = permissionService;
        }

        [HttpPost(ApiConstants.Auth.Login)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var employee = await _db.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.OrganizationalLevel)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(e => e.Email.ToLower() == normalizedEmail);

            if (employee == null || !employee.IsActive)
            {
                return Unauthorized(new { message = "Invalid email or password. Note: Individual users cannot self-register; your Company's Main HR must create your account in the Employee Directory first." });
            }

            if (!string.IsNullOrWhiteSpace(request.CompanyCode) &&
                employee.Company != null &&
                !employee.Company.CompanyCode.Equals(request.CompanyCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = $"Employee '{employee.Email}' does not belong to company '{request.CompanyCode}'." });
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var roleCodes = employee.EmployeeRoles
                .Where(er => er.Role != null)
                .Select(er => er.Role!.Code)
                .ToList();

            var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(employee.Id);
            var token = GenerateJwtToken(employee, roleCodes);

            return Ok(new LoginResponseDto
            {
                Token = token,
                User = BuildEmployeeInfoDto(employee, roleCodes, effectivePermissions)
            });
        }

        [Authorize]
        [HttpGet(ApiConstants.Auth.Me)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var employeeId))
            {
                return Unauthorized();
            }

            var employee = await _db.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.OrganizationalLevel)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null || !employee.IsActive)
            {
                return Unauthorized();
            }

            var roleCodes = employee.EmployeeRoles
                .Where(er => er.Role != null)
                .Select(er => er.Role!.Code)
                .ToList();

            var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(employee.Id);

            return Ok(BuildEmployeeInfoDto(employee, roleCodes, effectivePermissions));
        }

        private static EmployeeInfoDto BuildEmployeeInfoDto(
            Employee employee,
            List<string> roleCodes,
            List<EffectiveModulePermissionDto> effectivePermissions)
        {
            return new EmployeeInfoDto
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                CompanyId = employee.CompanyId,
                CompanyCode = employee.Company?.CompanyCode,
                CompanyName = employee.Company?.Name,
                CompanyDomain = employee.Company?.Domain,
                IsMainHr = employee.IsMainHr,
                PortalType = employee.PortalType,
                HierarchyTrack = employee.OrganizationalLevel?.HierarchyTrack,
                LevelCode = employee.OrganizationalLevel?.Code,
                LevelName = employee.OrganizationalLevel?.Name,
                DesignationTitle = employee.Designation?.Title,
                DepartmentName = employee.Department?.Name,
                Roles = roleCodes,
                Permissions = effectivePermissions
            };
        }

        private string GenerateJwtToken(Employee employee, List<string> roleCodes)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? jwtSettings["Key"]
                ?? throw new InvalidOperationException("JWT key is missing.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new(ClaimTypes.Email, employee.Email),
                new(ClaimTypes.Name, employee.FullName),
                new("portal_type", employee.PortalType),
                new("employee_code", employee.EmployeeCode),
                new("is_main_hr", employee.IsMainHr.ToString().ToLowerInvariant())
            };

            if (employee.Company != null)
            {
                claims.Add(new Claim("company_code", employee.Company.CompanyCode));
                claims.Add(new Claim("company_name", employee.Company.Name));
            }

            if (employee.OrganizationalLevel != null)
            {
                claims.Add(new Claim("level_code", employee.OrganizationalLevel.Code));
                claims.Add(new Claim("hierarchy_track", employee.OrganizationalLevel.HierarchyTrack));
            }

            if (employee.Designation != null)
            {
                claims.Add(new Claim("designation", employee.Designation.Title));
            }

            foreach (var roleCode in roleCodes)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleCode));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
