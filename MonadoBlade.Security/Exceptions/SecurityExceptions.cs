namespace MonadoBlade.Security.Exceptions;

/// <summary>
/// Base exception for MonadoBlade.Security operations
/// </summary>
public class MonadoBladeSecurityException : Exception
{
    public MonadoBladeSecurityException() { }
    public MonadoBladeSecurityException(string message) : base(message) { }
    public MonadoBladeSecurityException(string message, Exception innerException) : base(message, innerException) { }
}

public class VhdxOperationException : MonadoBladeSecurityException
{
    public VhdxOperationException(string message) : base(message) { }
    public VhdxOperationException(string message, Exception innerException) : base(message, innerException) { }
}

public class VhdxMountException : VhdxOperationException
{
    public VhdxMountException(string message) : base(message) { }
    public VhdxMountException(string message, Exception innerException) : base(message, innerException) { }
}

public class VhdxUnmountException : VhdxOperationException
{
    public VhdxUnmountException(string message) : base(message) { }
    public VhdxUnmountException(string message, Exception innerException) : base(message, innerException) { }
}

public class VhdxCreationException : VhdxOperationException
{
    public VhdxCreationException(string message) : base(message) { }
    public VhdxCreationException(string message, Exception innerException) : base(message, innerException) { }
}

public class BitLockerException : MonadoBladeSecurityException
{
    public BitLockerException(string message) : base(message) { }
    public BitLockerException(string message, Exception innerException) : base(message, innerException) { }
}

public class BitLockerEncryptionException : BitLockerException
{
    public BitLockerEncryptionException(string message) : base(message) { }
    public BitLockerEncryptionException(string message, Exception innerException) : base(message, innerException) { }
}

public class TpmException : MonadoBladeSecurityException
{
    public TpmException(string message) : base(message) { }
    public TpmException(string message, Exception innerException) : base(message, innerException) { }
}

public class VaultException : MonadoBladeSecurityException
{
    public VaultException(string message) : base(message) { }
    public VaultException(string message, Exception innerException) : base(message, innerException) { }
}

public class QuarantineException : MonadoBladeSecurityException
{
    public QuarantineException(string message) : base(message) { }
    public QuarantineException(string message, Exception innerException) : base(message, innerException) { }
}

public class EncryptionException : MonadoBladeSecurityException
{
    public EncryptionException(string message) : base(message) { }
    public EncryptionException(string message, Exception innerException) : base(message, innerException) { }
}
