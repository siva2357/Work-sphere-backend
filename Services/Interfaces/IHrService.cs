using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Services.Interfaces
{
    public interface IHrService
    {
        IEnumerable<object> GetEmployees();
        IEnumerable<LeaveRequest> GetAllLeaves();
        LeaveRequest? UpdateLeaveStatus(string leaveId, UpdateLeaveStatusDto dto);
        IEnumerable<AttendanceRecord> GetAllAttendance();

        IEnumerable<PayrollRecord> GetAllPayroll();
        PayrollRecord GeneratePayroll(GeneratePayrollDto dto);
        
        IEnumerable<Letter> GetAllLetters();
        Letter IssueLetter(IssueLetterDto dto);

        IEnumerable<Announcement> GetAllAnnouncements();
        Announcement PostAnnouncement(string authorId, CreateAnnouncementDto dto);

        IEnumerable<Ticket> GetAllTickets();
        Ticket? UpdateTicketStatus(string ticketId, string status);

        IEnumerable<FinancialEvent> GetAllFinancialEvents();
    }
}
