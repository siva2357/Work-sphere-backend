using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkSphere.API.Constants;
using WorkSphere.API.DTOs;
using WorkSphere.API.Services.Interfaces;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "HR")]
    public class HrController : ControllerBase
    {
        private readonly IHrService _hrService;

        public HrController(IHrService hrService)
        {
            _hrService = hrService;
        }

        [HttpGet(ApiRoutes.Hr.Dashboard)]
        public IActionResult GetDashboard()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            return Ok(new { message = $"Welcome to the HR Dashboard, {userEmail}!" });
        }

        [HttpGet(ApiRoutes.Hr.EmployeeDirectory)]
        public IActionResult GetEmployees()
        {
            var employees = _hrService.GetEmployees();
            return Ok(employees);
        }

        [HttpGet(ApiRoutes.Hr.LeaveRequests)]
        public IActionResult GetAllLeaves()
        {
            var leaves = _hrService.GetAllLeaves();
            return Ok(leaves);
        }

        [HttpPut(ApiRoutes.Hr.LeaveRequestById)]
        public IActionResult UpdateLeaveStatus(string id, UpdateLeaveStatusDto dto)
        {
            var leave = _hrService.UpdateLeaveStatus(id, dto);
            if (leave == null) return NotFound("Leave request not found");

            return Ok(new { message = $"Leave request {dto.Status}", leave });
        }

        [HttpGet(ApiRoutes.Hr.AttendanceOverview)]
        public IActionResult GetAllAttendance()
        {
            var attendance = _hrService.GetAllAttendance();
            return Ok(attendance);
        }

        [HttpGet(ApiRoutes.Hr.Payroll)]
        public IActionResult GetAllPayroll()
        {
            var payroll = _hrService.GetAllPayroll();
            return Ok(payroll);
        }

        [HttpPost(ApiRoutes.Hr.Payroll)]
        public IActionResult GeneratePayroll(GeneratePayrollDto dto)
        {
            var payroll = _hrService.GeneratePayroll(dto);
            return Ok(new { message = "Payroll generated", payroll });
        }

        [HttpGet(ApiRoutes.Hr.Letters)]
        public IActionResult GetAllLetters()
        {
            var letters = _hrService.GetAllLetters();
            return Ok(letters);
        }

        [HttpPost(ApiRoutes.Hr.Letters)]
        public IActionResult IssueLetter(IssueLetterDto dto)
        {
            var letter = _hrService.IssueLetter(dto);
            return Ok(new { message = "Letter issued", letter });
        }

        [HttpGet(ApiRoutes.Hr.Announcements)]
        public IActionResult GetAllAnnouncements()
        {
            var announcements = _hrService.GetAllAnnouncements();
            return Ok(announcements);
        }

        [HttpPost(ApiRoutes.Hr.Announcements)]
        public IActionResult PostAnnouncement(CreateAnnouncementDto dto)
        {
            var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
            var announcement = _hrService.PostAnnouncement(authorId, dto);
            return Ok(new { message = "Announcement posted", announcement });
        }

        [HttpGet(ApiRoutes.Hr.Tickets)]
        public IActionResult GetAllTickets()
        {
            var tickets = _hrService.GetAllTickets();
            return Ok(tickets);
        }

        [HttpPut(ApiRoutes.Hr.TicketById)]
        public IActionResult UpdateTicketStatus(string id, [FromBody] string status)
        {
            var ticket = _hrService.UpdateTicketStatus(id, status);
            if (ticket == null) return NotFound("Ticket not found");
            return Ok(new { message = $"Ticket {status}", ticket });
        }

        [HttpGet(ApiRoutes.Hr.FinancialCalendar)]
        public IActionResult GetAllFinancialEvents()
        {
            var events = _hrService.GetAllFinancialEvents();
            return Ok(events);
        }
    }
}
