using System;
using System.Collections.Generic;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.Helpers
{
    /// <summary>
    /// Boundary validation for data coming from the SignalR hub.
    /// </summary>
    internal static class InputValidator
    {
        internal const int USERNAME_MIN = 3;
        internal const int USERNAME_MAX = 32;
        internal const int PASSWORD_MIN = 8;
        internal const int PASSWORD_MAX = 64;
        internal const int EMAIL_MAX = 254;
        internal const int COOKIE_MAX_COUNT = 80;
        internal const int COOKIE_VALUE_MAX = 4096;
        internal const int COOKIE_NAME_MAX = 256;
        internal const int COOKIE_DOMAIN_MAX = 253;
        internal const int COOKIE_PATH_MAX = 1024;

        // Domains we accept for MS Rewards session cookies.
        // Match is "endsWith" so subdomains are covered.
        private static readonly string[] _allowedCookieDomains =
        {
            ".live.com",
            ".microsoft.com",
            ".bing.com",
            ".microsoftonline.com",
            "live.com",
            "microsoft.com",
            "bing.com",
            "microsoftonline.com"
        };

        internal static bool IsValidUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            if (username.Length < USERNAME_MIN || username.Length > USERNAME_MAX)
            {
                return false;
            }

            foreach (char c in username)
            {
                bool ok =
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' || c == '-' || c == '.';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < PASSWORD_MIN || password.Length > PASSWORD_MAX)
            {
                return false;
            }

            foreach (char c in password)
            {
                if (char.IsControl(c))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (email.Length > EMAIL_MAX)
            {
                return false;
            }

            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
            {
                return false;
            }

            string local = email.Substring(0, at);
            string domain = email.Substring(at + 1);

            if (local.Length == 0 || domain.Length == 0)
            {
                return false;
            }

            if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            {
                return false;
            }

            foreach (char c in email)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsValidCookieDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Length > COOKIE_DOMAIN_MAX)
            {
                return false;
            }

            string normalized = domain.ToLowerInvariant().TrimEnd('.');

            foreach (string allowed in _allowedCookieDomains)
            {
                if (normalized == allowed.TrimStart('.') || normalized.EndsWith(allowed))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsValidCookies(IList<AccountCookie> cookies, out string reason)
        {
            reason = null;

            if (cookies == null || cookies.Count == 0)
            {
                reason = "Cookie list is empty.";
                return false;
            }

            if (cookies.Count > COOKIE_MAX_COUNT)
            {
                reason = $"Too many cookies (>{COOKIE_MAX_COUNT}).";
                return false;
            }

            for (int i = 0; i < cookies.Count; i++)
            {
                AccountCookie c = cookies[i];

                if (c == null)
                {
                    reason = $"Cookie at index {i} is null.";
                    return false;
                }
                if (string.IsNullOrEmpty(c.Name) || c.Name.Length > COOKIE_NAME_MAX)
                {
                    reason = $"Cookie at index {i} has an invalid name.";
                    return false;
                }
                if (c.Value == null || c.Value.Length > COOKIE_VALUE_MAX)
                {
                    reason = $"Cookie '{c.Name}' has an invalid value length.";
                    return false;
                }
                if (!IsValidCookieDomain(c.Domain))
                {
                    reason = $"Cookie '{c.Name}' has a domain not in the allowed list ({c.Domain}).";
                    return false;
                }
                if (!string.IsNullOrEmpty(c.Path) && c.Path.Length > COOKIE_PATH_MAX)
                {
                    reason = $"Cookie '{c.Name}' has a path longer than {COOKIE_PATH_MAX}.";
                    return false;
                }
            }

            return true;
        }
    }
}
