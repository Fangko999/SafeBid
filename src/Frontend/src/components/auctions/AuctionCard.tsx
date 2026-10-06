import Link from 'next/link';

interface AuctionCardProps {
  id: string;
  title: string;
  currentPrice: number;
  endTime: string;
  mainImageUrl: string;
}

export function AuctionCard({ id, title, currentPrice, endTime, mainImageUrl }: AuctionCardProps) {
  return (
    <Link href={`/auctions/${id}`} className="block">
      <div className="bg-slate-800 rounded-xl overflow-hidden border border-slate-700 hover:border-emerald-500 transition-colors shadow-lg group">
        <div className="relative h-48 bg-slate-900 w-full">
          {mainImageUrl ? (
            <img 
              src={mainImageUrl} 
              alt={title} 
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" 
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-slate-500">
              No Image
            </div>
          )}
          <div className="absolute top-2 right-2 bg-emerald-500 text-white text-xs px-2 py-1 rounded shadow">
            ACTIVE
          </div>
        </div>
        <div className="p-4">
          <h3 className="font-bold text-lg text-slate-100 truncate mb-2">{title}</h3>
          <div className="flex justify-between items-end">
            <div>
              <p className="text-sm text-slate-400">Current Bid</p>
              <p className="font-mono font-bold text-xl text-emerald-400">
                {currentPrice.toLocaleString('vi-VN')} đ
              </p>
            </div>
          </div>
        </div>
      </div>
    </Link>
  );
}
