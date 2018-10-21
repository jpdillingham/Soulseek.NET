namespace Soulseek.NET.Messaging.Responses
{
    [MessageResponse(MessageCode.ServerParentMinSpeed)]
    [MessageResponse(MessageCode.ServerParentSpeedRatio)]
    [MessageResponse(MessageCode.ServerWishlistInterval)]
    public class IntegerResponse : IMessageResponse<IntegerResponse>
    {
        public int Value { get; private set; }

        public IntegerResponse Map(Message message)
        {
            var reader = new MessageReader(message);
            Value = reader.ReadInteger();
            return this;
        }
    }
}
