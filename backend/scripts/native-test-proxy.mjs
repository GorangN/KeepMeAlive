import { timingSafeEqual } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { connect } from 'node:net';
import { pathToFileURL } from 'node:url';

export function stagingTunnelHosts(config) {
  const staging = config.staging;
  const issuer = 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth';
  const api = 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/';
  if (staging?.projectId !== 'sweet-truth-01118709' || staging.branchId !== 'br-red-flower-b9wmbkmb' || staging.issuer !== issuer || staging.apiBaseUrl !== api) {
    throw new Error('Only the isolated KeepMeAlive staging root is allowed.');
  }
  return new Set([new URL(issuer).hostname, new URL(api).hostname]);
}

export function parseConnectTarget(authority, allowedHosts) {
  const match = /^([a-z0-9.-]+):443$/i.exec(authority ?? '');
  const host = match?.[1]?.toLowerCase();
  return host && allowedHosts.has(host) ? { host, port: 443 } : undefined;
}

export function createNativeTestProxy(config, controlKey, openConnection = connect) {
  const allowedHosts = stagingTunnelHosts(config);
  if (typeof controlKey !== 'string' || !/^[A-Za-z0-9_-]{32,128}$/.test(controlKey) || new Set(controlKey).size < 16) {
    throw new Error('Supply an unpredictable random control key through NATIVE_TEST_PROXY_KEY.');
  }
  const expectedAuthorization = Buffer.from('Bearer ' + controlKey);
  const tunnels = new Set();
  let enabled = true;
  let connectionCount = 0;
  let closing = false;
  const health = () => ({ enabled, connectionCount, activeTunnels: tunnels.size });
  const destroyTunnels = () => {
    for (const tunnel of tunnels) { tunnel.client.destroy(); tunnel.upstream.destroy(); }
    tunnels.clear();
  };
  const server = createServer((request, response) => {
    response.setHeader('Cache-Control', 'no-store');
    response.setHeader('Content-Type', 'application/json');
    if (request.method === 'GET' && request.url === '/healthz') { response.end(JSON.stringify(health())); return; }
    if (!['/enable', '/disable'].includes(request.url)) { response.writeHead(404).end('{}'); return; }
    if (request.method !== 'POST') { response.writeHead(405, { Allow: 'POST' }).end('{}'); return; }
    const received = Buffer.from(request.headers.authorization ?? '');
    if (received.length !== expectedAuthorization.length || !timingSafeEqual(received, expectedAuthorization)) { response.writeHead(401).end('{}'); return; }
    enabled = request.url === '/enable' && !closing;
    if (!enabled) { destroyTunnels(); }
    response.end(JSON.stringify(health()));
  });
  server.requestTimeout = 5000;
  server.headersTimeout = 5000;
  server.maxHeadersCount = 20;
  server.on('clientError', (_error, socket) => { socket.end('HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n'); });
  server.on('connect', (request, client, head) => {
    connectionCount++;
    const target = parseConnectTarget(request.url, allowedHosts);
    if (!target || !enabled || closing || tunnels.size >= 64) {
      const status = target ? '503 Service Unavailable' : '403 Forbidden';
      client.end('HTTP/1.1 ' + status + '\r\nConnection: close\r\nContent-Length: 0\r\n\r\n');
      return;
    }
    const upstream = openConnection(target);
    const tunnel = { client, upstream };
    tunnels.add(tunnel);
    const cleanup = () => { tunnels.delete(tunnel); client.destroy(); upstream.destroy(); };
    client.on('error', cleanup);
    client.on('close', cleanup);
    upstream.on('error', cleanup);
    upstream.on('close', cleanup);
    upstream.setTimeout(15000, cleanup);
    upstream.once('connect', () => {
      if (!enabled || closing || client.destroyed) { cleanup(); return; }
      upstream.setTimeout(0);
      client.write('HTTP/1.1 200 Connection Established\r\n\r\n');
      if (head.length) { upstream.write(head); }
      client.pipe(upstream);
      upstream.pipe(client);
    });
  });
  const close = () => {
    closing = true;
    enabled = false;
    destroyTunnels();
    server.closeAllConnections();
    return new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve()));
  };
  return { server, close, health };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
    const key = process.env.NATIVE_TEST_PROXY_KEY;
    delete process.env.NATIVE_TEST_PROXY_KEY;
    const proxy = createNativeTestProxy(config, key);
    await new Promise((resolve, reject) => { proxy.server.once('error', reject); proxy.server.listen(0, '127.0.0.1', resolve); });
    console.log(JSON.stringify({ ready: true, host: '127.0.0.1', port: proxy.server.address().port, ...proxy.health() }));
    let stopping = false;
    const stop = async () => { if (!stopping) { stopping = true; await proxy.close(); } };
    process.once('SIGINT', stop);
    process.once('SIGTERM', stop);
  } catch {
    console.error('The staging native test proxy could not start. Check its public staging configuration and private control-key environment.');
    process.exitCode = 1;
  }
}
