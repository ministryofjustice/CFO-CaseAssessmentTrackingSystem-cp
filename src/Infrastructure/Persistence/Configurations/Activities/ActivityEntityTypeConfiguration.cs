﻿using Cfo.Cats.Domain.Common.Enums;
using Cfo.Cats.Domain.Entities.Activities;
using Cfo.Cats.Domain.Entities.Administration;
using Cfo.Cats.Infrastructure.Constants.Database;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfo.Cats.Infrastructure.Persistence.Configurations.Activities;

internal class ActivityEntityTypeConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.ToTable(
            DatabaseConstants.Tables.Activity,
            DatabaseConstants.Schemas.Activities);

        builder.HasOne(a => a.Participant)
            .WithMany()
            .HasForeignKey(a => a.ParticipantId);

        builder.Property(a => a.Definition)
            .IsRequired()
            .HasConversion(
                d => d!.Value,
                d => ActivityDefinition.FromValue(d)
            );

        builder.HasOne(a => a.TookPlaceAtLocation)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TookPlaceAtContract)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ParticipantCurrentLocation)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ParticipantCurrentContract)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.ParticipantStatus)
            .IsRequired()
            .HasConversion(
                ps => ps!.Value,
                ps => EnrolmentStatus.FromValue(ps)
            );

        builder.Property(a => a.AdditionalInformation)
            .HasMaxLength(1000);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion(
                s => s!.Value,
                s => ActivityStatus.FromValue(s)
            );

        builder.Property(a => a.Category)
            .IsRequired()
            .HasConversion(
                c => c!.Value,
                c => ActivityCategory.FromValue(c)
            );

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion(
                t => t!.Value,
                t => ActivityType.FromValue(t)
            );

        builder.Property(a => a.CreatedBy).IsRequired()
            .HasMaxLength(DatabaseConstants.FieldLengths.GuidId);

        builder.Property(a => a.LastModifiedBy).IsRequired()
            .HasMaxLength(DatabaseConstants.FieldLengths.GuidId);
    }
}
