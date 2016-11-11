using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopLoss
{
    public interface IMessage
    {
            
    }

    public class PositionAcquired
    {
        public PositionAcquired(decimal price)
        {
            Price = price;
        }

        public decimal Price { get; private set; }
    }

    public class PriceUpdated : IEvent
    {
        public decimal Price { get; set; }
    }

    public class SendToMeInX<T> : IMessage
    {
        public SendToMeInX(decimal x, T message)
        {
            X = x;
            Message = message;
        }

        public decimal X { get; set; }
        public T Message { get; set; }
    }

    public interface IEvent : IMessage
    {
    }

    public class TargetUpdated : IEvent
    {
        private readonly decimal _targetPrice;

        public TargetUpdated(decimal targetPrice)
        {
            _targetPrice = targetPrice;
        }

        public decimal TargetPrice { get { return _targetPrice; }}
    }

    public class StopLossTriggered : IEvent
    {
        
    }
}
