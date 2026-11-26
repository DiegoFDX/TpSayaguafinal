using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

const int Port = 8080;
const int MaxClients = 20;
string token = Environment.GetEnvironmentVariable("CHAT_TOKEN") ?? "TP-Sayagua-local";

if (args.Length == 0 || args[0] == "help") { Help(); return; }
switch (args[0].ToLowerInvariant())
{
    case "server": await RunServer(Port, MaxClients, token); break;
    case "client":
        string host = args.Length > 1 ? args[1] : "127.0.0.1";
        await RunClient(host, Port, token);
        break;
    case "loadtest":
        string target = args.Length > 1 ? args[1] : "127.0.0.1";
        int port = args.Length > 2 && int.TryParse(args[2], out int p) ? p : Port;
        int count = args.Length > 3 && int.TryParse(args[3], out int n) ? Math.Clamp(n, 1, MaxClients) : 10;
        await LoadTest(target, port, count, token);
        break;
    default: Help(); break;
}

static void Help() {
 Console.WriteLine("Modos: server | client [host] | loadtest [host] [puerto] [cantidad<=20]");
}

static byte[] KeyFromToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

static async Task WriteFrame(Stream stream, string text, byte[] key) {
 byte[] nonce = RandomNumberGenerator.GetBytes(12), plain = Encoding.UTF8.GetBytes(text), cipher = new byte[plain.Length], tag = new byte[16];
 using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plain, cipher, tag);
 byte[] payload = nonce.Concat(tag).Concat(cipher).ToArray();
 byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
 await stream.WriteAsync(header); await stream.WriteAsync(payload); await stream.FlushAsync();
}
static async Task<string?> ReadFrame(Stream stream, byte[] key) {
 byte[] h = new byte[4]; if (!await ReadExact(stream,h)) return null;
 int len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(h));
 if (len < 28 || len > 65536) throw new IOException("Tamaño de trama inválido.");
 byte[] payload = new byte[len]; if (!await ReadExact(stream,payload)) return null;
 byte[] nonce=payload[..12], tag=payload[12..28], cipher=payload[28..], plain=new byte[cipher.Length];
 using var aes=new AesGcm(key,16); aes.Decrypt(nonce,cipher,tag,plain);
 return Encoding.UTF8.GetString(plain);
}
static async Task<bool> ReadExact(Stream s, byte[] b) {
 int off=0; while(off<b.Length) { int n=await s.ReadAsync(b.AsMemory(off)); if(n==0)return false; off+=n; } return true;
}
static async Task RunServer(int port, int max, string token) {
 var listener=new TcpListener(IPAddress.Any,port); listener.Start(max);
 Console.WriteLine($"Servidor TCP escuchando en {port}. Límite: {max}. Ctrl+C para terminar.");
 var clients=new List<TcpClient>(); var gate=new SemaphoreSlim(max,max);
 try {
  while(true) {
   var tcp=await listener.AcceptTcpClientAsync();
   if(!await gate.WaitAsync(0)){tcp.Close();Console.WriteLine("Rechazo: límite de clientes.");continue;}
   lock(clients) clients.Add(tcp);
   _=Task.Run(async()=>{try{await Handle(tcp,clients,gate,token);}catch(Exception ex){Console.WriteLine($"Cliente: {ex.Message}");}});
  }
 } finally {listener.Stop();}
}
static async Task Handle(TcpClient tcp,List<TcpClient> clients,SemaphoreSlim gate,string token) {
 string? nick=null; var stream=tcp.GetStream(); byte[] key=KeyFromToken(token);
 try {
  using var reader=new StreamReader(stream,Encoding.UTF8,leaveOpen:true);
  using var writer=new StreamWriter(stream,Encoding.UTF8,leaveOpen:true){AutoFlush=true};
  string? submitted=await reader.ReadLineAsync();
  if(submitted!=token){await writer.WriteLineAsync("AUTH_FAIL");return;}
  await writer.WriteLineAsync("AUTH_OK");
  nick=await reader.ReadLineAsync(); if(string.IsNullOrWhiteSpace(nick))nick="Usuario";
  Console.WriteLine($"{nick} conectado.");
  await Broadcast(clients,key,$"*** {nick} se unió ***");
  while(true){string? msg=await ReadFrame(stream,key);if(msg==null)break; if(msg.Length>2000)continue;Console.WriteLine($"{nick}: {msg}");await Broadcast(clients,key,$"{nick}: {msg}",tcp);}
 } catch(IOException){} catch(SocketException){} finally {
  tcp.Close();lock(clients)clients.Remove(tcp);gate.Release();
  if(nick!=null)await Broadcast(clients,key,$"*** {nick} se desconectó ***");
 }
}
static async Task Broadcast(List<TcpClient> clients,byte[] key,string message,TcpClient? except=null) {
 TcpClient[] snapshot;lock(clients)snapshot=clients.ToArray();
 foreach(var c in snapshot){if(c==except)continue;try{await WriteFrame(c.GetStream(),message,key);}catch{try{c.Close();}catch{}}}
}
static async Task RunClient(string host,int port,string token) {
 using var tcp=new TcpClient(); await tcp.ConnectAsync(host,port);var stream=tcp.GetStream();
 using var reader=new StreamReader(stream,Encoding.UTF8,leaveOpen:true);using var writer=new StreamWriter(stream,Encoding.UTF8,leaveOpen:true){AutoFlush=true};
 await writer.WriteLineAsync(token);string? auth=await reader.ReadLineAsync();if(auth!="AUTH_OK"){Console.WriteLine("Token incorrecto.");return;}
 Console.Write("Apodo: ");string nick=Console.ReadLine()??"Usuario";await writer.WriteLineAsync(nick);
 byte[] key=KeyFromToken(token);
 var receiving=Task.Run(async()=>{try{while(true){var m=await ReadFrame(stream,key);if(m==null)break;Console.WriteLine("\n"+m);}}catch(Exception e){Console.WriteLine("Recepción finalizada: "+e.Message);}});
 Console.WriteLine("Chat conectado. Escribe mensajes (Ctrl+C para salir).");
 while(true){string? msg=Console.ReadLine();if(msg==null)break;if(msg.Trim().Length==0)continue;await WriteFrame(stream,msg,key);}
}
static async Task LoadTest(string host,int port,int count,string token) {
 if(host!="127.0.0.1" && host!="localhost" && host!="::1"){Console.WriteLine("La prueba está restringida a localhost.");return;}
 Console.WriteLine($"Prueba local: {count} conexiones máximo.");
 for(int i=0;i<count;i++){try{using var c=new TcpClient();await c.ConnectAsync(host,port);using var r=new StreamReader(c.GetStream());using var w=new StreamWriter(c.GetStream()){AutoFlush=true};await w.WriteLineAsync(token);var res=await r.ReadLineAsync();Console.WriteLine($"Conexión {i+1}: {res}");}catch(Exception ex){Console.WriteLine($"Intento {i+1}: {ex.Message}");}}
 Console.WriteLine("Prueba terminada.");
}
