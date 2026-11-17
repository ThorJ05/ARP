import {Api} from "./api";

export const API_URL =
    window.location.hostname === "localhost"
        ? "http://localhost:5288"
        : "https://arp-api.fly.dev";

// After login the token is stored here with api.setSecurityData(token),
// and it's sent as "Authorization: Bearer <token>" on every request the API marks as secured.
export const api = new Api<string>({
    baseUrl: API_URL,
    securityWorker: (token) => (token ? {headers: {Authorization: `Bearer ${token}`}} : {}),
});
