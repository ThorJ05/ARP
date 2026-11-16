import {Api} from "./api";

export const API_URL =
    window.location.hostname === "localhost"
        ? "http://localhost:5288"
        : "https://arp-api.fly.dev";

export const api = new Api({baseUrl: API_URL});
