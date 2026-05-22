"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { getProduct, addToCart } from "@/lib/api";
import { useCartStore } from "@/lib/cart";
import type { Product } from "@/types";

export default function ProductPage({ params }: { params: { id: string } }) {
  const { sessionId, setCount } = useCartStore();
  const [product, setProduct]   = useState<Product | null>(null);
  const [qty, setQty]           = useState(1);
  const [adding, setAdding]     = useState(false);
  const [added, setAdded]       = useState(false);

  useEffect(() => {
    getProduct(Number(params.id)).then(setProduct).catch(console.error);
  }, [params.id]);

  if (!product) return <div className="text-center py-20 text-gray-500">Cargando...</div>;

  const handleAdd = async () => {
    setAdding(true);
    try {
      const res = await addToCart(sessionId, product.id, qty);
      setCount(res.itemCount);
      setAdded(true);
      setTimeout(() => setAdded(false), 2500);
    } finally {
      setAdding(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      <Link href="/catalog" className="text-sky-400 hover:text-sky-300 text-sm mb-6 inline-block">
        ← Volver al catálogo
      </Link>
      <div className="grid md:grid-cols-2 gap-10">
        {/* Imagen */}
        <div className="relative aspect-square rounded-2xl overflow-hidden">
          <Image
            src={product.imageUrl}
            alt={product.name}
            fill
            className="object-cover"
            priority
          />
        </div>

        {/* Info */}
        <div className="space-y-5">
          <div>
            <p className="text-sky-400 font-medium">{product.brand}</p>
            <h1 className="text-3xl font-black mt-1">{product.name}</h1>
            <p className="text-sm text-gray-500 mt-1">{product.categoryName}</p>
          </div>

          <p className="text-4xl font-bold">${product.price.toFixed(2)}</p>

          <p className="text-gray-400 leading-relaxed">{product.description}</p>

          <p className={`text-sm font-medium ${product.stock > 5 ? "text-green-400" : product.stock > 0 ? "text-amber-400" : "text-red-400"}`}>
            {product.stock > 5 ? `✓ En stock (${product.stock} disponibles)` :
             product.stock > 0 ? `⚠ Solo quedan ${product.stock}` :
             "✗ Agotado"}
          </p>

          {product.stock > 0 && (
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 bg-gray-800 rounded-lg">
                <button onClick={() => setQty(Math.max(1, qty - 1))} className="px-3 py-2 text-lg hover:text-sky-400 transition-colors">−</button>
                <span className="w-8 text-center font-bold">{qty}</span>
                <button onClick={() => setQty(Math.min(product.stock, qty + 1))} className="px-3 py-2 text-lg hover:text-sky-400 transition-colors">+</button>
              </div>
              <button onClick={handleAdd} disabled={adding} className="btn-primary flex-1 text-lg">
                {added ? "✓ Agregado al carrito" : adding ? "Agregando..." : "Agregar al carrito"}
              </button>
            </div>
          )}

          {added && (
            <Link href="/cart" className="btn-secondary w-full text-center block">
              Ver carrito →
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}
