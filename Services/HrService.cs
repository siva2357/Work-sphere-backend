using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;
using WorkSphere.API.Services.Interfaces;

namespace WorkSphere.API.Services
{
    public class HrService : IHrService
    {
        private readonly AppDbContext _context;

        public HrService(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<object> GetEmployees()
        {
            return _context.Users.Select(u => new {
                u.Id,
                u.Email,
                u.Role
            }).ToList();
        }

        public IEnumerable<LeaveRequest> GetAllLeaves()
        {
            return _context.LeaveRequests.ToList();
        }

        public LeaveRequest? UpdateLeaveStatus(string leaveId, UpdateLeaveStatusDto dto)
        {
            var leave = _context.LeaveRequests.FirstOrDefault(l => l.Id == leaveId);
            if (leave == null) return null;

            leave.Status = dto.Status;
            _context.LeaveRequests.Update(leave);
            _context.SaveChanges();

            return leave;
        }

        public IEnumerable<AttendanceRecord> GetAllAttendance()
        {
            return _context.AttendanceRecords.ToList();
        }

        public IEnumerable<PayrollRecord> GetAllPayroll()
        {
            return _context.PayrollRecords.ToList();
        }

        public PayrollRecord GeneratePayroll(GeneratePayrollDto dto)
        {
            var payroll = new PayrollRecord
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                BaseSalary = dto.BaseSalary,
                Bonus = dto.Bonus,
                NetSalary = dto.BaseSalary + dto.Bonus
            };
            _context.PayrollRecords.Add(payroll);
            _context.SaveChanges();
            return payroll;
        }

        public IEnumerable<Letter> GetAllLetters()
        {
            return _context.Letters.ToList();
        }

        public Letter IssueLetter(IssueLetterDto dto)
        {
            var letter = new Letter
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Content = dto.Content,
                IssueDate = DateTime.UtcNow
            };
            _context.Letters.Add(letter);
            _context.SaveChanges();
            return letter;
        }

        public IEnumerable<Announcement> GetAllAnnouncements()
        {
            return _context.Announcements.OrderByDescending(a => a.DatePosted).ToList();
        }

        public Announcement PostAnnouncement(string authorId, CreateAnnouncementDto dto)
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Content = dto.Content,
                AuthorId = authorId,
                DatePosted = DateTime.UtcNow
            };
            _context.Announcements.Add(announcement);
            _context.SaveChanges();
            return announcement;
        }

        public IEnumerable<Ticket> GetAllTickets()
        {
            return _context.Tickets.ToList();
        }

        public Ticket? UpdateTicketStatus(string ticketId, string status)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null) return null;

            ticket.Status = status;
            _context.Tickets.Update(ticket);
            _context.SaveChanges();
            return ticket;
        }

        public IEnumerable<FinancialEvent> GetAllFinancialEvents()
        {
            return _context.FinancialEvents.OrderBy(e => e.Date).ToList();
        }
    }
}
