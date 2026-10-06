// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { BidHistoryPanel } from './BidHistoryPanel';

global.fetch = vi.fn() as unknown as typeof fetch;

describe('BidHistoryPanel', () => {
  it('renders loading skeleton initially', () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));
    render(<BidHistoryPanel auctionId="auc-1" />);
    expect(screen.getByTestId('loading-skeleton')).toBeInTheDocument();
  });

  it('renders empty state when no bids', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: [] })
    });
    render(<BidHistoryPanel auctionId="auc-1" />);
    const msg = await screen.findByText(/Chưa có lượt đặt giá nào/i);
    expect(msg).toBeInTheDocument();
  });

  it('renders masked bid history', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        items: [
          { amount: 150000, timestamp: new Date().toISOString(), maskedBidderName: 'Nguyễn V***' }
        ]
      })
    });
    
    render(<BidHistoryPanel auctionId="auc-1" />);
    
    const bidAmount = await screen.findByText(/150.000\s*đ/);
    expect(bidAmount).toBeInTheDocument();
    expect(screen.getByText('Nguyễn V***')).toBeInTheDocument();
  });
});
