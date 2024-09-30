using System;

namespace ProjectOrigin.Stamp.Exceptions;

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}
