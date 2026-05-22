"use client";

import Image from "next/image";
import Link from "next/link";
import { useState } from "react";
import { addToCart } from "@/lib/api";
import { useCartStore } from "@/lib/cart";
import type { Product } from "@/types";

export function ProductCard({ product }: { product: Product }) {
  const { sessionId, setCount } = useCartStore();
  const [adding, setAdding]     = useState(false);
  const [added, setAdded]       = useState(false);

  const handleAdd = async (e: React.MouseEvent) => {
    e.preventDefault();
    setAdding(true);
    try {
      const res = await addToCart(sessionId, product.id, 1);
      setCount(res.itemCount);
      setAdded(true);
      setTimeout(() => setAdded(false), 2000);
    } finally {
      setAdding(false);
    }
  };

  return (
    <Link href={`/product/${product.id}`} className="card group flex flex-col hover:border-sky-500 transition-colors">
      <div className="relative aspect-square overflow-hidden">
        <Image
          src={product.imageUrl}
          alt={product.name}
          fill
          className="object-cover group-hover:scale-105 transition-transform duration-300"
        />
        {product.stock < 5 && product.stock > 0 && (
          <span className="absolute top-2 right-2 bg-amber-500 text-black text-xs font-bold px-2 py-0.5 rounded">
            ¡Últimas!
          </span>
        )}
        {product.stock === 0 && (
          <div className="absolute inset-0 bg-black/60 flex items-center justify-center">
            <span className="text-white font-bold">Agotado</span>
          </div>
        )}
      </div>

      <div className="p-4 flex flex-col flex-1 gap-2">
        <p className="text-xs text-sky-400 font-medium">{product.brand}</p>
        <h3 className="font-semibold leading-snug line-clamp-2">{product.name}</h3>
        <div className="mt-auto flex items-center justify-between gap-2 pt-2">
          <span className="text-xl font-bold">${product.price.toFixed(2)}</span>
          <button
            onClick={handleAdd}
            disabled={adding || product.stock === 0}
            className="btn-primary text-sm px-3 py-1.5 shrink-0"
          >
            {added ? "✓ Agregado" : adding ? "..." : "+ Carrito"}
          </button>
        </div>
      </div>
    </Link>
  );
}
