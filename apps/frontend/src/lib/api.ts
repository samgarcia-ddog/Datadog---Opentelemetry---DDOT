import type { Cart, Category, OrderResponse, Product, ProductsResponse } from "@/types";

// En K8s: URLs relativas — el Ingress rutea /api/* al backend
// En dev local: apuntar a http://localhost:5000
const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    // Desactivar caché de Next.js para datos dinámicos del e-commerce
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`API error ${res.status}: ${path}`);
  return res.json() as Promise<T>;
}

// ─── Products ─────────────────────────────────────────────────────────────────
export const getProducts = (params?: {
  category?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}) => {
  const qs = new URLSearchParams();
  if (params?.category) qs.set("category", params.category);
  if (params?.search)   qs.set("search",   params.search);
  if (params?.page)     qs.set("page",     String(params.page));
  if (params?.pageSize) qs.set("pageSize", String(params.pageSize));
  return fetchJson<ProductsResponse>(`/api/products?${qs.toString()}`);
};

export const getProduct    = (id: number)    => fetchJson<Product>(`/api/products/${id}`);
export const getCategories = ()              => fetchJson<Category[]>("/api/products/categories");
export const getFeatured   = ()              => fetchJson<Product[]>("/api/products/featured");

// ─── Cart ─────────────────────────────────────────────────────────────────────
export const getCart = (sessionId: string) =>
  fetchJson<Cart>(`/api/cart/${sessionId}`);

export const addToCart = (sessionId: string, productId: number, quantity = 1) =>
  fetchJson<{ message: string; itemCount: number }>(`/api/cart/${sessionId}/add`, {
    method: "POST",
    body: JSON.stringify({ productId, quantity }),
  });

export const removeFromCart = (sessionId: string, productId: number) =>
  fetchJson<{ message: string }>(`/api/cart/${sessionId}/remove/${productId}`, {
    method: "DELETE",
  });

export const clearCart = (sessionId: string) =>
  fetchJson<{ message: string }>(`/api/cart/${sessionId}`, { method: "DELETE" });

// ─── Orders ───────────────────────────────────────────────────────────────────
export const checkout = (data: {
  sessionId:     string;
  customerEmail: string;
  customerName:  string;
  address:       string;
}) =>
  fetchJson<OrderResponse>("/api/orders/checkout", {
    method: "POST",
    body: JSON.stringify(data),
  });
