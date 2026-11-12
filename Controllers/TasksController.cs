using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.DTOs;
using WorkSphere.API.Models;

namespace WorkSphere.API.Controllers
{
    [ApiController]
    [Route(ApiConstants.Tasks.Base)]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lists tasks for "My Tasks" (filter by assigneeCode) or "Task Management" (filter by assignedByCode, departmentCode, projectCode, status).
        /// Matches Angular's ManagedTask schema.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string? assigneeCode = null,
            [FromQuery] string? assignedByCode = null,
            [FromQuery] string? departmentCode = null,
            [FromQuery] string? projectCode = null,
            [FromQuery] string? status = null)
        {
            var query = _db.TaskItems
                .Include(t => t.Assignee)
                    .ThenInclude(a => a!.Department)
                .Include(t => t.Assignee)
                    .ThenInclude(a => a!.Designation)
                .Include(t => t.AssignedBy)
                    .ThenInclude(m => m!.Designation)
                .Include(t => t.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(assigneeCode))
            {
                var code = assigneeCode.ToUpper();
                query = query.Where(t => t.Assignee != null && t.Assignee.EmployeeCode == code);
            }

            if (!string.IsNullOrWhiteSpace(assignedByCode))
            {
                var code = assignedByCode.ToUpper();
                query = query.Where(t => t.AssignedBy != null && t.AssignedBy.EmployeeCode == code);
            }

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                var deptCode = departmentCode.ToUpper();
                query = query.Where(t => t.Assignee != null && t.Assignee.Department != null && t.Assignee.Department.Code == deptCode);
            }

            if (!string.IsNullOrWhiteSpace(projectCode))
            {
                var pCode = projectCode.ToUpper();
                query = query.Where(t => t.Project != null && t.Project.Code == pCode);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status.ToLower() == status.ToLower());
            }

            var tasks = await query
                .OrderByDescending(t => t.AssignedDate)
                .ToListAsync();

            var formattedTasks = tasks.Select(MapTaskToDto).ToList();

            return Ok(new
            {
                summary = new
                {
                    totalTasks = tasks.Count,
                    completedTasks = tasks.Count(t => t.Status == "Completed"),
                    inProgressTasks = tasks.Count(t => t.Status == "In Progress"),
                    pendingTasks = tasks.Count(t => t.Status == "Pending"),
                    urgentTasks = tasks.Count(t => t.Priority == "Urgent" && t.Status != "Completed"),
                    totalEstimatedHours = tasks.Sum(t => t.EstimatedHours),
                    totalLoggedHours = tasks.Sum(t => t.LoggedHours)
                },
                tasks = formattedTasks
            });
        }

        /// <summary>
        /// Assigns a new task to a team member ("Task Management" / "Create Task").
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAndAssignTask([FromBody] CreateTaskRequestDto dto)
        {
            var assignee = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeCode == dto.AssigneeEmployeeCode.ToUpper());

            if (assignee == null)
                return NotFound(new { message = $"Assignee employee '{dto.AssigneeEmployeeCode}' not found." });

            Employee? assignedBy = null;
            if (!string.IsNullOrWhiteSpace(dto.AssignedByEmployeeCode))
            {
                assignedBy = await _db.Employees
                    .Include(e => e.Designation)
                    .FirstOrDefaultAsync(e => e.EmployeeCode == dto.AssignedByEmployeeCode.ToUpper());
            }

            Project? project = null;
            if (!string.IsNullOrWhiteSpace(dto.ProjectCode))
            {
                project = await _db.Projects
                    .FirstOrDefaultAsync(p => p.Code == dto.ProjectCode.ToUpper());
            }

            var count = await _db.TaskItems.CountAsync();
            var taskCode = $"TSK-{2001 + count}";

            var task = new TaskItem
            {
                TaskCode = taskCode,
                Title = dto.Title,
                Description = dto.Description,
                ProjectId = project?.Id,
                ProjectName = !string.IsNullOrWhiteSpace(dto.ProjectName)
                    ? dto.ProjectName
                    : (project?.Name ?? "Work-Sphere Enterprise Platform"),
                AssigneeEmployeeId = assignee.Id,
                AssignedByEmployeeId = assignedBy?.Id,
                AssignedDate = DateTime.UtcNow,
                DueDate = dto.DueDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(5),
                Status = "Pending",
                Priority = dto.Priority,
                EstimatedHours = dto.EstimatedHours,
                LoggedHours = 0m,
                ProgressPercent = 0,
                Category = dto.Category,
                SubtasksJson = dto.Subtasks != null && dto.Subtasks.Count > 0
                    ? string.Join(";", dto.Subtasks)
                    : null
            };

            _db.TaskItems.Add(task);
            await _db.SaveChangesAsync();

            task.Assignee = assignee;
            task.AssignedBy = assignedBy;
            task.Project = project;

            return Ok(new
            {
                message = $"Task '{task.TaskCode}' assigned to {assignee.FullName} ({assignee.EmployeeCode}).",
                task = MapTaskToDto(task)
            });
        }

        /// <summary>
        /// Updates task status ("Pending", "In Progress", "Completed"), progressPercent, and loggedHours.
        /// </summary>
        [HttpPatch(ApiConstants.Tasks.UpdateStatus)]
        public async Task<IActionResult> UpdateTaskStatus(string taskCode, [FromBody] UpdateTaskStatusRequestDto dto)
        {
            var task = await _db.TaskItems
                .Include(t => t.Assignee)
                    .ThenInclude(a => a!.Department)
                .Include(t => t.Assignee)
                    .ThenInclude(a => a!.Designation)
                .Include(t => t.AssignedBy)
                    .ThenInclude(m => m!.Designation)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskCode == taskCode.ToUpper());

            if (task == null)
                return NotFound(new { message = $"Task '{taskCode}' not found." });

            task.Status = dto.Status;

            if (dto.ProgressPercent.HasValue)
            {
                task.ProgressPercent = Math.Clamp(dto.ProgressPercent.Value, 0, 100);
            }
            else
            {
                task.ProgressPercent = dto.Status switch
                {
                    "Completed" => 100,
                    "Pending" => 0,
                    "In Progress" when task.ProgressPercent == 0 || task.ProgressPercent == 100 => 45,
                    _ => task.ProgressPercent
                };
            }

            if (dto.LoggedHours.HasValue)
            {
                task.LoggedHours = dto.LoggedHours.Value;
            }
            else if (dto.Status == "Completed" && task.LoggedHours == 0)
            {
                task.LoggedHours = task.EstimatedHours;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Task '{task.TaskCode}' updated to '{task.Status}' ({task.ProgressPercent}%).",
                task = MapTaskToDto(task)
            });
        }

        /// <summary>
        /// Deletes a task by taskCode.
        /// </summary>
        [HttpDelete(ApiConstants.Tasks.Delete)]
        public async Task<IActionResult> DeleteTask(string taskCode)
        {
            var task = await _db.TaskItems
                .FirstOrDefaultAsync(t => t.TaskCode == taskCode.ToUpper());

            if (task == null)
                return NotFound(new { message = $"Task '{taskCode}' not found." });

            _db.TaskItems.Remove(task);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Task '{taskCode}' deleted." });
        }

        private static object MapTaskToDto(TaskItem t)
        {
            var assigneeName = t.Assignee?.FullName ?? "Unassigned";
            var initials = string.Join("", assigneeName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(s => char.ToUpperInvariant(s[0])));

            var subtasks = string.IsNullOrWhiteSpace(t.SubtasksJson)
                ? Array.Empty<string>()
                : t.SubtasksJson.Split(';', StringSplitOptions.RemoveEmptyEntries);

            return new
            {
                id = t.TaskCode,
                taskCode = t.TaskCode,
                title = t.Title,
                description = t.Description,
                projectId = t.Project?.Code ?? "PRJ-101",
                projectName = t.ProjectName,
                assignee = new
                {
                    id = t.Assignee?.EmployeeCode ?? "EMP-005",
                    name = assigneeName,
                    role = t.Assignee?.Designation?.Title ?? "Developer",
                    department = t.Assignee?.Department?.Name ?? "Engineering",
                    avatar = initials
                },
                assignedBy = t.AssignedBy != null ? new
                {
                    id = t.AssignedBy.EmployeeCode,
                    name = t.AssignedBy.FullName,
                    role = t.AssignedBy.Designation?.Title ?? "Manager"
                } : null,
                assignedDate = t.AssignedDate.ToString("MMM dd, yyyy"),
                dueDate = t.DueDate.ToString("MMM dd, yyyy"),
                status = t.Status,
                priority = t.Priority,
                estimatedHours = t.EstimatedHours,
                loggedHours = t.LoggedHours,
                progressPercent = t.ProgressPercent,
                category = t.Category,
                subtasks
            };
        }
    }
}

