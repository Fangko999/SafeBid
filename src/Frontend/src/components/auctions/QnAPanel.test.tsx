import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QnAPanel } from './QnAPanel';

describe('QnAPanel', () => {
  const mockQuestions = [
    {
      id: 'q1',
      maskedAskerName: 'Nguyễn A***',
      content: 'Hàng mới bao nhiêu %?',
      createdAt: new Date().toISOString(),
      answer: null,
      answeredAt: null
    },
    {
      id: 'q2',
      maskedAskerName: 'Lê B***',
      content: 'Có fix không shop?',
      createdAt: new Date().toISOString(),
      answer: 'Không bạn nhé, giá chót.',
      answeredAt: new Date().toISOString()
    }
  ];

  it('renders list of questions correctly', () => {
    render(<QnAPanel auctionId="auc-1" isSeller={false} isAuthenticated={true} initialQuestions={mockQuestions} />);
    expect(screen.getByText('Hàng mới bao nhiêu %?')).toBeInTheDocument();
    expect(screen.getByText('Có fix không shop?')).toBeInTheDocument();
    expect(screen.getByText('Không bạn nhé, giá chót.')).toBeInTheDocument();
  });

  it('shows question form if authenticated', () => {
    render(<QnAPanel auctionId="auc-1" isSeller={false} isAuthenticated={true} initialQuestions={[]} />);
    expect(screen.getByPlaceholderText('Nhập câu hỏi của bạn...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /gửi câu hỏi/i })).toBeInTheDocument();
  });

  it('hides question form if not authenticated', () => {
    render(<QnAPanel auctionId="auc-1" isSeller={false} isAuthenticated={false} initialQuestions={[]} />);
    expect(screen.queryByPlaceholderText('Nhập câu hỏi của bạn...')).not.toBeInTheDocument();
    expect(screen.getByText(/Vui lòng đăng nhập để đặt câu hỏi/i)).toBeInTheDocument();
  });

  it('shows "Trả lời" button only if isSeller is true and answer is null', () => {
    render(<QnAPanel auctionId="auc-1" isSeller={true} isAuthenticated={true} initialQuestions={mockQuestions} />);
    
    // q1 has no answer, so "Trả lời" button should be present
    const answerButtons = screen.getAllByRole('button', { name: /trả lời/i });
    expect(answerButtons).toHaveLength(1);

    // q2 already has an answer, so no "Trả lời" button for it
  });

  it('does not show "Trả lời" button if isSeller is false', () => {
    render(<QnAPanel auctionId="auc-1" isSeller={false} isAuthenticated={true} initialQuestions={mockQuestions} />);
    expect(screen.queryByRole('button', { name: /trả lời/i })).not.toBeInTheDocument();
  });

  it('calls API when submitting a new question', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({ id: 'q3', maskedAskerName: 'Me***', content: 'Test?', createdAt: new Date().toISOString() })
    });

    render(<QnAPanel auctionId="auc-1" isSeller={false} isAuthenticated={true} initialQuestions={[]} />);
    
    const input = screen.getByPlaceholderText('Nhập câu hỏi của bạn...');
    const button = screen.getByRole('button', { name: /gửi câu hỏi/i });

    fireEvent.change(input, { target: { value: 'Test?' } });
    fireEvent.click(button);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith('/api/auctions/auc-1/questions', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ content: 'Test?' })
      }));
    });

    expect(screen.getByText('Test?')).toBeInTheDocument();
  });

  it('calls API when seller answers a question', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true });

    render(<QnAPanel auctionId="auc-1" isSeller={true} isAuthenticated={true} initialQuestions={mockQuestions} />);
    
    const answerButton = screen.getByRole('button', { name: /trả lời/i });
    fireEvent.click(answerButton);

    const input = screen.getByPlaceholderText('Nhập câu trả lời...');
    const submitButton = screen.getByRole('button', { name: /gửi trả lời/i });

    fireEvent.change(input, { target: { value: 'Hàng like new nhé' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith('/api/auctions/auc-1/questions/q1/answer', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ answer: 'Hàng like new nhé' })
      }));
    });

    expect(screen.getByText('Hàng like new nhé')).toBeInTheDocument();
  });
});
