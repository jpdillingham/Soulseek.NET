namespace Soulseek.NET.Messaging
{
    public interface IMessageMap<T>
    {
        T MapFrom(Message message);
    }
}
