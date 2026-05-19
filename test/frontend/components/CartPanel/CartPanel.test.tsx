import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CartPanel } from '../../../../src/frontend/src/components/CartPanel';
import type { CartItem } from '../../../../src/frontend/src/types';

const mockCartItems: CartItem[] = [
  {
    productId: 1,
    productName: 'Wireless Headphones',
    unitPrice: 79.99,
    quantity: 2,
    totalPrice: 159.98,
  },
  {
    productId: 2,
    productName: 'Running Shoes',
    unitPrice: 59.99,
    quantity: 1,
    totalPrice: 59.99,
  },
];

const defaultProps = {
  isOpen: true,
  onClose: vi.fn(),
  cartItems: mockCartItems,
  onUpdateQuantity: vi.fn(),
  onRemoveItem: vi.fn(),
  onClearCart: vi.fn(),
};

describe('CartPanel', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders nothing when isOpen is false', () => {
    const { container } = render(<CartPanel {...defaultProps} isOpen={false} />);

    expect(container.firstChild).toBeNull();
  });

  it('shows empty state message when cartItems is empty', () => {
    render(<CartPanel {...defaultProps} cartItems={[]} />);

    expect(screen.getByText('Your cart is empty.')).toBeInTheDocument();
  });

  it('renders item names', () => {
    render(<CartPanel {...defaultProps} />);

    expect(screen.getByText('Wireless Headphones')).toBeInTheDocument();
    expect(screen.getByText('Running Shoes')).toBeInTheDocument();
  });

  it('renders quantity inputs with correct values', () => {
    render(<CartPanel {...defaultProps} />);

    expect(screen.getByRole('spinbutton', { name: /quantity for wireless headphones/i })).toHaveValue(2);
    expect(screen.getByRole('spinbutton', { name: /quantity for running shoes/i })).toHaveValue(1);
  });

  it('calls onRemoveItem with correct productId when remove button clicked', async () => {
    const onRemoveItem = vi.fn();
    render(<CartPanel {...defaultProps} onRemoveItem={onRemoveItem} />);

    await userEvent.click(screen.getByRole('button', { name: /remove wireless headphones from cart/i }));

    expect(onRemoveItem).toHaveBeenCalledWith(1);
  });

  it('calls onClearCart when clear cart button is clicked', async () => {
    const onClearCart = vi.fn();
    render(<CartPanel {...defaultProps} onClearCart={onClearCart} />);

    await userEvent.click(screen.getByRole('button', { name: /clear cart/i }));

    expect(onClearCart).toHaveBeenCalled();
  });

  it('calls onUpdateQuantity with correct args when quantity input changes', () => {
    const onUpdateQuantity = vi.fn();
    render(<CartPanel {...defaultProps} onUpdateQuantity={onUpdateQuantity} />);

    fireEvent.change(
      screen.getByRole('spinbutton', { name: /quantity for wireless headphones/i }),
      { target: { value: '3' } },
    );

    expect(onUpdateQuantity).toHaveBeenCalledWith(1, 3);
  });

  it('calls onClose when Escape key is pressed', () => {
    const onClose = vi.fn();
    render(<CartPanel {...defaultProps} onClose={onClose} />);

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(onClose).toHaveBeenCalled();
  });

  it('does not add Escape listener when panel is closed', () => {
    const onClose = vi.fn();
    render(<CartPanel {...defaultProps} isOpen={false} onClose={onClose} />);

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(onClose).not.toHaveBeenCalled();
  });
});
