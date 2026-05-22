"use client";

import Link from "next/link";
import { useCartStore } from "@/lib/cart";

export function Header() {
  const count = useCartStore((s) => s.count);

  return (
    <header className="sticky top-0 z-50 bg-gray-950/90 backdrop-blur border-b border-gray-800">
      <div className="container mx-auto px-4 max-w-7xl flex items-center justify-between h-16">
        <Link href="/" className="flex items-center gap-2 font-black text-xl">
          <span className="text-2xl">🧢</span>
          <span>GorraShop</span>
        </Link>

        <nav className="hidden md:flex items-center gap-6 text-sm text-gray-400">
          <Link href="/catalog"                     className="hover:text-white transition-colors">Catálogo</Link>
          <Link href="/catalog?category=snapback"   className="hover:text-white transition-colors">Snapbacks</Link>
          <Link href="/catalog?category=fitted"     className="hover:text-white transition-colors">Fitteds</Link>
          <Link href="/catalog?category=trucker"    className="hover:text-white transition-colors">Truckers</Link>
        </nav>

        <Link href="/cart" className="relative p-2 hover:text-sky-400 transition-colors">
          <span className="text-2xl">🛒</span>
          {count > 0 && (
            <span className="absolute -top-0.5 -right-0.5 bg-sky-600 text-white text-xs font-bold rounded-full w-5 h-5 flex items-center justify-center">
              {count > 99 ? "99+" : count}
            </span>
          )}
        </Link>
      </div>
    </header>
  );
}
