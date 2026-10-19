namespace TO2.RateLimiting
{
    public static class RateLimitPolicies
    {
        /// <summary>
        /// Anonymous auth endpoints (login, register, organization check): per client IP, against guessing passwords and invite codes.
        /// </summary>
        public const string Auth = "auth";
    }
}
