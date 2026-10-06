// @vitest-environment jsdom
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { WatchlistButton } from './WatchlistButton';

global.fetch = vi.fn() as unknown as typeof fetch;

describe('WatchlistButton', () => {
  it('toggles state optimistically and calls API', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true });
    
    render(<WatchlistButton auctionId="auc-1" initialIsWatched={false} />);
    
    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('data-watched', 'false');
    
    // Click to watch
    fireEvent.click(button);
    
    // Optimistic UI update
    expect(button).toHaveAttribute('data-watched', 'true');
    expect(global.fetch).toHaveBeenCalledWith('/api/auctions/auc-1/watchlist', expect.objectContaining({
      method: 'POST'
    }));
  });

  it('reverts state optimistically if API fails', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
    
    render(<WatchlistButton auctionId="auc-1" initialIsWatched={false} />);
    
    const button = screen.getByRole('button');
    fireEvent.click(button);
    expect(button).toHaveAttribute('data-watched', 'true');
    
    // Reverts after fail
    await waitFor(() => {
      expect(button).toHaveAttribute('data-watched', 'false');
    });
  });
});
