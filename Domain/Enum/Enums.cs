namespace BusinessEsbjerg.Holiday.Domain.Enums;

public enum Role
{
    Employee,    // normal employee login
    Accountant,  // approves and rejects requests
                 // (only one)
    SystemOwner  // creates users and sets global rules (only one)
}

public enum AccountType
{
    Weeks1To5, // Danish Holiday Act (Ferieloven), concurrent holiday
    Week6      // 6th holiday week (KL agreement)
}

public enum LedgerEntryType
{
    Accrual,            // (+) hours earned
    Usage,              // (-) holiday taken
    Transfer,           // hours moved to the next period
    Payout,             // (-) hours paid out by payroll (we don't store the amount)
    Correction,         // manual fix
    ScheduleConversion, // balance changed because the work schedule changed
    Cancellation        // (+) approved holiday that was cancelled, gives the hours back
}


public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    CancellationRequested, // employee wants to cancel approved holiday
    Cancelled
}
