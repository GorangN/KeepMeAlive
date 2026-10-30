import assert from 'node:assert/strict';
import { once } from 'node:events';
import { createServer, connect } from 'node:net';
import { test } from 'node:test';
import { createNativeTestProxy, parseConnectTarget, stagingTunnelHosts } from '../scripts/native-test-proxy.mjs';

const config = { staging: {
  projectId: 'sweet-truth-01118709', branchId: 'br-red-flower-b9wmbkmb',
  issuer: 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth',
  apiBaseUrl: 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/',
} };
const controlKey = 'SyntheticProxyControl-0123456789-AbCdEfGhIjKlMnOpQrStUvWxYz';
const issuerHost = new URL(config.staging.issuer).hostname;

test('native tunnel permits only the pinned staging TLS hosts and port', () => {
  const hosts = stagingTunnelHosts(config);
  for (const host of hosts) { assert.deepEqual(parseConnectTarget(host + ':443', hosts), { host, port: 443 }); }
  for (const target of [issuerHost, issuerHost + ':80', issuerHost + ':0443', issuerHost + ':443/path', issuerHost + '.:443', 'user@' + issuerHost + ':443', issuerHost + '.evil.test:443', '127.0.0.1:443', '[::1]:443', 'ep-rough-flower-b9s4hagj.neonauth.c-7.eu-central-1.aws.neon.tech:443', issuerHost + ':443\r\n']) {
    assert.equal(parseConnectTarget(target, hosts), undefined);
  }
  for (const field of ['projectId', 'branchId', 'issuer', 'apiBaseUrl']) {
    assert.throws(() => stagingTunnelHosts({ staging: { ...config.staging, [field]: 'production' } }), /Only the isolated/);
  }
  assert.throws(() => createNativeTestProxy(config, 'weak'), /unpredictable/);
});

async function startProxy(t, openConnection) {
  const proxy = createNativeTestProxy(config, controlKey, openConnection);
  proxy.server.listen(0, '127.0.0.1');
  await once(proxy.server, 'listening');
  t.after(() => proxy.close());
  const address = proxy.server.address();
  assert.equal(address.address, '127.0.0.1');
  return { ...proxy, port: address.port, baseUrl: 'http://127.0.0.1:' + address.port };
}

async function tunnel(port, target = issuerHost + ':443') {
  const socket = connect({ host: '127.0.0.1', port });
  await once(socket, 'connect');
  socket.write('CONNECT ' + target + ' HTTP/1.1\r\nHost: ' + target + '\r\n\r\n');
  const [response] = await once(socket, 'data');
  return { socket, response: response.toString() };
}

test('private controls reject unauthenticated changes and health never exposes the key', async (t) => {
  const proxy = await startProxy(t);
  assert.equal((await fetch(proxy.baseUrl + '/disable', { method: 'POST' })).status, 401);
  assert.equal((await fetch(proxy.baseUrl + '/disable', { method: 'POST', headers: { Authorization: 'Bearer wrong' } })).status, 401);
  assert.equal((await fetch(proxy.baseUrl + '/disable')).status, 405);
  const health = await (await fetch(proxy.baseUrl + '/healthz')).json();
  assert.deepEqual(health, { enabled: true, connectionCount: 0, activeTunnels: 0 });
  assert.ok(!JSON.stringify(health).includes(controlKey));
  const disabled = await (await fetch(proxy.baseUrl + '/disable', { method: 'POST', headers: { Authorization: 'Bearer ' + controlKey } })).json();
  assert.equal(disabled.enabled, false);
  const blocked = await tunnel(proxy.port);
  assert.match(blocked.response, /^HTTP\/1\.1 503 /);
  blocked.socket.destroy();
  const forbidden = await tunnel(proxy.port, 'example.com:443');
  assert.match(forbidden.response, /^HTTP\/1\.1 403 /);
  forbidden.socket.destroy();
});

test('opaque tunnels are cut on disable and restored only for new enabled connections', async (t) => {
  const echo = createServer((socket) => socket.pipe(socket));
  echo.listen(0, '127.0.0.1');
  await once(echo, 'listening');
  t.after(() => new Promise((resolve) => echo.close(resolve)));
  const opened = [];
  const proxy = await startProxy(t, (target) => {
    opened.push(target);
    return connect({ host: '127.0.0.1', port: echo.address().port });
  });
  const first = await tunnel(proxy.port);
  assert.match(first.response, /^HTTP\/1\.1 200 /);
  assert.deepEqual(opened, [{ host: issuerHost, port: 443 }]);
  const opaque = Buffer.from([0x16, 0x03, 0x03, 0, 8, 0xff, 0x00, 0x99]);
  first.socket.write(opaque);
  assert.deepEqual((await once(first.socket, 'data'))[0], opaque);
  const closed = once(first.socket, 'close');
  await fetch(proxy.baseUrl + '/disable', { method: 'POST', headers: { Authorization: 'Bearer ' + controlKey } });
  await closed;
  assert.equal(proxy.health().activeTunnels, 0);
  await fetch(proxy.baseUrl + '/enable', { method: 'POST', headers: { Authorization: 'Bearer ' + controlKey } });
  const second = await tunnel(proxy.port);
  assert.match(second.response, /^HTTP\/1\.1 200 /);
  second.socket.destroy();
  assert.equal(proxy.health().connectionCount, 2);
});
