using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Services.Interfaces
{
    public interface IEmployeeService
    {
        User? GetProfile(string userId);
        IEnumerable<LeaveRequest> GetMyLeaves(string userId);
        LeaveRequest SubmitLeave(string userId, CreateLeaveRequestDto dto);
        IEnumerable<AttendanceRecord> GetMyAttendance(string userId);
        AttendanceRecord? MarkAttendance(string userId, MarkAttendanceDto dto, out string errorMessage);

        IEnumerable<PayrollRecord> GetMyPayroll(string userId);
        IEnumerable<Letter> GetMyLetters(string userId);
        IEnumerable<Announcement> GetAnnouncements();
        IEnumerable<Ticket> GetMyTickets(string userId);
        Ticket RaiseTicket(string userId, CreateTicketDto dto);
        IEnumerable<FinancialEvent> GetFinancialCalendar();
    }
}
