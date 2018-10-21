namespace Soulseek.NET.Messaging
{
    public interface IMessageResponse<T>
    {
        T Map(Message message);
    }
}
