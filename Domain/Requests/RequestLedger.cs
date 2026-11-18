using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

// The ledger entries a decision adds. Old entries are never changed, a cancellation adds new entries instead.
public static class RequestLedger
{
    // approved holiday: one entry per working day
    public static IReadOnlyList<LedgerEntry> Usage(HolidayRequest request, Guid accountId,
        IEnumerable<HolidayDay> days) =>
        days.Select(d => new LedgerEntry
            {
                AccountId = accountId,
                Type = LedgerEntryType.Usage,
                Hours = -d.Hours,
                Date = d.Date,
                RequestId = request.Id,
                Reason = "Holiday"
            })
            .ToList();

    // approved cancellation: gives back exactly what the request used
    public static IReadOnlyList<LedgerEntry> Cancellation(HolidayRequest request, IEnumerable<LedgerEntry> ledger) =>
        ledger.Where(e => e.RequestId == request.Id && e.Type == LedgerEntryType.Usage)
            .Select(e => new LedgerEntry
            {
                AccountId = e.AccountId,
                Type = LedgerEntryType.Cancellation,
                Hours = -e.Hours,
                Date = e.Date,
                RequestId = request.Id,
                Reason = "Holiday cancelled"
            })
            .ToList();

    // approved transfer: the hours leave the period they were earned in and get the next period's expiry date
    public static IReadOnlyList<LedgerEntry> Transfer(TransferRequest transfer, Guid accountId, DateOnly today)
    {
        var from = HolidayPeriods.ExpiresOn(transfer.AccountType, transfer.FromPeriodStart);
        var to = from.AddYears(1);

        return
        [
            TransferEntry(transfer, accountId, today, -transfer.Hours, from,
                $"Transferred to the period ending {to:yyyy-MM-dd}"),
            TransferEntry(transfer, accountId, today, transfer.Hours, to,
                $"Transferred from the period ending {from:yyyy-MM-dd}")
        ];
    }

    private static LedgerEntry TransferEntry(TransferRequest transfer, Guid accountId, DateOnly today, decimal hours,
        DateOnly expiresOn, string reason) => new()
    {
        AccountId = accountId,
        Type = LedgerEntryType.Transfer,
        Hours = hours,
        Date = today,
        ExpiresOn = expiresOn,
        TransferRequestId = transfer.Id,
        Reason = reason
    };
}
