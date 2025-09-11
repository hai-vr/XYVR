using System.Collections.Immutable;

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
                individual.note = individual.note with
                {
                    status = NoteState.NeverHad
                };
            }

            // Fix anomalies
            for (var index = 0; index < individual.accounts.Count; index++)
            {
                var originalAccount = individual.accounts[index];
                
                var modifiedAccount = originalAccount;
                if (string.IsNullOrWhiteSpace(modifiedAccount.guid))
                {
                    modifiedAccount = modifiedAccount with { guid = XYVRGuids.ForAccount() };
                }
                if (modifiedAccount.allDisplayNames == null || modifiedAccount.allDisplayNames.Length == 0)
                {
                    modifiedAccount = modifiedAccount with { allDisplayNames = [individual.displayName] };
                }

                if (originalAccount != modifiedAccount)
                {
                    individual.accounts[index] = modifiedAccount;
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

    /// The list of accounts may contain references to the same account but with different data.
    /// It needs to be applied sequentially without deduplication.
    public void MergeIncompleteAccounts(List<ImmutableIncompleteAccount> incompleteAccounts)
    {
        foreach (var inputAccount in incompleteAccounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividual))
            {
                for (var index = 0; index < existingIndividual.accounts.Count; index++)
                {
                    var existingAccount = existingIndividual.accounts[index];
                    if (TrySynchronizeIncompleteAccount(existingAccount, inputAccount, out var modifiedAccount))
                    {
                        existingIndividual.accounts[index] = modifiedAccount!;
                        break;
                    }
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

    /// The list of accounts may contain references to the same account but with different data.
    /// It needs to be applied sequentially without deduplication.
    public void MergeAccounts(List<ImmutableNonIndexedAccount> accounts)
    {
        foreach (var inputAccount in accounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividual))
            {
                for (var index = 0; index < existingIndividual.accounts.Count; index++)
                {
                    var existingAccount = existingIndividual.accounts[index];
                    if (TrySynchronizeAccount(existingAccount, inputAccount, out var modifiedAccount))
                    {
                        existingIndividual.accounts[index] = modifiedAccount!;
                        break;
                    }
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
            newIndividual.note = new ImmutableNote
            {
                status = toDesolidarize.note.status,
                text = toDesolidarize.note.text
            };
        }
    }

    private static bool TrySynchronizeAccount(ImmutableAccount existingAccount, ImmutableNonIndexedAccount inputAccount, out ImmutableAccount? result)
    {
        var isSameAppAndIdentifier = IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier;
        if (isSameAppAndIdentifier)
        {
            var modifiedCallers = existingAccount.callers.ToList();
            foreach (var inputCaller in inputAccount.callers)
            {
                var callerExists = false;
                for (var index = 0; index < existingAccount.callers.Length; index++)
                {
                    var existingCaller = modifiedCallers[index];
                    if (inputCaller.isAnonymous && existingCaller.isAnonymous
                        || !inputCaller.isAnonymous && !existingCaller.isAnonymous && existingCaller.inAppIdentifier == inputCaller.inAppIdentifier)
                    {
                        callerExists = true;
                        
                        modifiedCallers[index] = existingCaller with
                        {
                            isContact = inputCaller.isContact,
                            note = UpdateExistingNote(existingCaller.note, inputCaller.note)
                        };

                        break;
                    }
                }

                if (!callerExists)
                {
                    modifiedCallers.Add(inputCaller);
                }
            }

            result = existingAccount with
            {
                inAppDisplayName = inputAccount.inAppDisplayName,
                specifics = inputAccount.specifics, // Specifics are always replaced entirely
                callers = [..modifiedCallers],
                allDisplayNames = BuildDisplayNames(existingAccount, inputAccount.inAppDisplayName),
                isPendingUpdate = false // Merging a complete inputAccount means it's no longer pending update.
            };
            
            return true;
        }

        result = null;
        return false;
    }

    private static bool TrySynchronizeIncompleteAccount(ImmutableAccount existingAccount, ImmutableIncompleteAccount inputAccount, out ImmutableAccount? result)
    {
        var isSameAppAndIdentifier = IsSameApp(existingAccount, inputAccount) && existingAccount.inAppIdentifier == inputAccount.inAppIdentifier;
        if (isSameAppAndIdentifier)
        {
            var modifiedCallers = existingAccount.callers.ToList();
            foreach (var inputCaller in inputAccount.callers)
            {
                var callerExists = false;
                for (var index = 0; index < existingAccount.callers.Length; index++)
                {
                    var existingCaller = modifiedCallers[index];
                    if (inputCaller.isAnonymous && existingCaller.isAnonymous
                        || !inputCaller.isAnonymous && !existingCaller.isAnonymous && existingCaller.inAppIdentifier == inputCaller.inAppIdentifier)
                    {
                        callerExists = true;

                        var modifiedCaller = existingCaller;
                        
                        if (inputCaller.isContact != null) { modifiedCaller = modifiedCaller with { isContact = (bool)inputCaller.isContact }; }
                        if (inputCaller.note != null) { modifiedCaller = modifiedCaller with { note = UpdateExistingNote(existingCaller.note, inputCaller.note) }; }

                        modifiedCallers[index] = modifiedCaller;

                        break;
                    }
                }

                if (!callerExists)
                {
                    modifiedCallers.Add(ImmutableIncompleteCallerAccount.MakeComplete(inputCaller));
                }
            }
            
            result = existingAccount with
            {
                inAppDisplayName = inputAccount.inAppDisplayName,
                allDisplayNames = BuildDisplayNames(existingAccount, inputAccount.inAppDisplayName),
                callers = [..modifiedCallers],
                // Notice how we don't set account.isPendingUpdate to true here, it just stays default previous value.
            };
            return true;
        }

        result = null;
        return false;
    }

    private static ImmutableArray<string> BuildDisplayNames(ImmutableAccount existingAccount, string inputAccountDisplayName)
    {
        var workingDisplayNames = existingAccount.allDisplayNames.ToList();
        if (!workingDisplayNames.Contains(inputAccountDisplayName))
        {
            workingDisplayNames.Add(inputAccountDisplayName);
        }

        return [..workingDisplayNames.Distinct()];
    }

    private static ImmutableNote UpdateExistingNote(ImmutableNote existingNote, ImmutableNote inputNote)
    {
        return inputNote.status switch
        {
            NoteState.Exists => existingNote with
            {
                status = NoteState.Exists,
                text = inputNote.text
            },
            NoteState.NeverHad => existingNote.status == NoteState.Exists 
                // We don't overwrite the text
                ? existingNote with { status = NoteState.WasRemoved }
                : existingNote,
            NoteState.WasRemoved => existingNote.status is NoteState.Exists or NoteState.NeverHad
                ? existingNote with { status = NoteState.WasRemoved }
                : existingNote,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void UpdateIndividualBasedOnAccounts(Individual existingIndividual)
    {
        existingIndividual.isAnyContact = existingIndividual.accounts.Any(account => account.IsAnyCallerContact());
        existingIndividual.isExposed = existingIndividual.isAnyContact
                                       || existingIndividual.note.status == NoteState.Exists
                                       || existingIndividual.accounts.Any(account => account.HasAnyCallerNote());
        
        existingIndividual.displayName = existingIndividual.accounts.First().inAppDisplayName;
    }

    private static bool IsSameApp(ImmutableAccount existingAccount, ImmutableNonIndexedAccount inputAccount)
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

    private static bool IsSameApp(ImmutableAccount existingAccount, ImmutableIncompleteAccount inputAccount)
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
    private Individual CreateNewIndividualFromAccount(ImmutableAccount account)
    {
        return InternalCreateFromAccount(account);
    }

    private Individual InternalCreateFromAccount(ImmutableAccount account)
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
    private Individual CreateNewIndividualFromNonIndexedAccount(ImmutableNonIndexedAccount nonIndexedAccount)
    {
        var account = ImmutableNonIndexedAccount.MakeIndexed(nonIndexedAccount);
        return InternalCreateFromAccount(account);
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private Individual CreateNewIndividualFromIncompleteAccount(ImmutableIncompleteAccount incompleteAccount)
    {
        var account = ImmutableIncompleteAccount.MakeIndexed(incompleteAccount);
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
                toAugment.note = toAugment.note with
                {
                    status = toDestroy.note.status,
                    text = toDestroy.note.text
                };
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

    public Individual GetIndividualByAccount(ImmutableAccountIdentification accountIdentification)
    {
        return _namedAppToInAppIdToIndividual[accountIdentification.namedApp][accountIdentification.inAppIdentifier];
    }

    public Individual GetByGuid(string guid)
    {
        return Individuals.First(individual => individual.guid == guid);
    }
}