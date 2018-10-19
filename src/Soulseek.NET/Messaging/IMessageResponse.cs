namespace Soulseek.NET.Messaging
{
    public interface IMessageResponse<T>
    {
        T MapFrom(Message message);
    }
}
