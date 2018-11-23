using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;

namespace SocketServer
{
    public partial class Server : Form
    {
        int numeroPorta = 8000;
        TcpListener tcpListener;
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thInteraction;

        public Server()
        {
            InitializeComponent();
        }

        private bool Connect()
        {
            bool retorno = false;

            try
            {
                tcpListener = new TcpListener(System.Net.IPAddress.Any, numeroPorta);
                tcpListener.Start();
                retorno = true;
            }
            catch
            {

            }

            return retorno;
        }

        private void Disconnect()
        {
            if (thInteraction != null)
            {
                if (thInteraction.ThreadState == ThreadState.Running)
                {
                    thInteraction.Abort();
                }
            }

            if (tcpClient != null)
            {
                tcpClient.Client.Disconnect(true);
            }

            tcpListener.Stop();

            SetMsg("## Conexões perdidas...", true);
        }

        private void AcceptConnection()
        {
            try
            {
                tcpClient = tcpListener.AcceptTcpClient();
            }
            catch
            {

            }
        }

        private void EnviarMsg(string mensagem)
        {
            if (PodeEscrever())
            {
                byte[] sendBytes = Encoding.ASCII.GetBytes(mensagem);
                networkStream.Write(sendBytes, 0, sendBytes.Length);
            }
        }

        private bool PodeEscrever()
        {
            return (networkStream == null || !(networkStream.CanWrite && tcpClient != null)) ? false : true;
        }

        delegate void delSetMsg(string mensagem, bool burlar);
        private void SetMsg(string mensagem, bool burlar)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new delSetMsg(SetMsg), mensagem, burlar);
            }
            else
            {
                if (burlar || PodeEscrever())
                {
                    rtbConversa.Text += "\nEu: " + mensagem;
                }
            }
        }

        delegate void delGetMsg(string mensagem);
        private void GetMsg(string mensagem)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new delGetMsg(GetMsg), mensagem);
            }
            else
            {
                if (PodeEscrever())
                {
                    rtbConversa.Text += "\nClient: " + mensagem;
                }
            }
        }

        private void Start()
        {
            if (Connect())
            {
                SetMsg("## Aguardando uma conexão...", true);
            }

            thInteraction = new Thread(new ThreadStart(Interaction));
            thInteraction.IsBackground = true;
            thInteraction.Priority = ThreadPriority.Highest;
            thInteraction.Name = "thInteraction";
            thInteraction.Start();
        }

        private void Interaction()
        {
            try
            {
                AcceptConnection();
                SetMsg("## Conexão aceita...", true);

                do
                {
                    networkStream = tcpClient.GetStream();

                    if (networkStream.CanRead)
                    {
                        byte[] bytes = new byte[tcpClient.ReceiveBufferSize];
                        networkStream.Read(bytes, 0, Convert.ToInt32(tcpClient.ReceiveBufferSize));

                        string clientData = Encoding.ASCII.GetString(bytes);

                        if (!clientData.Replace("\0", "").Trim().Equals(""))
                        {
                            GetMsg(clientData);
                        }
                        else
                        {
                            tcpClient = null;
                        }
                    }
                } while (tcpClient != null);

                Disconnect();
                Start();
            }
            catch
            {

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetMsg("## Encerrando conexão com o servidor...", true);
            Disconnect();
        }

        private void RtbMensagem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string mensagem = rtbMensagem.Text;
                EnviarMsg(mensagem);
                SetMsg(mensagem, false);
            }
        }

        private void RtbMensagem_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                rtbMensagem.Clear();
            }
        }
    }
}
