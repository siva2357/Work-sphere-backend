using Microsoft.EntityFrameworkCore;
using WorkSphere.API.Models;

namespace WorkSphere.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        // 0-5: Companies, Organization & Hierarchy
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<OrganizationalLevel> OrganizationalLevels => Set<OrganizationalLevel>();
        public DbSet<Designation> Designations => Set<Designation>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<ReportingRelationship> ReportingRelationships => Set<ReportingRelationship>();

        // 6-10: Roles & Centralized Permissions
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
        public DbSet<EmployeePermissionOverride> EmployeePermissionOverrides => Set<EmployeePermissionOverride>();

        // 11-14: Teams & Projects
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

        // 15-17: Configurable Approval Workflows
        public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
        public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

        // 18-21: 4-Phase Recruitment & Onboarding Funnel
        public DbSet<JobOpening> JobOpenings => Set<JobOpening>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<CandidatePhaseEvaluation> CandidatePhaseEvaluations => Set<CandidatePhaseEvaluation>();
        public DbSet<CandidateOffer> CandidateOffers => Set<CandidateOffer>();

        // 22-23: Unified Request Management & Multi-Stage Approval Chains (Leave, Resignation, Hike, Promotion)
        public DbSet<EmployeeRequest> EmployeeRequests => Set<EmployeeRequest>();
        public DbSet<RequestApprovalChainStep> RequestApprovalChainSteps => Set<RequestApprovalChainStep>();

        // 24-29: Employee & Manager Workspace Modules (Attendance, Leaves, Tasks, Documents, Monthly Payroll)
        public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
        public DbSet<LeaveBalanceRecord> LeaveBalances => Set<LeaveBalanceRecord>();
        public DbSet<LeaveProposal> LeaveProposals => Set<LeaveProposal>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<EmployeeDocumentRecord> EmployeeDocuments => Set<EmployeeDocumentRecord>();
        public DbSet<MonthlyPayrollSlip> MonthlyPayrollSlips => Set<MonthlyPayrollSlip>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasIndex(c => c.CompanyCode).IsUnique();
                entity.HasIndex(c => c.Domain).IsUnique();
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.Code).IsUnique();
                entity.HasOne(d => d.ParentDepartment)
                    .WithMany(d => d.SubDepartments)
                    .HasForeignKey(d => d.ParentDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrganizationalLevel>(entity =>
            {
                entity.HasIndex(l => l.Code).IsUnique();
            });

            modelBuilder.Entity<Designation>(entity =>
            {
                entity.HasIndex(d => d.Code).IsUnique();
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmployeeCode).IsUnique();
            });

            modelBuilder.Entity<ReportingRelationship>(entity =>
            {
                entity.HasOne(r => r.Employee)
                    .WithMany()
                    .HasForeignKey(r => r.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Manager)
                    .WithMany()
                    .HasForeignKey(r => r.ManagerEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Code).IsUnique();
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique();
                entity.HasIndex(p => new { p.Module, p.Action }).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            });

            modelBuilder.Entity<EmployeeRole>(entity =>
            {
                entity.HasIndex(er => new { er.EmployeeId, er.RoleId }).IsUnique();
            });

            modelBuilder.Entity<EmployeePermissionOverride>(entity =>
            {
                entity.HasIndex(o => new { o.EmployeeId, o.PermissionId }).IsUnique();
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique();
            });

            modelBuilder.Entity<JobOpening>(entity =>
            {
                entity.HasIndex(j => j.JobCode).IsUnique();
            });

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.HasIndex(c => c.CandidateCode).IsUnique();
            });

            modelBuilder.Entity<CandidatePhaseEvaluation>(entity =>
            {
                entity.HasIndex(e => new { e.CandidateId, e.PhaseNumber }).IsUnique();
            });

            modelBuilder.Entity<EmployeeRequest>(entity =>
            {
                entity.HasIndex(r => r.RequestCode).IsUnique();
            });

            modelBuilder.Entity<RequestApprovalChainStep>(entity =>
            {
                entity.HasIndex(s => new { s.EmployeeRequestId, s.LevelNumber }).IsUnique();
            });

            modelBuilder.Entity<AttendanceLog>(entity =>
            {
                entity.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            });

            modelBuilder.Entity<LeaveBalanceRecord>(entity =>
            {
                entity.HasIndex(lb => new { lb.EmployeeId, lb.ShortCode }).IsUnique();
            });

            modelBuilder.Entity<LeaveProposal>(entity =>
            {
                entity.HasIndex(lp => lp.LeaveCode).IsUnique();
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasIndex(t => t.TaskCode).IsUnique();
                entity.HasOne(t => t.Assignee)
                    .WithMany()
                    .HasForeignKey(t => t.AssigneeEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedBy)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedByEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeDocumentRecord>(entity =>
            {
                entity.HasIndex(d => d.DocumentCode).IsUnique();
            });

            modelBuilder.Entity<MonthlyPayrollSlip>(entity =>
            {
                entity.HasIndex(p => new { p.EmployeeId, p.Month }).IsUnique();
            });
        }
    }
}
