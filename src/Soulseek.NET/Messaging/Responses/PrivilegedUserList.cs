namespace Soulseek.NET.Messaging.Responses
{
    using System.Collections.Generic;

    public static class PrivilegedUserList
    {
        public static IEnumerable<string> Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerPrivilegedUsers)
            {
                throw new MessageException($"Message Code mismatch creating Privileged Users response (expected: {(int)MessageCode.ServerPrivilegedUsers}, received: {(int)reader.Code}");
            }

            var count = reader.ReadInteger();
            var list = new List<string>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }

            return list;
        }
    }
}
