import {NamedApp, type NamedAppType} from "./types/CoreTypes.ts";
import React from "react";
import resoniteIcon from "./assets/Resonite_Wiki-Icon.png";
import clusterIcon from "./assets/cluster.png";
import cvrIcon from "./assets/cvr_logo_small.png";
import {LiveSessionMarker, type LiveSessionMarkerType} from "./types/LiveUpdateTypes.ts";
import i18n from "./i18n.ts";

export interface SupportedApp {
    namedApp: NamedAppType;
    displayName: string;
    searchTerm: string;
    iconClass: string;
    isSessionCapacityKnowable: boolean;
    isTotalNumberOfUsersKnowable: boolean;
    areOtherSessionUsersKnowable: boolean;
    isJoinButtonMessage: boolean;
    isSessionOpenableOnWeb: boolean;
    hasLinkToProfile: boolean;
    areFriendsCalledFriendsInsteadOfContact: boolean;
    isKnowingInAppIdentifierRelevantForInAppFunctions: boolean;
    canUserBeInMultipleSessionsSimultaneously: boolean;

    getIcon(): React.ReactNode;
    getAccessLevelText(markers: LiveSessionMarkerType[]): string;
    getSessionLink(inAppSessionIdentifier: string, supplementalIdentifier?: string): string;
    getProfileLink(inAppIdentifier: string): string
}

export const ResoniteApp: SupportedApp = {
    namedApp: NamedApp.Resonite,
    displayName: "Resonite",
    searchTerm: 'resonite',
    iconClass: 'resonite',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
    isJoinButtonMessage: false,
    isSessionOpenableOnWeb: true,
    hasLinkToProfile: false,
    areFriendsCalledFriendsInsteadOfContact: false,
    isKnowingInAppIdentifierRelevantForInAppFunctions: true,
    canUserBeInMultipleSessionsSimultaneously: true,

    getIcon(): React.ReactNode {
        return <img src={resoniteIcon} alt="Resonite" className="app-icon-img" title="Resonite"/>;
    },

    getAccessLevelText(markers: LiveSessionMarkerType[]): string {
        const t = i18n.t;
        if (markers.includes(LiveSessionMarker.ResoniteAnyone)) return t('live.session.markers.resoniteAnyone');
        if (markers.includes(LiveSessionMarker.ResoniteRegisteredUsers)) return t('live.session.markers.resoniteRegisteredUsers');
        if (markers.includes(LiveSessionMarker.ResoniteContactsPlus)) return t('live.session.markers.resoniteContactsPlus');
        if (markers.includes(LiveSessionMarker.ResoniteContacts)) return t('live.session.markers.resoniteContacts');
        if (markers.includes(LiveSessionMarker.ResoniteLAN)) return t('live.session.markers.resoniteLAN');
        if (markers.includes(LiveSessionMarker.ResonitePrivate)) return t('live.session.markers.resonitePrivate');
        return '';
    },

    getSessionLink(inAppSessionIdentifier: string, _a?: string): string {
        return `https://api.resonite.com/open/session/${inAppSessionIdentifier}`;
    },
    
    getProfileLink(_: string): string {
        return '';
    }
};

export const VRChatApp: SupportedApp = {
    namedApp: NamedApp.VRChat,
    displayName: "VRChat",
    searchTerm: 'vrchat',
    iconClass: 'vrchat',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: false,
    isJoinButtonMessage: true,
    isSessionOpenableOnWeb: true,
    hasLinkToProfile: true,
    areFriendsCalledFriendsInsteadOfContact: true,
    isKnowingInAppIdentifierRelevantForInAppFunctions: false,
    canUserBeInMultipleSessionsSimultaneously: false,

    getIcon(): React.ReactNode {
        return '💬';
    },

    getAccessLevelText(markers: LiveSessionMarkerType[]): string {
        const t = i18n.t;
        if (markers.includes(LiveSessionMarker.VRCPublic)) return t('live.session.markers.vrcPublic');
        if (markers.includes(LiveSessionMarker.VRCInvitePlus)) return t('live.session.markers.vrcInvitePlus');
        if (markers.includes(LiveSessionMarker.VRCInvite)) return t('live.session.markers.vrcInvite');
        if (markers.includes(LiveSessionMarker.VRCFriends)) return t('live.session.markers.vrcFriends');
        if (markers.includes(LiveSessionMarker.VRCFriendsPlus)) return t('live.session.markers.vrcFriendsPlus');
        if (markers.includes(LiveSessionMarker.VRCGroup)) return t('live.session.markers.vrcGroup');
        if (markers.includes(LiveSessionMarker.VRCGroupPublic)) return t('live.session.markers.vrcGroupPublic');
        if (markers.includes(LiveSessionMarker.VRCGroupPlus)) return t('live.session.markers.vrcGroupPlus');
        return '';
    },

    getSessionLink(inAppSessionIdentifier: string, _?: string): string {
        const separator = inAppSessionIdentifier.indexOf(':');
        if (separator === -1) {
            return `https://vrchat.com/home/launch?worldId=${inAppSessionIdentifier}`;
        }
        else {
            return `https://vrchat.com/home/launch?worldId=${inAppSessionIdentifier.substring(0, separator)}&instanceId=${inAppSessionIdentifier.substring(separator + 1)}`;
        }
    },
    
    getProfileLink(inAppIdentifier: string): string {
        return `https://vrchat.com/home/user/${inAppIdentifier}`;
    }
};

export const ChilloutVRApp: SupportedApp = {
    namedApp: NamedApp.ChilloutVR,
    displayName: "ChilloutVR",
    searchTerm: 'chilloutvr',
    iconClass: 'chilloutvr',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
    isJoinButtonMessage: false,
    isSessionOpenableOnWeb: false,
    hasLinkToProfile: true,
    areFriendsCalledFriendsInsteadOfContact: true,
    isKnowingInAppIdentifierRelevantForInAppFunctions: false,
    canUserBeInMultipleSessionsSimultaneously: false,

    getIcon(): React.ReactNode {
        return <img src={cvrIcon} alt="ChilloutVR" className="app-icon-img" title="ChilloutVR"/>;
    },

    getAccessLevelText(markers: LiveSessionMarkerType[]): string {
        const t = i18n.t;
        if (markers.includes(LiveSessionMarker.CVRPublic)) return t('live.session.markers.cvrPublic');
        if (markers.includes(LiveSessionMarker.CVRFriendsOfFriends)) return t('live.session.markers.cvrFriendsOfFriends');
        if (markers.includes(LiveSessionMarker.CVRFriends)) return t('live.session.markers.cvrFriends');
        if (markers.includes(LiveSessionMarker.CVRGroup)) return t('live.session.markers.cvrGroup');
        if (markers.includes(LiveSessionMarker.CVREveryoneCanInvite)) return t('live.session.markers.cvrEveryoneCanInvite');
        if (markers.includes(LiveSessionMarker.CVROwnerMustInvite)) return t('live.session.markers.cvrOwnerMustInvite');
        if (markers.includes(LiveSessionMarker.CVRGroupPlus)) return t('live.session.markers.cvrGroupPlus');
        if (markers.includes(LiveSessionMarker.CVRGroupPublic)) return t('live.session.markers.cvrGroupPublic');
        return '';
    },

    getSessionLink(_: string, _a?: string): string {
        return '';
    },
    
    getProfileLink(inAppIdentifier: string): string {
        return `https://hub.chilloutvr.net/social/profile?guid=${inAppIdentifier}`;
    }
}

export const ClusterVRApp: SupportedApp = {
    namedApp: NamedApp.Cluster,
    displayName: "Cluster",
    searchTerm: 'cluster',
    iconClass: 'cluster',
    isSessionCapacityKnowable: false,
    isTotalNumberOfUsersKnowable: false,
    areOtherSessionUsersKnowable: false,
    isJoinButtonMessage: false,
    isSessionOpenableOnWeb: true,
    hasLinkToProfile: false, // cluster profile links use the handle (e.g. @vr_hai), not the in-app identifier (e.g. 05533700-b384-41f8-a73e-040dfeec6f28)
    areFriendsCalledFriendsInsteadOfContact: true,
    isKnowingInAppIdentifierRelevantForInAppFunctions: false,
    canUserBeInMultipleSessionsSimultaneously: false,

    getIcon(): React.ReactNode {
        return <img src={clusterIcon} alt="cluster" className="app-icon-img" title="Cluster"/>;
    },

    getAccessLevelText(markers: LiveSessionMarkerType[]): string {
        const t = i18n.t;
        if (markers.includes(LiveSessionMarker.ClusterEvent)) return t('live.session.markers.clusterEvent');
        
        return '';
    },

    getSessionLink(_: string, supplementalIdentifier?: string): string {
        return `https://cluster.mu/w/s/${supplementalIdentifier}/entry`;
    },
    
    getProfileLink(_: string): string {
        return '';
    }
}

export const SupportedAppsByNamedApp: Record<string, SupportedApp> = {
    [NamedApp.Resonite]: ResoniteApp,
    [NamedApp.VRChat]: VRChatApp,
    [NamedApp.ChilloutVR]: ChilloutVRApp,
    [NamedApp.Cluster]: ClusterVRApp,
};

export const SupportedApps: SupportedApp[] = Object.values(SupportedAppsByNamedApp);