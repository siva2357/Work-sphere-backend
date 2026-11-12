using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkSphere.API.Models
{
    [Table("users")]
    public class User
    {
        
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Main role: "hr" or "employee"
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Sub-level within the role (e.g., "HR Admin", "HR Executive", "Team Lead", "Software Engineer")
        /// </summary>
        [MaxLength(80)]
        [Column("level")]
        public string? Level { get; set; }

        [MaxLength(80)]
        [Column("department")]
        public string? Department { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

