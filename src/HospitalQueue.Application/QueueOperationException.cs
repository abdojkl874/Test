namespace HospitalQueue.Application;

/// <summary>Thrown for invalid queue operations (e.g. calling next with no one waiting, wrong ticket state) so UI can show a friendly Arabic message instead of a stack trace.</summary>
public class QueueOperationException : Exception
{
    public QueueOperationException(string message) : base(message)
    {
    }
}
