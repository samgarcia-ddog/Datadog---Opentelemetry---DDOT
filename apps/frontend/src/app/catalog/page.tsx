import Link from "next/link";
import { getProducts, getCategories } from "@/lib/api";
import { ProductCard } from "@/components/ProductCard";

interface Props {
  searchParams: { category?: string; search?: string; page?: string };
}

export default async function CatalogPage({ searchParams }: Props) {
  const page     = Number(searchParams.page ?? 1);
  const category = searchParams.category;
  const search   = searchParams.search;

  const [data, categories] = await Promise.all([
    getProducts({ category, search, page, pageSize: 12 }),
    getCategories(),
  ]);

  const totalPages = Math.ceil(data.total / data.pageSize);

  return (
    <div className="flex gap-8">
      {/* Sidebar de categorías */}
      <aside className="hidden lg:block w-52 shrink-0 space-y-2">
        <h3 className="font-semibold text-gray-400 uppercase text-xs tracking-wider mb-4">Categorías</h3>
        <Link
          href="/catalog"
          className={`block px-3 py-2 rounded-lg text-sm transition-colors ${
            !category ? "bg-sky-600 text-white" : "text-gray-300 hover:bg-gray-800"
          }`}
        >
          Todas las gorras
        </Link>
        {categories.map((cat) => (
          <Link
            key={cat.id}
            href={`/catalog?category=${cat.slug}`}
            className={`block px-3 py-2 rounded-lg text-sm transition-colors ${
              category === cat.slug ? "bg-sky-600 text-white" : "text-gray-300 hover:bg-gray-800"
            }`}
          >
            {cat.name}
            <span className="ml-2 text-xs text-gray-500">({cat.count})</span>
          </Link>
        ))}
      </aside>

      {/* Grid de productos */}
      <div className="flex-1 space-y-6">
        {/* Barra de búsqueda */}
        <form className="flex gap-3">
          <input
            type="search"
            name="search"
            defaultValue={search}
            placeholder="Buscar gorras, marcas..."
            className="input flex-1"
          />
          <button type="submit" className="btn-primary">Buscar</button>
        </form>

        {/* Resultado */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-gray-400">
            {data.total} producto{data.total !== 1 ? "s" : ""}
            {category && ` en ${categories.find((c) => c.slug === category)?.name}`}
            {search && ` para "${search}"`}
          </p>
        </div>

        {data.items.length === 0 ? (
          <div className="text-center py-20 text-gray-500">
            No se encontraron productos.
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-6">
            {data.items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}

        {/* Paginación */}
        {totalPages > 1 && (
          <div className="flex justify-center gap-2 pt-4">
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
              <Link
                key={p}
                href={`/catalog?${new URLSearchParams({
                  ...(category && { category }),
                  ...(search   && { search }),
                  page: String(p),
                })}`}
                className={`w-9 h-9 flex items-center justify-center rounded-lg text-sm font-medium transition-colors ${
                  p === page ? "bg-sky-600 text-white" : "bg-gray-800 hover:bg-gray-700 text-gray-300"
                }`}
              >
                {p}
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
