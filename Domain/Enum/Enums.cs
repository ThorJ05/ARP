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
    Weeks1To5, // Ferieloven, samtidighedsferie
    Week6      // 6. ferieuge (KL-aftale)
}

public enum LedgerEntryType
{
    Accrual,    // optjening (+) amount you earned +
    Usage,      // afholdt ferie (-) vacation you already used -
    Transfer,   // transaction for next period
    Payout,     // payout - 
    Correction  // manuel correction after chargeback
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}