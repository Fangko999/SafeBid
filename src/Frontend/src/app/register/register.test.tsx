import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'
import userEvent from '@testing-library/user-event'
import RegisterPage from './page'
import { vi } from 'vitest'
import { toast } from 'sonner'

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  }
}))

describe('Register UI (TDD)', () => {
  beforeEach(() => {
    global.fetch = vi.fn()
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.clearAllMocks()
  })

  it('RED: should show validation errors when form is submitted empty', async () => {
    render(<RegisterPage />)
    const form = screen.getByTestId('register-form')
    
    // Attempt submit without filling
    fireEvent.submit(form)
    
    // We expect zod to complain about email, password, etc.
    expect(await screen.findByText(/email không hợp lệ/i)).toBeInTheDocument()
    expect(await screen.findByText(/mật khẩu ít nhất 6 ký tự/i)).toBeInTheDocument()
  })

  it('RED: should submit form and show success toast', async () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({})
    })

    render(<RegisterPage />)
    const user = userEvent.setup()

    await user.type(screen.getByTestId('register-email'), 'test@abc.com')
    await user.type(screen.getByTestId('register-password'), 'P@ssw0rd123')
    await user.type(screen.getByTestId('register-confirm-password'), 'P@ssw0rd123')
    await user.type(screen.getByTestId('register-fullname'), 'Nguyen Van A')
    await user.type(screen.getByTestId('register-phone'), '0912345678')

    fireEvent.submit(screen.getByTestId('register-form'))

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith('/api/auth/register', expect.objectContaining({
        method: 'POST'
      }))
      expect(toast.success).toHaveBeenCalledWith(expect.stringMatching(/đăng ký thành công/i))
    })
  })

  it('RED: should show 429 Rate Limit error toast', async () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: false,
      status: 429,
      json: async () => ({})
    })

    render(<RegisterPage />)
    const user = userEvent.setup()

    await user.type(screen.getByTestId('register-email'), 'test2@abc.com')
    await user.type(screen.getByTestId('register-password'), 'P@ssw0rd123')
    await user.type(screen.getByTestId('register-confirm-password'), 'P@ssw0rd123')
    await user.type(screen.getByTestId('register-fullname'), 'Nguyen Van A')
    await user.type(screen.getByTestId('register-phone'), '0912345678')

    fireEvent.submit(screen.getByTestId('register-form'))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(expect.stringMatching(/quá nhiều yêu cầu/i))
    })
  })
})
