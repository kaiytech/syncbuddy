namespace SyncBuddyLib;

public class SyncChangedEventArgs(SyncStatus previousStatus, SyncStatus currentStatus) : EventArgs
{
    public readonly SyncStatus PreviousStatus = previousStatus;
    public readonly SyncStatus CurrentStatus = currentStatus;
}