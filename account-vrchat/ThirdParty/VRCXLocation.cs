// This file is based on prior work by the VRCX Team on the VRCX project located at ( https://github.com/vrcx-team/VRCX )
// The LICENSE of VRCX has been copied into this file to acknowledge the original work.
// 
// [[[

// MIT License
// 
// Copyright (c) 2019-2025 pypy and individual contributors.
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
namespace XYVR.AccountAuthority.VRChat.ThirdParty;

internal static class VRCXLocation
{
    // Based on: https://github.com/vrcx-team/VRCX/blob/master/src/shared/utils/location.js#L35C1-L145C2
    public static VRCXLocationContext ParseLocation(string tag)
    {
        var _tag = tag;
        
        var ctx = new VRCXLocationContext { Tag = _tag };
        
        if (_tag == "offline" || _tag == "offline:offline")
        {
            ctx = ctx with { IsOffline = true };
        }
        else if (_tag == "private" || _tag == "private:private")
        {
            ctx = ctx with { IsPrivate = true };
        }
        else if (_tag == "traveling" || _tag == "traveling:traveling")
        {
            ctx = ctx with { IsTraveling = true };
        }
        else if (!string.IsNullOrEmpty(tag) && !_tag.StartsWith("local"))
        {
            ctx = ctx with { IsRealInstance = true };
            var sep = _tag.IndexOf(':');
            
            // technically not part of instance id, but might be there when copying id from url so why not support it
            const string shortNameQualifier = "&shortName=";
            var shortNameIndex = _tag.IndexOf(shortNameQualifier);
            if (shortNameIndex >= 0)
            {
                ctx = ctx with { ShortName = _tag.Substring(shortNameIndex + shortNameQualifier.Length) };
                _tag = _tag.Substring(0, shortNameIndex);
            }
            
            if (sep >= 0)
            {
                ctx = ctx with 
                { 
                    WorldId = _tag.Substring(0, sep),
                    InstanceId = _tag.Substring(sep + 1)
                };
                
                var parts = ctx.InstanceId.Split('~');
                for (int i = 0; i < parts.Length; i++)
                {
                    var s = parts[i];
                    if (i > 0)
                    {
                        var A = s.IndexOf('(');
                        var Z = A >= 0 ? s.LastIndexOf(')') : -1;
                        var key = Z >= 0 ? s.Substring(0, A) : s;
                        var value = A < Z ? s.Substring(A + 1, Z - A - 1) : string.Empty;
                        
                        ctx = key switch
                        {
                            "hidden" => ctx with { HiddenId = value },
                            "private" => ctx with { PrivateId = value },
                            "friends" => ctx with { FriendsId = value },
                            "canRequestInvite" => ctx with { CanRequestInvite = true },
                            "region" => ctx with { Region = value },
                            "group" => ctx with { GroupId = value },
                            "groupAccessType" => ctx with { GroupAccessType = value },
                            "strict" => ctx with { Strict = true },
                            "ageGate" => ctx with { AgeGate = true },
                            _ => ctx
                        };
                    }
                    else
                    {
                        ctx = ctx with { InstanceName = s };
                    }
                }
                
                ctx = ctx with { AccessType = VRCXLocationInferredAccessType.Public };
                
                if (ctx.PrivateId != null)
                {
                    if (ctx.CanRequestInvite)
                    {
                        // InvitePlus
                        ctx = ctx with { AccessType = VRCXLocationInferredAccessType.InvitePlus, UserId = ctx.PrivateId };
                    }
                    else
                    {
                        // InviteOnly
                        ctx = ctx with { AccessType = VRCXLocationInferredAccessType.Invite, UserId = ctx.PrivateId };
                    }
                }
                else if (ctx.FriendsId != null)
                {
                    // FriendsOnly
                    ctx = ctx with { AccessType = VRCXLocationInferredAccessType.Friends, UserId = ctx.FriendsId };
                }
                else if (ctx.HiddenId != null)
                {
                    // FriendsOfGuests
                    ctx = ctx with { AccessType = VRCXLocationInferredAccessType.FriendsPlus, UserId = ctx.HiddenId };
                }
                else if (ctx.GroupId != null)
                {
                    // Group
                    ctx = ctx with { AccessType = VRCXLocationInferredAccessType.Group };
                }
                
                // ctx = ctx with { AccessTypeName = ctx.AccessType };
                
                if (ctx.GroupAccessType != null)
                {
                    if (ctx.GroupAccessType == "public")
                    {
                        ctx = ctx with { AccessType = VRCXLocationInferredAccessType.GroupPublic };
                    }
                    else if (ctx.GroupAccessType == "plus")
                    {
                        ctx = ctx with { AccessType = VRCXLocationInferredAccessType.GroupPlus};
                    }
                }
            }
            else
            {
                ctx = ctx with { WorldId = _tag };
            }
        }
        
        return ctx;
    }
}

internal record VRCXLocationContext
{
    /* UNUSED */ internal string Tag { get; init; } = string.Empty;
    /* UNUSED */ internal bool IsOffline { get; init; }
    /* UNUSED */ internal bool IsPrivate { get; init; }
    /* UNUSED */ internal bool IsTraveling { get; init; }
    /* UNUSED */ internal bool IsRealInstance { get; init; }
    /* UNUSED */ internal string WorldId { get; init; } = string.Empty;
    /* UNUSED */ internal string InstanceId { get; init; } = string.Empty;
    /* UNUSED */ internal string InstanceName { get; init; } = string.Empty;
    /* UNUSED */ public VRCXLocationInferredAccessType AccessType { get; init; } = VRCXLocationInferredAccessType.Indeterminate;
    // /* UNUSED */ internal string AccessTypeName { get; init; } = string.Empty;
    /* UNUSED */ internal string Region { get; init; } = string.Empty;
    /* UNUSED */ internal string ShortName { get; init; } = string.Empty;
    /* UNUSED */ internal string? UserId { get; init; }
    /* UNUSED */ internal string? HiddenId { get; init; }
    /* UNUSED */ internal string? PrivateId { get; init; }
    /* UNUSED */ internal string? FriendsId { get; init; }
    /* UNUSED */ internal string? GroupId { get; init; }
    /* UNUSED */ internal string? GroupAccessType { get; init; }
    /* UNUSED */ internal bool CanRequestInvite { get; init; }
    /* UNUSED */ internal bool Strict { get; init; }
    /* UNUSED */ internal bool AgeGate { get; init; }
}

internal enum VRCXLocationInferredAccessType
{
    Indeterminate,
    Public,
    InvitePlus,
    Invite,
    Friends,
    FriendsPlus,
    Group,
    GroupPublic,
    GroupPlus
}
