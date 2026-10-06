'use client';
import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface AuctionCarouselProps {
  mediaUrls: string[];
}

export function AuctionCarousel({ mediaUrls }: AuctionCarouselProps) {
  const [currentIndex, setCurrentIndex] = useState(0);

  if (!mediaUrls || mediaUrls.length === 0) {
    return (
      <div className="w-full aspect-video bg-slate-800 rounded-xl flex items-center justify-center text-slate-500 border border-slate-700">
        No Images
      </div>
    );
  }

  const handlePrev = () => {
    setCurrentIndex(prev => (prev === 0 ? mediaUrls.length - 1 : prev - 1));
  };

  const handleNext = () => {
    setCurrentIndex(prev => (prev === mediaUrls.length - 1 ? 0 : prev + 1));
  };

  return (
    <div className="relative w-full aspect-video bg-black rounded-xl overflow-hidden group">
      <img
        src={mediaUrls[currentIndex]}
        alt={`Auction media ${currentIndex + 1}`}
        className="w-full h-full object-contain"
      />
      
      {mediaUrls.length > 1 && (
        <>
          <button
            onClick={handlePrev}
            className="absolute left-2 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/80 text-white p-2 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
          >
            <ChevronLeft size={24} />
          </button>
          
          <button
            onClick={handleNext}
            className="absolute right-2 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/80 text-white p-2 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
          >
            <ChevronRight size={24} />
          </button>
          
          <div className="absolute bottom-4 left-1/2 -translate-y-1/2 flex gap-2">
            {mediaUrls.map((_, idx) => (
              <button
                key={idx}
                onClick={() => setCurrentIndex(idx)}
                className={`w-2 h-2 rounded-full transition-colors ${
                  idx === currentIndex ? 'bg-emerald-500' : 'bg-white/50 hover:bg-white/80'
                }`}
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
}
