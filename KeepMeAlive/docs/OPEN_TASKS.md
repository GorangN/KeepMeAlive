# Remaining launch work

Updated: 2026-10-29.

The production-readiness implementation replaces the previous Supabase proposal with Neon. Current unresolved operational gates are tracked in [RELEASE_READINESS.md](RELEASE_READINESS.md), and verified results belong in the repository task review.

The local utility remains usable offline. Account release requires production SMTP delivery, managed OAuth desktop-client configuration and staging acceptance, plus a trusted distribution-signing certificate. No paid plan, checkout, telemetry, or subscription restriction is introduced by this change.

See [RELEASE.md](RELEASE.md) for package verification, signing, backend promotion, rollback, and database recovery, and [PRIVACY_DATA_FLOW.md](PRIVACY_DATA_FLOW.md) for actual storage/network behavior.
