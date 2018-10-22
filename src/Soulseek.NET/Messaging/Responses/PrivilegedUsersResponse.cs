using System;
using System.Collections.Generic;
using System.Net;

namespace Soulseek.NET.Messaging.Responses
{
    public class PrivilegedUsersResponse
    {
        public IEnumerable<string> PrivilegedUsers => PrivilegedUserList;

        private int PrivilegedUserCount { get; set; }
        private List<string> PrivilegedUserList { get; set; } = new List<string>();

        public static PrivilegedUsersResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerPrivilegedUsers)
            {
                throw new MessageException($"Message Code mismatch creating Privileged Users response (expected: {(int)MessageCode.ServerPrivilegedUsers}, received: {(int)reader.Code}");
            }

            var response = new PrivilegedUsersResponse
            {
                PrivilegedUserCount = reader.ReadInteger()
            };

            for (int i = 0; i < response.PrivilegedUserCount; i++)
            {
                response.PrivilegedUserList.Add(reader.ReadString());
            }

            return response;
        }
    }
}
