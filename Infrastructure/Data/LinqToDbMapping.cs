using BusinessEsbjerg.Holiday.Domain.Entities;
using LinqToDB.Mapping;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

// Extra Linq2db mapping on top of the EF Core model.
// Linq2db reads tables and columns from the EF Core model, but it also treats every other
// public value property as a column. Calculated properties must therefore be excluded here,
// in addition to the Ignore() calls in EntityConfigurations.
public static class LinqToDbMapping
{
    public static readonly MappingSchema Schema = Build();

    private static MappingSchema Build()
    {
        var builder = new FluentMappingBuilder();

        builder.Entity<WorkScheduleEntry>().Ignore(x => x.WeeklyHours);
        builder.Entity<WorkScheduleEntry>().Ignore(x => x.WorkingDays);
        builder.Entity<WorkScheduleEntry>().Ignore(x => x.AverageDayHours);

        builder.Build();
        return builder.MappingSchema;
    }
}
