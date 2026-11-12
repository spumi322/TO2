namespace TO2.RateLimiting
{
    public static class RateLimitPolicies
    {
        /// <summary>
        /// Anonymous auth endpoints (login, register, refresh, organization check): per client IP, against guessing and request floods.
        /// </summary>
        public const string Auth = "auth";
    }
}
