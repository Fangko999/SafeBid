import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import Page from './page';

// Mock params
vi.mock('next/navigation', () => ({
  useParams: () => ({ id: 'auc-1' }),
}));

global.fetch = vi.fn() as unknown as typeof fetch;

describe('Auction Detail Page', () => {
  it('shows error state when 404', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false,
      status: 404
    });
    
    render(<Page />);
    
    const errorMsg = await screen.findByText(/Phiên đấu giá không tồn tại/i);
    expect(errorMsg).toBeInTheDocument();
  });

  it('shows auction detail on success', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        id: 'auc-1',
        title: 'Detail Auction',
        currentPrice: 200000,
        startPrice: 100000,
        stepPrice: 10000,
        reservePrice: null,
        buyNowPrice: null,
        endTime: new Date().toISOString(),
        status: 'Active',
        categoryName: 'Tech',
        sellerName: 'User B',
        mediaUrls: ['http://img1.jpg', 'http://img2.jpg']
      }),
    });
    
    render(<Page />);
    
    const title = await screen.findByText('Detail Auction');
    expect(title).toBeInTheDocument();
    
    const images = await screen.findAllByRole('img');
    expect(images.length).toBeGreaterThan(0);
  });
});
