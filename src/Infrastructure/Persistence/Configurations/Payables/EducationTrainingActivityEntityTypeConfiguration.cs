﻿using Cfo.Cats.Domain.Common.Enums;
using Cfo.Cats.Domain.Entities.Payables;
using Cfo.Cats.Infrastructure.Constants.Database;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfo.Cats.Infrastructure.Persistence.Configurations.Payables;

public class EducationTrainingActivityEntityTypeConfiguration : IEntityTypeConfiguration<EducationTrainingActivity>
{
    public void Configure(EntityTypeBuilder<EducationTrainingActivity> builder)
    {
        builder.ToTable(
            DatabaseConstants.Tables.EducationTrainingActivities,
            DatabaseConstants.Schemas.Participant);

        builder.HasOne(a => a.Document)
            .WithMany()
            .IsRequired(true);

        builder.Property(a => a.CourseCompletionStatus)
            .IsRequired()
            .HasConversion(
                c => c!.Value,
                c => CourseCompletionStatus.FromValue(c)
            );
    }
}
