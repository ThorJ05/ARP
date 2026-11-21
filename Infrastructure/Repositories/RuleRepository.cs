using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Rule values, read with linq2db.
public class RuleRepository(AppDbContext context) : IRuleRepository
{
    public async Task<IReadOnlyList<RuleParameter>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<RuleParameter>().ToListAsync(ct);
    }

    
    // New values are always added as a new row, old rows are never changed (so the history is kept).
    public async Task AddAsync(RuleParameter rule, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.InsertAsync(rule, token: ct);
    }
    
}