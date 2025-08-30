namespace XYVR.Core;

public class IndividualRepository
{
    public List<Individual> Individuals { get; }
    private readonly Dictionary<string, Individual> _resoniteIdToIndividual;

    public IndividualRepository(Individual[] individuals)
    {
        Individuals = individuals.ToList();
        
        _resoniteIdToIndividual = Individuals
            .ToDictionary(individual => individual.accounts.First(account => account.namedApp == NamedApp.Resonite).inAppIdentifier);
    }

    public void MergeAccounts(List<Account> accounts)
    {
        foreach (var inputAccount in accounts)
        {
            if (inputAccount.namedApp == NamedApp.Resonite && _resoniteIdToIndividual.TryGetValue(inputAccount.inAppIdentifier, out var existingIndividual))
            {
                foreach (var existingAccount in existingIndividual.accounts)
                {
                    if (IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier)
                    {
                        existingAccount.inAppDisplayName = inputAccount.inAppDisplayName;
                        existingAccount.isContact = inputAccount.isContact;
                        
                        // Order matters
                        var newPreviousLiveServerData = existingAccount.liveServerData;
                        existingAccount.previousLiveServerData = newPreviousLiveServerData;
                        existingAccount.liveServerData = inputAccount.liveServerData;
                        
                        break;
                    }
                }

                UpdateIndividualBasedOnAccounts(inputAccount, existingIndividual);
            }
            else
            {
                var newIndividual = CreateNewIndividualFromAccount(inputAccount);
                if (inputAccount.namedApp == NamedApp.Resonite)
                {
                    _resoniteIdToIndividual.Add(inputAccount.inAppIdentifier, newIndividual);
                }
            }
        }
    }

    private static void UpdateIndividualBasedOnAccounts(Account inputAccount, Individual existingIndividual)
    {
        if (inputAccount.isContact != existingIndividual.isAnyContact)
        {
            existingIndividual.isAnyContact = existingIndividual.accounts.Any(account => account.isContact);
        }
        
        if (existingIndividual.accounts.Count == 1 && existingIndividual.accounts[0].inAppDisplayName != inputAccount.inAppDisplayName
            || existingIndividual.accounts.All(it => it.inAppDisplayName != existingIndividual.displayName))
        {
            existingIndividual.displayName = MajorMainAccountOf(existingIndividual).inAppDisplayName;
        }
    }

    // The "major main account" is the main account of the Individual's major platform.
    private static Account MajorMainAccountOf(Individual existingIndividual)
    {
        return existingIndividual.accounts.First();
    }

    private static bool IsSameApp(Account existingAccount, Account inputAccount)
    {
        var sameNamedApp = existingAccount.namedApp == inputAccount.namedApp;
        if (!sameNamedApp)
        {
            return false;
        }
        if (existingAccount.namedApp == NamedApp.NotNamed)
        {
            return existingAccount.qualifiedAppName == inputAccount.qualifiedAppName;
        }
        return true;
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private Individual CreateNewIndividualFromAccount(Account account)
    {
        var individual = new Individual
        {
            guid = Guid.NewGuid().ToString(),
            accounts = [account],
            displayName = account.inAppDisplayName,
            isAnyContact = account.isContact
        };
        Individuals.Add(individual);
        return individual;
    }
}