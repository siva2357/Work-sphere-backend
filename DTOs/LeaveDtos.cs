namespace WorkSphere.API.DTOs
{
    public class CreateLeaveRequestDto
    {
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateLeaveStatusDto
    {
        public string Status { get; set; } = string.Empty; // Approved or Rejected
    }
}
