namespace XYVR.Core;

public class IndividualRepository
{
    public List<Individual> Individuals { get; }
    
    private Dictionary<string, Individual> _resoniteIdToIndividual;
    private readonly Dictionary<string, Individual> _vrchatIdToIndividual;

    public IndividualRepository(Individual[] individuals)
    {
        Individuals = individuals.ToList();
        
        _resoniteIdToIndividual = CreateAccountDictionary(NamedApp.Resonite);
        _vrchatIdToIndividual = CreateAccountDictionary(NamedApp.VRChat);
    }

    public HashSet<string> CollectAllInAppIdentifiers(NamedApp namedApp)
    {
        return Individuals
            .SelectMany(individual => individual.accounts)
            .Where(account => account.namedApp == namedApp)
            .Select(account => account.inAppIdentifier)
            .ToHashSet();
    }

    private Dictionary<string, Individual> CreateAccountDictionary(NamedApp namedApp)
    {
        var withResoniteAccount = Individuals.Where(individual => individual.accounts.Any(account => account.namedApp == namedApp)).ToList();
        return withResoniteAccount
            .ToDictionary(individual => individual.accounts.First(account => account.namedApp == namedApp).inAppIdentifier);
    }

    public void MergeAccounts(List<Account> accounts)
    {
        foreach (var inputAccount in accounts)
        {
            if (inputAccount.namedApp == NamedApp.Resonite && _resoniteIdToIndividual.TryGetValue(inputAccount.inAppIdentifier, out var existingResoniteIndividual))
            {
                foreach (var existingAccount in existingResoniteIndividual.accounts)
                {
                    if (SynchronizeAccount(existingAccount, inputAccount)) break;
                }

                UpdateIndividualBasedOnAccounts(inputAccount, existingResoniteIndividual);
            }
            else if (inputAccount.namedApp == NamedApp.VRChat && _vrchatIdToIndividual.TryGetValue(inputAccount.inAppIdentifier, out var existingVRChatIndividual))
            {
                foreach (var existingAccount in existingVRChatIndividual.accounts)
                {
                    if (SynchronizeAccount(existingAccount, inputAccount)) break;
                }

                UpdateIndividualBasedOnAccounts(inputAccount, existingVRChatIndividual);
            }
            else
            {
                var newIndividual = CreateNewIndividualFromAccount(inputAccount);
                if (inputAccount.namedApp == NamedApp.Resonite)
                {
                    _resoniteIdToIndividual.Add(inputAccount.inAppIdentifier, newIndividual);
                }
                else if (inputAccount.namedApp == NamedApp.VRChat)
                {
                    _vrchatIdToIndividual.Add(inputAccount.inAppIdentifier, newIndividual);
                }
            }
        }
    }

    private static bool SynchronizeAccount(Account existingAccount, Account inputAccount)
    {
        if (IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier)
        {
            existingAccount.inAppDisplayName = inputAccount.inAppDisplayName;
            existingAccount.isContact = inputAccount.isContact;
                        
            // Order matters
            var newPreviousLiveServerData = existingAccount.liveServerData;
            existingAccount.previousLiveServerData = newPreviousLiveServerData;
            existingAccount.liveServerData = inputAccount.liveServerData;
            return true;
        }

        return false;
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

    public void FusionIndividuals(Individual toAugment, Individual toDestroy)
    {
        if (toAugment == toDestroy || toAugment.guid == toDestroy.guid) throw new ArgumentException("Cannot fusion an Individual with itself");
        
        var indexToDestroy = Individuals.IndexOf(toDestroy);
        if (indexToDestroy == -1) throw new ArgumentException("Individual not found in this repository");

        toAugment.accounts.AddRange(toDestroy.accounts);
        toAugment.isAnyContact = toAugment.accounts.Any(account => account.isContact);
        
        foreach (var accountFromDestroyed in toDestroy.accounts)
        {
            if (accountFromDestroyed.namedApp == NamedApp.Resonite)
            {
                _resoniteIdToIndividual[accountFromDestroyed.inAppIdentifier] = toAugment;
            }
            else if (accountFromDestroyed.namedApp == NamedApp.VRChat)
            {
                _vrchatIdToIndividual[accountFromDestroyed.inAppIdentifier] = toAugment;
            }
        }
        
        Individuals.RemoveAt(indexToDestroy);
    }
}