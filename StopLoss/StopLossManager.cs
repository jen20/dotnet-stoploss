using System.Collections.Generic;
using System.Linq;

namespace StopLoss
{
    public class StopLossManager
    {
        private const decimal MoveStopLossThreshold = 10.0M;
        private const decimal TriggerStopLossThreshold = 7.0M;

        private readonly IMessageBus _bus;
        private readonly List<decimal> _priceUpdates = new List<decimal>();

        private decimal? _boughtAt;
        private decimal _currentStopTarget;

        public StopLossManager(IMessageBus bus)
        {
            _bus = bus;
        }

        public void Consume(PositionAcquired positionAcquired)
        {
            _boughtAt = positionAcquired.Price;
            RaiseTargetUpdated(positionAcquired.Price);
        }

        public void Consume(PriceUpdated priceUpdated)
        {
            if (!_boughtAt.HasValue)
                return;

            _priceUpdates.Add(priceUpdated.Price);

            _bus.Publish(new SendToMeInX<PriceUpdated>(MoveStopLossThreshold, priceUpdated));
            _bus.Publish(new SendToMeInX<PriceUpdated>(TriggerStopLossThreshold, priceUpdated));
        }

        public void Consume(SendToMeInX<PriceUpdated> msg)
        {
            if (!_boughtAt.HasValue)
                return;

            var minPrice = _priceUpdates.Any() ? _priceUpdates.Min() : _boughtAt.Value;
            if (minPrice > _currentStopTarget)
            {
                RaiseTargetUpdated(minPrice);   
            }

            var maxPrice = _priceUpdates.Any() ? _priceUpdates.Max() : _boughtAt.Value;
            if (maxPrice < _currentStopTarget)
            {
                _boughtAt = null;
                _priceUpdates.Clear();
                _bus.Publish(new StopLossTriggered());
            }
            _priceUpdates.Remove(msg.Message.Price);
        }

        private void RaiseTargetUpdated(decimal price)
        {
            _currentStopTarget = price - StopLossThreshold;
            var targetUpdated = new TargetUpdated(_currentStopTarget);
            _bus.Publish(targetUpdated); 
        }

        public const decimal StopLossThreshold = 0.1m;
    }
}