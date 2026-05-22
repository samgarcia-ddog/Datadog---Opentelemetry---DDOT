import Link from "next/link";

interface Props { searchParams: { orderId?: string; total?: string } }

export default function OrderSuccessPage({ searchParams }: Props) {
  return (
    <div className="text-center py-20 space-y-6 max-w-md mx-auto">
      <div className="text-6xl">🎉</div>
      <h1 className="text-3xl font-bold text-sky-400">¡Orden confirmada!</h1>
      <p className="text-gray-400">
        Tu orden <strong className="text-white">#{searchParams.orderId}</strong> fue
        procesada exitosamente.
      </p>
      {searchParams.total && (
        <p className="text-2xl font-bold">Total: ${Number(searchParams.total).toFixed(2)}</p>
      )}
      <p className="text-sm text-gray-500">Recibirás un correo con los detalles del envío.</p>
      <Link href="/" className="btn-primary inline-block text-lg px-8">
        Volver a la tienda
      </Link>
    </div>
  );
}
