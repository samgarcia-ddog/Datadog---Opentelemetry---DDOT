import Image from "next/image";
import Link from "next/link";
import { getFeatured, getCategories } from "@/lib/api";
import { ProductCard } from "@/components/ProductCard";

export default async function HomePage() {
  const [featured, categories] = await Promise.all([getFeatured(), getCategories()]);

  return (
    <div className="space-y-16">
      {/* Hero */}
      <section className="text-center py-16 space-y-6">
        <h1 className="text-5xl font-black tracking-tight">
          <span className="text-sky-400">Gorras</span> para todos los estilos
        </h1>
        <p className="text-gray-400 text-lg max-w-xl mx-auto">
          Snapbacks, fitteds, truckers, bucket hats y beanies.
          Los mejores brands, envío a todo México.
        </p>
        <div className="flex gap-4 justify-center">
          <Link href="/catalog" className="btn-primary text-lg px-8 py-3">
            Ver catálogo
          </Link>
          <Link href="/catalog?category=snapback" className="btn-secondary text-lg px-8 py-3">
            Snapbacks
          </Link>
        </div>
      </section>

      {/* Categorías */}
      <section>
        <h2 className="text-2xl font-bold mb-6">Explorar por categoría</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4">
          {categories.map((cat) => (
            <Link
              key={cat.id}
              href={`/catalog?category=${cat.slug}`}
              className="card p-4 text-center hover:border-sky-500 transition-colors group"
            >
              <div className="text-3xl mb-2">
                {categoryEmoji(cat.slug)}
              </div>
              <p className="font-semibold group-hover:text-sky-400 transition-colors">{cat.name}</p>
              <p className="text-xs text-gray-500">{cat.count} productos</p>
            </Link>
          ))}
        </div>
      </section>

      {/* Destacados */}
      <section>
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Destacados</h2>
          <Link href="/catalog" className="text-sky-400 hover:text-sky-300 text-sm">
            Ver todos →
          </Link>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {featured.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      </section>

      {/* Banner de observabilidad (solo en lab) */}
      <section className="card p-6 border-sky-800 bg-sky-950/30">
        <h3 className="text-sky-400 font-bold text-lg mb-2">🔭 Lab de Observabilidad</h3>
        <p className="text-gray-400 text-sm">
          Esta app está instrumentada con <strong className="text-white">OpenTelemetry SDK</strong>.
          Cambia el escenario activo con <code className="text-sky-400">make scenario-N</code> para
          comparar: DDOT Collector, OTel Collector + Datadog Exporter, Datadog Agent OTLP,
          Datadog SDK y Prometheus + Grafana.
        </p>
      </section>
    </div>
  );
}

function categoryEmoji(slug: string): string {
  const map: Record<string, string> = {
    snapback:    "🧢",
    fitted:      "👒",
    trucker:     "🚚",
    "bucket-hat":"🪣",
    beanie:      "🎿",
  };
  return map[slug] ?? "🎩";
}
