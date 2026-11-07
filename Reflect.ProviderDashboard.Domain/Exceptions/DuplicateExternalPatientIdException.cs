namespace Reflect.ProviderDashboard.Domain.Exceptions;

public sealed class DuplicateExternalPatientIdException : Exception
{
    public DuplicateExternalPatientIdException()
        : base("That patient ID is already on this caseload.")
    {
    }
}
