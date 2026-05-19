import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from '../../src/frontend/src/App';
import type { Product } from '../../src/frontend/src/types';

const mockProducts: Product[] = [
  {
    id: 1,
    name: 'Test Headphones',
    description: 'Great sound quality.',
    price: 79.99,
    category: 'Electronics',
    stock: 10,
    imageUrl: 'https://example.com/headphones.jpg',
  },
];

vi.mock('../../src/frontend/src/hooks/useProducts');
vi.mock('../../src/frontend/src/api');

import { useProducts } from '../../src/frontend/src/hooks/useProducts';
import { addToCart, fetchCart } from '../../src/frontend/src/api';

const mockedUseProducts = vi.mocked(useProducts);
const mockedAddToCart = vi.mocked(addToCart);
const mockedFetchCart = vi.mocked(fetchCart);

describe('App', () => {
  beforeEach(() => {
    mockedFetchCart.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the header with shop name', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: false, error: null });

    render(<App />);

    expect(screen.getByText('Mock Shop')).toBeInTheDocument();
  });

  it('renders the hero banner', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: false, error: null });

    render(<App />);

    expect(screen.getByText(/discover quality products/i)).toBeInTheDocument();
  });

  it('renders the products section heading', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: false, error: null });

    render(<App />);

    expect(screen.getByRole('heading', { name: /our products/i })).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: true, error: null });

    render(<App />);

    expect(screen.getByText(/loading products/i)).toBeInTheDocument();
  });

  it('shows error state', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: false, error: 'Network error' });

    render(<App />);

    expect(screen.getByText(/error: network error/i)).toBeInTheDocument();
  });

  it('renders product list when loaded', () => {
    mockedUseProducts.mockReturnValue({ products: mockProducts, loading: false, error: null });

    render(<App />);

    expect(screen.getByText('Test Headphones')).toBeInTheDocument();
  });

  it('shows notification after adding to cart', async () => {
    mockedUseProducts.mockReturnValue({ products: mockProducts, loading: false, error: null });
    mockedAddToCart.mockResolvedValue({
      productId: 1,
      productName: 'Test Headphones',
      unitPrice: 79.99,
      quantity: 1,
      totalPrice: 79.99,
    });

    render(<App />);
    await userEvent.click(screen.getByRole('button', { name: /add test headphones to cart/i }));

    expect(await screen.findByRole('status')).toHaveTextContent('"Test Headphones" added to cart!');
  });

  it('shows error notification when add to cart fails', async () => {
    mockedUseProducts.mockReturnValue({ products: mockProducts, loading: false, error: null });
    mockedAddToCart.mockRejectedValue(new Error('Server error'));

    render(<App />);
    await userEvent.click(screen.getByRole('button', { name: /add test headphones to cart/i }));

    expect(await screen.findByRole('status')).toHaveTextContent('Failed to add item to cart.');
  });

  it('cartItemCount is 0 when cartItems is empty', () => {
    mockedUseProducts.mockReturnValue({ products: [], loading: false, error: null });

    render(<App />);

    expect(screen.getByRole('button', { name: /shopping cart with 0 items/i })).toBeInTheDocument();
  });

  it('cartItemCount reflects sum of quantities returned by fetchCart', async () => {
    mockedUseProducts.mockReturnValue({ products: mockProducts, loading: false, error: null });
    mockedAddToCart.mockResolvedValue({
      productId: 1,
      productName: 'Test Headphones',
      unitPrice: 79.99,
      quantity: 1,
      totalPrice: 79.99,
    });
    mockedFetchCart.mockResolvedValue([
      { productId: 1, productName: 'Test Headphones', unitPrice: 79.99, quantity: 3, totalPrice: 239.97 },
    ]);

    render(<App />);
    await userEvent.click(screen.getByRole('button', { name: /add test headphones to cart/i }));

    expect(await screen.findByRole('button', { name: /shopping cart with 3 items/i })).toBeInTheDocument();
  });
});
