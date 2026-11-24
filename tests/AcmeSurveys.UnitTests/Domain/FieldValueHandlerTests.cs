using System;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;
using Xunit;

namespace AcmeSurveys.UnitTests.Domain
{
    public class FieldValueHandlerTests
    {
        private readonly FieldValueHandlerRegistry _registry = FieldValueHandlerRegistry.CreateDefault();

        [Theory]
        [InlineData("10.50", "10.5")]
        [InlineData("-3", "-3")]
        [InlineData(" 0.000001 ", "0.000001")]
        public void TryNormalize_ValidNumber_ReturnsInvariantCanonicalForm(string raw, string expected)
        {
            var handler = _registry.Get(FieldType.Number);

            string canonical, error;
            var accepted = handler.TryNormalize(raw, out canonical, out error);

            Assert.True(accepted);
            Assert.Equal(expected, canonical);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1e5")]
        [InlineData("1,000")]
        [InlineData("0.0000001")]
        [InlineData("9999999999999999")]
        public void TryNormalize_InvalidNumber_ReturnsFalseWithMessage(string raw)
        {
            var handler = _registry.Get(FieldType.Number);

            string canonical, error;
            var accepted = handler.TryNormalize(raw, out canonical, out error);

            Assert.False(accepted);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryNormalize_IsoDate_ConvertsToTypedDate()
        {
            var handler = _registry.Get(FieldType.Date);

            string canonical, error;
            var accepted = handler.TryNormalize("2026-11-25", out canonical, out error);

            Assert.True(accepted);
            Assert.Equal(new DateTime(2026, 11, 25), handler.ToTypedValue(canonical));
        }

        [Theory]
        [InlineData("25/11/2026")]
        [InlineData("2026-02-30")]
        [InlineData("1800-01-01")]
        public void TryNormalize_InvalidOrOutOfRangeDate_ReturnsFalse(string raw)
        {
            var handler = _registry.Get(FieldType.Date);

            string canonical, error;
            var accepted = handler.TryNormalize(raw, out canonical, out error);

            Assert.False(accepted);
        }

        [Fact]
        public void TryNormalize_TextWithSpacesAndCrLf_TrimsAndUnifiesNewLines()
        {
            var handler = _registry.Get(FieldType.Text);

            string canonical, error;
            var accepted = handler.TryNormalize("  hola\r\nmundo  ", out canonical, out error);

            Assert.True(accepted);
            Assert.Equal("hola\nmundo", canonical);
        }

        [Fact]
        public void TryNormalize_TextWithControlCharacter_ReturnsFalse()
        {
            var handler = _registry.Get(FieldType.Text);

            string canonical, error;
            var accepted = handler.TryNormalize("hola\u0000", out canonical, out error);

            Assert.False(accepted);
        }

        [Fact]
        public void TryNormalize_TextOverMaxLength_ReturnsFalse()
        {
            var handler = _registry.Get(FieldType.Text);

            string canonical, error;
            var accepted = handler.TryNormalize(new string('x', TextFieldHandler.MaxLength + 1), out canonical, out error);

            Assert.False(accepted);
        }

        [Fact]
        public void Constructor_FieldTypeWithoutHandler_ThrowsInvalidOperationException()
        {
            var handlers = new IFieldValueHandler[] { new TextFieldHandler() };

            Assert.Throws<InvalidOperationException>(() => new FieldValueHandlerRegistry(handlers));
        }
    }
}
