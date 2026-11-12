namespace WorkSphere.API.Constants
{
    public static class PortalTypes
    {
        public const string Hr = "hr";
        public const string Employee = "employee";
    }

    public static class HierarchyTracks
    {
        public const string Employee = "Employee";
        public const string Hr = "HR";
    }

    public static class PermissionActions
    {
        public const string View = "View";
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Delete = "Delete";
        public const string Assign = "Assign";
        public const string Approve = "Approve";
        public const string Reject = "Reject";
        public const string Review = "Review";
        public const string Export = "Export";
        public const string Manage = "Manage";

        public static readonly string[] All =
        [
            View, Create, Edit, Delete, Assign,
            Approve, Reject, Review, Export, Manage
        ];
    }

    public static class PermissionModules
    {
        public const string Dashboard = "Dashboard";
        public const string Employees = "Employees";
        public const string Departments = "Departments";
        public const string Teams = "Teams";
        public const string Projects = "Projects";
        public const string Tasks = "Tasks";
        public const string Attendance = "Attendance";
        public const string Leave = "Leave";
        public const string WorkLogs = "WorkLogs";
        public const string Performance = "Performance";
        public const string Payroll = "Payroll";
        public const string SalaryStructure = "SalaryStructure";
        public const string Payslips = "Payslips";
        public const string Documents = "Documents";
        public const string Recruitment = "Recruitment";
        public const string JobOpenings = "JobOpenings";
        public const string Candidates = "Candidates";
        public const string Interviews = "Interviews";
        public const string Offers = "Offers";
        public const string Onboarding = "Onboarding";
        public const string Requests = "Requests";
        public const string Reports = "Reports";
        public const string Settings = "Settings";
        public const string HrSettings = "HrSettings";

        public static readonly string[] All =
        [
            Dashboard, Employees, Departments, Teams, Projects, Tasks,
            Attendance, Leave, WorkLogs, Performance, Payroll,
            SalaryStructure, Payslips, Documents, Recruitment,
            JobOpenings, Candidates, Interviews, Offers, Onboarding,
            Requests, Reports, Settings, HrSettings
        ];
    }

    /// <summary>
    /// Ranked data visibility scopes from narrowest (1) to broadest (6).
    /// </summary>
    public static class DataScopes
    {
        public const string Own = "Own";
        public const string Delegated = "Delegated";
        public const string Team = "Team";
        public const string Project = "Project";
        public const string Department = "Department";
        public const string Organization = "Organization";

        public static int GetRank(string? scope) => scope switch
        {
            Own => 1,
            Delegated => 2,
            Team => 3,
            Project => 4,
            Department => 5,
            Organization => 6,
            _ => 0
        };
    }

    public static class SystemRoleCodes
    {
        // Employee Hierarchy (L1 - L10)
        public const string Cto = "EMP_CTO";
        public const string DirectorVp = "EMP_DIRECTOR_VP";
        public const string EngineeringManager = "EMP_ENG_MANAGER";
        public const string ProjectManager = "EMP_PROJECT_MANAGER";
        public const string ProductManager = "EMP_PRODUCT_MANAGER";
        public const string SolutionArchitect = "EMP_SOLUTION_ARCHITECT";
        public const string TechnicalLead = "EMP_TECH_LEAD";
        public const string TeamLead = "EMP_TEAM_LEAD";
        public const string SeniorDeveloper = "EMP_SENIOR_DEV";
        public const string Developer = "EMP_DEVELOPER";
        public const string JuniorDeveloper = "EMP_JUNIOR_DEV";
        public const string InternTrainee = "EMP_INTERN";

        // HR Hierarchy (H1 - H6)
        public const string Chro = "HR_CHRO";
        public const string HrManager = "HR_MANAGER";
        public const string HrBusinessPartner = "HR_BUSINESS_PARTNER";
        public const string HrOperationsManager = "HR_OPERATIONS_MANAGER";
        public const string TalentAcquisitionManager = "HR_TA_MANAGER";
        public const string PayrollManager = "HR_PAYROLL_MANAGER";
        public const string HrExecutive = "HR_EXECUTIVE";
        public const string Recruiter = "HR_RECRUITER";
        public const string PayrollExecutive = "HR_PAYROLL_EXECUTIVE";
        public const string HrAssistant = "HR_ASSISTANT";
        public const string RecruitmentCoordinator = "HR_RECRUITMENT_COORDINATOR";
    }
}

