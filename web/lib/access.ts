// The Access cookie: an HttpOnly session value the API signed when a Client exchanged their code. The API
// checks the grant behind it on every read, so revoking a code locks the cookie out at once.
export const accessCookie = "sl_access";
export const accessCookieMaxAge = 60 * 60 * 24 * 365;
