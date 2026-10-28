namespace Api.Security;

public enum SsnValidationError
{
    None,
    Empty,
    InvalidFormat,
    InvalidArea,
    InvalidGroup,
    InvalidSerial,
    KnownInvalid
}
