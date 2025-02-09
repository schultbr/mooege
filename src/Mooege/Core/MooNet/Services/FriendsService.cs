﻿/*
 * Copyright (C) 2011 mooege project
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using Google.ProtocolBuffers;
using Mooege.Common;
using Mooege.Common.Extensions;
using Mooege.Core.MooNet.Accounts;
using Mooege.Core.MooNet.Friends;
using Mooege.Net.MooNet;

namespace Mooege.Core.MooNet.Services
{
    [Service(serviceID: 0x6, serviceName: "bnet.protocol.friends.FriendsService")]
    public class FriendsService : bnet.protocol.friends.FriendsService,IServerService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public MooNetClient Client { get; set; }

        public override void SubscribeToFriends(IRpcController controller, bnet.protocol.friends.SubscribeToFriendsRequest request, Action<bnet.protocol.friends.SubscribeToFriendsResponse> done)
        {
            Logger.Trace("Subscribe() {0}", this.Client);

            FriendManager.Instance.AddSubscriber(this.Client, request.ObjectId);
            
            var builder = bnet.protocol.friends.SubscribeToFriendsResponse.CreateBuilder()
                .SetMaxFriends(127)
                .SetMaxReceivedInvitations(127)
                .SetMaxSentInvitations(127);

            foreach (var friend in FriendManager.Friends[this.Client.Account.BnetAccountID.Low]) // send friends list.
            {
                builder.AddFriends(friend);
            }

            done(builder.Build());
        }

        public override void SendInvitation(IRpcController controller, bnet.protocol.invitation.SendInvitationRequest request, Action<bnet.protocol.invitation.SendInvitationResponse> done)
        {
            // somehow protobuf lib doesnt handle this extension, so we're using a workaround to get that channelinfo.
            var extensionBytes = request.UnknownFields.FieldDictionary[103].LengthDelimitedList[0].ToByteArray();
            var friendRequest = bnet.protocol.friends.SendInvitationRequest.ParseFrom(extensionBytes);

            if (friendRequest.TargetEmail.ToLower() == this.Client.Account.Email.ToLower()) return; // don't allow him to invite himself - and we should actually return an error!
                                                                                                    // also he shouldn't be allowed to invite his current friends - put that check too!. /raist
            var inviteee = AccountManager.GetAccountByEmail(friendRequest.TargetEmail);
            if (inviteee == null) return; // we need send an error response here /raist.

            Logger.Trace("{0} sent {1} friend invitation.", this.Client.Account, inviteee);

            var invitation = bnet.protocol.invitation.Invitation.CreateBuilder()
                .SetId(FriendManager.InvitationIdCounter++) // we may actually need to store invitation ids in database with the actual invitation there. /raist.                
                .SetInviterIdentity(this.Client.GetIdentity(true, false, false))
                .SetInviterName(this.Client.Account.Email) // we shoulde be instead using account owner's name here.
                .SetInviteeIdentity(bnet.protocol.Identity.CreateBuilder().SetAccountId(inviteee.BnetAccountID))
                .SetInviteeName(inviteee.Email) // again we should be instead using invitee's name.
                .SetInvitationMessage(request.InvitationMessage)
                .SetCreationTime(DateTime.Now.ToUnixTime())
                .SetExpirationTime(DateTime.Now.ToUnixTime() + request.ExpirationTime);

            var response = bnet.protocol.invitation.SendInvitationResponse.CreateBuilder()
                .SetInvitation(invitation.Clone());

            done(response.Build());

            // notify the invitee on invitation.
            FriendManager.HandleInvitation(this.Client, invitation.Build());
        }

        public override void AcceptInvitation(IRpcController controller, bnet.protocol.invitation.GenericRequest request, Action<bnet.protocol.NoData> done)
        {
            Logger.Trace("{0} invited friend invitation.", this.Client.Account);

            var response = bnet.protocol.NoData.CreateBuilder();
            done(response.Build());

            FriendManager.HandleAccept(this.Client, request);
        }

        public override void RevokeInvitation(IRpcController controller, bnet.protocol.invitation.GenericRequest request, Action<bnet.protocol.NoData> done)
        {
            throw new NotImplementedException();
        }

        public override void DeclineInvitation(IRpcController controller, bnet.protocol.invitation.GenericRequest request, Action<bnet.protocol.NoData> done)
        {
            throw new NotImplementedException();
        }

        public override void IgnoreInvitation(IRpcController controller, bnet.protocol.invitation.GenericRequest request, Action<bnet.protocol.NoData> done)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFriend(IRpcController controller, bnet.protocol.friends.GenericFriendRequest request, Action<bnet.protocol.friends.GenericFriendResponse> done)
        {
            throw new NotImplementedException();
        }

        public override void ViewFriends(IRpcController controller, bnet.protocol.friends.ViewFriendsRequest request, Action<bnet.protocol.friends.ViewFriendsResponse> done)
        {
            throw new NotImplementedException();
        }

        public override void UpdateFriendState(IRpcController controller, bnet.protocol.friends.UpdateFriendStateRequest request, Action<bnet.protocol.friends.UpdateFriendStateResponse> done)
        {
            throw new NotImplementedException();
        }

        public override void UnsubscribeToFriends(IRpcController controller, bnet.protocol.friends.UnsubscribeToFriendsRequest request, Action<bnet.protocol.NoData> done)
        {
            throw new NotImplementedException();
        }
    }
}
