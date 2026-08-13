using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkSphere.API.Constants;
using WorkSphere.API.DTOs;
using WorkSphere.API.Services.Interfaces;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employee,HR")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        private string GetUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        [HttpGet(ApiRoutes.Employee.Profile)]
        public IActionResult GetProfile()
        {
            var user = _employeeService.GetProfile(GetUserId());
            if (user == null) return NotFound("User not found");
            
            return Ok(new {
                user.Id,
                user.Email,
                user.Role
            });
        }

        [HttpGet(ApiRoutes.Employee.LeaveRequests)]
        public IActionResult GetMyLeaves()
        {
            var leaves = _employeeService.GetMyLeaves(GetUserId());
            return Ok(leaves);
        }

        [HttpPost(ApiRoutes.Employee.LeaveRequests)]
        public IActionResult SubmitLeave(CreateLeaveRequestDto dto)
        {
            var leave = _employeeService.SubmitLeave(GetUserId(), dto);
            return Ok(new { message = "Leave request submitted successfully", leave });
        }

        [HttpGet(ApiRoutes.Employee.Attendance)]
        public IActionResult GetMyAttendance()
        {
            var attendance = _employeeService.GetMyAttendance(GetUserId());
            return Ok(attendance);
        }

        [HttpPost(ApiRoutes.Employee.MarkAttendance)]
        public IActionResult MarkAttendance(MarkAttendanceDto dto)
        {
            var record = _employeeService.MarkAttendance(GetUserId(), dto, out string errorMessage);
            if (record == null)
            {
                return BadRequest(errorMessage);
            }
            return Ok(new { message = "Attendance marked successfully", record });
        }

        [HttpGet(ApiRoutes.Employee.Payroll)]
        public IActionResult GetMyPayroll()
        {
            var payroll = _employeeService.GetMyPayroll(GetUserId());
            return Ok(payroll);
        }

        [HttpGet(ApiRoutes.Employee.Letters)]
        public IActionResult GetMyLetters()
        {
            var letters = _employeeService.GetMyLetters(GetUserId());
            return Ok(letters);
        }

        [HttpGet(ApiRoutes.Employee.Announcements)]
        public IActionResult GetAnnouncements()
        {
            var announcements = _employeeService.GetAnnouncements();
            return Ok(announcements);
        }

        [HttpGet(ApiRoutes.Employee.Tickets)]
        public IActionResult GetMyTickets()
        {
            var tickets = _employeeService.GetMyTickets(GetUserId());
            return Ok(tickets);
        }

        [HttpPost(ApiRoutes.Employee.Tickets)]
        public IActionResult RaiseTicket(CreateTicketDto dto)
        {
            var ticket = _employeeService.RaiseTicket(GetUserId(), dto);
            return Ok(new { message = "Ticket raised successfully", ticket });
        }

        [HttpGet(ApiRoutes.Employee.FinancialCalendar)]
        public IActionResult GetFinancialCalendar()
        {
            var events = _employeeService.GetFinancialCalendar();
            return Ok(events);
        }
    }
}
