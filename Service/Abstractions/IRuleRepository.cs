using System.Data;
using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

// The rule values (with their start dates).
public interface IRuleRepository
{
    Task<IReadOnlyList<RuleParameter>>  GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RuleParameter rule, CancellationToken ct = default);
}