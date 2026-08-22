using HospitalQueue.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueue.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<EmployeeService> EmployeeServices => Set<EmployeeService>();
    public DbSet<ServiceSession> ServiceSessions => Set<ServiceSession>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
    public DbSet<DailySequence> DailySequences => Set<DailySequence>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity tables keep their default names but we trim them to our schema needs.
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<Service>(b =>
        {
            b.Property(s => s.NameAr).HasMaxLength(150).IsRequired();
            b.Property(s => s.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(s => s.Code).IsUnique();
        });

        builder.Entity<EmployeeService>(b =>
        {
            b.HasKey(es => new { es.EmployeeId, es.ServiceId });
            b.HasOne(es => es.Employee)
                .WithMany()
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(es => es.Service)
                .WithMany()
                .HasForeignKey(es => es.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceSession>(b =>
        {
            b.HasOne(ss => ss.Employee).WithMany().HasForeignKey(ss => ss.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(ss => ss.Service).WithMany().HasForeignKey(ss => ss.ServiceId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(ss => new { ss.EmployeeId, ss.EndedAt });
        });

        builder.Entity<DailySequence>(b =>
        {
            b.HasKey(ds => new { ds.ServiceId, ds.QueueDate });
            b.HasOne(ds => ds.Service).WithMany().HasForeignKey(ds => ds.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Ticket>(b =>
        {
            b.Property(t => t.Number).HasMaxLength(20).IsRequired();
            b.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            b.HasIndex(t => new { t.ServiceId, t.QueueDate, t.SequenceNumber }).IsUnique();
            b.HasIndex(t => new { t.Status, t.ServiceId });

            b.HasOne(t => t.Service).WithMany(s => s.Tickets).HasForeignKey(t => t.ServiceId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(t => t.Employee).WithMany().HasForeignKey(t => t.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(t => t.TransferredFromTicket).WithMany().HasForeignKey(t => t.TransferredFromTicketId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketStatusHistory>(b =>
        {
            b.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
            b.HasOne(h => h.Ticket).WithMany(t => t.History).HasForeignKey(h => h.TicketId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(h => h.TicketId);
        });

        builder.Entity<OrganizationSettings>(b =>
        {
            b.Property(s => s.OrgNameAr).HasMaxLength(200).IsRequired();
            b.Property(s => s.TickerText).HasMaxLength(500);
        });

        builder.Entity<ReceiptTemplate>(b =>
        {
            b.Property(t => t.Name).HasMaxLength(150).IsRequired();
            b.Property(t => t.ElementsJson).HasColumnType("text").IsRequired();
        });
    }
}
