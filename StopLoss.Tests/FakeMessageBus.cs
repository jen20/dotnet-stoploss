using System.Collections.Generic;
using System.Linq;

namespace StopLoss.Tests
{
    public class FakeMessageBus : IMessageBus
    {
        private readonly IList<IMessage> messages = new List<IMessage>();

        public void Publish(IMessage message)
        {
            messages.Add(message);
        }

        public IMessage[] GetMessages()
        {
            return messages.ToArray();
        }

        public T GetLastMessage<T>()
        {
            return messages.OfType<T>().LastOrDefault();
        }

        public void Clear()
        {
            messages.Clear();
        }
    }
}