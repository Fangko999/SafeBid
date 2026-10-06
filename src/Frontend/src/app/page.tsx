'use client';
import { useEffect, useState } from 'react';
import { AuctionCard } from '@/components/auctions/AuctionCard';

interface Auction {
  id: string;
  title: string;
  currentPrice: number;
  status: string;
  mainImageUrl: string;
  categoryName: string;
  sellerName: string;
}

export default function Home() {
  const [auctions, setAuctions] = useState<Auction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAuctions = () => {
    setLoading(true);
    setError(null);
    fetch('http://localhost:5000/api/auctions')
      .then(res => {
        if (!res.ok) throw new Error('Lỗi kết nối máy chủ.');
        return res.json();
      })
      .then(data => {
        setAuctions(data.items || []);
        setLoading(false);
      })
      .catch(err => {
        setError(err.message);
        setLoading(false);
      });
  };

  useEffect(() => {
    const init = async () => {
      fetchAuctions();
    };
    init();
  }, []);

  return (
    <main className="min-h-screen bg-[#0f172a] text-white p-8">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-3xl font-bold mb-8 text-slate-100">Đang diễn ra</h1>

        {loading && (
          <div data-testid="loading-skeleton" className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {[1, 2, 3, 4].map(i => (
              <div key={i} className="bg-slate-800 rounded-xl h-72 animate-pulse border border-slate-700" />
            ))}
          </div>
        )}

        {error && !loading && (
          <div className="flex flex-col items-center justify-center p-12 bg-slate-800/50 rounded-xl border border-slate-700">
            <p className="text-red-400 mb-4">{error}</p>
            <button onClick={fetchAuctions} className="bg-slate-700 hover:bg-slate-600 px-4 py-2 rounded">
              Thử lại
            </button>
          </div>
        )}

        {!loading && !error && auctions.length === 0 && (
          <div className="flex flex-col items-center justify-center p-12 bg-slate-800/50 rounded-xl border border-slate-700 text-slate-400">
            <p className="text-xl mb-4">Không có phiên đấu giá nào đang mở.</p>
            <button onClick={fetchAuctions} className="bg-slate-700 hover:bg-slate-600 px-4 py-2 rounded text-white">
              Tải lại
            </button>
          </div>
        )}

        {!loading && !error && auctions.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {auctions.map(auction => (
              <AuctionCard
                key={auction.id}
                id={auction.id}
                title={auction.title}
                currentPrice={auction.currentPrice}
                endTime={auction.status}
                mainImageUrl={auction.mainImageUrl}
              />
            ))}
          </div>
        )}
      </div>
    </main>
  );
}
