import { jwtVerify, type JWTVerifyGetKey } from 'jose';

export function createTokenVerifier(issuer: string, audience: string, keys: JWTVerifyGetKey) {
  return async (token: string) => {
    const result = await jwtVerify(token, keys, {
      issuer, audience, algorithms: ['EdDSA'], requiredClaims: ['sub', 'exp', 'iat'],
    });
    return result.payload;
  };
}
