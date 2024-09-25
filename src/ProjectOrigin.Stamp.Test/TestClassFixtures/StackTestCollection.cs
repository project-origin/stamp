
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using Xunit;

[CollectionDefinition("EntireStackCollection")]
public class EntireStackCollection : ICollectionFixture<EntireStackFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
