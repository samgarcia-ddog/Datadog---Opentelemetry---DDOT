"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";

interface CartState {
  sessionId: string;
  count:     number;
  setCount:  (count: number) => void;
}

// sessionId se persiste en localStorage para mantener el carrito entre sesiones
function generateSessionId() {
  return crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2);
}

export const useCartStore = create<CartState>()(
  persist(
    (set) => ({
      sessionId: generateSessionId(),
      count:     0,
      setCount:  (count) => set({ count }),
    }),
    { name: "gorrashop-cart" }
  )
);
