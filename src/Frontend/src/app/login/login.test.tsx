import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'
import userEvent from '@testing-library/user-event'
import LoginPage from './page'
import { vi } from 'vitest'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/store/authStore'

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  }
}))

vi.mock('next/navigation', () => ({
  useRouter: vi.fn(),
}))

describe('Login UI (TDD)', () => {
  const mockPush = vi.fn()

  beforeEach(() => {
    global.fetch = vi.fn()
    ;(useRouter as import("vitest").Mock).mockReturnValue({ push: mockPush })
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.clearAllMocks()
  })

  it('RED: should show validation errors when form is submitted empty', async () => {
    render(<LoginPage />)
    const form = screen.getByTestId('login-form')
    
    fireEvent.submit(form)
    
    expect(await screen.findByText(/email không hợp lệ/i)).toBeInTheDocument()
    expect(await screen.findByText(/mật khẩu ít nhất 6 ký tự/i)).toBeInTheDocument()
  })

  it('RED: should submit form, update auth state and redirect on success', async () => {
    ;(global.fetch as import("vitest").Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ message: 'Login successful' })
    }).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ Id: '123', Email: 'test@abc.com', FullName: 'Test User' })
    })

    render(<LoginPage />)
    const user = userEvent.setup()

    await user.type(screen.getByTestId('login-email'), 'test@abc.com')
    await user.type(screen.getByTestId('login-password'), 'P@ssw0rd123')

    fireEvent.submit(screen.getByTestId('login-form'))

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith('/api/auth/login', expect.objectContaining({
        method: 'POST'
      }))
      expect(useAuthStore.getState().isAuthenticated).toBe(true)
      expect(toast.success).toHaveBeenCalledWith(expect.stringMatching(/đăng nhập thành công/i))
      expect(mockPush).toHaveBeenCalledWith('/')
    })
  })

  it('RED: should show 401 Unauthorized error toast when credentials are wrong', async () => {
    ;(global.fetch as import("vitest").Mock).mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: async () => ({ error: { message: 'Invalid email or password.' } })
    })

    render(<LoginPage />)
    const user = userEvent.setup()

    await user.type(screen.getByTestId('login-email'), 'test@abc.com')
    await user.type(screen.getByTestId('login-password'), 'WrongPass123')

    fireEvent.submit(screen.getByTestId('login-form'))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(expect.stringMatching(/invalid email or password/i))
      expect(useAuthStore.getState().isAuthenticated).toBe(false)
      expect(mockPush).not.toHaveBeenCalled()
    })
  })
})
