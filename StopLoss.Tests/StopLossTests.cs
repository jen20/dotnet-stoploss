using System.Linq;
using Xunit;

namespace StopLoss.Tests
{
    public class StopLossTests
    {
        private FakeMessageBus bus;
        private StopLossManager stopLossManager;

        public StopLossTests()
        {
            bus = new FakeMessageBus();
            stopLossManager = new StopLossManager(bus);   
        }

        [Fact]
        public void WhenPositionAcquiredThenTargetUpdated()
        {
            var  positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var events = bus.GetMessages();
            var targetUpdated = events.First() as TargetUpdated;

            Assert.NotNull(targetUpdated);
            Assert.Equal(targetUpdated.TargetPrice, 0.9M);
        }

        [Fact]
        public void WhenPriceUpdatedWithinTenSecondsThenRaisesTwoSendInX()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated {Price = 1.5M};
            stopLossManager.Consume(priceUpdated);

            var events = bus.GetMessages().OfType<SendToMeInX<PriceUpdated>>().ToArray();

            Assert.Equal(events.Length, 2);
            var sendToMeInX1 = events.First();
            var sendToMeInX2 = events.Last();


            Assert.NotNull(sendToMeInX1);
            Assert.Equal(sendToMeInX1.Message, priceUpdated);
            Assert.Equal(sendToMeInX1.X, 10.0M);

            Assert.NotNull(sendToMeInX2);
            Assert.Equal(sendToMeInX2.Message, priceUpdated);
            Assert.Equal(sendToMeInX2.X, 7.0M);
        }

        [Fact]
        public void WhenPriceSustainedForLongerThan10SecondsThenTargetUpdated()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 1.5M };
            stopLossManager.Consume(priceUpdated);
            var sendToMeInX = new SendToMeInX<PriceUpdated>(10.0M, priceUpdated);
            stopLossManager.Consume(sendToMeInX);

            var targetUpdated = bus.GetLastMessage<TargetUpdated>();

            Assert.NotNull(targetUpdated);
            Assert.Equal(targetUpdated.TargetPrice, 1.4M);
        }

        [Fact]
        public void WhenPriceNotSustainedForLongerThan10SecondsThenTargetNotUpdated()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 0.8M };
            stopLossManager.Consume(priceUpdated);

            bus.Clear();

            priceUpdated = new PriceUpdated { Price = 1.5M };
            var sendToMeInX = new SendToMeInX<PriceUpdated>(10.0M, priceUpdated);
            stopLossManager.Consume(sendToMeInX);

            var targetUpdated = bus.GetLastMessage<TargetUpdated>();

            Assert.Null(targetUpdated);
        }

        [Fact]
        public void WhenPriceGoesUpAndThenDownThenTakesTheMinimumSustainedIncrease()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 1.5M };
            stopLossManager.Consume(priceUpdated);

            priceUpdated = new PriceUpdated { Price = 1.2M };
            stopLossManager.Consume(priceUpdated);

            bus.Clear();

            priceUpdated = new PriceUpdated { Price = 1.5M };
            var sendToMeInX = new SendToMeInX<PriceUpdated>(10.0M, priceUpdated);
            stopLossManager.Consume(sendToMeInX);

            var targetUpdated = bus.GetLastMessage<TargetUpdated>();

            Assert.NotNull(targetUpdated);
            Assert.Equal(targetUpdated.TargetPrice, 1.1M);
        }

        [Fact]
        public void WhenPriceGoesDownAndIsSustainedForLongerThan7SecondsThenTriggerStopLoss()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 0.89M };
            stopLossManager.Consume(priceUpdated);

            var sendToMeInX = new SendToMeInX<PriceUpdated>(7.0M, priceUpdated);
            stopLossManager.Consume(sendToMeInX);

            var stopLossTriggered = bus.GetLastMessage<StopLossTriggered>();

            Assert.NotNull(stopLossTriggered);
        }

        [Fact]
        public void WhenPriceGoesDownAndIsNotSustainedForLongerThan7SecondsThenDoesntTriggerStopLoss()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 0.89M };
            stopLossManager.Consume(priceUpdated);

            var priceUpdated2 = new PriceUpdated { Price = 0.91M };
            stopLossManager.Consume(priceUpdated2);

            var sendToMeInX = new SendToMeInX<PriceUpdated>(7.0M, priceUpdated);
            stopLossManager.Consume(sendToMeInX);

            var stopLossTriggered = bus.GetLastMessage<StopLossTriggered>();

            Assert.Null(stopLossTriggered);
        }


        [Fact]
        public void WhenPriceUpdatedAfterThresholdThenTargetUpdatedWithNewPriceWithoutObsoletePricesAffectingIt()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 1.0M };
            stopLossManager.Consume(priceUpdated);

            var priceUpdated2 = new PriceUpdated { Price = 1.2M };
            stopLossManager.Consume(priceUpdated2);

            bus.Clear();

            stopLossManager.Consume(new SendToMeInX<PriceUpdated>(10.0M, priceUpdated));
            stopLossManager.Consume(new SendToMeInX<PriceUpdated>(10.0M, priceUpdated2));

            var targetUpdated = bus.GetLastMessage<TargetUpdated>();

            Assert.NotNull(targetUpdated);
            Assert.Equal(targetUpdated.TargetPrice, 1.1M);
        }

        [Fact]
        public void AfterSellingDoesntSellAgain()
        {
            var positionAcquired = new PositionAcquired(1.0M);
            stopLossManager.Consume(positionAcquired);

            var priceUpdated = new PriceUpdated { Price = 0.89M };
            stopLossManager.Consume(priceUpdated);

            var priceUpdated2 = new PriceUpdated { Price = 0.5M };
            stopLossManager.Consume(priceUpdated);

            stopLossManager.Consume(new SendToMeInX<PriceUpdated>(7.0M, priceUpdated));

            bus.Clear();
            stopLossManager.Consume(new SendToMeInX<PriceUpdated>(7.0M, priceUpdated2));

            var stopLossTriggered = bus.GetLastMessage<StopLossTriggered>();

            Assert.Null(stopLossTriggered);
        }
    }
}
