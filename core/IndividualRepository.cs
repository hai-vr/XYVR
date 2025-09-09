namespace XYVR.Core;

public class IndividualRepository
{
    public List<Individual> Individuals { get; }
    
    private readonly Dictionary<NamedApp, Dictionary<string, Individual>> _namedAppToInAppIdToIndividual = new();

    public IndividualRepository(Individual[] individuals)
    {
        Individuals = individuals.ToList();
        EvaluateDataMigrations(Individuals);

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
        
        foreach (var individual in individuals)
        {
            if (individual.note.status == 0)
            {
                individual.note.status = NoteState.NeverHad;
            }

            foreach (var account in individual.accounts)
            {
                if (string.IsNullOrWhiteSpace(account.guid))
                {
                    account.guid = XYVRGuids.ForAccount();
                }

                if (account.allDisplayNames == null || account.allDisplayNames.Count == 0)
                {
                    account.allDisplayNames = [individual.displayName];
                }
            }
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

    public void MergeIncompleteAccounts(List<IncompleteAccount> incompleteAccounts)
    {
        foreach (var inputAccount in incompleteAccounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividual))
            {
                foreach (var existingAccount in existingIndividual.accounts)
                {
                    if (SynchronizeIncompleteAccount(existingAccount, inputAccount)) break;
                }

                UpdateIndividualBasedOnAccounts(existingIndividual);
            }
            else
            {
                Console.WriteLine($"Creating new individual from incomplete account: {inputAccount.namedApp} {inputAccount.inAppIdentifier} {inputAccount.inAppDisplayName}");
                var newIndividual = CreateNewIndividualFromIncompleteAccount(inputAccount);
                _namedAppToInAppIdToIndividual[inputAccount.namedApp].Add(inputAccount.inAppIdentifier, newIndividual);
            }
        }
    }

    public void MergeAccounts(List<NonIndexedAccount> accounts)
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
                var newIndividual = CreateNewIndividualFromNonIndexedAccount(inputAccount);
                _namedAppToInAppIdToIndividual[inputAccount.namedApp].Add(inputAccount.inAppIdentifier, newIndividual);
            }
        }
    }

    public void DesolidarizeIndividualAccounts(Individual toDesolidarize)
    {
        if (toDesolidarize.accounts.Count <= 1) return;

        var originalAccounts = toDesolidarize.accounts.ToList();
        
        toDesolidarize.accounts = [originalAccounts[0]];
        RebuildAccountDictionary();

        for (var index = 1; index < originalAccounts.Count; index++)
        {
            var account = originalAccounts[index];
            var newIndividual = CreateNewIndividualFromAccount(account);
            newIndividual.customName = toDesolidarize.customName;
            newIndividual.note = new Note
            {
                status = toDesolidarize.note.status,
                text = toDesolidarize.note.text
            };
        }
    }

    private static bool SynchronizeAccount(Account existingAccount, NonIndexedAccount inputAccount)
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

            var workingDisplayNames = existingAccount.allDisplayNames.ToList();
            if (!workingDisplayNames.Contains(inputAccount.inAppDisplayName))
            {
                workingDisplayNames.Add(inputAccount.inAppDisplayName);
            }
            existingAccount.allDisplayNames = workingDisplayNames.Distinct().ToList();

            existingAccount.isPendingUpdate = false; // Merging a complete inputAccount means it's no longer pending update.
            
            return true;
        }

        return false;
    }

    private static bool SynchronizeIncompleteAccount(Account existingAccount, IncompleteAccount inputAccount)
    {
        var isSameAppAndIdentifier = IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier;
        if (isSameAppAndIdentifier)
        {
            existingAccount.inAppDisplayName = inputAccount.inAppDisplayName;
            
            foreach (var inputCaller in inputAccount.callers)
            {
                var callerExists = false;
                foreach (var existingCaller in existingAccount.callers)
                {
                    if (inputCaller.isAnonymous && existingCaller.isAnonymous
                        || !inputCaller.isAnonymous && !existingCaller.isAnonymous && existingCaller.inAppIdentifier == inputCaller.inAppIdentifier)
                    {
                        callerExists = true;
                        if (inputCaller.isContact != null) { existingCaller.isContact = (bool)inputCaller.isContact; }
                        if (inputCaller.note != null) { UpdateExistingNote(existingCaller.note, inputCaller.note); }
                        
                        break;
                    }
                }
                if (!callerExists)
                {
                    existingAccount.callers.Add(IncompleteCallerAccount.MakeComplete(inputCaller));
                }
            }

            var workingDisplayNames = existingAccount.allDisplayNames.ToList();
            if (!workingDisplayNames.Contains(inputAccount.inAppDisplayName))
            {
                workingDisplayNames.Add(inputAccount.inAppDisplayName);
            }
            existingAccount.allDisplayNames = workingDisplayNames.Distinct().ToList();
            
            // Notice how we don't set account.isPendingUpdate to true here, it just stays default previous value.
            
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
        
        existingIndividual.displayName = existingIndividual.accounts.First().inAppDisplayName;
    }

    private static bool IsSameApp(Account existingAccount, NonIndexedAccount inputAccount)
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

    private static bool IsSameApp(Account existingAccount, IncompleteAccount inputAccount)
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
        return InternalCreateFromAccount(account);
    }

    private Individual InternalCreateFromAccount(Account account)
    {
        var isAnyContact = account.IsAnyCallerContact();
        var individual = new Individual
        {
            guid = XYVRGuids.ForIndividual(),
            accounts = [account],
            displayName = account.inAppDisplayName,
            isAnyContact = isAnyContact,
            isExposed = isAnyContact || account.HasAnyCallerNote()
        };
        Individuals.Add(individual);
        return individual;
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private Individual CreateNewIndividualFromNonIndexedAccount(NonIndexedAccount nonIndexedAccount)
    {
        var account = NonIndexedAccount.MakeIndexed(nonIndexedAccount);
        return InternalCreateFromAccount(account);
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private Individual CreateNewIndividualFromIncompleteAccount(IncompleteAccount incompleteAccount)
    {
        var account = IncompleteAccount.MakeIndexed(incompleteAccount);
        return InternalCreateFromAccount(account);
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

        if (toAugment.customName == null && toDestroy.customName != null)
        {
            toAugment.customName = toDestroy.customName;
        }
        
        foreach (var accountFromDestroyed in toDestroy.accounts)
        {
            _namedAppToInAppIdToIndividual[accountFromDestroyed.namedApp][accountFromDestroyed.inAppIdentifier] = toAugment;
        }
        
        Individuals.RemoveAt(indexToDestroy);
    }

    public Individual GetIndividualByAccount(AccountIdentification accountIdentification)
    {
        return _namedAppToInAppIdToIndividual[accountIdentification.namedApp][accountIdentification.inAppIdentifier];
    }

    public Individual GetByGuid(string guid)
    {
        return Individuals.First(individual => individual.guid == guid);
    }
}