import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import WalletPage from './page';
import { toast } from 'sonner';

// Mock matchMedia for components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

const mockFetch = vi.fn();
global.fetch = mockFetch;

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('WalletPage', () => {
  beforeEach(() => {
    mockFetch.mockReset();
  });

  it('renders Loading skeleton initially', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {})); // Never resolves
    render(<WalletPage />);
    expect(screen.getByTestId('wallet-loading-skeleton')).toBeInTheDocument();
  });

  it('renders normal state with balance and transactions', async () => {
    mockFetch.mockImplementation((url) => {
      if (url.includes('/api/wallet/balance')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ availableBalance: 100000, holdAmount: 50000 }),
        });
      }
      if (url.includes('/api/wallet/transactions')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({
            items: [
              { id: '1', amount: 20000, type: 'DEPOSIT', status: 'COMPLETED', createdAt: '2023-10-01T10:00:00Z' },
            ],
            totalCount: 1, totalPages: 1, currentPage: 1, hasNext: false
          }),
        });
      }
      return Promise.resolve({ ok: false });
    });

    render(<WalletPage />);

    await waitFor(() => {
      expect(screen.queryByTestId('wallet-loading-skeleton')).not.toBeInTheDocument();
    });

    expect(screen.getByText(/100\.000/i)).toBeInTheDocument();
    expect(screen.getByText(/50\.000/i)).toBeInTheDocument();
    expect(screen.getByText(/DEPOSIT/i)).toBeInTheDocument();
    expect(screen.getByText(/20\.000/i)).toBeInTheDocument();
  });

  it('renders empty state when no transactions', async () => {
    mockFetch.mockImplementation((url) => {
      if (url.includes('/api/wallet/balance')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ availableBalance: 100000, holdAmount: 0 }),
        });
      }
      if (url.includes('/api/wallet/transactions')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ items: [], totalCount: 0, totalPages: 0, currentPage: 1, hasNext: false }),
        });
      }
      return Promise.resolve({ ok: false });
    });

    render(<WalletPage />);

    await waitFor(() => {
      expect(screen.getByText(/Chưa có giao dịch nào/i)).toBeInTheDocument();
    });
  });

  it('renders error state when balance fetch fails', async () => {
    mockFetch.mockImplementation(() => Promise.resolve({ ok: false, status: 500 }));

    render(<WalletPage />);

    await waitFor(() => {
      expect(screen.getByText(/Không tải được số dư/i)).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: /Thử lại/i })).toBeInTheDocument();
  });

  it('validates minimum withdrawal amount', async () => {
    mockFetch.mockImplementation((url) => {
      if (url.includes('/api/wallet/balance')) return Promise.resolve({ ok: true, json: () => Promise.resolve({ availableBalance: 100000 }) });
      if (url.includes('/api/wallet/transactions')) return Promise.resolve({ ok: true, json: () => Promise.resolve({ items: [] }) });
      return Promise.resolve({ ok: false });
    });

    render(<WalletPage />);
    await waitFor(() => expect(screen.queryByTestId('wallet-loading-skeleton')).not.toBeInTheDocument());

    const input = screen.getByPlaceholderText(/Số tiền/i);
    const submitBtn = screen.getByRole('button', { name: /Rút tiền/i });

    await userEvent.type(input, '5000');
    fireEvent.submit(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/Tối thiểu 10,000/i)).toBeInTheDocument();
    });
  });

  it('calls POST API successfully on valid withdrawal', async () => {
    mockFetch.mockImplementation((url, config) => {
      if (url.includes('/api/wallet/balance')) return Promise.resolve({ ok: true, json: () => Promise.resolve({ availableBalance: 100000 }) });
      if (url.includes('/api/wallet/transactions')) return Promise.resolve({ ok: true, json: () => Promise.resolve({ items: [] }) });
      if (url.includes('/api/wallet/withdraw') && config?.method === 'POST') {
        return Promise.resolve({ ok: true });
      }
      return Promise.resolve({ ok: false });
    });

    render(<WalletPage />);
    await waitFor(() => expect(screen.queryByTestId('wallet-loading-skeleton')).not.toBeInTheDocument());

    const input = screen.getByPlaceholderText(/Số tiền/i);
    const submitBtn = screen.getByRole('button', { name: /Rút tiền/i });

    await userEvent.type(input, '20000');
    fireEvent.submit(submitBtn);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith('/api/wallet/withdraw', expect.any(Object));
      expect(toast.success).toHaveBeenCalledWith(expect.stringMatching(/thành công/i));
    });
  });
});
