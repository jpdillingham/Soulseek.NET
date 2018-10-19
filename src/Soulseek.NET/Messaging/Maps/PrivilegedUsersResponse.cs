using System;
using System.Collections.Generic;
using System.Net;

namespace Soulseek.NET.Messaging.Maps
{
    [MessageMap(MessageCode.PrivilegedUsers)]
    public class PrivilegedUsersResponse : IMessageMap<PrivilegedUsersResponse>
    {
        public IEnumerable<string> PrivilegedUsers => PrivilegedUserList;

        private int PrivilegedUserCount { get; set; }
        private List<string> PrivilegedUserList { get; set; } = new List<string>();

        public PrivilegedUsersResponse MapFrom(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PrivilegedUsers)
            {
                throw new MessageException($"Message Code mismatch creating Privileged Users response (expected: {(int)MessageCode.PrivilegedUsers}, received: {(int)reader.Code}");
            }

            PrivilegedUserCount = reader.ReadInteger();

            for (int i = 0; i < PrivilegedUserCount; i++)
            {
                PrivilegedUserList.Add(reader.ReadString());
            }

            return this;
        }
    }
}
