using BusinessEsbjerg.Holiday.Service.Schedules;
using FluentValidation;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

/// <summary>Monday-Friday start and end times as sent over the API. A day without times is a day off.</summary>
public record WorkWeekDto(
    TimeOnly? MondayStart, TimeOnly? MondayEnd,
    TimeOnly? TuesdayStart, TimeOnly? TuesdayEnd,
    TimeOnly? WednesdayStart, TimeOnly? WednesdayEnd,
    TimeOnly? ThursdayStart, TimeOnly? ThursdayEnd,
    TimeOnly? FridayStart, TimeOnly? FridayEnd)
{
    public WorkWeek ToWorkWeek() => new(
        MondayStart, MondayEnd, TuesdayStart, TuesdayEnd, WednesdayStart, WednesdayEnd,
        ThursdayStart, ThursdayEnd, FridayStart, FridayEnd);

    public static WorkWeekDto From(WorkWeek w) => new(
        w.MondayStart, w.MondayEnd, w.TuesdayStart, w.TuesdayEnd, w.WednesdayStart, w.WednesdayEnd,
        w.ThursdayStart, w.ThursdayEnd, w.FridayStart, w.FridayEnd);
}

public class WorkWeekValidator : AbstractValidator<WorkWeekDto>
{
    public WorkWeekValidator()
    {
        Day("Monday", w => w.MondayStart, w => w.MondayEnd);
        Day("Tuesday", w => w.TuesdayStart, w => w.TuesdayEnd);
        Day("Wednesday", w => w.WednesdayStart, w => w.WednesdayEnd);
        Day("Thursday", w => w.ThursdayStart, w => w.ThursdayEnd);
        Day("Friday", w => w.FridayStart, w => w.FridayEnd);

        RuleFor(w => w)
            .Must(w => w.MondayStart is not null || w.TuesdayStart is not null || w.WednesdayStart is not null
                       || w.ThursdayStart is not null || w.FridayStart is not null)
            .WithName("Schedule")
            .WithMessage("The schedule needs at least one working day.");
    }

    // a day needs both start and end (or none), and end must be after start
    private void Day(string name, Func<WorkWeekDto, TimeOnly?> start, Func<WorkWeekDto, TimeOnly?> end) =>
        RuleFor(w => w)
            .Must(w => start(w) is null ? end(w) is null : end(w) > start(w))
            .WithName(name)
            .WithMessage($"{name}: give both a start and an end time (or none), and the end must be after the start.");
}
