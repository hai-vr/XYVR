using Shouldly;
using XYVR.Core;

namespace Hai.XYVR.Core.Tests;

public class LiveStatusMonitoringTest
{
    private const string GuidRegex = "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";

    private readonly ImmutableLiveUserUpdate BaseImmutableLiveUserUpdate = new ImmutableLiveUserUpdate
    {
        trigger = "UserStatusUpdate",
        namedApp = NamedApp.Resonite,
        qualifiedAppName = "resonite",
        inAppIdentifier = "U-someUserId",
        onlineStatus = OnlineStatus.Online,
        mainSession = new ImmutableLiveUserSessionState
        {
            knowledge = LiveUserSessionKnowledge.Known,
            knownSession = new ImmutableLiveUserKnownSession
            {
                inAppSessionIdentifier = "S-someSessionId",
                inAppHost = new ImmutableLiveSessionHost
                {
                    inAppHostIdentifier = "U-someHostId"
                },
                inAppSessionName = "My Cool World",
                inAppVirtualSpaceName = "The Grassy Meadow",
            }
        },
        callerInAppIdentifier = "U-callerUserId"
    };

    private static ImmutableLiveSession Session(string guid)
    {
        return new ImmutableLiveSession
        {
            guid = guid,
            namedApp = NamedApp.Resonite,
            qualifiedAppName = "resonite",
            inAppSessionIdentifier = "S-someSessionId",
            inAppSessionName = "My Cool World",
            inAppVirtualSpaceName = "The Grassy Meadow",
            inAppHost = new ImmutableLiveSessionHost
            {
                inAppHostIdentifier = "U-someHostId"
            },
            participants = [new ImmutableParticipant
            {
                isKnown = true,
                knownAccount = new ImmutableKnownParticipantAccount
                {
                    inAppIdentifier = "U-someUserId"
                },
                isHost = false
            }]
        };
    }

    [Test]
    public async Task MergeUser_should_create_a_session()
    {
        // Given
        var sut = new LiveStatusMonitoring();
        
        // When
        await sut.MergeUser(BaseImmutableLiveUserUpdate);
        var result = sut.GetAllSessions();

        // Then
        result.ShouldHaveSingleItem();
        result[0].guid.ShouldMatch(GuidRegex);
        result[0].ShouldBe(Session(result[0].guid));
    }

    [Test]
    public async Task MergeUser_twice_should_be_a_no_op()
    {
        // Given
        var sut = new LiveStatusMonitoring();
        
        // When
        await sut.MergeUser(BaseImmutableLiveUserUpdate);
        await sut.MergeUser(BaseImmutableLiveUserUpdate);
        var result = sut.GetAllSessions();

        // Then
        result.ShouldHaveSingleItem();
        result[0].guid.ShouldMatch(GuidRegex);
        result[0].ShouldBe(Session(result[0].guid));
    }

    [Test]
    public async Task MergeUser_two_different_sessions_should_switch_participation()
    {
        // Given
        var sut = new LiveStatusMonitoring();
        
        // When
        await sut.MergeUser(BaseImmutableLiveUserUpdate);
        await sut.MergeUser(BaseImmutableLiveUserUpdate with
        {
            mainSession = BaseImmutableLiveUserUpdate.mainSession! with
            {
                knownSession = BaseImmutableLiveUserUpdate.mainSession!.knownSession! with
                {
                    inAppSessionIdentifier = "S-someOtherSessionId"
                }
            }
        });
        var result = sut.GetAllSessions();

        // Then
        result.Count.ShouldBe(2);
        result[0].guid.ShouldMatch(GuidRegex);
        result[1].guid.ShouldMatch(GuidRegex);
        result[0].ShouldBe(Session(result[0].guid) with { participants = [] });
        result[1].ShouldBe(Session(result[1].guid) with { inAppSessionIdentifier = "S-someOtherSessionId" });
    }
}