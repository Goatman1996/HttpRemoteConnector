namespace HttpRemoteConnector
{
    public static class RemoteConnectorUtility
    {
        public static void StartListen(this IRemoteListener listener)
        {
            RemoteListenerManager.Instance.StartListener(listener);
        }
    }
}