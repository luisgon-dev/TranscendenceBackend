import "server-only";

import type { components } from "@transcendence/api-client/schema";

import {
  clearAuthCookies,
  getAuthCookies,
  setAuthCookies,
  shouldRefreshAccessToken,
  type AuthTokenResponse
} from "@/lib/authCookies";
import { getTrnClient } from "@/lib/trnClient";

export type SessionMe =
  | { authenticated: false }
  | {
      authenticated: true;
      subject: string | null;
      name: string | null;
      roles: string[];
      authType: string | null;
    };

type AuthMeResponse = components["schemas"]["AuthMeResponse"];

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken } = await getAuthCookies();
  if (!refreshToken) return null;

  const client = getTrnClient();
  const { data } = await client.POST("/api/auth/refresh", {
    body: { refreshToken }
  });

  if (!data) return null;
  const token = data as AuthTokenResponse;
  await setAuthCookies(token);
  return token.accessToken ?? null;
}

export async function getSessionMe(): Promise<SessionMe> {
  const client = getTrnClient();
  const { accessToken, accessExpiresAtUtc } = await getAuthCookies();
  let token = accessToken;

  if (!token || shouldRefreshAccessToken(accessExpiresAtUtc)) {
    token = await refreshAccessToken();
  }

  if (!token) {
    return { authenticated: false };
  }

  let me = await client.GET("/api/auth/me", {
    headers: { authorization: `Bearer ${token}` }
  });

  if (me.response.status === 401) {
    token = await refreshAccessToken();
    if (!token) {
      await clearAuthCookies();
      return { authenticated: false };
    }

    me = await client.GET("/api/auth/me", {
      headers: { authorization: `Bearer ${token}` }
    });
  }

  if (!me.data) {
    if (me.response.status === 401) await clearAuthCookies();
    return { authenticated: false };
  }

  const data = me.data as AuthMeResponse;
  return {
    authenticated: true,
    subject: data.subject ?? null,
    name: data.name ?? null,
    roles: data.roles ?? [],
    authType: data.authType ?? null
  };
}
