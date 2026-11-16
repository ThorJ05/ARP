namespace BusinessEsbjerg.Holiday.Domain.Enums;

public enum Role
{
    Employee,
    Accounting,
    Admin
}

public enum PayType
{
    Salaried, // fixed vacation with salary
    Hourly    // hourly salary, holiday allowance
}

public enum AccountType
{
    Weeks1To5, // Danish Holiday Act (Ferieloven), concurrent holiday
    Week6      // 6th holiday week (KL agreement)
}

public enum LedgerEntryType
{
    Accrual,    // accrual (+): hours earned
    Usage,      // holiday taken (-): hours already used
    Transfer,   // transaction for next period
    Payout,     // payout - 
    Correction  // manual correction after chargeback
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}