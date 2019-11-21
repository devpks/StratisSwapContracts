using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;

namespace CirrusSwap.Tests
{
    public class SellOfferTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Mock<IInternalTransactionExecutor> MockInternalExecutor;
        private readonly Address Owner;
        private readonly Address TakerOne;
        private readonly Address TakerTwo;
        private readonly Address Token;
        private readonly Address ContractAddress;
        private readonly ulong Amount;
        private readonly ulong Price;
        private readonly bool IsActive;
        private readonly string ContractType;

        public SellOfferTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            MockContractState.Setup(x => x.InternalTransactionExecutor).Returns(MockInternalExecutor.Object);
            Owner = "0x0000000000000000000000000000000000000001".HexToAddress();
            TakerOne = "0x0000000000000000000000000000000000000002".HexToAddress();
            TakerTwo = "0x0000000000000000000000000000000000000003".HexToAddress();
            Token = "0x0000000000000000000000000000000000000004".HexToAddress();
            ContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private SellOffer NewSellOffer(Address sender, ulong value, string contractType, Address token, ulong amount, ulong price)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, sender, value));
            MockPersistentState.Setup(x => x.GetAddress(nameof(Owner))).Returns(Owner);
            MockPersistentState.Setup(x => x.GetString(nameof(ContractType))).Returns(contractType);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Token))).Returns(token);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Price))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new SellOffer(MockContractState.Object, token, amount, price);
        }

        [Theory]
        [InlineData(0, 10_000_000, 5_000_000_000, "sell")]
        [InlineData(500_000_000, 10_000_000, 5_000_000_000, "buy")]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount, string type)
        {
            var trade = NewSellOffer(Owner, value, type, Token, amount, price);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Owner), Owner));
            Assert.Equal(Owner, trade.Owner);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(Token), Token));
            Assert.Equal(Token, trade.Token);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Price), price));
            Assert.Equal(price, trade.Price);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), amount));
            Assert.Equal(amount, trade.Amount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true));
            Assert.Equal(true, trade.IsActive);

            MockPersistentState.Verify(x => x.SetString(nameof(ContractType), type));
            Assert.Equal(type, trade.ContractType);
        }

        [Theory]
        [InlineData(5_000_000_000, 10_000_000, 500_000_000)]    // 50 .1 5
        [InlineData(250_000_000_000, 40_000_000, 100_000_000_000)]    // 2500 .4 1000
        // [InlineData(5_000_000_000, 10_000_000, 500_000_000)]
        public void Calculates_Cost_Correctly(ulong amount, ulong price, ulong expectedCost)
        {
            const ulong oneFullToken = 100_000_000;
            double test = (double)oneFullToken / (double)price;
            double remaidner = oneFullToken % price;

            var calc = amount / (oneFullToken / price);

            Assert.Equal(expectedCost, calc);
        }
    }
}
