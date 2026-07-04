namespace EHBYS.Services;

public static class InterestService
{
    public static decimal CalculateCompoundDebt(decimal principal, DateTime dueDate, DateTime asOf, decimal monthlyRate)
    {
        if (principal <= 0 || asOf <= dueDate)
        {
            return principal;
        }

        var months = ((asOf.Year - dueDate.Year) * 12) + asOf.Month - dueDate.Month;
        if (asOf.Day > dueDate.Day)
        {
            months++;
        }

        months = Math.Max(1, months);
        var total = principal * (decimal)Math.Pow((double)(1 + monthlyRate), months);
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateInterest(decimal principal, DateTime dueDate, DateTime asOf, decimal monthlyRate)
    {
        return CalculateCompoundDebt(principal, dueDate, asOf, monthlyRate) - principal;
    }
}
