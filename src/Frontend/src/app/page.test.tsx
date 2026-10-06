import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import Page from './page';

// Mock fetch
global.fetch = vi.fn() as unknown as typeof fetch;

describe('Home Page', () => {
  it('shows loading state initially', () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {})); // Never resolves
    render(<Page />);
    expect(screen.getByTestId('loading-skeleton')).toBeInTheDocument();
  });

  it('shows empty state when no auctions', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: [] }),
    });
    
    render(<Page />);
    
    const emptyMsg = await screen.findByText(/Không có phiên đấu giá nào/i);
    expect(emptyMsg).toBeInTheDocument();
  });

  it('shows auction list on success', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        items: [
          {
            id: 'auc-1',
            title: 'Test Auction 1',
            currentPrice: 150000,
            status: 'Active',
            mainImageUrl: 'http://img.jpg',
            categoryName: 'Tech',
            sellerName: 'User A'
          }
        ]
      }),
    });
    
    render(<Page />);
    
    const title = await screen.findByText('Test Auction 1');
    expect(title).toBeInTheDocument();
    expect(screen.getByText(/150[.,]000\s*đ/)).toBeInTheDocument();
  });
});
