"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { getCart, removeFromCart } from "@/lib/api";
import { useCartStore } from "@/lib/cart";
import type { Cart } from "@/types";

export default function CartPage() {
  const { sessionId, setCount } = useCartStore();
  const [cart, setCart]     = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    try {
      const data = await getCart(sessionId);
      setCart(data);
      setCount(data.count);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [sessionId]);

  const handleRemove = async (productId: string) => {
    await removeFromCart(sessionId, Number(productId));
    await load();
  };

  if (loading) return <div className="text-center py-20 text-gray-500">Cargando carrito...</div>;

  if (!cart?.items.length) {
    return (
      <div className="text-center py-20 space-y-4">
        <div className="text-6xl">🛒</div>
        <h2 className="text-2xl font-bold">Tu carrito está vacío</h2>
        <p className="text-gray-400">Agrega algunas gorras para continuar</p>
        <Link href="/catalog" className="btn-primary inline-block">Ver catálogo</Link>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <h1 className="text-3xl font-bold">Carrito</h1>

      <div className="space-y-4">
        {cart.items.map((item) => (
          <div key={item.productId} className="card p-4 flex gap-4 items-center">
            <div className="relative w-20 h-20 shrink-0 rounded-lg overflow-hidden">
              <Image
                src={item.imageUrl}
                alt={item.productName}
                fill
                className="object-cover"
              />
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-semibold truncate">{item.productName}</p>
              <p className="text-gray-400 text-sm">x{item.quantity}</p>
            </div>
            <div className="text-right shrink-0">
              <p className="font-bold">${(item.unitPrice * item.quantity).toFixed(2)}</p>
              <p className="text-sm text-gray-500">${item.unitPrice.toFixed(2)} c/u</p>
            </div>
            <button
              onClick={() => handleRemove(item.productId)}
              className="text-red-400 hover:text-red-300 p-2 transition-colors"
              aria-label="Eliminar"
            >
              ✕
            </button>
          </div>
        ))}
      </div>

      <div className="card p-6 space-y-4">
        <div className="flex justify-between text-xl font-bold">
          <span>Total</span>
          <span className="text-sky-400">${cart.total.toFixed(2)}</span>
        </div>
        <Link href="/checkout" className="btn-primary w-full text-center block text-lg">
          Proceder al checkout →
        </Link>
        <Link href="/catalog" className="btn-secondary w-full text-center block">
          Seguir comprando
        </Link>
      </div>
    </div>
  );
}
