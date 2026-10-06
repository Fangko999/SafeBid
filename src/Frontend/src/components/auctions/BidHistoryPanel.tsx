import { useEffect, useState } from 'react';
import { Skeleton } from '@/components/ui/skeleton';

type BidItem = {
  amount: number;
  timestamp: string;
  maskedBidderName: string;
};

export function BidHistoryPanel({ auctionId }: { auctionId: string }) {
  const [bids, setBids] = useState<BidItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const fetchBids = async () => {
      try {
        const res = await fetch(`/api/auctions/${auctionId}/bids`);
        if (!res.ok) throw new Error('Lỗi tải lịch sử');
        const data = await res.json();
        if (active) setBids(data.items);
      } catch (err: any) {
        if (active) setError(err.message);
      }
    };
    fetchBids();
    return () => { active = false; };
  }, [auctionId]);

  if (error) {
    return (
      <div className="p-4 border border-rose-500/30 bg-rose-500/10 rounded-xl text-center text-rose-500">
        <p>{error}</p>
        <button onClick={() => window.location.reload()} className="mt-2 text-sm underline">Thử lại</button>
      </div>
    );
  }

  if (!bids) {
    return (
      <div data-testid="loading-skeleton" className="space-y-3">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (bids.length === 0) {
    return (
      <div className="text-center py-8 text-slate-400">
        <p>Chưa có lượt đặt giá nào</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {bids.map((bid, i) => (
        <div key={i} className="flex justify-between items-center p-3 rounded-lg bg-slate-800/50 border border-slate-700/50">
          <div>
            <p className="font-medium text-slate-200">{bid.maskedBidderName}</p>
            <p className="text-xs text-slate-400">{new Date(bid.timestamp).toLocaleString('vi-VN')}</p>
          </div>
          <div className="font-outfit font-bold text-emerald-400">
            {bid.amount.toLocaleString('vi-VN')} đ
          </div>
        </div>
      ))}
    </div>
  );
}
