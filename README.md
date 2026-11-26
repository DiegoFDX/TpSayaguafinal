# TP Sayagua - Chat TCP en C#

Chat de consola cliente-servidor TCP con autenticación por token, cifrado AES-GCM (confidencialidad e integridad), mensajes grupales, manejo de desconexiones y prueba de conexiones local limitada.

## Requisitos
- .NET 8 SDK
- Windows 10/11 (también puede ejecutarse en otros sistemas con .NET 8)

## Ejecutar
1. Instalar .NET 8 SDK: https://dotnet.microsoft.com/download
2. Clonar este repo y abrir una terminal en la carpeta.
3. Compilar: `dotnet build`
4. En una terminal iniciar servidor: `dotnet run -- server`
5. En otras terminales: `dotnet run -- client 127.0.0.1` (repetir para más clientes).

Todos deben usar el mismo token. Por defecto se usa un token de práctica (`TP-Sayagua-local`). Para cambiarlo, establecer la variable de entorno `CHAT_TOKEN` antes de ejecutar. **No usar el token de práctica fuera de pruebas.**

## Prueba de carga local
`dotnet run -- loadtest 127.0.0.1 8080 20` realiza un máximo de 20 intentos de conexión secuenciales y los cierra. Úsalo únicamente en tu propia máquina o entorno autorizado.

## Capturas
Ver [CAPTURAS.md](CAPTURAS.md). La guía explica qué evidencia real obtener. Wireshark podrá mostrar tráfico cifrado de aplicación tras autenticación, pero los metadatos TCP siguen siendo visibles.

## Limitaciones
Este proyecto no implementa TLS con certificados: AES-GCM protege los mensajes de aplicación, pero la autenticación inicial por token se transmite en texto y la clave se deriva del token compartido. Para redes no confiables se recomienda TLS y un mecanismo de autenticación más robusto. La prueba de carga está limitada a 20 conexiones y es local/experimental.
