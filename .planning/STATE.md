# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-31)

**Core value:** Summoner profiles with comprehensive stats — the foundation that enables the desktop app to be built against this API

**Current focus:** Phase 1 - Foundation (Data Infrastructure & API Safety)

## Current Position

Phase: 1 of 5 - Foundation
Plan: 4 of 4 in current phase
Status: Phase complete
Last activity: 2026-02-02 - Completed 01-04-PLAN.md

Progress: ████░░░░░░ 100% (4/4 plans in phase)

## Performance Metrics

**Requirements:**
- Total v1 requirements: 21
- Requirements complete: 0
- Requirements remaining: 21

**Phases:**
- Total phases: 5
- Phases complete: 0
- Current phase: Phase 1

**Velocity:**
- Not yet measured

## Recent Decisions

| Phase | Decision | Rationale |
|-------|----------|-----------|
| 01 | L1 TTL (5min) shorter than L2 TTL (1hr) | Prevents stale-distributed-fresh scenarios where one server has stale in-memory cache but Redis has updated data |
| 01 | Use HybridCache built-in stampede protection | No manual locking needed - HybridCache guarantees only one concurrent caller executes factory for given key |
| 01 | CacheService wrapper abstraction | Centralizes cache key generation, improves testability, provides domain-specific API over infrastructure |
| 01 | 6-hour patch check interval | Satisfies requirement while minimizing API calls - patch cycle is ~2 weeks, hourly checks waste 95% of requests |
| 01 | 30-day cache TTL for static data | Outlives 2-week patch cycle, old patch data persists for historical queries |
| 01 | IsActive flag for current patch | Simplifies queries to FirstOrDefault(p => p.IsActive) instead of ordering by ReleaseDate |
| 01 | Tag-based cache invalidation | Cache entries tagged with patch version allow bulk invalidation on patch change |
| 01 | Exponential backoff delays: 30s, 60s, 120s, 300s | Balances responsiveness with API courtesy - first retry quick for transient failures, backs off if issue persists |
| 01 | Max 5 retry attempts before PermanentlyUnfetchable | Prevents infinite retry loops while being persistent - after ~10 minutes data likely permanently unavailable |
| 01 | 2-year retention window check before fetch | Riot API only retains match data for 2 years - checking before fetch prevents wasted API calls |
| 01 | Global query filter for PermanentlyUnfetchable | Unfetchable matches are historical records but shouldn't appear in normal queries - use IgnoreQueryFilters() for admin/reporting |
| 01 | Trust Camille SDK rate limiting | Camille parses X-Rate-Limit-* headers and respects Retry-After - custom throttling creates double-throttling |
| 01 | RetryFailedMatchesJob hourly as safety net | Catches edge cases from service restarts - primary retry is per-match exponential backoff |
| 01 | Separate ProfileAge and RankAge metadata | Profile data changes rarely, rank data changes frequently - different freshness expectations |
| 01 | Human-friendly age descriptions | Just now (<5 min), X minutes ago (<1 hr), X hours ago (<1 day), X days ago (>1 day) for UI display |
| 01 | UpdatedAt on Summoner entity | Added for ProfileAge tracking - was missing but required by plan |
| 01 | API contract change to SummonerProfileResponse | Acceptable in Phase 1 Foundation - no existing clients to break, desktop app will be built against this contract |

## Pending Todos

(None yet - awaiting Phase 1 planning)

## Known Blockers

(None yet)

## Session Continuity

**Last session:** 2026-02-02
**Activity:** Plan execution
**Stopped at:** Completed 01-04-PLAN.md (Phase 1 complete)
**Resume file:** None

---

## Context for Next Session

**What we just did:**
- Created roadmap with 5 phases (compressed from research's 6 phases for "quick" depth)
- Mapped all 21 v1 requirements to phases (100% coverage)
- Derived goal-backward success criteria for each phase

**Phase 1 priority:** Foundation work addresses rate limiting, caching, and data retention—critical pitfalls that have no recovery if handled incorrectly. This phase must succeed before any downstream features.

**Brownfield reminder:** Existing code has basic summoner refresh flow. Phase 2 will polish existing features rather than build from scratch.

---

*Last updated: 2026-02-02*
