# Native staging outage check

This local test helper accepts CONNECT tunnels to the two pinned staging hosts on port 443. It forwards encrypted bytes without terminating TLS. It never records headers, bodies, cookies, tokens, target names or credentials. It cannot reach production or arbitrary hosts.

Generate an unpredictable control key in the test harness's memory, supply it as the child process environment variable `NATIVE_TEST_PROXY_KEY`, then start `node backend/scripts/native-test-proxy.mjs`. The sole startup JSON line contains a random loopback port, readiness and connection counts. Keep the key outside files, command output and the application environment.

Launch the validation application with `HTTPS_PROXY=http://127.0.0.1:<port>` in **that process's** environment. Do not change Windows proxy settings. The browser remains on its normal connection. Successful staging account traffic increments `GET http://127.0.0.1:<port>/healthz`'s connection count.

From the test harness, send `POST /disable` or `POST /enable` to that loopback origin with `Authorization: Bearer <private control key>`. Disabling closes existing tunnels and rejects new ones; enabling permits fresh connections. These controls do not change cloud configuration. Stop the helper after the application exits. The helper is a local validation tool and is excluded from desktop packages.
