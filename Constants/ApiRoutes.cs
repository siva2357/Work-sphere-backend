namespace WorkSphere.API.Constants
{
    public static class ApiRoutes
    {
        public const string Root = "api";

        public static class Auth
        {
            public const string Login = $"{Root}/auth/login";
            public const string Signup = $"{Root}/auth/signup";
        }

        public static class Employee
        {
            public const string Profile = $"{Root}/employee/profile";
            public const string LeaveRequests = $"{Root}/employee/leave-requests";
            public const string Attendance = $"{Root}/employee/attendance";
            public const string MarkAttendance = $"{Root}/employee/mark-attendance";
            
            public const string Payroll = $"{Root}/employee/payroll";
            public const string Letters = $"{Root}/employee/letters";
            public const string Announcements = $"{Root}/employee/announcements";
            public const string Tickets = $"{Root}/employee/tickets";
            public const string FinancialCalendar = $"{Root}/employee/financial-calendar";
        }

        public static class Hr
        {
            public const string Dashboard = $"{Root}/hr/dashboard";
            public const string EmployeeDirectory = $"{Root}/hr/employee-directory";
            public const string LeaveRequests = $"{Root}/hr/leave-requests";
            public const string LeaveRequestById = $"{Root}/hr/leave-requests/{{id}}";
            public const string AttendanceOverview = $"{Root}/hr/attendance-overview";

            public const string Payroll = $"{Root}/hr/payroll";
            public const string Letters = $"{Root}/hr/letters";
            public const string Announcements = $"{Root}/hr/announcements";
            public const string Tickets = $"{Root}/hr/tickets";
            public const string TicketById = $"{Root}/hr/tickets/{{id}}";
            public const string FinancialCalendar = $"{Root}/hr/financial-calendar";
        }
    }
}
