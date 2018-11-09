// <copyright file="MessageCode.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET.Messaging
{
    /// <summary>
    ///     Server and peer message codes.
    /// </summary>
    public enum MessageCode
    {
        /// <summary>
        ///     0/Unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     1
        /// </summary>
        ServerLogin = 10001,

        /// <summary>
        ///     2
        /// </summary>
        ServerSetListenPort = 10002,

        /// <summary>
        ///     3
        /// </summary>
        ServerGetPeerAddress = 10003,

        /// <summary>
        ///     5
        /// </summary>
        ServerAddUser = 10005,

        /// <summary>
        ///     7
        /// </summary>
        ServerGetStatus = 10007,

        /// <summary>
        ///     13
        /// </summary>
        ServerSayInChatRoom = 10013,

        /// <summary>
        ///     14
        /// </summary>
        ServerJoinRoom = 10014,

        /// <summary>
        ///     15
        /// </summary>
        ServerLeaveRoom = 10015,

        /// <summary>
        ///     16
        /// </summary>
        ServerUserJoinedRoom = 10016,

        /// <summary>
        ///     17
        /// </summary>
        ServerUserLeftRoom = 10017,

        /// <summary>
        ///     18
        /// </summary>
        ServerConnectToPeer = 10018,

        /// <summary>
        ///     22
        /// </summary>
        ServerPrivateMessages = 10022,

        /// <summary>
        ///     23
        /// </summary>
        ServerAcknowledgePrivateMessage = 10023,

        /// <summary>
        ///     26
        /// </summary>
        ServerFileSearch = 10026,

        /// <summary>
        ///     28
        /// </summary>
        ServerSetOnlineStatus = 10028,

        /// <summary>
        ///     32
        /// </summary>
        ServerPing = 10032,

        /// <summary>
        ///     34
        /// </summary>
        ServerSendSpeed = 10034,

        /// <summary>
        ///     35
        /// </summary>
        ServerSharedFoldersAndFiles = 10035,

        /// <summary>
        ///     36
        /// </summary>
        ServerGetUserStats = 10036,

        /// <summary>
        ///     40
        /// </summary>
        ServerQueuedDownloads = 10040,

        /// <summary>
        ///     41
        /// </summary>
        ServerKickedFromServer = 10041,

        /// <summary>
        ///     42
        /// </summary>
        ServerUserSearch = 10042,

        /// <summary>
        ///     51
        /// </summary>
        ServerInterestAdd = 10051,

        /// <summary>
        ///     52
        /// </summary>
        ServerInterestRemove = 10052,

        /// <summary>
        ///     54
        /// </summary>
        ServerGetRecommendations = 10054,

        /// <summary>
        ///     56
        /// </summary>
        ServerGetGlobalRecommendations = 10056,

        /// <summary>
        ///     57
        /// </summary>
        ServerGetUserInterests = 10057,

        /// <summary>
        ///     64
        /// </summary>
        ServerRoomList = 10064,

        /// <summary>
        ///     65
        /// </summary>
        ServerExactFileSearch = 10065,

        /// <summary>
        ///     66
        /// </summary>
        ServerGlobalAdminMessage = 10066,

        /// <summary>
        ///     69
        /// </summary>
        ServerPrivilegedUsers = 10069,

        /// <summary>
        ///     71
        /// </summary>
        ServerHaveNoParents = 10071,

        /// <summary>
        ///     73
        /// </summary>
        ServerParentsIP = 10073,

        /// <summary>
        ///     83
        /// </summary>
        ServerParentMinSpeed = 10083,

        /// <summary>
        ///     84
        /// </summary>
        ServerParentSpeedRatio = 10084,

        /// <summary>
        ///     86
        /// </summary>
        ServerParentInactivityTimeout = 10086,

        /// <summary>
        ///     87
        /// </summary>
        ServerSearchInactivityTimeout = 10087,

        /// <summary>
        ///     88
        /// </summary>
        ServerMinimumParentsInCache = 10088,

        /// <summary>
        ///     90
        /// </summary>
        ServerDistributedAliveInterval = 10090,

        /// <summary>
        ///     91
        /// </summary>
        ServerAddPrivilegedUser = 10091,

        /// <summary>
        ///     92
        /// </summary>
        ServerCheckPrivileges = 10092,

        /// <summary>
        ///     93
        /// </summary>
        ServerSearchRequest = 10093,

        /// <summary>
        ///     100
        /// </summary>
        ServerAcceptChildren = 10100,

        /// <summary>
        ///     102
        /// </summary>
        ServerNetInfo = 10102,

        /// <summary>
        ///     103
        /// </summary>
        ServerWishlistSearch = 10103,

        /// <summary>
        ///     104
        /// </summary>
        ServerWishlistInterval = 10104,

        /// <summary>
        ///     110
        /// </summary>
        ServerGetSimilarUsers = 10110,

        /// <summary>
        ///     111
        /// </summary>
        ServerGetItemRecommendations = 10111,

        /// <summary>
        ///     112
        /// </summary>
        ServerGetItemSimilarUsers = 10112,

        /// <summary>
        ///     113
        /// </summary>
        ServerRoomTickers = 10113,

        /// <summary>
        ///     114
        /// </summary>
        ServerRoomTickerAdd = 10114,

        /// <summary>
        ///     115
        /// </summary>
        ServerRoomTickerRemove = 10115,

        /// <summary>
        ///     116
        /// </summary>
        ServerSetRoomTicker = 10116,

        /// <summary>
        ///     117
        /// </summary>
        ServerHatedInterestAdd = 10117,

        /// <summary>
        ///     118
        /// </summary>
        ServerHatedInterestRemove = 10118,

        /// <summary>
        ///     120
        /// </summary>
        ServerRoomSearch = 10120,

        /// <summary>
        ///     121
        /// </summary>
        ServerSendUploadSpeed = 10121,

        /// <summary>
        ///     122
        /// </summary>
        ServerUserPrivileges = 10122,

        /// <summary>
        ///     123
        /// </summary>
        ServerGivePrivileges = 10123,

        /// <summary>
        ///     124
        /// </summary>
        ServerNotifyPrivileges = 10124,

        /// <summary>
        ///     125
        /// </summary>
        ServerAcknowledgeNotifyPrivileges = 10125,

        /// <summary>
        ///     126
        /// </summary>
        ServerBranchLevel = 10126,

        /// <summary>
        ///     127
        /// </summary>
        ServerBranchRoot = 10127,

        /// <summary>
        ///     129
        /// </summary>
        ServerChildDepth = 10129,

        /// <summary>
        ///     133
        /// </summary>
        ServerPrivateRoomUsers = 10133,

        /// <summary>
        ///     134
        /// </summary>
        ServerPrivateRoomAddUser = 10134,

        /// <summary>
        ///     135
        /// </summary>
        ServerPrivateRoomRemoveUser = 10135,

        /// <summary>
        ///     136
        /// </summary>
        ServerPrivateRoomDropMembership = 10136,

        /// <summary>
        ///     137
        /// </summary>
        ServerPrivateRoomDropOwnership = 10137,

        /// <summary>
        ///     138
        /// </summary>
        ServerPrivateRoomUnknown = 10138,

        /// <summary>
        ///     139
        /// </summary>
        ServerPrivateRoomAdded = 10139,

        /// <summary>
        ///     140
        /// </summary>
        ServerPrivateRoomRemoved = 10140,

        /// <summary>
        ///     141
        /// </summary>
        ServerPrivateRoomToggle = 10141,

        /// <summary>
        ///     142
        /// </summary>
        ServerNewPassword = 10142,

        /// <summary>
        ///     143
        /// </summary>
        ServerPrivateRoomAddOperator = 10143,

        /// <summary>
        ///     144
        /// </summary>
        ServerPrivateRoomRemoveOperator = 10144,

        /// <summary>
        ///     145
        /// </summary>
        ServerPrivateRoomOperatorAdded = 10145,

        /// <summary>
        ///     146
        /// </summary>
        ServerPrivateRoomOperatorRemoved = 10146,

        /// <summary>
        ///     148
        /// </summary>
        ServerPrivateRoomOwned = 10148,

        /// <summary>
        ///     149
        /// </summary>
        ServerMessageUsers = 10149,

        /// <summary>
        ///     150
        /// </summary>
        ServerAskPublicChat = 10150,

        /// <summary>
        ///     151
        /// </summary>
        ServerStopPublicChat = 10151,

        /// <summary>
        ///     152
        /// </summary>
        ServerPublicChat = 10152,

        /// <summary>
        ///     1
        /// </summary>
        ServerCannotConnect = 11001,

        /// <summary>
        ///     4
        /// </summary>
        PeerBrowseRequest = 20004,

        /// <summary>
        ///     5
        /// </summary>
        PeerBrowseResponse = 20005,

        /// <summary>
        ///     8
        /// </summary>
        PeerSearchRequest = 20008,

        /// <summary>
        ///     9
        /// </summary>
        PeerSearchResponse = 20009,

        /// <summary>
        ///     15
        /// </summary>
        PeerInfoRequest = 20015,

        /// <summary>
        ///     16
        /// </summary>
        PeerInfoResponse = 20016,

        /// <summary>
        ///     36
        /// </summary>
        PeerFolderContentsRequest = 20036,

        /// <summary>
        ///     37
        /// </summary>
        PeerFolderContentsResponse = 20037,

        /// <summary>
        ///     40
        /// </summary>
        PeerTransferRequest = 20040,

        /// <summary>
        ///     41
        /// </summary>
        PeerUploadResponse = 20041,

        /// <summary>
        ///     41
        /// </summary>
        PeerDownloadResponse = 20041,

        /// <summary>
        ///     41
        /// </summary>
        PeerTransferResponse = 20041,

        /// <summary>
        ///     42
        /// </summary>
        PeerUploadPlacehold = 20042,

        /// <summary>
        ///     43
        /// </summary>
        PeerQueueDownload = 20043,

        /// <summary>
        ///     44
        /// </summary>
        PeerUploadQueueNotification = 20044,

        /// <summary>
        ///     46
        /// </summary>
        PeerUploadFailed = 20046,

        /// <summary>
        ///     50
        /// </summary>
        PeerQueueFailed = 20050,

        /// <summary>
        ///     51
        /// </summary>
        PeerPlaceInQueueRequest = 20051,
    }
}
