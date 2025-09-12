using System.Collections.Immutable;

namespace XYVR.Core;

public class IndividualRepository
{
    public IEnumerable<ImmutableIndividual> Individuals => _individualRefs.Select(individual => individual.value).ToList();

    private readonly Dictionary<NamedApp, Dictionary<string, IndividualRef>> _namedAppToInAppIdToIndividual = new();
    private readonly List<IndividualRef> _individualRefs;

    private class IndividualRef(ImmutableIndividual value)
    {
        internal string guid => value.guid;
        internal ImmutableIndividual value = value;
    }

    public IndividualRepository(ImmutableIndividual[] individuals)
    {
        _individualRefs = individuals.Select(individual => new IndividualRef(individual)).ToList();
        EvaluateDataMigrations();

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
    private void EvaluateDataMigrations()
    {
        for (var index = 0; index < _individualRefs.Count; index++)
        {
            var existingIndividual = _individualRefs[index].value;
            var modifiedIndividual = ModifyIndividualBasedOnAccounts(existingIndividual);
            if (existingIndividual != modifiedIndividual)
            {
                _individualRefs[index].value = modifiedIndividual;
            }
        }

        for (var i = 0; i < _individualRefs.Count; i++)
        {
            var originalIndividual = _individualRefs[i].value;
            
            var modifiedIndividual = originalIndividual;
            
            if (modifiedIndividual.note.status == 0)
            {
                modifiedIndividual = modifiedIndividual with
                {
                    note = modifiedIndividual.note with
                    {
                        status = NoteState.NeverHad
                    }
                };
            }

            // Fix anomalies
            var modifiedAccounts = modifiedIndividual.accounts.ToList();
            for (var index = 0; index < modifiedAccounts.Count; index++)
            {
                var originalAccount = modifiedAccounts[index];

                var modifiedAccount = originalAccount;
                if (string.IsNullOrWhiteSpace(modifiedAccount.guid))
                {
                    modifiedAccount = modifiedAccount with { guid = XYVRGuids.ForAccount() };
                }

                if (modifiedAccount.allDisplayNames == null || modifiedAccount.allDisplayNames.Length == 0)
                {
                    modifiedAccount = modifiedAccount with { allDisplayNames = [modifiedIndividual.displayName] };
                }

                if (originalAccount != modifiedAccount)
                {
                    modifiedAccounts[index] = modifiedAccount;
                }
            }
            modifiedIndividual = modifiedIndividual with { accounts = [..modifiedAccounts] };

            if (originalIndividual != modifiedIndividual)
            {
                _individualRefs[i].value = modifiedIndividual;
            }
        }

        var duplicateRecords = _individualRefs.SelectMany(individual => individual.value.accounts)
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
                var problematics = _individualRefs
                    .Select(ind => ind.value)
                    .Where(individual => individual.accounts.Any(account => account.qualifiedAppName == discriminatorRecord.AccountQualifiedAppName && account.inAppIdentifier == discriminatorRecord.AccountInAppIdentifier))
                    .ToList();
                Console.WriteLine($"Problematic ({problematics.Count}): {string.Join(",", problematics.Select(individual => individual.accounts.Length).ToList())}");
                if (problematics.Any(individual => individual.accounts.Length > 1))
                {
                    var max = problematics.Max(individual => individual.accounts.Length);
                    var maximumInd = problematics.First(individual => individual.accounts.Length == max);
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
        return _individualRefs
            .SelectMany(individual => individual.value.accounts)
            .Where(account => account.namedApp == namedApp)
            .Select(account => account.inAppIdentifier)
            .ToHashSet();
    }

    private Dictionary<string, IndividualRef> CreateAccountDictionary(NamedApp namedApp)
    {
        var results = new Dictionary<string, IndividualRef>();
        
        foreach (var individualRef in _individualRefs)
        {
            foreach (var account in individualRef.value.accounts)
            {
                if (account.namedApp == namedApp)
                {
                    results[account.inAppIdentifier] = individualRef;
                }
            }
        }
        
        return results;
    }

    /// The list of accounts may contain references to the same account but with different data.
    /// It needs to be applied sequentially without deduplication.
    public HashSet<ImmutableAccountIdentification> MergeIncompleteAccounts(List<ImmutableIncompleteAccount> incompleteAccounts)
    {
        var actuallyModified = new HashSet<ImmutableAccountIdentification>();

        foreach (var inputAccount in incompleteAccounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividualRef))
            {
                var existingIndividual = existingIndividualRef.value;
                var modifiedAccounts = existingIndividual.accounts.ToList();
                for (var index = 0; index < modifiedAccounts.Count; index++)
                {
                    var existingAccount = modifiedAccounts[index];
                    if (TrySynchronizeIncompleteAccount(existingAccount, inputAccount, out var modifiedAccount))
                    {
                        modifiedAccounts[index] = modifiedAccount!;
                        break;
                    }
                }
                
                var modifiedIndividual = ModifyIndividualBasedOnAccounts(existingIndividual with { accounts = [..modifiedAccounts] });
                if (existingIndividual != modifiedIndividual)
                {
                    Console.WriteLine($"Something about {inputAccount.inAppDisplayName} changed.");
                    existingIndividualRef.value = modifiedIndividual;
                    
                    actuallyModified.Add(inputAccount.AsIdentification());
                }
                else
                {
                    Console.WriteLine($"Merging account of {inputAccount.inAppDisplayName} resulted in no change.");
                }
            }
            else
            {
                Console.WriteLine($"Creating new individual from incomplete account: {inputAccount.namedApp} {inputAccount.inAppIdentifier} {inputAccount.inAppDisplayName}");
                _ = CreateNewIndividualFromIncompleteAccount(inputAccount);
                    
                actuallyModified.Add(inputAccount.AsIdentification());
            }
        }

        return actuallyModified;
    }

    /// The list of accounts may contain references to the same account but with different data.
    /// It needs to be applied sequentially without deduplication.
    public HashSet<ImmutableAccountIdentification> MergeAccounts(List<ImmutableNonIndexedAccount> accounts)
    {
        var actuallyModified = new HashSet<ImmutableAccountIdentification>();
        
        foreach (var inputAccount in accounts)
        {
            if (_namedAppToInAppIdToIndividual[inputAccount.namedApp].TryGetValue(inputAccount.inAppIdentifier, out var existingIndividualRef))
            {
                var existingIndividual = existingIndividualRef.value;
                var modifiedAccounts = existingIndividual.accounts.ToList();
                for (var index = 0; index < modifiedAccounts.Count; index++)
                {
                    var existingAccount = modifiedAccounts[index];
                    if (TrySynchronizeAccount(existingAccount, inputAccount, out var modifiedAccount))
                    {
                        modifiedAccounts[index] = modifiedAccount!;
                        break;
                    }
                }

                var modifiedIndividual = ModifyIndividualBasedOnAccounts(existingIndividual with { accounts = [..modifiedAccounts] });
                if (existingIndividual != modifiedIndividual)
                {
                    Console.WriteLine($"Something about {inputAccount.inAppDisplayName} changed.");
                    existingIndividualRef.value = modifiedIndividual;
                    
                    actuallyModified.Add(inputAccount.AsIdentification());
                }
                else
                {
                    Console.WriteLine($"Merging account of {inputAccount.inAppDisplayName} resulted in no change.");
                }
            }
            else
            {
                Console.WriteLine($"Creating new individual: {inputAccount.namedApp} {inputAccount.inAppIdentifier} {inputAccount.inAppDisplayName}");
                _ = CreateNewIndividualFromNonIndexedAccount(inputAccount);
                
                actuallyModified.Add(inputAccount.AsIdentification());
            }
        }

        return actuallyModified;
    }

    public void DesolidarizeIndividualAccounts(ImmutableIndividual toDesolidarize)
    {
        var indexOfIndividualToDesolidarize = IndexOfGuid(toDesolidarize.guid);
        if (indexOfIndividualToDesolidarize == -1) throw new InvalidOperationException("Individual not found in this repository");
        
        if (toDesolidarize.accounts.Length <= 1) return;

        var originalAccounts = toDesolidarize.accounts;

        _individualRefs[indexOfIndividualToDesolidarize].value = toDesolidarize with
        {
            accounts = [toDesolidarize.accounts[0]]
        };

        RebuildAccountDictionary();

        for (var index = 1; index < originalAccounts.Length; index++)
        {
            var account = originalAccounts[index];
            var newInd = CreateNewIndividualFromAccount(account);
            var newIndIndex = IndexOfGuid(newInd.guid);
            if (newIndIndex == -1) throw new InvalidOperationException("Individual we just created wasn't found. This is not normal");
            
            var newIndividual = newInd with
            {
                customName = toDesolidarize.customName,
                note = new ImmutableNote
                {
                    status = toDesolidarize.note.status,
                    text = toDesolidarize.note.text
                }
            };
            
            _individualRefs[newIndIndex].value = newIndividual;
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

    private static ImmutableIndividual ModifyIndividualBasedOnAccounts(ImmutableIndividual existingIndividual)
    {
        return existingIndividual with
        {
            isAnyContact = existingIndividual.accounts.Any(account => account.IsAnyCallerContact()),
            isExposed = existingIndividual.isAnyContact
                        || existingIndividual.note.status == NoteState.Exists
                        || existingIndividual.accounts.Any(account => account.HasAnyCallerNote()),
            displayName = existingIndividual.accounts.First().inAppDisplayName
        };
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
    private ImmutableIndividual CreateNewIndividualFromAccount(ImmutableAccount account)
    {
        return InternalCreateFromAccount(account);
    }

    private ImmutableIndividual InternalCreateFromAccount(ImmutableAccount account)
    {
        var isAnyContact = account.IsAnyCallerContact();
        var individual = new ImmutableIndividual
        {
            guid = XYVRGuids.ForIndividual(),
            accounts = [account],
            displayName = account.inAppDisplayName,
            isAnyContact = isAnyContact,
            isExposed = isAnyContact || account.HasAnyCallerNote()
        };
        var individualRef = new IndividualRef(individual);
        _individualRefs.Add(individualRef);
        _namedAppToInAppIdToIndividual[account.namedApp][account.inAppIdentifier] = individualRef;
        return individual;
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private ImmutableIndividual CreateNewIndividualFromNonIndexedAccount(ImmutableNonIndexedAccount nonIndexedAccount)
    {
        var account = ImmutableNonIndexedAccount.MakeIndexed(nonIndexedAccount);
        return InternalCreateFromAccount(account);
    }

    // It is the responsibility of the caller to never call this when that account is already owned by an Individual.
    private ImmutableIndividual CreateNewIndividualFromIncompleteAccount(ImmutableIncompleteAccount incompleteAccount)
    {
        var account = ImmutableIncompleteAccount.MakeIndexed(incompleteAccount);
        return InternalCreateFromAccount(account);
    }

    public void FusionIndividuals(ImmutableIndividual toAugment, ImmutableIndividual toDestroy)
    {
        if (toAugment == toDestroy || toAugment.guid == toDestroy.guid) throw new ArgumentException("Cannot fusion an Individual with itself");
        
        var indexToDestroy = IndexOfGuid(toDestroy.guid);
        if (indexToDestroy == -1) throw new ArgumentException("Individual to destroy not found in this repository");
        
        var indexToAugment = IndexOfGuid(toAugment.guid);
        if (indexToAugment == -1) throw new ArgumentException("Individual to augment not found in this repository");

        var isAnyContact = toAugment.accounts.Any(account => account.IsAnyCallerContact());
        var augmented = toAugment with
        {
            accounts = [..toAugment.accounts.Concat(toDestroy.accounts)],
            isAnyContact = isAnyContact,
            note = toAugment.note.status == NoteState.NeverHad && toDestroy.note.status is NoteState.Exists or NoteState.WasRemoved
                ? toAugment.note with
                {
                    status = toDestroy.note.status,
                    text = toDestroy.note.text
                }
                : toAugment.note,
            isExposed = isAnyContact
                        || toAugment.note.status == NoteState.Exists
                        || toAugment.accounts.Any(account => account.HasAnyCallerNote()),
            customName = toAugment.customName == null && toDestroy.customName != null
                ? toDestroy.customName
                : toAugment.customName
        };
        
        var toAugmentRef = _individualRefs[indexToAugment];
        toAugmentRef.value = augmented;
        
        foreach (var accountFromDestroyed in toDestroy.accounts)
        {
            _namedAppToInAppIdToIndividual[accountFromDestroyed.namedApp][accountFromDestroyed.inAppIdentifier] = toAugmentRef;
        }
        
        _individualRefs.RemoveAt(indexToDestroy);
    }

    public ImmutableIndividual GetIndividualByAccount(ImmutableAccountIdentification accountIdentification)
    {
        return _namedAppToInAppIdToIndividual[accountIdentification.namedApp][accountIdentification.inAppIdentifier].value;
    }

    public ImmutableIndividual GetByGuid(string guid)
    {
        return _individualRefs.First(individual => individual.guid == guid).value;
    }

    private int IndexOfGuid(string guid)
    {
        return _individualRefs.FindIndex(it => it.guid == guid);
    }
}