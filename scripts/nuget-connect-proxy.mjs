/**
 * Local CONNECT proxy: map api.nuget.org -> working Azure Front Door edge.
 * Used only for TB-P05-T017-UNBLOCK-01 validation recovery (no TLS weaken).
 */
import net from "node:net";
import http from "node:http";

const LISTEN_HOST = process.env.TOOBA_NUGET_PROXY_HOST || "127.0.0.1";
const LISTEN_PORT = Number(process.env.TOOBA_NUGET_PROXY_PORT || 18888);
const NUGET_EDGE = process.env.TOOBA_NUGET_EDGE_IP || "150.171.109.34";

const server = http.createServer((req, res) => {
  res.writeHead(400);
  res.end("CONNECT proxy only");
});

server.on("connect", (req, clientSocket, head) => {
  const [host, portText] = String(req.url || "").split(":");
  const port = Number(portText || 443);
  const targetHost = host === "api.nuget.org" ? NUGET_EDGE : host;
  const upstream = net.connect(port, targetHost, () => {
    clientSocket.write("HTTP/1.1 200 Connection Established\r\n\r\n");
    if (head?.length) upstream.write(head);
    upstream.pipe(clientSocket);
    clientSocket.pipe(upstream);
  });
  upstream.on("error", () => clientSocket.destroy());
  clientSocket.on("error", () => upstream.destroy());
});

server.listen(LISTEN_PORT, LISTEN_HOST, () => {
  console.log(`nuget-connect-proxy listening on ${LISTEN_HOST}:${LISTEN_PORT} edge=${NUGET_EDGE}`);
});
