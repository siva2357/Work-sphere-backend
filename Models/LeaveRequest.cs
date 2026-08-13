using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorkSphere.API.Models
{
    public class LeaveRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty; // Sick, Vacation, Personal
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string Reason { get; set; } = string.Empty;
    }
}
