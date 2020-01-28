namespace RemCon
{
    public class RCon
    {
        public PrimS.Telnet.Client client;

        public void Echo(string s)
        {
            client.WriteLine($"echo [Loudmouth] - {s}\n");
        }

        public void Run(string s)
        {
            if (!Utils.settings.state) return;
            client.WriteLine($"{s}\n");
        }

        public bool isAlive()
        {
            return client.IsConnected;
        }
        
        public RCon(string addr, int port)
        {
            client = new PrimS.Telnet.Client(addr, port, new System.Threading.CancellationToken()); // blocking
        }

    }
}