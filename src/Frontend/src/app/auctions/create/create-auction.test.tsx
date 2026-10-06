import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import CreateAuctionPage from './page';

// Mock the API calls
vi.mock('@/services/auctionService', () => ({
  createDraft: vi.fn(),
}));

vi.mock('@/services/categoryService', () => ({
  getCategories: vi.fn().mockResolvedValue([{ id: 'cat-1', name: 'Electronics', children: [] }]),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

describe('CreateAuctionPage Validation', () => {
  it('should show validation errors when submitting empty form', async () => {
    render(<CreateAuctionPage />);
    
    const submitBtn = screen.getByRole('button', { name: /lưu nháp/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/tên phiên đấu giá không được để trống/i)).toBeInTheDocument();
      expect(screen.getByText(/giá khởi điểm phải lớn hơn 0/i)).toBeInTheDocument();
    });
  });
});
