namespace XYVR.AccountAuthority.VRChat;

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
public class VRChatGroup
{
    // public bool? ageVerificationSlotsAvailable { get; set; }
    // public string? ageVerificationBetaCode { get; set; }
    // public int? ageVerificationBetaSlots { get; set; }
    // public string[]? badges { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string shortCode { get; set; }
    public string discriminator { get; set; }
    public string description { get; set; }
    // public string? iconUrl { get; set; }
    // public string? bannerUrl { get; set; }
    // public string? privacy { get; set; }
    // public string? ownerId { get; set; }
    // public string? rules { get; set; }
    // public string[]? links { get; set; }
    // public string[]? languages { get; set; }
    // public string? iconId { get; set; }
    // public string? bannerId { get; set; }
    // public int? memberCount { get; set; }
    // public DateTime? memberCountSyncedAt { get; set; }
    // public bool? isVerified { get; set; }
    // public string? joinState { get; set; }
    // public string[]? tags { get; set; }
    // public string? transferTargetId { get; set; }
    // public List<VRChatGroupGallery>? galleries { get; set; }
    // public DateTime? createdAt { get; set; }
    // public DateTime? updatedAt { get; set; }
    // public DateTime? lastPostCreatedAt { get; set; }
    // public int? onlineMemberCount { get; set; }
    // public string? membershipStatus { get; set; }
    // public VRChatGroupMember? myMember { get; set; }
    // public List<VRChatGroupRole>? roles { get; set; }
}
/*
[Serializable]
public class VRChatGroupGallery
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public bool? membersOnly { get; set; }
    public string[]? roleIdsToView { get; set; }
    public string[]? roleIdsToSubmit { get; set; }
    public string[]? roleIdsToAutoApprove { get; set; }
    public string[]? roleIdsToManage { get; set; }
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}

[Serializable]
public class VRChatGroupMember
{
    public string? id { get; set; }
    public string? groupId { get; set; }
    public string? userId { get; set; }
    public string[]? roleIds { get; set; }
    public string? acceptedByDisplayName { get; set; }
    public string? acceptedById { get; set; }
    public DateTime? createdAt { get; set; }
    public string? managerNotes { get; set; }
    public string? membershipStatus { get; set; }
    public bool? isSubscribedToAnnouncements { get; set; }
    public bool? isSubscribedToEventAnnouncements { get; set; }
    public string? visibility { get; set; }
    public bool? isRepresenting { get; set; }
    public DateTime? joinedAt { get; set; }
    public string? bannedAt { get; set; }
    public bool? has2FA { get; set; }
    public bool? hasJoinedFromPurchase { get; set; }
    public DateTime? lastPostReadAt { get; set; }
    public string[]? mRoleIds { get; set; }
    public string[]? permissions { get; set; }
}

[Serializable]
public class VRChatGroupRole
{
    public string? id { get; set; }
    public string? groupId { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public bool? isSelfAssignable { get; set; }
    public string[]? permissions { get; set; }
    public bool? isManagementRole { get; set; }
    public bool? requiresTwoFactor { get; set; }
    public bool? requiresPurchase { get; set; }
    public int? order { get; set; }
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}
*/
#pragma warning restore 0649
#pragma warning restore CS8618