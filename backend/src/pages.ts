export function readOAuthQuery(search: string): string {
  const raw = search.startsWith('?') ? search.slice(1) : search;
  const outer = new URLSearchParams(raw);
  const wrapped = outer.getAll('keepmealive_oauth');
  if (wrapped.length > 1 || wrapped.length === 1 && (outer.has('client_id') || outer.has('redirect_uri'))) {
    throw new Error('The sign-in continuation is ambiguous. Restart sign-in from KeepMeAlive.');
  }
  if (wrapped.length === 1) { return wrapped[0] ?? ''; }
  return outer.has('client_id') && outer.has('redirect_uri') ? raw : '';
}

export function authContinuationUrl(path: string, origin: string, query: string): string {
  const destination = new URL(path, origin);
  if (query) { destination.search = 'keepmealive_oauth=' + encodeURIComponent(query); }
  return destination.href;
}

export function safeAuthDestination(value: string, origin: string, issuer: string, query: string): string {
  const destination = new URL(value, origin);
  if (destination.username || destination.password || destination.hash) { throw new Error('The sign-in destination is invalid.'); }
  const app = new URL(origin);
  if (destination.origin === app.origin && ['/login', '/register', '/reset-password', '/verify-email', '/consent'].includes(destination.pathname)) {
    return destination.href;
  }
  const auth = new URL(issuer);
  if (destination.origin === auth.origin && destination.pathname === auth.pathname.replace(/\/$/, '') + '/oauth2/authorize') {
    return destination.href;
  }
  const redirect = new URLSearchParams(query).get('redirect_uri');
  if (redirect) {
    const expected = new URL(redirect);
    if (expected.protocol === 'http:' && expected.hostname === '127.0.0.1' && ['49152', '49153', '49154'].includes(expected.port) &&
      expected.pathname === '/callback/' && !expected.username && !expected.password && !expected.search && !expected.hash &&
      destination.origin === expected.origin && destination.pathname === expected.pathname) {
      return destination.href;
    }
  }
  throw new Error('The sign-in destination is not registered for KeepMeAlive.');
}

export function authPage(path: string, issuer: string): Response {
  const nonce = crypto.randomUUID();
  const titles: Record<string, string> = { '/login': 'Sign in', '/register': 'Create account', '/reset-password': 'Reset password', '/verify-email': 'Verify email', '/consent': 'Connect KeepMeAlive' };
  const title = titles[path] ?? 'KeepMeAlive';
  const fields = path === '/consent' ? '<div id="details"></div><p>KeepMeAlive runs on your computer. A loopback address sends the sign-in result back to this device.</p><button type="button" id="accept" disabled>Allow access</button><button type="button" id="deny" class="secondary">Cancel</button><button type="button" id="switch-account" class="secondary">Use another account</button>' :
    `<form id="auth"><label id="name-label" ${path !== '/register' ? 'hidden' : ''}>Name<input id="name" autocomplete="name" maxlength="100"></label><label id="email-label">Email<input id="email" type="email" autocomplete="email" required maxlength="254"></label><label id="password-label" ${['/verify-email', '/reset-password'].includes(path) ? 'hidden' : ''}>Password<input id="password" type="password" autocomplete="${path === '/register' ? 'new-password' : 'current-password'}" minlength="8" maxlength="128"></label><button id="submit">${title}</button></form><nav><a id="login" href="/login">Sign in</a><a id="register" href="/register">Create account</a><a id="reset" href="/reset-password">Forgot password?</a><a id="verify" href="/verify-email">Verify email</a></nav>`;
  const html = `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${title} · KeepMeAlive</title><style nonce="${nonce}">:root{font-family:Inter,Segoe UI,system-ui,sans-serif;color:#1c1c1e;background:#f2f2f7;color-scheme:light dark}*{box-sizing:border-box}body{margin:0;padding:32px 16px;display:grid;min-height:100vh;place-items:center}main{max-width:420px;width:100%;padding:32px;background:#fff;border:1px solid #ddd;border-radius:24px}h1{font-size:28px;letter-spacing:-.8px}p{line-height:1.5;color:#636366}label{display:block;margin:18px 0;font-weight:600}input{display:block;width:100%;margin-top:8px;padding:12px;border:1px solid #888;border-radius:10px;font:inherit;background:transparent;color:inherit}button{display:block;width:100%;padding:13px;margin-top:16px;border:0;border-radius:24px;background:#0066cc;color:white;font:inherit;font-weight:600;cursor:pointer}button:disabled{opacity:.6;cursor:wait}.secondary{background:#e9e9ed;color:#1c1c1e}a{color:#0066cc}nav{display:flex;flex-wrap:wrap;gap:14px;margin-top:24px;font-size:13px}[hidden]{display:none!important}:focus-visible{outline:3px solid #0066cc;outline-offset:3px}#status{white-space:pre-wrap;min-height:24px}#details{overflow-wrap:anywhere;line-height:1.6}@media(prefers-color-scheme:dark){:root{color:#f5f5f7;background:#111113}main{background:#1c1c1e;border-color:#48484a}p{color:#aeaeb2}a{color:#66b0ff}.secondary{background:#38383a;color:#f5f5f7}}</style><main><p>KeepMeAlive</p><h1>${title}</h1>${fields}<p id="status" role="status" aria-live="polite"></p></main><script nonce="${nonce}">
const readOAuthQuery=${readOAuthQuery.toString()},authContinuationUrl=${authContinuationUrl.toString()},safeAuthDestination=${safeAuthDestination.toString()};
const issuer=${JSON.stringify(issuer)},path=${JSON.stringify(path)},query=readOAuthQuery(location.search),params=new URLSearchParams(location.search),authParams=new URLSearchParams(query),status=document.getElementById('status');
const say=(message)=>status.textContent=message;
async function call(route,body){const response=await fetch(issuer+route,{method:body?'POST':'GET',credentials:'include',headers:body?{'Content-Type':'application/json'}:{},body:body?JSON.stringify(body):undefined});const data=await response.json();if(!response.ok){throw new Error(data.message||'Unable to complete this request. Please try again.');}return data;}
const continuation=(route)=>authContinuationUrl(route,location.origin,query);
function go(data){if(data.url){location.assign(safeAuthDestination(data.url,location.origin,issuer,query));return true;}return false;}
for(const link of document.querySelectorAll('nav a')){link.href=continuation(new URL(link.href,location.origin).pathname);}
if(path==='/consent'){
  let verifiedClient=false,pending=false;const acceptButton=document.getElementById('accept'),denyButton=document.getElementById('deny'),switchButton=document.getElementById('switch-account');
  function setPending(value){pending=value;acceptButton.disabled=value||!verifiedClient;denyButton.disabled=value;switchButton.disabled=value;}
  const clientId=authParams.get('client_id');call('/oauth2/public-client?client_id='+encodeURIComponent(clientId||'')).then(data=>{if(!clientId||data.client_id!==clientId){throw new Error('The requesting app could not be verified.');}const redirect=authParams.get('redirect_uri');if(!redirect){throw new Error('The app callback is missing.');}safeAuthDestination(redirect,location.origin,issuer,query);document.getElementById('details').textContent='App: '+(data.client_name||'Unknown app')+'\\nCallback: '+redirect+'\\nResource: '+(authParams.get('resource')||'')+'\\nPermissions: '+(authParams.get('scope')||'');verifiedClient=true;setPending(pending);}).catch(error=>say(error.message));
  for(const [id,accept] of [['accept',true],['deny',false]]){document.getElementById(id).onclick=async()=>{if(pending||(accept&&!verifiedClient)){return;}setPending(true);try{if(!go(await call('/oauth2/consent',{accept,oauth_query:query}))){throw new Error('The sign-in service returned no continuation.');}}catch(error){say(error.message);}finally{setPending(false);}};}
  switchButton.onclick=async()=>{if(pending){return;}setPending(true);say('Signing out…');try{await call('/sign-out',{});location.assign(continuation('/login'));}catch(error){say(error.message);}finally{setPending(false);}};
}else{
  const form=document.getElementById('auth'),button=document.getElementById('submit'),email=document.getElementById('email'),password=document.getElementById('password');
  if(path==='/reset-password'&&params.has('token')){document.getElementById('email-label').hidden=true;email.required=false;document.getElementById('password-label').hidden=false;password.required=true;password.autocomplete='new-password';}
  form.onsubmit=async(event)=>{event.preventDefault();button.disabled=true;say('Working…');try{
    if(path==='/login'){const data=await call('/sign-in/email',{email:email.value,password:password.value,...(query?{oauth_query:query}:{})});if(!go(data)&&query){go(await call('/oauth2/continue',{selected:true,oauth_query:query}));}else if(!data.url){say('Signed in. You can return to KeepMeAlive.');}}
    if(path==='/register'){await call('/sign-up/email',{name:document.getElementById('name').value.trim(),email:email.value,password:password.value,callbackURL:continuation('/login')});password.value='';say('Account created. Check your email for verification, then sign in.');}
    if(path==='/verify-email'){await call('/send-verification-email',{email:email.value,callbackURL:continuation('/login')});say('If this account needs verification, an email has been sent.');}
    if(path==='/reset-password'){if(params.has('token')){await call('/reset-password',{token:params.get('token'),newPassword:password.value});password.value='';say('Password changed. You can sign in.');}else{await call('/request-password-reset',{email:email.value,redirectTo:continuation('/reset-password')});say('If an account exists, a password reset email has been sent.');}}
  }catch(error){say(error.message);}finally{button.disabled=false;}};
  if(path==='/login'&&query){call('/get-session').then(async(data)=>{if(data?.session){go(await call('/oauth2/continue',{selected:true,oauth_query:query}));}}).catch(()=>{});}
}
</script></html>`;
  return new Response(html, { headers: { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store', 'Referrer-Policy': 'no-referrer', 'Content-Security-Policy': `default-src 'none'; script-src 'nonce-${nonce}'; style-src 'nonce-${nonce}'; connect-src ${new URL(issuer).origin}; base-uri 'none'; frame-ancestors 'none'; form-action 'none'` } });
}
