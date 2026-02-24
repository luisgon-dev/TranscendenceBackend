import Image from "next/image";
import Link from "next/link";
import type { components } from "@transcendence/api-client/schema";

import { Card } from "@/components/ui/Card";
import { fetchBackendJson } from "@/lib/backendCall";
import { getBackendBaseUrl } from "@/lib/env";
import { roleDisplayLabel } from "@/lib/roles";
import { championIconUrl, fetchChampionMap } from "@/lib/staticData";
import { normalizeTierListEntries } from "@/lib/tierlist";

type TierListResponse = components["schemas"]["TierListResponse"];

export default async function MatchupsIndexPage() {
  const [{ version, champions }, tierListRes] = await Promise.all([
    fetchChampionMap(),
    fetchBackendJson<TierListResponse>(`${getBackendBaseUrl()}/api/analytics/tierlist`, {
      next: { revalidate: 60 * 60 }
    })
  ]);

  const tierEntries = tierListRes.ok
    ? normalizeTierListEntries(tierListRes.body?.entries ?? [])
    : [];

  const popular = tierEntries
    .slice()
    .sort((a, b) => b.games - a.games)
    .filter(
      (entry, idx, arr) => arr.findIndex((candidate) => candidate.championId === entry.championId) === idx
    )
    .slice(0, 16);

  return (
    <div className="grid gap-6">
      <header className="grid gap-2">
        <h1 className="font-[var(--font-sora)] text-3xl font-semibold tracking-tight">
          Matchup Analysis
        </h1>
        <p className="text-sm text-fg/75">
          Deep dive into lane counters, win rates, and matchup trends.
        </p>
      </header>

      <Card className="p-5">
        <h2 className="font-[var(--font-sora)] text-lg font-semibold">Popular Matchup Pages</h2>
        <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {popular.map((entry) => {
            const champion = champions[String(entry.championId)];
            const championName = champion?.name ?? `Champion ${entry.championId}`;
            return (
              <Link
                key={`${entry.championId}-${entry.role}`}
                href={`/matchups/${entry.championId}?role=${encodeURIComponent(entry.role)}`}
                className="rounded-lg border border-border/60 bg-white/[0.03] p-3 transition hover:bg-white/[0.08]"
              >
                <div className="flex items-center gap-2.5">
                  <Image
                    src={championIconUrl(version, champion?.id ?? "Unknown")}
                    alt={championName}
                    width={34}
                    height={34}
                    className="rounded-md"
                  />
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-fg">{championName}</p>
                    <p className="truncate text-xs text-muted">{roleDisplayLabel(entry.role)}</p>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      </Card>
    </div>
  );
}
