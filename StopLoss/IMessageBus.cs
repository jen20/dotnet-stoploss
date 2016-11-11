namespace StopLoss
{
    public interface IMessageBus
    {
        void Publish(IMessage msg);
    }
}