using XYVR.API.Resonite;

namespace XYVR.AccountAuthority.Resonite;

internal class CombinedContactAndUser(string contactId, ContactResponseElementJsonObject contact, UserResponseJsonObject user)
{
    public string ContactId { get; } = contactId;
    public ContactResponseElementJsonObject Contact { get; } = contact;
    public UserResponseJsonObject User { get; } = user;
}