import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import QuickCapture from '../../components/features/QuickCapture'
import { QueryClient, QueryClientProvider } from 'react-query'
import { BrowserRouter } from 'react-router-dom'

// Mock the API service
vi.mock('../../services/api', () => ({
  captureService: {
    create: vi.fn(),
  },
}))

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

const TestWrapper = ({ children }: { children: React.ReactNode }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  )
}

describe('QuickCapture', () => {
  const user = userEvent.setup()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the quick capture form', () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    expect(screen.getByPlaceholderText(/what's on your mind/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /capture/i })).toBeInTheDocument()
  })

  it('allows user to type in the input field', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    await user.type(input, 'Buy groceries')

    expect(input).toHaveValue('Buy groceries')
  })

  it('displays character count', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    await user.type(input, 'Test')

    expect(screen.getByText('4 / 500')).toBeInTheDocument()
  })

  it('disables submit button when input is empty', () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const submitButton = screen.getByRole('button', { name: /capture/i })
    expect(submitButton).toBeDisabled()
  })

  it('enables submit button when input has content', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    const submitButton = screen.getByRole('button', { name: /capture/i })

    await user.type(input, 'Some content')

    expect(submitButton).toBeEnabled()
  })

  it('prevents input longer than 500 characters', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    const longText = 'a'.repeat(501)

    await user.type(input, longText)

    expect(input).toHaveValue('a'.repeat(500))
    expect(screen.getByText('500 / 500')).toBeInTheDocument()
  })

  it('shows validation error for empty submission', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const form = screen.getByRole('form', { name: /quick capture/i })
    fireEvent.submit(form)

    await waitFor(() => {
      expect(screen.getByText(/content is required/i)).toBeInTheDocument()
    })
  })

  it('clears form after successful submission', async () => {
    const { captureService } = await import('../../services/api')
    vi.mocked(captureService.create).mockResolvedValue({
      id: '1',
      content: 'Test capture',
      type: 'thought',
      createdAt: new Date().toISOString(),
    })

    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    const submitButton = screen.getByRole('button', { name: /capture/i })

    await user.type(input, 'Test capture')
    await user.click(submitButton)

    await waitFor(() => {
      expect(input).toHaveValue('')
    })
  })

  it('handles API errors gracefully', async () => {
    const { captureService } = await import('../../services/api')
    vi.mocked(captureService.create).mockRejectedValue(new Error('API Error'))

    const toast = await import('react-hot-toast')

    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    const submitButton = screen.getByRole('button', { name: /capture/i })

    await user.type(input, 'Test capture')
    await user.click(submitButton)

    await waitFor(() => {
      expect(toast.default.error).toHaveBeenCalledWith('Failed to capture thought')
    })
  })

  it('supports keyboard shortcuts', async () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    await user.type(input, 'Quick thought')

    // Test Ctrl+Enter shortcut
    fireEvent.keyDown(input, { key: 'Enter', ctrlKey: true })

    // Should trigger form submission
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /capture/i })).toHaveAttribute('aria-busy', 'true')
    })
  })

  it('shows appropriate capture type options', () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    // Check if capture type selector is present
    expect(screen.getByLabelText(/capture type/i)).toBeInTheDocument()
    
    // Check for common capture types
    expect(screen.getByText(/thought/i)).toBeInTheDocument()
    expect(screen.getByText(/task/i)).toBeInTheDocument()
    expect(screen.getByText(/idea/i)).toBeInTheDocument()
  })

  it('maintains focus after capturing when configured', async () => {
    render(
      <TestWrapper>
        <QuickCapture keepFocus={true} />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/what's on your mind/i)
    await user.type(input, 'Test')
    await user.click(screen.getByRole('button', { name: /capture/i }))

    await waitFor(() => {
      expect(document.activeElement).toBe(input)
    })
  })

  it('applies ADHD-friendly styling and interactions', () => {
    render(
      <TestWrapper>
        <QuickCapture />
      </TestWrapper>
    )

    const container = screen.getByTestId('quick-capture-container')
    
    // Check for ADHD-friendly design elements
    expect(container).toHaveClass('quick-capture')
    
    // Verify accessibility attributes
    const input = screen.getByPlaceholderText(/what's on your mind/i)
    expect(input).toHaveAttribute('aria-label')
    expect(input).toHaveAttribute('aria-describedby')
  })
})