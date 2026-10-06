'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { AuctionCarousel } from '@/components/auctions/AuctionCarousel';
import Link from 'next/link';

interface AuctionDetail {
  id: string;
  title: string;
  currentPrice: number;
  startPrice: number;
  stepPrice: number;
  reservePrice: number | null;
  buyNowPrice: number | null;
  endTime: string;
  status: string;
  categoryName: string;
  sellerName: string;
  mediaUrls: string[];
}

export default function AuctionDetailPage() {
  const params = useParams();
  const id = params.id as string;
  
  const [auction, setAuction] = useState<AuctionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    
    fetch(`http://localhost:5000/api/auctions/${id}`)
      .then(res => {
        if (!res.ok) {
          if (res.status === 404) throw new Error('Phiên đấu giá không tồn tại hoặc đã bị xóa.');
          throw new Error('Lỗi tải chi tiết phiên đấu giá.');
        }
        return res.json();
      })
      .then(data => {
        setAuction(data);
        setLoading(false);
      })
      .catch(err => {
        setError(err.message);
        setLoading(false);
      });
  }, [id]);

  if (loading) {
    return (
      <main className="min-h-screen bg-[#0f172a] text-white p-8">
        <div className="max-w-4xl mx-auto grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="aspect-video bg-slate-800 rounded-xl animate-pulse" />
          <div className="space-y-4">
            <div className="h-8 w-3/4 bg-slate-800 rounded animate-pulse" />
            <div className="h-6 w-1/4 bg-slate-800 rounded animate-pulse" />
            <div className="h-24 w-full bg-slate-800 rounded animate-pulse" />
          </div>
        </div>
      </main>
    );
  }

  if (error || !auction) {
    return (
      <main className="min-h-screen bg-[#0f172a] text-white p-8 flex flex-col items-center justify-center">
        <div className="bg-slate-800/50 p-8 rounded-xl border border-slate-700 text-center max-w-md">
          <p className="text-red-400 text-xl mb-6">{error || 'Lỗi không xác định'}</p>
          <Link href="/" className="bg-slate-700 hover:bg-slate-600 px-6 py-2 rounded text-white transition-colors">
            Quay lại trang chủ
          </Link>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#0f172a] text-white p-8">
      <div className="max-w-5xl mx-auto">
        <Link href="/" className="text-emerald-500 hover:text-emerald-400 mb-6 inline-block">
          &larr; Quay lại
        </Link>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          {/* Left: Media */}
          <div>
            <AuctionCarousel mediaUrls={auction.mediaUrls} />
          </div>
          
          {/* Right: Info */}
          <div>
            <div className="flex items-center gap-3 mb-2">
              <span className="bg-slate-800 text-slate-300 px-3 py-1 rounded-full text-sm border border-slate-700">
                {auction.categoryName}
              </span>
              <span className="bg-emerald-500/20 text-emerald-400 px-3 py-1 rounded-full text-sm border border-emerald-500/30 font-semibold">
                {auction.status}
              </span>
            </div>
            
            <h1 className="text-3xl font-bold mb-4 text-slate-100">{auction.title}</h1>
            
            <p className="text-slate-400 mb-6">Đăng bởi <span className="text-emerald-400 font-semibold">{auction.sellerName}</span></p>

            <div className="bg-slate-800/80 p-6 rounded-xl border border-slate-700 mb-8 shadow-lg">
              <p className="text-slate-400 text-sm mb-1">Giá hiện tại</p>
              <p className="text-4xl font-mono font-bold text-emerald-400 mb-4">
                {auction.currentPrice.toLocaleString('vi-VN')} đ
              </p>
              
              <div className="grid grid-cols-2 gap-4 text-sm text-slate-300">
                <div>
                  <p className="text-slate-500">Giá khởi điểm</p>
                  <p className="font-mono">{auction.startPrice.toLocaleString('vi-VN')} đ</p>
                </div>
                <div>
                  <p className="text-slate-500">Bước giá</p>
                  <p className="font-mono">{auction.stepPrice.toLocaleString('vi-VN')} đ</p>
                </div>
              </div>
            </div>

            <div className="bg-slate-800/50 p-6 rounded-xl border border-slate-700 shadow-inner">
              <h3 className="font-bold mb-2">Kết thúc vào</h3>
              <p className="font-mono text-xl text-amber-400">
                {new Date(auction.endTime).toLocaleString('vi-VN')}
              </p>
            </div>
            
            {/* Future Bid Action Area */}
            <div className="mt-8 p-4 border border-slate-700 border-dashed rounded-xl text-center text-slate-500">
              [Vùng đặt giá sẽ được phát triển ở Task sau]
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
