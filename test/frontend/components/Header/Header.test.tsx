import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Header } from '../../../../src/frontend/src/components/Header';

describe('Header', () => {
  it('renders the shop name', () => {
    render(<Header cartItemCount={0} onCartClick={vi.fn()} />);
    expect(screen.getByText('Mock Shop')).toBeInTheDocument();
  });

  it('renders main navigation', () => {
    render(<Header cartItemCount={0} onCartClick={vi.fn()} />);
    expect(screen.getByRole('navigation', { name: /main navigation/i })).toBeInTheDocument();
  });

  it('renders navigation links', () => {
    render(<Header cartItemCount={0} onCartClick={vi.fn()} />);
    expect(screen.getByText('Products')).toBeInTheDocument();
    expect(screen.getByText('Deals')).toBeInTheDocument();
    expect(screen.getByText('New')).toBeInTheDocument();
  });

  it('renders the cart button with item count', () => {
    render(<Header cartItemCount={3} onCartClick={vi.fn()} />);
    expect(screen.getByRole('button', { name: /shopping cart with 3 items/i })).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('does not show count when cart is empty', () => {
    render(<Header cartItemCount={0} onCartClick={vi.fn()} />);
    expect(screen.getByRole('button', { name: /shopping cart with 0 items/i })).toBeInTheDocument();
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('renders the home link with accessible label', () => {
    render(<Header cartItemCount={0} onCartClick={vi.fn()} />);
    expect(screen.getByLabelText('Mock Shop home')).toBeInTheDocument();
  });

  it('calls onCartClick when cart button is clicked', async () => {
    const onCartClick = vi.fn();
    render(<Header cartItemCount={0} onCartClick={onCartClick} />);

    await userEvent.click(screen.getByRole('button', { name: /shopping cart/i }));

    expect(onCartClick).toHaveBeenCalledOnce();
  });
});

