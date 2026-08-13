using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;
using WorkSphere.API.Services.Interfaces;

namespace WorkSphere.API.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        public User? GetProfile(string userId)
        {
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        public IEnumerable<LeaveRequest> GetMyLeaves(string userId)
        {
            return _context.LeaveRequests.Where(l => l.UserId == userId).ToList();
        }

        public LeaveRequest SubmitLeave(string userId, CreateLeaveRequestDto dto)
        {
            var leave = new LeaveRequest
            {
                UserId = userId,
                LeaveType = dto.LeaveType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason
            };

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();
            return leave;
        }

        public IEnumerable<AttendanceRecord> GetMyAttendance(string userId)
        {
            return _context.AttendanceRecords.Where(a => a.UserId == userId).ToList();
        }

        public AttendanceRecord? MarkAttendance(string userId, MarkAttendanceDto dto, out string errorMessage)
        {
            errorMessage = string.Empty;
            var today = DateTime.UtcNow.Date;
            var record = _context.AttendanceRecords.FirstOrDefault(a => a.UserId == userId && a.Date == today);

            if (dto.Action == "CheckIn")
            {
                if (record != null)
                {
                    errorMessage = "Already checked in today.";
                    return null;
                }

                record = new AttendanceRecord
                {
                    UserId = userId,
                    Date = today,
                    CheckInTime = DateTime.UtcNow
                };
                _context.AttendanceRecords.Add(record);
            }
            else if (dto.Action == "CheckOut")
            {
                if (record == null)
                {
                    errorMessage = "Cannot check out without checking in first.";
                    return null;
                }
                if (record.CheckOutTime != null)
                {
                    errorMessage = "Already checked out today.";
                    return null;
                }

                record.CheckOutTime = DateTime.UtcNow;
                _context.AttendanceRecords.Update(record);
            }
            else
            {
                errorMessage = "Invalid action. Use 'CheckIn' or 'CheckOut'.";
                return null;
            }

            _context.SaveChanges();
            return record;
        }

        public IEnumerable<PayrollRecord> GetMyPayroll(string userId)
        {
            return _context.PayrollRecords.Where(p => p.UserId == userId).ToList();
        }

        public IEnumerable<Letter> GetMyLetters(string userId)
        {
            return _context.Letters.Where(l => l.UserId == userId).ToList();
        }

        public IEnumerable<Announcement> GetAnnouncements()
        {
            return _context.Announcements.OrderByDescending(a => a.DatePosted).ToList();
        }

        public IEnumerable<Ticket> GetMyTickets(string userId)
        {
            return _context.Tickets.Where(t => t.UserId == userId).ToList();
        }

        public Ticket RaiseTicket(string userId, CreateTicketDto dto)
        {
            var ticket = new Ticket
            {
                UserId = userId,
                Subject = dto.Subject,
                Description = dto.Description,
                DateRaised = DateTime.UtcNow
            };
            _context.Tickets.Add(ticket);
            _context.SaveChanges();
            return ticket;
        }

        public IEnumerable<FinancialEvent> GetFinancialCalendar()
        {
            return _context.FinancialEvents.OrderBy(e => e.Date).ToList();
        }
    }
}
