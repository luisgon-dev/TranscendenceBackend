import Link from "next/link";

import { logoutAction } from "@/app/account/actions";
import { Button } from "@/components/ui/Button";
import { getSessionMe } from "@/lib/session";

export async function AccountNav() {
  const me = await getSessionMe();
  if (!me.authenticated) {
    return (
      <div className="flex items-center gap-2">
        <Link className="text-sm text-fg/80 hover:text-fg" href="/account/login">
          Login
        </Link>
        <Link
          className="text-sm text-fg/80 hover:text-fg"
          href="/account/register"
        >
          Register
        </Link>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <Link
        className="text-sm text-fg/80 hover:text-fg"
        href="/account/favorites"
      >
        Favorites
      </Link>
      <form action={logoutAction}>
        <Button variant="ghost" size="sm" type="submit">
          Logout
        </Button>
      </form>
    </div>
  );
}
