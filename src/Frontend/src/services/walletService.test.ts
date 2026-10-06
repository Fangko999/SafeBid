import { describe, it, expect, vi, beforeEach } from 'vitest';
import { withdrawWithRetry } from './walletService';

// Mock global fetch
const fetchMock = vi.fn();
global.fetch = fetchMock;

describe('walletService - withdrawWithRetry', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should succeed on first try if 200 OK', async () => {
    fetchMock.mockResolvedValueOnce({ ok: true });

    const promise = withdrawWithRetry(50000);
    await promise;

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('should retry up to 2 times on 409 Conflict', async () => {
    // Fail with 409 twice, then succeed
    fetchMock
      .mockResolvedValueOnce({ ok: false, status: 409 })
      .mockResolvedValueOnce({ ok: false, status: 409 })
      .mockResolvedValueOnce({ ok: true });

    const promise = withdrawWithRetry(50000);
    
    // Fast-forward timers for retries (300-500ms each)
    await vi.runAllTimersAsync();
    await promise;

    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it('should throw error after 2 retries if still 409', async () => {
    // Fail 3 times with 409
    fetchMock.mockResolvedValue({ ok: false, status: 409 });

    const promise = withdrawWithRetry(50000);
    
    // Attach the expect handler immediately so there's no unhandled rejection
    const expectPromise = expect(promise).rejects.toThrow('Hệ thống đang bận. Vui lòng thử lại sau.');
    
    await vi.runAllTimersAsync();

    await expectPromise;
    expect(fetchMock).toHaveBeenCalledTimes(3); // 1 initial + 2 retries
  });
});
