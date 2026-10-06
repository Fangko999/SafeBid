export async function withdrawWithRetry(amount: number, maxRetries = 2): Promise<void> {
  let retries = 0;

  while (true) {
    const response = await fetch('/api/wallet/withdraw', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ amount }),
    });

    if (response.ok) {
      return;
    }

    if (response.status === 409 && retries < maxRetries) {
      retries++;
      // Wait for a random duration between 300ms and 500ms
      const delay = Math.floor(Math.random() * (500 - 300 + 1)) + 300;
      await new Promise((resolve) => setTimeout(resolve, delay));
      continue;
    }

    if (response.status === 500) {
      throw new Error('Lỗi kết nối hệ thống, vui lòng thử lại sau.');
    }

    if (response.status === 409) {
      throw new Error('Hệ thống đang bận. Vui lòng thử lại sau.');
    }

    const data = await response.json().catch(() => ({}));
    throw new Error(data?.message || data?.error?.message || 'Đã có lỗi xảy ra');
  }
}

export async function fetchWalletBalance() {
  const response = await fetch('/api/wallet/balance');
  if (!response.ok) {
    throw new Error('Lỗi tải số dư');
  }
  return response.json();
}

export async function fetchWalletTransactions(page = 1, pageSize = 10) {
  const response = await fetch(`/api/wallet/transactions?page=${page}&pageSize=${pageSize}`);
  if (!response.ok) {
    throw new Error('Lỗi tải lịch sử giao dịch');
  }
  return response.json();
}
