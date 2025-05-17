namespace AccountingModule.Domain;

public class TheAccountingModule
{
    public int DoAccounting()
    {
#pragma warning disable CA5394 // Do not use insecure randomness
        int amount = Random.Shared.Next();
#pragma warning restore CA5394 // Do not use insecure randomness

        // DEMO: 4b analyzers : limit number of return statements in a method
        if (amount < 0)
        {
            return 0;
        }

        if (amount == 0)
        {
            return 0;
        }

        if (amount < 500)
        {
            return 0;
        }

        if (amount == 1000)
        {
            return 0;
        }

        if (amount > 2000)
        {
            return 0;
        }

        if (amount % 2 == 0)
        {
            amount = 0;
        }

        return amount;
    }
}
