using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Models;

namespace WorkSphere.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await db.Database.EnsureCreatedAsync();

            // Clean up any previously seeded dummy operational data
            await CleanupSeededDummyDataAsync(db);

            // Ensure old unique index on department name (if created previously) doesn't block subdepartments with same name
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"DROP INDEX IF EXISTS ""IX_departments_name"";");
            }
            catch
            {
                // Ignore if index does not exist
            }

            // 1. departments (22 Top-Level Departments & 14 Subdepartments)
            var deptByCode = await SeedDepartmentsAsync(db);

            // 2. organizational_levels, designations, roles, permissions, role_permissions
            if (!await db.OrganizationalLevels.AnyAsync())
            {
                await SeedLevelsRolesDesignationsAndPermissionsAsync(db);
            }

            // 3. companies (and their Main HR Administrator login accounts)
            await SeedCompaniesAsync(db, deptByCode);
        }

        /// <summary>
        /// Removes previously seeded dummy employees (EMP-001..EMP-006, HR-002..HR-004, NX-002)
        /// or seeded dummy policy documents so only the core lookup tables and companies remain.
        /// </summary>
        private static async Task CleanupSeededDummyDataAsync(AppDbContext db)
        {
            var dummyEmails = new[]
            {
                "cto@worksphere.com",
                "engmanager@worksphere.com",
                "pm@worksphere.com",
                "teamlead@worksphere.com",
                "employee@worksphere.com",
                "intern@worksphere.com",
                "hr@worksphere.com",
                "payroll@worksphere.com",
                "recruiter@worksphere.com",
                "dev@nexacloud.io"
            };

            var hasDummyEmployees = await db.Employees.AnyAsync(e => dummyEmails.Contains(e.Email.ToLower()));
            var hasSeededPolicyDocs = await db.EmployeeDocuments.AnyAsync(d => d.DocumentCode.StartsWith("DOC-POL-"));

            if (!hasDummyEmployees && !hasSeededPolicyDocs)
            {
                return;
            }

            // Recreate database cleanly without the dummy seed data
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        private static async Task SeedLevelsRolesDesignationsAndPermissionsAsync(AppDbContext db)
        {
            // ---------------------------------------------------------
            // 1. organizational_levels (L1-L10 & H1-H6)
            // ---------------------------------------------------------
            var levels = new List<OrganizationalLevel>
            {
                // Employee Hierarchy (L1 - L10)
                new() { Code = "L1", Name = "Executive Technology Leadership", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 1, Description = "CTO" },
                new() { Code = "L2", Name = "Department Leadership", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 2, Description = "Director / VP" },
                new() { Code = "L3", Name = "Engineering Management", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 3, Description = "Engineering Manager" },
                new() { Code = "L4", Name = "Project & Product Management", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 4, Description = "Project Manager / Product Manager" },
                new() { Code = "L5", Name = "Technical & Architecture Leadership", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 5, Description = "Solution Architect / Technical Lead" },
                new() { Code = "L6", Name = "Team Leadership", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 6, Description = "Team Lead" },
                new() { Code = "L7", Name = "Senior Individual Contributor", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 7, Description = "Senior Developer" },
                new() { Code = "L8", Name = "Mid-Level Individual Contributor", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 8, Description = "Developer" },
                new() { Code = "L9", Name = "Junior Individual Contributor", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 9, Description = "Junior Developer" },
                new() { Code = "L10", Name = "Entry / Training Level", HierarchyTrack = HierarchyTracks.Employee, RankOrder = 10, Description = "Intern / Trainee" },

                // HR Hierarchy (H1 - H6)
                new() { Code = "H1", Name = "HR Executive Leadership", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 1, Description = "CHRO / HR Director" },
                new() { Code = "H2", Name = "HR Department Management", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 2, Description = "HR Manager" },
                new() { Code = "H3", Name = "HR Partner & Operations Management", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 3, Description = "HR Business Partner / HR Operations Manager" },
                new() { Code = "H4", Name = "Specialized HR Management", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 4, Description = "Talent Acquisition Manager / Payroll Manager" },
                new() { Code = "H5", Name = "HR Specialist / Executive", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 5, Description = "HR Executive / Recruiter / Payroll Executive" },
                new() { Code = "H6", Name = "HR Support & Coordination", HierarchyTrack = HierarchyTracks.Hr, RankOrder = 6, Description = "HR Assistant / Recruitment Coordinator" }
            };

            db.OrganizationalLevels.AddRange(levels);
            await db.SaveChangesAsync();

            var levelByCode = levels.ToDictionary(l => l.Code);

            // ---------------------------------------------------------
            // 2. roles (23 System Roles across Employee & HR tracks)
            // ---------------------------------------------------------
            var roles = new List<Role>
            {
                // Employee Roles (12)
                new() { Code = SystemRoleCodes.Cto, Name = "CTO", HierarchyTrack = HierarchyTracks.Employee, Description = "Define company-wide technology strategy and oversee engineering." },
                new() { Code = SystemRoleCodes.DirectorVp, Name = "Director / VP", HierarchyTrack = HierarchyTracks.Employee, Description = "Manage business or technology departments and resource allocation." },
                new() { Code = SystemRoleCodes.EngineeringManager, Name = "Engineering Manager", HierarchyTrack = HierarchyTracks.Employee, Description = "Manage engineering teams, resources, and delivery quality." },
                new() { Code = SystemRoleCodes.ProjectManager, Name = "Project Manager", HierarchyTrack = HierarchyTracks.Employee, Description = "Create and manage projects, timelines, milestones, and tasks." },
                new() { Code = SystemRoleCodes.ProductManager, Name = "Product Manager", HierarchyTrack = HierarchyTracks.Employee, Description = "Define product vision, roadmap, and prioritize backlog." },
                new() { Code = SystemRoleCodes.SolutionArchitect, Name = "Solution Architect", HierarchyTrack = HierarchyTracks.Employee, Description = "Design application architecture and technical standards." },
                new() { Code = SystemRoleCodes.TechnicalLead, Name = "Technical Lead", HierarchyTrack = HierarchyTracks.Employee, Description = "Lead technical implementation, code reviews, and guide developers." },
                new() { Code = SystemRoleCodes.TeamLead, Name = "Team Lead", HierarchyTrack = HierarchyTracks.Employee, Description = "Manage daily team activities, distribute work, and review logs." },
                new() { Code = SystemRoleCodes.SeniorDeveloper, Name = "Senior Developer", HierarchyTrack = HierarchyTracks.Employee, Description = "Develop complex features, conduct code reviews, and mentor juniors." },
                new() { Code = SystemRoleCodes.Developer, Name = "Developer", HierarchyTrack = HierarchyTracks.Employee, Description = "Develop features, integrate APIs, fix bugs, and update logs." },
                new() { Code = SystemRoleCodes.JuniorDeveloper, Name = "Junior Developer", HierarchyTrack = HierarchyTracks.Employee, Description = "Complete assigned development tasks under supervision." },
                new() { Code = SystemRoleCodes.InternTrainee, Name = "Intern / Trainee", HierarchyTrack = HierarchyTracks.Employee, Description = "Complete training assignments and supervised tasks." },

                // HR Roles (11)
                new() { Code = SystemRoleCodes.Chro, Name = "CHRO / HR Director", HierarchyTrack = HierarchyTracks.Hr, Description = "Define HR strategy, policies, and oversee workforce planning." },
                new() { Code = SystemRoleCodes.HrManager, Name = "HR Manager", HierarchyTrack = HierarchyTracks.Hr, Description = "Manage HR operations, employee lifecycle, and escalations." },
                new() { Code = SystemRoleCodes.HrBusinessPartner, Name = "HR Business Partner", HierarchyTrack = HierarchyTracks.Hr, Description = "Support assigned departments, employee relations, and reviews." },
                new() { Code = SystemRoleCodes.HrOperationsManager, Name = "HR Operations Manager", HierarchyTrack = HierarchyTracks.Hr, Description = "Manage employee records, onboarding, and attendance administration." },
                new() { Code = SystemRoleCodes.TalentAcquisitionManager, Name = "Talent Acquisition Manager", HierarchyTrack = HierarchyTracks.Hr, Description = "Manage recruitment operations, hiring plans, and offers." },
                new() { Code = SystemRoleCodes.PayrollManager, Name = "Payroll Manager", HierarchyTrack = HierarchyTracks.Hr, Description = "Manage payroll processing, salary structures, and approvals." },
                new() { Code = SystemRoleCodes.HrExecutive, Name = "HR Executive", HierarchyTrack = HierarchyTracks.Hr, Description = "Maintain employee records, documents, and onboarding." },
                new() { Code = SystemRoleCodes.Recruiter, Name = "Recruiter", HierarchyTrack = HierarchyTracks.Hr, Description = "Source candidates, screen applications, and coordinate interviews." },
                new() { Code = SystemRoleCodes.PayrollExecutive, Name = "Payroll Executive", HierarchyTrack = HierarchyTracks.Hr, Description = "Prepare payroll inputs, salary calculations, and payslip drafts." },
                new() { Code = SystemRoleCodes.HrAssistant, Name = "HR Assistant", HierarchyTrack = HierarchyTracks.Hr, Description = "Maintain documentation and support onboarding checklists." },
                new() { Code = SystemRoleCodes.RecruitmentCoordinator, Name = "Recruitment Coordinator", HierarchyTrack = HierarchyTracks.Hr, Description = "Schedule interviews and coordinate recruitment activities." }
            };

            db.Roles.AddRange(roles);
            await db.SaveChangesAsync();

            var roleByCode = roles.ToDictionary(r => r.Code);

            // ---------------------------------------------------------
            // 3. designations (23 Designations linked to Levels & Default Roles)
            // ---------------------------------------------------------
            var designations = new List<Designation>
            {
                // Employee Designations
                new() { Code = "DESIG_CTO", Title = "CTO", OrganizationalLevelId = levelByCode["L1"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "CEO / Board", DefaultRoleCode = SystemRoleCodes.Cto },
                new() { Code = "DESIG_DIRECTOR_VP", Title = "Director / VP", OrganizationalLevelId = levelByCode["L2"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "CTO", DefaultRoleCode = SystemRoleCodes.DirectorVp },
                new() { Code = "DESIG_ENG_MANAGER", Title = "Engineering Manager", OrganizationalLevelId = levelByCode["L3"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Director / VP", DefaultRoleCode = SystemRoleCodes.EngineeringManager },
                new() { Code = "DESIG_PROJ_MANAGER", Title = "Project Manager", OrganizationalLevelId = levelByCode["L4"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Engineering Manager / Director", DefaultRoleCode = SystemRoleCodes.ProjectManager },
                new() { Code = "DESIG_PROD_MANAGER", Title = "Product Manager", OrganizationalLevelId = levelByCode["L4"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Director / Product Head", DefaultRoleCode = SystemRoleCodes.ProductManager },
                new() { Code = "DESIG_SOL_ARCHITECT", Title = "Solution Architect", OrganizationalLevelId = levelByCode["L5"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Engineering Manager / CTO", DefaultRoleCode = SystemRoleCodes.SolutionArchitect },
                new() { Code = "DESIG_TECH_LEAD", Title = "Technical Lead", OrganizationalLevelId = levelByCode["L5"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Engineering Manager", DefaultRoleCode = SystemRoleCodes.TechnicalLead },
                new() { Code = "DESIG_TEAM_LEAD", Title = "Team Lead", OrganizationalLevelId = levelByCode["L6"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Technical Lead / Engineering Manager", DefaultRoleCode = SystemRoleCodes.TeamLead },
                new() { Code = "DESIG_SR_DEV", Title = "Senior Developer", OrganizationalLevelId = levelByCode["L7"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Team Lead", DefaultRoleCode = SystemRoleCodes.SeniorDeveloper },
                new() { Code = "DESIG_DEV", Title = "Developer", OrganizationalLevelId = levelByCode["L8"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Senior Developer / Team Lead", DefaultRoleCode = SystemRoleCodes.Developer },
                new() { Code = "DESIG_JR_DEV", Title = "Junior Developer", OrganizationalLevelId = levelByCode["L9"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Developer / Team Lead", DefaultRoleCode = SystemRoleCodes.JuniorDeveloper },
                new() { Code = "DESIG_INTERN", Title = "Intern / Trainee", OrganizationalLevelId = levelByCode["L10"].Id, HierarchyTrack = HierarchyTracks.Employee, TypicalReportingTo = "Assigned Mentor / Team Lead", DefaultRoleCode = SystemRoleCodes.InternTrainee },

                // HR Designations
                new() { Code = "DESIG_CHRO", Title = "CHRO / HR Director", OrganizationalLevelId = levelByCode["H1"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "CEO / Board", DefaultRoleCode = SystemRoleCodes.Chro },
                new() { Code = "DESIG_HR_MANAGER", Title = "HR Manager", OrganizationalLevelId = levelByCode["H2"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "CHRO", DefaultRoleCode = SystemRoleCodes.HrManager },
                new() { Code = "DESIG_HR_BP", Title = "HR Business Partner", OrganizationalLevelId = levelByCode["H3"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR Manager", DefaultRoleCode = SystemRoleCodes.HrBusinessPartner },
                new() { Code = "DESIG_HR_OPS_MANAGER", Title = "HR Operations Manager", OrganizationalLevelId = levelByCode["H3"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR Manager", DefaultRoleCode = SystemRoleCodes.HrOperationsManager },
                new() { Code = "DESIG_TA_MANAGER", Title = "Talent Acquisition Manager", OrganizationalLevelId = levelByCode["H4"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR Manager", DefaultRoleCode = SystemRoleCodes.TalentAcquisitionManager },
                new() { Code = "DESIG_PAYROLL_MANAGER", Title = "Payroll Manager", OrganizationalLevelId = levelByCode["H4"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR / Finance Director", DefaultRoleCode = SystemRoleCodes.PayrollManager },
                new() { Code = "DESIG_HR_EXEC", Title = "HR Executive", OrganizationalLevelId = levelByCode["H5"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR Operations Manager", DefaultRoleCode = SystemRoleCodes.HrExecutive },
                new() { Code = "DESIG_RECRUITER", Title = "Recruiter", OrganizationalLevelId = levelByCode["H5"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "Talent Acquisition Manager", DefaultRoleCode = SystemRoleCodes.Recruiter },
                new() { Code = "DESIG_PAYROLL_EXEC", Title = "Payroll Executive", OrganizationalLevelId = levelByCode["H5"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "Payroll Manager", DefaultRoleCode = SystemRoleCodes.PayrollExecutive },
                new() { Code = "DESIG_HR_ASSISTANT", Title = "HR Assistant", OrganizationalLevelId = levelByCode["H6"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "HR Executive / HR Manager", DefaultRoleCode = SystemRoleCodes.HrAssistant },
                new() { Code = "DESIG_REC_COORD", Title = "Recruitment Coordinator", OrganizationalLevelId = levelByCode["H6"].Id, HierarchyTrack = HierarchyTracks.Hr, TypicalReportingTo = "Recruiter / TA Manager", DefaultRoleCode = SystemRoleCodes.RecruitmentCoordinator }
            };

            db.Designations.AddRange(designations);
            await db.SaveChangesAsync();

            // ---------------------------------------------------------
            // 4. permissions (All Modules x All 10 Standard Actions)
            // ---------------------------------------------------------
            var permissions = new List<Permission>();
            foreach (var module in PermissionModules.All)
            {
                foreach (var action in PermissionActions.All)
                {
                    permissions.Add(new Permission
                    {
                        Code = $"{module}.{action}",
                        Module = module,
                        Action = action,
                        Description = $"{action} access for {module} module"
                    });
                }
            }

            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();

            var permByCode = permissions.ToDictionary(p => p.Code);

            // ---------------------------------------------------------
            // 5. role_permissions
            // ---------------------------------------------------------
            var rolePermissions = new List<RolePermission>();
            void Grant(string roleCode, string module, string scope, params string[] actions)
            {
                var rId = roleByCode[roleCode].Id;
                foreach (var act in actions)
                {
                    var pCode = $"{module}.{act}";
                    if (permByCode.TryGetValue(pCode, out var perm))
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = rId,
                            PermissionId = perm.Id,
                            Scope = scope
                        });
                    }
                }
            }

            const string V = PermissionActions.View;
            const string C = PermissionActions.Create;
            const string E = PermissionActions.Edit;
            const string D = PermissionActions.Delete;
            const string A = PermissionActions.Assign;
            const string App = PermissionActions.Approve;
            const string Rej = PermissionActions.Reject;
            const string Rev = PermissionActions.Review;
            const string Exp = PermissionActions.Export;
            const string Mng = PermissionActions.Manage;

            // Employee Hierarchy Role Permissions (L1 - L10)
            Grant(SystemRoleCodes.Cto, PermissionModules.Dashboard, DataScopes.Organization, V);
            Grant(SystemRoleCodes.Cto, PermissionModules.Employees, DataScopes.Organization, V);
            Grant(SystemRoleCodes.Cto, PermissionModules.Departments, DataScopes.Organization, V, C, E, Mng);
            Grant(SystemRoleCodes.Cto, PermissionModules.Projects, DataScopes.Organization, V, Rev, App);
            Grant(SystemRoleCodes.Cto, PermissionModules.Tasks, DataScopes.Organization, V, Rev);
            Grant(SystemRoleCodes.Cto, PermissionModules.Attendance, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.Cto, PermissionModules.Leave, DataScopes.Delegated, V, App, Rej);
            Grant(SystemRoleCodes.Cto, PermissionModules.WorkLogs, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.Cto, PermissionModules.Performance, DataScopes.Organization, V, Rev, App);
            Grant(SystemRoleCodes.Cto, PermissionModules.Reports, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.Cto, PermissionModules.Settings, DataScopes.Organization, V, E, Mng);

            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Employees, DataScopes.Department, V);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Departments, DataScopes.Department, V, E, Mng);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Projects, DataScopes.Department, V, Rev, App);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Tasks, DataScopes.Department, V);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Attendance, DataScopes.Department, V, Exp);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Leave, DataScopes.Department, V, App, Rej);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.WorkLogs, DataScopes.Department, V, Exp);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Performance, DataScopes.Department, V, Rev, App);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Reports, DataScopes.Department, V, Exp);
            Grant(SystemRoleCodes.DirectorVp, PermissionModules.Settings, DataScopes.Department, V, E, Mng);

            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Employees, DataScopes.Department, V);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Teams, DataScopes.Department, V, C, E, A, Mng);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Projects, DataScopes.Department, V, Rev);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Tasks, DataScopes.Department, V, A, Rev);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Attendance, DataScopes.Department, V);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Leave, DataScopes.Delegated, V, Rev, App, Rej);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.WorkLogs, DataScopes.Department, V, Rev);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Performance, DataScopes.Department, V, C, E, Rev);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.JobOpenings, DataScopes.Department, V, C);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Interviews, DataScopes.Department, V, Rev, App, Rej);
            Grant(SystemRoleCodes.EngineeringManager, PermissionModules.Reports, DataScopes.Department, V, Exp);

            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Dashboard, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Employees, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Projects, DataScopes.Project, V, C, E, A, Mng);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Tasks, DataScopes.Project, V, C, E, A, Rev);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Attendance, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Leave, DataScopes.Project, V, Rev);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.WorkLogs, DataScopes.Project, V, Rev, App);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Performance, DataScopes.Project, V, C, Rev);
            Grant(SystemRoleCodes.ProjectManager, PermissionModules.Reports, DataScopes.Project, V, Exp);

            Grant(SystemRoleCodes.ProductManager, PermissionModules.Dashboard, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.Employees, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.Projects, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.Tasks, DataScopes.Project, V, C, E, Rev);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.WorkLogs, DataScopes.Project, V);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.Performance, DataScopes.Project, V, C);
            Grant(SystemRoleCodes.ProductManager, PermissionModules.Reports, DataScopes.Project, V, Exp);

            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Dashboard, DataScopes.Project, V);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Employees, DataScopes.Project, V);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Projects, DataScopes.Project, V, Rev);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Tasks, DataScopes.Project, V, C, A, Rev);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.WorkLogs, DataScopes.Project, V, Rev);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Performance, DataScopes.Project, V, C, Rev);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Interviews, DataScopes.Delegated, V, Rev);
            Grant(SystemRoleCodes.SolutionArchitect, PermissionModules.Reports, DataScopes.Project, V);

            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Dashboard, DataScopes.Team, V);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Projects, DataScopes.Project, V);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Tasks, DataScopes.Team, V, C, E, A, Rev);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Attendance, DataScopes.Team, V);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Leave, DataScopes.Team, V, Rev, App);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.WorkLogs, DataScopes.Team, V, E, Rev);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Performance, DataScopes.Team, V, C, Rev);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Interviews, DataScopes.Delegated, V, Rev);
            Grant(SystemRoleCodes.TechnicalLead, PermissionModules.Reports, DataScopes.Team, V, Exp);

            Grant(SystemRoleCodes.TeamLead, PermissionModules.Dashboard, DataScopes.Team, V);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Projects, DataScopes.Project, V);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Tasks, DataScopes.Team, V, C, E, A, Rev);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Attendance, DataScopes.Team, V);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Leave, DataScopes.Team, V, Rev);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.WorkLogs, DataScopes.Team, V, Rev);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Performance, DataScopes.Team, V, C, Rev);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Interviews, DataScopes.Delegated, V, Rev);
            Grant(SystemRoleCodes.TeamLead, PermissionModules.Reports, DataScopes.Team, V);

            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Dashboard, DataScopes.Team, V);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Projects, DataScopes.Own, V);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Tasks, DataScopes.Own, V, C, E, Rev);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Attendance, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Leave, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.WorkLogs, DataScopes.Own, V, C, E);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Performance, DataScopes.Own, V);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Payslips, DataScopes.Own, V);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Documents, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Interviews, DataScopes.Delegated, V, Rev);
            Grant(SystemRoleCodes.SeniorDeveloper, PermissionModules.Reports, DataScopes.Own, V);

            Grant(SystemRoleCodes.Developer, PermissionModules.Dashboard, DataScopes.Own, V);
            Grant(SystemRoleCodes.Developer, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.Developer, PermissionModules.Projects, DataScopes.Own, V);
            Grant(SystemRoleCodes.Developer, PermissionModules.Tasks, DataScopes.Own, V, E);
            Grant(SystemRoleCodes.Developer, PermissionModules.Attendance, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.Developer, PermissionModules.Leave, DataScopes.Own, V, C, D);
            Grant(SystemRoleCodes.Developer, PermissionModules.WorkLogs, DataScopes.Own, V, C, E);
            Grant(SystemRoleCodes.Developer, PermissionModules.Performance, DataScopes.Own, V);
            Grant(SystemRoleCodes.Developer, PermissionModules.Payslips, DataScopes.Own, V);
            Grant(SystemRoleCodes.Developer, PermissionModules.Documents, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.Developer, PermissionModules.Reports, DataScopes.Own, V);

            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Dashboard, DataScopes.Own, V);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Projects, DataScopes.Own, V);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Tasks, DataScopes.Own, V, E);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Attendance, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Leave, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.WorkLogs, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Performance, DataScopes.Own, V);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Payslips, DataScopes.Own, V);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Documents, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.JuniorDeveloper, PermissionModules.Reports, DataScopes.Own, V);

            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Dashboard, DataScopes.Own, V);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Employees, DataScopes.Team, V);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Projects, DataScopes.Own, V);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Tasks, DataScopes.Own, V, E);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Attendance, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Leave, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.WorkLogs, DataScopes.Own, V, C);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Performance, DataScopes.Own, V);
            Grant(SystemRoleCodes.InternTrainee, PermissionModules.Reports, DataScopes.Own, V);

            // HR Hierarchy Role Permissions (H1 - H6)
            Grant(SystemRoleCodes.Chro, PermissionModules.Dashboard, DataScopes.Organization, V);
            Grant(SystemRoleCodes.Chro, PermissionModules.Employees, DataScopes.Organization, V, Rev, Exp);
            Grant(SystemRoleCodes.Chro, PermissionModules.Departments, DataScopes.Organization, V, Mng);
            Grant(SystemRoleCodes.Chro, PermissionModules.Recruitment, DataScopes.Organization, V, Rev, App, Mng);
            Grant(SystemRoleCodes.Chro, PermissionModules.Onboarding, DataScopes.Organization, V, Rev, Mng);
            Grant(SystemRoleCodes.Chro, PermissionModules.Attendance, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.Chro, PermissionModules.Leave, DataScopes.Organization, V, App, Rej);
            Grant(SystemRoleCodes.Chro, PermissionModules.Performance, DataScopes.Organization, V, Rev, App);
            Grant(SystemRoleCodes.Chro, PermissionModules.Payroll, DataScopes.Organization, V, Rev, App);
            Grant(SystemRoleCodes.Chro, PermissionModules.Reports, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.Chro, PermissionModules.HrSettings, DataScopes.Organization, V, C, E, Mng);

            Grant(SystemRoleCodes.HrManager, PermissionModules.Dashboard, DataScopes.Organization, V);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Employees, DataScopes.Organization, V, C, E, D, Mng);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Departments, DataScopes.Organization, V, E, Mng);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Recruitment, DataScopes.Organization, V, C, E, App, Mng);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Onboarding, DataScopes.Organization, V, C, E, Mng);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Attendance, DataScopes.Organization, V, E, App, Mng);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Leave, DataScopes.Organization, V, Rev, App, Rej);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Performance, DataScopes.Organization, V, C, E, A, Rev);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Payroll, DataScopes.Delegated, V);
            Grant(SystemRoleCodes.HrManager, PermissionModules.Reports, DataScopes.Organization, V, Exp);
            Grant(SystemRoleCodes.HrManager, PermissionModules.HrSettings, DataScopes.Delegated, V, E, Mng);

            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Employees, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Recruitment, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Onboarding, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Attendance, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Leave, DataScopes.Department, V, Rev);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Performance, DataScopes.Department, V, C, E, Rev);
            Grant(SystemRoleCodes.HrBusinessPartner, PermissionModules.Reports, DataScopes.Department, V, Exp);

            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Dashboard, DataScopes.Organization, V);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Employees, DataScopes.Organization, V, C, E, D);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Onboarding, DataScopes.Organization, V, C, E, A, Mng);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Attendance, DataScopes.Organization, V, E, App, Mng);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Leave, DataScopes.Delegated, V, Rev, App, Rej);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Documents, DataScopes.Organization, V, C, E, D, Mng);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Payroll, DataScopes.Organization, V);
            Grant(SystemRoleCodes.HrOperationsManager, PermissionModules.Reports, DataScopes.Organization, V, Exp);

            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.JobOpenings, DataScopes.Organization, V, C, E, App, Mng);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Candidates, DataScopes.Organization, V, C, E, D, A, Rev, Mng);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Interviews, DataScopes.Organization, V, C, E, A, Mng);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Offers, DataScopes.Delegated, V, C, E, App, Rej);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Onboarding, DataScopes.Organization, V, C);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Employees, DataScopes.Delegated, V);
            Grant(SystemRoleCodes.TalentAcquisitionManager, PermissionModules.Reports, DataScopes.Organization, V, Exp);

            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Employees, DataScopes.Organization, V);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Payroll, DataScopes.Organization, V, C, E, Rev, App, Mng);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.SalaryStructure, DataScopes.Organization, V, C, E, Mng);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Payslips, DataScopes.Organization, V, C, E, App, Exp);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Attendance, DataScopes.Organization, V);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Leave, DataScopes.Organization, V);
            Grant(SystemRoleCodes.PayrollManager, PermissionModules.Reports, DataScopes.Organization, V, Exp);

            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Dashboard, DataScopes.Department, V);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Employees, DataScopes.Department, V, C, E);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Documents, DataScopes.Department, V, C, E);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Onboarding, DataScopes.Department, V, C, E);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Attendance, DataScopes.Department, V, C, E);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Leave, DataScopes.Department, V, Rev);
            Grant(SystemRoleCodes.HrExecutive, PermissionModules.Reports, DataScopes.Department, V);

            Grant(SystemRoleCodes.Recruiter, PermissionModules.Dashboard, DataScopes.Team, V);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Candidates, DataScopes.Team, V, C, E);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.JobOpenings, DataScopes.Team, V);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Interviews, DataScopes.Team, V, C, E);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Offers, DataScopes.Delegated, V, C);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Onboarding, DataScopes.Team, V, C);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Employees, DataScopes.Delegated, V);
            Grant(SystemRoleCodes.Recruiter, PermissionModules.Reports, DataScopes.Team, V);

            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Dashboard, DataScopes.Team, V);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Employees, DataScopes.Organization, V);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Payroll, DataScopes.Team, V, C, E);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.SalaryStructure, DataScopes.Team, V);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Payslips, DataScopes.Team, V, C);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Attendance, DataScopes.Organization, V);
            Grant(SystemRoleCodes.PayrollExecutive, PermissionModules.Reports, DataScopes.Team, V);

            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Dashboard, DataScopes.Own, V);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Employees, DataScopes.Team, V, E);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Documents, DataScopes.Team, V, C);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Onboarding, DataScopes.Team, V, E);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Attendance, DataScopes.Team, V);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Leave, DataScopes.Team, V);
            Grant(SystemRoleCodes.HrAssistant, PermissionModules.Reports, DataScopes.Own, V);

            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.Dashboard, DataScopes.Own, V);
            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.Candidates, DataScopes.Team, V, E);
            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.Interviews, DataScopes.Team, V, C, E);
            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.JobOpenings, DataScopes.Team, V);
            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.Offers, DataScopes.Team, V);
            Grant(SystemRoleCodes.RecruitmentCoordinator, PermissionModules.Reports, DataScopes.Own, V);

            db.RolePermissions.AddRange(rolePermissions);
            await db.SaveChangesAsync();
        }

        private static async Task SeedCompaniesAsync(
            AppDbContext db,
            Dictionary<string, Department> deptByCode)
        {
            if (await db.Companies.AnyAsync())
            {
                return;
            }

            var hasher = new PasswordHasher<Employee>();
            var levelByCode = await db.OrganizationalLevels.ToDictionaryAsync(l => l.Code);
            var desigByCode = await db.Designations.ToDictionaryAsync(d => d.Code);
            var roleByCode = await db.Roles.ToDictionaryAsync(r => r.Code);

            // Registered Company 1: WorkSphere Technologies Pvt. Ltd.
            var company1 = new Company
            {
                CompanyCode = "CMP-001",
                ExternalRegistrationId = "EXT-REG-2026-001",
                Name = "WorkSphere Technologies Pvt. Ltd.",
                Domain = "worksphere.com",
                Industry = "Enterprise SaaS & Cloud Engineering",
                CompanySize = "250-500 Employees",
                Headquarters = "Bangalore / Hyderabad, India",
                ExternalWebsiteUrl = "https://worksphere.com/companies/worksphere-tech",
                RegistrationSource = "EXTERNAL_MAIN_WEBSITE",
                SubscriptionPlan = "Enterprise Unlimited",
                MainHrEmail = "chro@worksphere.com",
                MainHrName = "Victoria Sterling",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow.AddMonths(-6)
            };

            // Registered Company 2: NexaCloud Enterprise Solutions Ltd.
            var company2 = new Company
            {
                CompanyCode = "CMP-002",
                ExternalRegistrationId = "EXT-REG-2026-002",
                Name = "NexaCloud Enterprise Solutions Ltd.",
                Domain = "nexacloud.io",
                Industry = "FinTech & AI Cloud Infrastructure",
                CompanySize = "100-250 Employees",
                Headquarters = "Mumbai / Pune, India",
                ExternalWebsiteUrl = "https://worksphere.com/companies/nexacloud-io",
                RegistrationSource = "EXTERNAL_MAIN_WEBSITE",
                SubscriptionPlan = "Enterprise Pro",
                MainHrEmail = "hr.admin@nexacloud.io",
                MainHrName = "Meera Deshmukh",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow.AddMonths(-2)
            };

            db.Companies.AddRange(company1, company2);
            await db.SaveChangesAsync();

            // Provision Main HR Account for Company 1 (CMP-001)
            var wsMainHr = new Employee
            {
                EmployeeCode = "HR-001",
                FullName = "Victoria Sterling",
                Email = "chro@worksphere.com",
                PortalType = PortalTypes.Hr,
                CompanyId = company1.Id,
                IsMainHr = true,
                DepartmentId = deptByCode["HR"].Id,
                DesignationId = desigByCode["DESIG_CHRO"].Id,
                OrganizationalLevelId = levelByCode["H1"].Id,
                Phone = "+91 98450 10001",
                EmploymentType = "Full-time",
                WorkLocation = "Bangalore, India",
                WorkStatus = "Active",
                IsActive = true
            };
            wsMainHr.PasswordHash = hasher.HashPassword(wsMainHr, "Chro@12345");

            // Provision Main HR Account for Company 2 (CMP-002)
            var nexaMainHr = new Employee
            {
                EmployeeCode = "NX-001",
                FullName = "Meera Deshmukh",
                Email = "hr.admin@nexacloud.io",
                PortalType = PortalTypes.Hr,
                CompanyId = company2.Id,
                IsMainHr = true,
                DepartmentId = deptByCode["HR"].Id,
                DesignationId = desigByCode["DESIG_CHRO"].Id,
                OrganizationalLevelId = levelByCode["H1"].Id,
                Phone = "+91 98201 44556",
                EmploymentType = "Full-time",
                WorkLocation = "Mumbai, India",
                WorkStatus = "Active",
                IsActive = true
            };
            nexaMainHr.PasswordHash = hasher.HashPassword(nexaMainHr, "NexaHr@12345");

            db.Employees.AddRange(wsMainHr, nexaMainHr);
            await db.SaveChangesAsync();

            company1.MainHrEmployeeId = wsMainHr.Id;
            company2.MainHrEmployeeId = nexaMainHr.Id;

            db.EmployeeRoles.AddRange(
                new EmployeeRole { EmployeeId = wsMainHr.Id, RoleId = roleByCode[SystemRoleCodes.Chro].Id, AssignedDepartmentId = deptByCode["HR"].Id, IsPrimary = true },
                new EmployeeRole { EmployeeId = wsMainHr.Id, RoleId = roleByCode[SystemRoleCodes.HrManager].Id, AssignedDepartmentId = deptByCode["HR"].Id, IsPrimary = false },
                new EmployeeRole { EmployeeId = nexaMainHr.Id, RoleId = roleByCode[SystemRoleCodes.Chro].Id, AssignedDepartmentId = deptByCode["HR"].Id, IsPrimary = true },
                new EmployeeRole { EmployeeId = nexaMainHr.Id, RoleId = roleByCode[SystemRoleCodes.HrManager].Id, AssignedDepartmentId = deptByCode["HR"].Id, IsPrimary = false }
            );

            await db.SaveChangesAsync();
        }

        private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(AppDbContext db)
        {
            var existing = await db.Departments.ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

            var topLevelSpecs = new (string Code, string Name, string Description)[]
            {
                ("EXEC",   "Executive Leadership",                      "Board, CEO, CTO, CHRO and executive strategy"),
                ("HR",     "Human Resources",                           "People operations, talent acquisition, and employee lifecycle"),
                ("ENG",    "Engineering & Software Development",        "Software engineering, architecture, and application delivery"),
                ("QA",     "Quality Assurance & Testing",               "Manual and automated testing, release quality"),
                ("UX",     "UI/UX & Product Design",                    "User research, interface design, and design systems"),
                ("PROD",   "Product Management",                        "Product vision, roadmap, and backlog management"),
                ("PMO",    "Project Management Office",                 "Project delivery, planning, milestones, and governance"),
                ("DEVOPS", "DevOps & Cloud Infrastructure",             "CI/CD pipelines, cloud infrastructure, and site reliability"),
                ("IT",     "IT Support & Administration",               "Internal IT systems, hardware, and helpdesk support"),
                ("SEC",    "Cybersecurity & Information Security",      "Application security, compliance, and threat protection"),
                ("DATA",   "Data Engineering & Analytics",              "Data pipelines, warehousing, and business intelligence"),
                ("AI",     "Artificial Intelligence & Machine Learning","AI/ML models, intelligent automation, and R&D"),
                ("SALES",  "Sales & Business Development",              "Enterprise sales, partnerships, and revenue growth"),
                ("MKT",    "Marketing & Communications",                "Brand strategy, digital marketing, and corporate communications"),
                ("FIN",    "Finance & Accounting",                      "Corporate finance, accounting, and tax compliance"),
                ("PAY",    "Payroll & Compensation",                    "Salary processing, benefits, and statutory compliance"),
                ("LEGAL",  "Legal & Compliance",                        "Contracts, regulatory compliance, and corporate governance"),
                ("OPS",    "Operations",                                "Business operations and process optimization"),
                ("CS",     "Customer Support & Success",                "Client onboarding, technical support, and customer retention"),
                ("ADMIN",  "Administration & Facilities",               "Workplace management, facilities, and office administration"),
                ("LND",    "Learning & Development",                    "Corporate training, upskilling, and certification programs"),
                ("PROC",   "Procurement & Vendor Management",           "Vendor relations, purchasing, and contract management")
            };

            foreach (var (code, name, desc) in topLevelSpecs)
            {
                if (existing.TryGetValue(code, out var dept))
                {
                    dept.Name = name;
                    dept.Description = desc;
                    dept.ParentDepartmentId = null;
                }
                else
                {
                    var newDept = new Department
                    {
                        Code = code,
                        Name = name,
                        Description = desc,
                        ParentDepartmentId = null,
                        IsActive = true
                    };
                    db.Departments.Add(newDept);
                    existing[code] = newDept;
                }
            }

            await db.SaveChangesAsync();

            var subDepartmentSpecs = new (string ParentCode, string Code, string Name, string Description)[]
            {
                // Engineering & Software Development (ENG)
                ("ENG", "ENG-FE",   "Frontend Development",             "Web UI and frontend client engineering"),
                ("ENG", "ENG-BE",   "Backend Development",              "APIs, microservices, and database engineering"),
                ("ENG", "ENG-MOB",  "Mobile Application Development",   "iOS, Android, and cross-platform mobile apps"),
                ("ENG", "ENG-FS",   "Full Stack Development",           "End-to-end web and application engineering"),
                ("ENG", "ENG-ARCH", "Software Architecture",            "System design, scalability, and technical standards"),

                // Human Resources (HR)
                ("HR",  "HR-TA",    "Talent Acquisition",               "Sourcing, recruiting, and candidate interviews"),
                ("HR",  "HR-OPS",   "HR Operations",                    "Employee records, onboarding, and attendance administration"),
                ("HR",  "HR-ER",    "Employee Relations",               "Workplace culture, grievances, and performance coordination"),
                ("HR",  "HR-PAY",   "Payroll & Benefits",               "HR compensation, benefits administration, and leave policies"),
                ("HR",  "HR-LND",   "Learning & Development",           "Employee training programs and onboarding education"),

                // Finance & Accounting (FIN)
                ("FIN", "FIN-AP",   "Accounts Payable",                 "Vendor invoices, expense payouts, and outgoing payments"),
                ("FIN", "FIN-AR",   "Accounts Receivable",              "Client billing, collections, and incoming revenue"),
                ("FIN", "FIN-FP",   "Financial Planning",               "Budgeting, forecasting, and financial analysis"),
                ("FIN", "FIN-TAX",  "Tax & Compliance",                 "Corporate tax filings, audits, and statutory compliance")
            };

            foreach (var (parentCode, code, name, desc) in subDepartmentSpecs)
            {
                var parentId = existing[parentCode].Id;
                if (existing.TryGetValue(code, out var subDept))
                {
                    subDept.Name = name;
                    subDept.Description = desc;
                    subDept.ParentDepartmentId = parentId;
                }
                else
                {
                    var newSub = new Department
                    {
                        Code = code,
                        Name = name,
                        Description = desc,
                        ParentDepartmentId = parentId,
                        IsActive = true
                    };
                    db.Departments.Add(newSub);
                    existing[code] = newSub;
                }
            }

            await db.SaveChangesAsync();
            return existing;
        }
    }
}
