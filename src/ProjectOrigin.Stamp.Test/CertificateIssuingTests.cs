using ProjectOrigin.Stamp.Server;
using ProjectOrigin.Stamp.Test.TestClassFixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProjectOrigin.Stamp.Test;

public class CertificateIssuingTests : IClassFixture<TestServerFixture<Startup>>, IClassFixture<PostgresDatabaseFixture>
{
    private readonly TestServerFixture<Startup> _fixture;

    public CertificateIssuingTests(TestServerFixture<Startup> fixture, PostgresDatabaseFixture postgres)
    {
        _fixture = fixture;
        fixture.PostgresConnectionString = postgres.ConnectionString;
    }
}
