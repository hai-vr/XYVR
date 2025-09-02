using System.Collections.Concurrent;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

public class CredentialsManagement
{
    private readonly ConcurrentDictionary<string, InMemoryCredentialsStorage> _connectorGuidToCredentialsStorageState = new();

    public async Task<ConnectionAttemptResult> TryConnect(Connector connector, ConnectionAttempt connectionAttempt)
    {
        return connector.type switch
        {
            ConnectorType.VRChatAPI => await ConnectToVRChat(connectionAttempt),
            ConnectorType.ResoniteAPI => await ConnectToResonite(connectionAttempt),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<ConnectionAttemptResult> ConnectToVRChat(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        
        var communicator = new VRChatCommunicator(
            new DoNotStoreAnythingStorage(),
            connectionAttempt.login__sensitive,
            connectionAttempt.password__sensitive,
            connectionAttempt.twoFactorCode__sensitive,
            credentialsStorage
        );
        
        Console.WriteLine("Connecting to VRChat...");
        LoginResponseStatus result;
        if (connectionAttempt.twoFactorCode__sensitive == null)
        {
            result = await communicator.VrcLoginUsingUsernameAndPassword();
        }
        else
        {
            result = await communicator.VrcLoginContinuationUsingTwoFactor();
        }
        
        Console.WriteLine($"The result was {result}");
        
        if (result == LoginResponseStatus.Success)
        {
            if (connectionAttempt.stayLoggedIn)
            {
                // TODO: Store the InMemoryCredentialsStorage token and then remove it from the dictionary
                // ????????????????????????
            }
            _connectorGuidToCredentialsStorageState.Remove(guid, out _);
        }

        return result switch
        {
            LoginResponseStatus.Success => new ConnectionAttemptResult { type = ConnectionAttemptResultType.Success },
            LoginResponseStatus.RequiresTwofer => new ConnectionAttemptResult { type = ConnectionAttemptResultType.NeedsTwoFactorCode },
            _ => new ConnectionAttemptResult { type = ConnectionAttemptResultType.Failure }
        };
    }

    private async Task<ConnectionAttemptResult> ConnectToResonite(ConnectionAttempt connectionAttempt)
    {
        await Task.CompletedTask;
        
        // TODO: implement resonite
        return new ConnectionAttemptResult { type = ConnectionAttemptResultType.Failure };
    }
}