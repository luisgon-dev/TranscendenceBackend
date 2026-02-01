# Requirements: Transcendence Backend

**Defined:** 2026-02-01
**Core Value:** Summoner profiles with comprehensive stats — the foundation that enables the desktop app to be built against this API

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Data Infrastructure

- [ ] **INFRA-01**: Static data (champions, items, runes) auto-updates on patch releases
- [ ] **INFRA-02**: Two-tier caching with memory (L1) and Redis (L2)

### Summoner Profiles

- [ ] **PROF-01**: User can look up any summoner by Riot ID (gameName#tagLine)
- [ ] **PROF-02**: User can view summoner's recent match history with full stats
- [ ] **PROF-03**: User can view summoner's current rank, LP, and win/loss record
- [ ] **PROF-04**: User can view summoner's performance breakdown by champion

### Champion Analytics

- [ ] **CHAMP-01**: User can view champion win rates by role and rank tier
- [ ] **CHAMP-02**: User can view popular builds (items, runes, skill orders) per champion
- [ ] **CHAMP-03**: User can view champion tier lists by role
- [ ] **CHAMP-04**: User can view champion matchup data (counters, synergies)

### Live Game

- [ ] **LIVE-01**: User can detect if a summoner is currently in game
- [ ] **LIVE-02**: User can view all participants' ranks and recent performance
- [ ] **LIVE-03**: User can see team composition analysis (strengths/weaknesses)
- [ ] **LIVE-04**: User can see estimated win probability for each team

### Authentication

- [ ] **AUTH-01**: Apps can authenticate via API keys
- [ ] **AUTH-02**: User can create account and log in
- [ ] **AUTH-03**: User can save favorites and preferences

### Management

- [ ] **MGMT-01**: API exposes health check endpoints
- [ ] **MGMT-02**: Admins can access Hangfire job dashboard
- [ ] **MGMT-03**: Admins can trigger manual data refreshes
- [ ] **MGMT-04**: System exposes metrics via OpenTelemetry

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Data Infrastructure

- **INFRA-03**: Rate limit compliance with dynamic throttling and backoff
- **INFRA-04**: Data retention handling for expired Riot data

### Authentication

- **AUTH-04**: Riot account linking via RSO (Riot Sign On)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Desktop application | Separate repo, consumes this API |
| Web frontend | Separate repo, consumes this API |
| Mobile app | Web-first approach |
| Real-time push notifications | Polling model for v1 |
| Social features (friends, sharing) | Not core to analytics |
| Real-time chat | Not relevant to analytics use case |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | — | Pending |
| INFRA-02 | — | Pending |
| PROF-01 | — | Pending |
| PROF-02 | — | Pending |
| PROF-03 | — | Pending |
| PROF-04 | — | Pending |
| CHAMP-01 | — | Pending |
| CHAMP-02 | — | Pending |
| CHAMP-03 | — | Pending |
| CHAMP-04 | — | Pending |
| LIVE-01 | — | Pending |
| LIVE-02 | — | Pending |
| LIVE-03 | — | Pending |
| LIVE-04 | — | Pending |
| AUTH-01 | — | Pending |
| AUTH-02 | — | Pending |
| AUTH-03 | — | Pending |
| MGMT-01 | — | Pending |
| MGMT-02 | — | Pending |
| MGMT-03 | — | Pending |
| MGMT-04 | — | Pending |

**Coverage:**
- v1 requirements: 21 total
- Mapped to phases: 0
- Unmapped: 21 ⚠️

---
*Requirements defined: 2026-02-01*
*Last updated: 2026-02-01 after initial definition*
