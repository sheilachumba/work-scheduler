using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<FormQuestion> FormQuestions => Set<FormQuestion>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormAnswer> FormAnswers => Set<FormAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FormQuestion>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Label).HasMaxLength(500).IsRequired();
            entity.Property(q => q.HelpText).HasMaxLength(1000);
            entity.Property(q => q.FieldType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(q => q.DisplayOrder);
            entity.HasIndex(q => q.IsActive);
        });

        builder.Entity<FormSubmission>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.AdminNotes).HasMaxLength(4000);
            entity.HasIndex(s => s.SubmittedAt);
            entity.HasIndex(s => s.Status);
        });

        builder.Entity<FormAnswer>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AnswerValue).IsRequired();
            entity.HasOne(a => a.FormSubmission)
                .WithMany(s => s.Answers)
                .HasForeignKey(a => a.FormSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.FormQuestion)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.FormQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => new { a.FormSubmissionId, a.FormQuestionId }).IsUnique();
        });
    }
}
