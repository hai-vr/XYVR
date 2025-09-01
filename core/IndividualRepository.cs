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
        RebuildAccountDictionary();
    }

    private void RebuildAccountDictionary()
    {
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

        var duplicateRecords = individuals.SelectMany(individual => individual.accounts)
            .Select(account => new DiscriminatorRecord(account.qualifiedAppName, account.inAppIdentifier))
            .GroupBy(record => record)
            .Select(records => records.ToList())
            .Where(list => list.Count > 1)
            .Select(list => list.First())
            .ToList();

        if (duplicateRecords.Count > 0)
        {
            Console.WriteLine($"Made a mistake, we have duplicate records:");
            foreach (var discriminatorRecord in duplicateRecords)
            {
                Console.WriteLine($"{discriminatorRecord.AccountQualifiedAppName} {discriminatorRecord.AccountInAppIdentifier}");
            }
            
            // Detect problematic duplicates
            foreach (var discriminatorRecord in duplicateRecords)
            {
                var problematics = individuals
                    .Where(individual => individual.accounts.Any(account => account.qualifiedAppName == discriminatorRecord.AccountQualifiedAppName && account.inAppIdentifier == discriminatorRecord.AccountInAppIdentifier))
                    .ToList();
                Console.WriteLine($"Problematic ({problematics.Count}): {string.Join(",", problematics.Select(individual => individual.accounts.Count).ToList())}");
                if (problematics.Any(individual => individual.accounts.Count > 1))
                {
                    var max = problematics.Max(individual => individual.accounts.Count);
                    var maximumInd = problematics.First(individual => individual.accounts.Count == max);
                    problematics.Remove(maximumInd);
                    
                    // foreach (var problematic in problematics)
                    // {
                    //     individuals.Remove(problematic);
                    // }
                }
            }
        }
    }

    internal record DiscriminatorRecord(string AccountQualifiedAppName, string AccountInAppIdentifier);

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
        var results = new Dictionary<string, Individual>();
        
        foreach (var individual in Individuals)
        {
            foreach (var account in individual.accounts)
            {
                if (account.namedApp == namedApp)
                {
                    results[account.inAppIdentifier] = individual;
                }
            }
        }
        
        return results;
    }

    public void MergeAccounts(List<Account> accounts)
    {
        foreach (var inputAccount in accounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividual))
            {
                foreach (var existingAccount in existingIndividual.accounts)
                {
                    if (SynchronizeAccount(existingAccount, inputAccount)) break;
                }

                UpdateIndividualBasedOnAccounts(existingIndividual);
            }
            else
            {
                Console.WriteLine($"Creating new individual: {inputAccount.namedApp} {inputAccount.inAppIdentifier} {inputAccount.inAppDisplayName}");
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
            
            // Specifics are always replaced entirely
            existingAccount.specifics = inputAccount.specifics;
            
            foreach (var inputCaller in inputAccount.callers)
            {
                var callerExists = false;
                foreach (var existingCaller in existingAccount.callers)
                {
                    if (inputCaller.isAnonymous && existingCaller.isAnonymous
                         || !inputCaller.isAnonymous && !existingCaller.isAnonymous && existingCaller.inAppIdentifier == inputCaller.inAppIdentifier)
                    {
                        callerExists = true;
                        existingCaller.isContact = inputCaller.isContact;
                        UpdateExistingNote(existingCaller.note, inputCaller.note);
                        
                        break;
                    }
                }
                if (!callerExists)
                {
                    existingAccount.callers.Add(inputCaller);
                }
            }
            
            return true;
        }

        return false;
    }

    private static void UpdateExistingNote(Note existingNote, Note inputNote)
    {
        switch (inputNote.status)
        {
            case NoteState.Exists:
            {
                existingNote.status = NoteState.Exists;
                existingNote.text = inputNote.text;
                break;
            }
            case NoteState.NeverHad:
            {
                if (existingNote.status == NoteState.Exists)
                {
                    existingNote.status = NoteState.WasRemoved;
                    // We don't overwrite the text
                }
                break;
            }
            case NoteState.WasRemoved:
            {
                if (existingNote.status is NoteState.Exists or NoteState.NeverHad)
                {
                    existingNote.status = NoteState.WasRemoved;
                    // We don't overwrite the text
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void UpdateIndividualBasedOnAccounts(Individual existingIndividual)
    {
        existingIndividual.isAnyContact = existingIndividual.accounts.Any(account => account.IsAnyCallerContact());
        existingIndividual.isExposed = existingIndividual.isAnyContact
                                       || existingIndividual.note.status == NoteState.Exists
                                       || existingIndividual.accounts.Any(account => account.HasAnyCallerNote());
        
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
        var isAnyContact = account.IsAnyCallerContact();
        var individual = new Individual
        {
            guid = Guid.NewGuid().ToString(),
            accounts = [account],
            displayName = account.inAppDisplayName,
            isAnyContact = isAnyContact,
            isExposed = isAnyContact || account.HasAnyCallerNote()
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
        toAugment.isAnyContact = toAugment.accounts.Any(account => account.IsAnyCallerContact());
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
                              || toAugment.accounts.Any(account => account.HasAnyCallerNote());
        
        foreach (var accountFromDestroyed in toDestroy.accounts)
        {
            _namedAppToInAppIdToIndividual[accountFromDestroyed.namedApp][accountFromDestroyed.inAppIdentifier] = toAugment;
        }
        
        Individuals.RemoveAt(indexToDestroy);
    }
}