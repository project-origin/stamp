using System;

namespace ProjectOrigin.Stamp.Server.Exceptions;

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}
