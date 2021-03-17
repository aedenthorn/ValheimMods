namespace CustomServerLoadingScreen
{
    internal class LoadingScreenData
    {
        public string screen = "";
        public string tip = "";

        public LoadingScreenData(string data)
        {
            string[] parts = data.Split('^');
            screen = parts[0];
            if (parts.Length == 2)
                tip = parts[1];
        }
    }
}