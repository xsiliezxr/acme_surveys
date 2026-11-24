using System;
using System.Collections.Generic;
using AcmeSurveys.Infrastructure.Security;
using Xunit;

namespace AcmeSurveys.UnitTests.Infrastructure
{
    public class CryptoRandomIdentifierGeneratorTests
    {
        private const int Samples = 1000;
        private readonly CryptoRandomIdentifierGenerator _generator = new CryptoRandomIdentifierGenerator();

        [Fact]
        public void NewRandomUuid_ManyCalls_ProducesRfcVersion4Identifiers()
        {
            for (var i = 0; i < Samples; i++)
            {
                var text = _generator.NewRandomUuid().ToString();

                Assert.Equal('4', text[14]);
                Assert.Contains(text[19], "89ab");
            }
        }

        [Fact]
        public void NewRandomUuid_ManyCalls_ProducesUniqueIdentifiers()
        {
            var ids = new HashSet<Guid>();

            for (var i = 0; i < Samples; i++)
            {
                ids.Add(_generator.NewRandomUuid());
            }

            Assert.Equal(Samples, ids.Count);
        }
    }
}
