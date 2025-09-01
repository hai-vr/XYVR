namespace XYVR.Core;

public class IndividualRepository
{
    public List<Individual> Individuals { get; }
    
    private Dictionary<NamedApp, Dictionary<string, Individual>> _namedAppToInAppIdToIndividual;

    public IndividualRepository(Individual[] individuals)
    {
        Individuals = individuals.ToList();
        EvaluateDataMigrations(Individuals);

        _namedAppToInAppIdToIndividual = new Dictionary<NamedApp, Dictionary<string, Individual>>();
        foreach (var namedApp in Enum.GetValues<NamedApp>())
        {
            _namedAppToInAppIdToIndividual[namedApp] = CreateAccountDictionary(namedApp);
        }
    }

    // This can change the data of an individual when we have data migrations
    // (i.e. when isExposed was added, none of the individuals had that info)
    private void EvaluateDataMigrations(List<Individual> individuals)
    {
        foreach (var individual in individuals)
        {
            UpdateIndividualBasedOnAccounts(individual);
        }
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
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingResoniteIndividual))
            {
                foreach (var existingAccount in existingResoniteIndividual.accounts)
                {
                    if (SynchronizeAccount(existingAccount, inputAccount)) break;
                }

                UpdateIndividualBasedOnAccounts(existingResoniteIndividual);
            }
            else
            {
                var newIndividual = CreateNewIndividualFromAccount(inputAccount);
                _namedAppToInAppIdToIndividual[inputAccount.namedApp].Add(inputAccount.inAppIdentifier, newIndividual);
            }
        }
    }

    private static bool SynchronizeAccount(Account existingAccount, Account inputAccount)
    {
        var isSameAppAndIdentifier = IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier;
        if (isSameAppAndIdentifier)
        {
            existingAccount.inAppDisplayName = inputAccount.inAppDisplayName;
            existingAccount.isContact = inputAccount.isContact;
            
            if (existingAccount.note.status != inputAccount.note.status)
            {
                if (existingAccount.note.status == NoteState.NeverHad)
                {
                    existingAccount.note.status = inputAccount.note.status;
                    existingAccount.note.text = inputAccount.note.text;
                }
                else
                {
                    if (inputAccount.note.status is NoteState.Exists)
                    {
                        existingAccount.note.status = NoteState.Exists;
                        existingAccount.note.text = inputAccount.note.text;
                    }
                    else
                    {
                        existingAccount.note.status = inputAccount.note.status;
                        // We don't overwrite the text
                    }
                }
            }
            else if (existingAccount.note.status == NoteState.Exists && existingAccount.note.text != inputAccount.note.text)
            {
                existingAccount.note.text = inputAccount.note.text;
            }
            
            return true;
        }

        return false;
    }

    private static void UpdateIndividualBasedOnAccounts(Individual existingIndividual)
    {
        existingIndividual.isAnyContact = existingIndividual.accounts.Any(account => account.isContact);
        existingIndividual.isExposed = existingIndividual.isAnyContact
                                       || existingIndividual.note.status == NoteState.Exists
                                       || existingIndividual.accounts.Any(account => account.note.status == NoteState.Exists);
        
        if (existingIndividual.accounts.All(it => it.inAppDisplayName != existingIndividual.displayName))
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
            isAnyContact = account.isContact,
            isExposed = account.isContact || account.note.status == NoteState.Exists
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
        if (toAugment.note.status == NoteState.NeverHad)
        {
            if (toDestroy.note.status is NoteState.Exists or NoteState.WasRemoved)
            {
                toAugment.note.status = toDestroy.note.status;
                toAugment.note.text = toDestroy.note.text;
            }
        }
        // Order matters
        toAugment.isExposed = toAugment.isAnyContact
                              || toAugment.note.status == NoteState.Exists
                              || toAugment.accounts.Any(account => account.note.status == NoteState.Exists);
        
        foreach (var accountFromDestroyed in toDestroy.accounts)
        {
            _namedAppToInAppIdToIndividual[accountFromDestroyed.namedApp][accountFromDestroyed.inAppIdentifier] = toAugment;
        }
        
        Individuals.RemoveAt(indexToDestroy);
    }
}