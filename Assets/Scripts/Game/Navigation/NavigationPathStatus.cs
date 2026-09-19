namespace Game.Navigation
{
    public enum NavigationPathStatus
    {
        Success = 0,
        InvalidStart = 1,
        InvalidDestination = 2,
        DestinationBlocked = 3,
        NoValidRoute = 4,

        MaxStepsReached = 5,
        MissingDependencies = 6
    }
}
