# Guía de evidencias (capturas reales)

1. **Servidor**: terminal con `dotnet run -- server` mostrando escucha en el puerto 8080.
2. **Chat con 2 clientes**: iniciar dos terminales con `dotnet run -- client 127.0.0.1`, poner apodos y enviar mensajes; capturar servidor y clientes.
3. **Desconexión**: cerrar un cliente y capturar el aviso de desconexión en el otro.
4. **Wireshark**: capturar en interfaz Loopback (Npcap) o interfaz local disponible, filtro `tcp.port == 8080`. Capturar el establecimiento TCP y la transferencia de mensajes. Tras autenticación, las tramas de aplicación se cifran con AES-GCM; no se puede leer su contenido como texto.
5. **Autenticación**: configurar token común en terminales mediante `CHAT_TOKEN`. Token incorrecto debe impedir ingresar. Evitar mostrar tokens reales en capturas.
6. **Prueba de carga controlada**: servidor activo y ejecutar `dotnet run -- loadtest 127.0.0.1 8080 20`. Capturar el resultado. No ejecutar sobre sistemas ajenos.
7. **Firewall**: para la demostración de Windows, mostrar reglas de Firewall de Windows Defender. No crear una regla de exposición permanente sin autorización.
8. **Certificado/TLS y VPN**: este prototipo no implementa TLS ni VPN. Esas demostraciones requieren configuración adicional y no deben presentarse como realizadas por esta app.

Las capturas deben mostrar el funcionamiento real de la aplicación; no usar imágenes simuladas como evidencia de ejecución.
