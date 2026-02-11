"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { SearchBar } from "@/components/SearchBar";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/cn";

const COMPACT_HEADER_PATHS = new Set(["/account/login", "/account/register"]);

function openGlobalSearch() {
  window.dispatchEvent(new Event("trn:open-command-palette"));
}

export function SiteHeaderClient({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const compact = pathname ? COMPACT_HEADER_PATHS.has(pathname) : false;

  return (
    <header className="sticky top-0 z-40 border-b border-border/60 bg-bg/70 backdrop-blur">
      <div
        className={cn(
          "mx-auto flex max-w-6xl flex-col gap-3 px-4 py-3",
          compact ? "sm:flex-row sm:items-center sm:justify-between" : null
        )}
      >
        <div className="flex items-center justify-between gap-3">
          <Link href="/" className="group inline-flex items-center gap-2">
            <span className="h-8 w-8 rounded-lg bg-gradient-to-br from-primary to-primary-2 shadow-glass" />
            <span className="text-sm font-semibold tracking-wide text-fg">
              Transcendence
            </span>
          </Link>

          <nav className="hidden items-center gap-4 sm:flex">
            <Link href="/tierlist" className="text-sm text-fg/80 hover:text-fg">
              Tier List
            </Link>
            <Link href="/champions" className="text-sm text-fg/80 hover:text-fg">
              Champions
            </Link>
          </nav>
        </div>

        {compact ? (
          <div className="flex flex-wrap items-center justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9 min-w-[150px] justify-between text-fg/80"
              onClick={openGlobalSearch}
            >
              Quick Search
              <span className="text-xs text-muted">Ctrl/Cmd+K</span>
            </Button>
            <div className="shrink-0">{children}</div>
          </div>
        ) : (
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="flex min-w-0 flex-col gap-2 sm:flex-row sm:items-center">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-11 min-w-[176px] justify-between text-fg/80"
                onClick={openGlobalSearch}
              >
                Quick Search
                <span className="text-xs text-muted">Ctrl/Cmd+K</span>
              </Button>
              <SearchBar className="min-w-0 flex-1 md:max-w-[560px]" />
            </div>
            <div className="shrink-0">{children}</div>
          </div>
        )}
      </div>
    </header>
  );
}
