import { useState } from 'react';
import { Heart } from 'lucide-react';
import { toast } from 'sonner';

export function WatchlistButton({ auctionId, initialIsWatched }: { auctionId: string, initialIsWatched: boolean }) {
  const [isWatched, setIsWatched] = useState(initialIsWatched);
  const [isLoading, setIsLoading] = useState(false);

  const toggleWatchlist = async () => {
    if (isLoading) return;
    setIsLoading(true);
    const prev = isWatched;
    setIsWatched(!prev); // Optimistic

    try {
      const res = await fetch(`/api/auctions/${auctionId}/watchlist`, {
        method: 'POST',
      });
      if (!res.ok) throw new Error('API Error');
    } catch (e) {
      setIsWatched(prev); // Rollback
      toast.error('Không thể cập nhật danh sách theo dõi');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <button 
      onClick={toggleWatchlist} 
      data-watched={isWatched}
      className={`p-2 rounded-full backdrop-blur-md transition-all active:scale-95 ${isWatched ? 'bg-rose-500/20 text-rose-500' : 'bg-slate-800/50 text-slate-300 hover:text-white hover:bg-slate-700/50'}`}
      aria-label="Theo dõi phiên đấu giá"
    >
      <Heart className={isWatched ? 'fill-current' : ''} />
    </button>
  );
}
