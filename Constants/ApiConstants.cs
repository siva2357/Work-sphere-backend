namespace WorkSphere.API.Constants
{
    /// <summary>
    /// Centralized API Route Constants for the entire Work-Sphere Backend.
    /// Every controller and endpoint in the backend references this single file.
    /// </summary>
    public static class ApiConstants
    {
        public const string ApiBase = "api";
        public const string Health = "/api/health";

        /// <summary>
        /// 1. Authentication & Current User Session (/api/auth)
        /// </summary>
        public static class Auth
        {
            public const string Base = "api/auth";
            public const string Login = "login";
            public const string Me = "me";

            public const string FullLogin = "/api/auth/login";
            public const string FullMe = "/api/auth/me";
        }

        /// <summary>
        /// 2. Companies (External Main Website Sync) & Main HR Employee Directory (/api/companies & /api/employee-directory)
        /// </summary>
        public static class Companies
        {
            public const string Base = "api";
            public const string GetAll = "companies";
            public const string GetByCode = "companies/{companyCode}";
            public const string ExternalRegister = "companies/external-register";

            public const string CompanyDirectory = "companies/{companyCode}/directory";
            public const string EmployeeDirectory = "employee-directory";

            public const string CompanyCreateEmployee = "companies/{companyCode}/directory/employees";
            public const string DirectoryCreateEmployee = "employee-directory/employees";

            public const string CompanyBulkImport = "companies/{companyCode}/directory/bulk-import";
            public const string DirectoryBulkImport = "employee-directory/bulk-import";

            public const string CompanyImportCsv = "companies/{companyCode}/directory/import-csv";
            public const string DirectoryImportCsv = "employee-directory/import-csv";

            public const string FullGetAll = "/api/companies";
            public const string FullExternalRegister = "/api/companies/external-register";
            public const string FullEmployeeDirectory = "/api/employee-directory";
            public const string FullDirectoryCreateEmployee = "/api/employee-directory/employees";
            public const string FullDirectoryBulkImport = "/api/employee-directory/bulk-import";
            public const string FullDirectoryImportCsv = "/api/employee-directory/import-csv";
        }

        /// <summary>
        /// 3. RBAC, Organizational Hierarchy (L1-L10 & H1-H6) & 22 Departments (/api/rbac)
        /// </summary>
        public static class Rbac
        {
            public const string Base = "api/rbac";
            public const string Departments = "departments";
            public const string DepartmentById = "departments/{id:guid}";
            public const string Levels = "levels";
            public const string Designations = "designations";
            public const string DesignationById = "designations/{id:guid}";
            public const string Roles = "roles";
            public const string RolePermissions = "roles/{roleCode}/permissions";
            public const string Permissions = "permissions";
            public const string Employees = "employees";
            public const string EmployeeOverrides = "employees/{employeeId:guid}/overrides";
            public const string ApprovalWorkflows = "approval-workflows";

            public const string FullDepartments = "/api/rbac/departments";
            public const string FullLevels = "/api/rbac/levels";
            public const string FullDesignations = "/api/rbac/designations";
            public const string FullRoles = "/api/rbac/roles";
            public const string FullPermissions = "/api/rbac/permissions";
            public const string FullEmployees = "/api/rbac/employees";
            public const string FullApprovalWorkflows = "/api/rbac/approval-workflows";
        }

        /// <summary>
        /// 4. 4-Phase Recruitment & Onboarding Funnel (/api/recruitment)
        /// </summary>
        public static class Recruitment
        {
            public const string Base = "api/recruitment";
            public const string WorkflowOverview = "workflow-overview";
            public const string Jobs = "jobs";
            public const string ExternalApply = "external/apply";
            public const string Candidates = "candidates";
            public const string EvaluatePhase = "candidates/{candidateId:guid}/evaluate-phase";
            public const string OnboardCandidate = "candidates/{candidateId:guid}/onboard";

            public const string FullWorkflowOverview = "/api/recruitment/workflow-overview";
            public const string FullJobs = "/api/recruitment/jobs";
            public const string FullExternalApply = "/api/recruitment/external/apply";
            public const string FullCandidates = "/api/recruitment/candidates";
        }

        /// <summary>
        /// 5. Unified Request Management Linked to Multi-Stage Approval Chains (/api/requests)
        /// (Leave, Resignation, Salary Hike / Appraisal, Promotion)
        /// </summary>
        public static class Requests
        {
            public const string Base = "api/requests";
            public const string Workflows = "workflows";
            public const string GetAll = "";
            public const string Create = "";
            public const string Approve = "{requestCode}/approve";
            public const string Reject = "{requestCode}/reject";

            public const string FullBase = "/api/requests";
            public const string FullWorkflows = "/api/requests/workflows";
        }

        /// <summary>
        /// 6. Attendance Management (Manager/Department Sheet) & My Attendance (/api/attendance)
        /// </summary>
        public static class Attendance
        {
            public const string Base = "api/attendance";
            public const string MyAttendance = "my";
            public const string MarkAttendance = "mark";
            public const string TeamAndDepartmentAttendance = "team";
            public const string RemindEmployee = "remind/{employeeCode}";
            public const string RemindBatch = "remind-batch";

            public const string FullMyAttendance = "/api/attendance/my";
            public const string FullMarkAttendance = "/api/attendance/mark";
            public const string FullTeamAttendance = "/api/attendance/team";
            public const string FullRemindBatch = "/api/attendance/remind-batch";
        }

        /// <summary>
        /// 7. Task Management (Assign Tasks to Team Members) & My Tasks (/api/tasks)
        /// </summary>
        public static class Tasks
        {
            public const string Base = "api/tasks";
            public const string GetAll = "";
            public const string Create = "";
            public const string UpdateStatus = "{taskCode}/status";
            public const string Delete = "{taskCode}";

            public const string FullBase = "/api/tasks";
        }

        /// <summary>
        /// 8. Leave Management (Manager Approval) & My Leaves (/api/leaves)
        /// </summary>
        public static class Leaves
        {
            public const string Base = "api/leaves";
            public const string MyLeaves = "my/{employeeCode}";
            public const string TeamLeaves = "team";
            public const string Apply = "";
            public const string Approve = "{leaveCode}/approve";
            public const string Reject = "{leaveCode}/reject";

            public const string FullBase = "/api/leaves";
            public const string FullTeamLeaves = "/api/leaves/team";
        }

        /// <summary>
        /// 9. Employee & Manager Workspace Modules: My Documents, Projects, Teams & My Payroll (/api)
        /// </summary>
        public static class Workspace
        {
            public const string Base = "api";

            // Documents (/api/documents)
            public const string MyDocuments = "documents/my/{employeeCode}";
            public const string CreateDocument = "documents";

            // Projects (/api/projects)
            public const string GetProjects = "projects";
            public const string CreateProject = "projects";

            // Teams (/api/teams/members)
            public const string TeamMembers = "teams/members";

            // Payroll (/api/payroll/my/{employeeCode})
            public const string MyPayroll = "payroll/my/{employeeCode}";

            public const string FullDocuments = "/api/documents";
            public const string FullProjects = "/api/projects";
            public const string FullTeamMembers = "/api/teams/members";
        }

        /// <summary>
        /// 10. Helpers & Form Dropdown Lookups (/api/helpers)
        /// (companies, organizational_levels, departments, designations, roles, permissions, role_permissions)
        /// </summary>
        public static class Helpers
        {
            public const string Base = "api/helpers";
            public const string Dropdowns = "dropdowns";
            public const string Companies = "companies";
            public const string OrganizationalLevels = "organizational-levels";
            public const string Departments = "departments";
            public const string Designations = "designations";
            public const string Roles = "roles";
            public const string Permissions = "permissions";
            public const string RolePermissions = "role-permissions";

            public const string FullBase = "/api/helpers";
            public const string FullDropdowns = "/api/helpers/dropdowns";
            public const string FullCompanies = "/api/helpers/companies";
            public const string FullOrganizationalLevels = "/api/helpers/organizational-levels";
            public const string FullDepartments = "/api/helpers/departments";
            public const string FullDesignations = "/api/helpers/designations";
            public const string FullRoles = "/api/helpers/roles";
            public const string FullPermissions = "/api/helpers/permissions";
            public const string FullRolePermissions = "/api/helpers/role-permissions";
        }
    }
}

