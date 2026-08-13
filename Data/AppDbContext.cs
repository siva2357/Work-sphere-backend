using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using WorkSphere.API.Models;

namespace WorkSphere.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<PayrollRecord> PayrollRecords { get; set; }
        public DbSet<Letter> Letters { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<FinancialEvent> FinancialEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToCollection("Users");
            modelBuilder.Entity<LeaveRequest>().ToCollection("LeaveRequests");
            modelBuilder.Entity<AttendanceRecord>().ToCollection("AttendanceRecords");
            modelBuilder.Entity<PayrollRecord>().ToCollection("PayrollRecords");
            modelBuilder.Entity<Letter>().ToCollection("Letters");
            modelBuilder.Entity<Announcement>().ToCollection("Announcements");
            modelBuilder.Entity<Ticket>().ToCollection("Tickets");
            modelBuilder.Entity<FinancialEvent>().ToCollection("FinancialEvents");
        }
    }
}
