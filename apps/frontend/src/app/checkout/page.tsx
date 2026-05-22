"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { checkout } from "@/lib/api";
import { useCartStore } from "@/lib/cart";

export default function CheckoutPage() {
  const router = useRouter();
  const { sessionId, setCount } = useCartStore();

  const [form, setForm]     = useState({ email: "", name: "", address: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const order = await checkout({
        sessionId,
        customerEmail: form.email,
        customerName:  form.name,
        address:       form.address,
      });
      setCount(0);
      router.push(`/order-success?orderId=${order.orderId}&total=${order.total}`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Error al procesar la orden");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-md mx-auto space-y-6">
      <h1 className="text-3xl font-bold">Checkout</h1>

      {error && (
        <div className="bg-red-900/30 border border-red-700 text-red-400 px-4 py-3 rounded-lg">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="card p-6 space-y-4">
        <div>
          <label className="block text-sm text-gray-400 mb-1">Nombre completo</label>
          <input
            type="text"
            className="input"
            required
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            placeholder="Juan Pérez"
          />
        </div>
        <div>
          <label className="block text-sm text-gray-400 mb-1">Correo electrónico</label>
          <input
            type="email"
            className="input"
            required
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            placeholder="juan@ejemplo.com"
          />
        </div>
        <div>
          <label className="block text-sm text-gray-400 mb-1">Dirección de envío</label>
          <textarea
            className="input resize-none h-24"
            required
            value={form.address}
            onChange={(e) => setForm({ ...form, address: e.target.value })}
            placeholder="Calle, número, ciudad, estado, CP"
          />
        </div>
        <button type="submit" disabled={loading} className="btn-primary w-full text-lg">
          {loading ? "Procesando..." : "Confirmar orden"}
        </button>
      </form>
    </div>
  );
}
