namespace XYVR.AccountAuthority.ChilloutVR;

// Portions of this file is based on prior work by AstroDogeDX on the CVRX project located at ( https://github.com/AstroDogeDX/CVRX )
// The LICENSE of CVRX has been copied into this file to acknowledge the original work.
// 
// [[[

// MIT License
// 
// Copyright (c) 2023 AstroDogeDX
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// ]]]

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
internal class CvrContactsResponse
{
    public string? message;
    public CvrContactsResponseData[] data;
}

[Serializable]
internal class CvrContactsResponseData
{
    public object[] categories;
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrInstanceResponse
{
    public string? message;
    public CvrInstanceResponseData data;
}

[Serializable]
internal class CvrInstanceResponseData
{
    public string instanceSettingPrivacy;
    public string privacy;
    public CvrInstanceResponsePlayer author;
    public CvrInstanceResponseOwner owner;
    public CvrInstanceResponseGroup? group;
    public string id;
    public string name;
    public string gameModeId;
    public string gameModeName;
    public string region;
    public CvrInstanceResponseWorld world;
    public int maxPlayer;
    public int currentPlayerCount;
    public CvrInstanceResponsePlayer[] members;
    public bool reserved;
}

[Serializable]
internal class CvrInstanceResponsePlayer
{
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrInstanceResponseOwner
{
    public string rank;
    public CvrInstanceResponseFeaturedBadge featuredBadge;
    public CvrInstanceResponseGroup? featuredGroup;
    public CvrInstanceResponseAvatar avatar;
    public string id;
    public string name;
    public string imageUrl;
}
[Serializable]
internal class CvrInstanceResponseFeaturedBadge
{
    public string name;
    public string image;
    public int badgeLevel;
}
[Serializable]
internal class CvrInstanceResponseGroup
{
    public string id;
    public string name;
    public string image;
}
[Serializable]
internal class CvrInstanceResponseAvatar
{
    public string id;
    public string name;
    public string imageUrl;
}
[Serializable]
internal class CvrInstanceResponseWorld
{
    public object[] tags;
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrWebsocketOnlineFriendsResponse
{
    public string Id;
    public bool IsOnline;
    public bool IsConnected;
    public CvrWebsocketInstance? Instance;
}
[Serializable]
internal class CvrWebsocketInstance
{
    public string Id;
    public string Name;
    public int Privacy;
}

internal class CvrPrivacyLevel
{
    internal const int Public = 0;
    internal const int FriendsOfFriends = 1;
    internal const int Friends = 2;
    internal const int Group = 3; // ChilloutVR defines this as Members Only.
    internal const int EveryoneCanInvite = 4;
    internal const int OwnerMustInvite = 5;
    internal const int GroupPlus = 6; // CVRX defines this as GroupsPlus (plural groups), ChilloutVR defines this as Friends of Members
    internal const int GroupPublic = 7;
}
#pragma warning restore 0649
#pragma warning restore CS8618