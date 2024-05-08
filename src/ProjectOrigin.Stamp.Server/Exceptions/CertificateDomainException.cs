using System;

namespace ProjectOrigin.Stamp.Server.Exceptions;

[Serializable]
public class CertificateDomainException : Exception
{
    public CertificateDomainException(Guid certificateId, string message) : base(message)
    {
    }

    public CertificateDomainException(string message) : base(message)
    {
    }
}
