import { useEffect } from 'react';
import type { CartItem } from '../../types';

interface CartPanelProps {
  isOpen: boolean;
  onClose: () => void;
  cartItems: CartItem[];
  onUpdateQuantity: (productId: number, quantity: number) => void;
  onRemoveItem: (productId: number) => void;
  onClearCart: () => void;
}

const formatCurrency = (amount: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

export function CartPanel({
  isOpen,
  onClose,
  cartItems,
  onUpdateQuantity,
  onRemoveItem,
  onClearCart,
}: CartPanelProps) {
  useEffect(() => {
    if (!isOpen) return;
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const subtotal = cartItems.reduce((sum, item) => sum + item.totalPrice, 0);

  return (
    <div className="cart-panel__backdrop" onClick={onClose}>
      <aside
        className="cart-panel"
        role="dialog"
        aria-modal="true"
        aria-label="Shopping cart"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="cart-panel__header">
          <h2 className="cart-panel__title">Your cart</h2>
          <button className="cart-panel__close" onClick={onClose} aria-label="Close cart">
            ×
          </button>
        </div>

        {cartItems.length === 0 ? (
          <p>Your cart is empty.</p>
        ) : (
          <>
            <ul className="cart-panel__list">
              {cartItems.map((item) => (
                <li key={item.productId} className="cart-panel__item">
                  <span className="cart-panel__item-name">{item.productName}</span>
                  <span className="cart-panel__item-price">{formatCurrency(item.unitPrice)}</span>
                  <input
                    type="number"
                    min="1"
                    max="5"
                    value={item.quantity}
                    onChange={(e) => onUpdateQuantity(item.productId, Number(e.target.value))}
                    aria-label={`Quantity for ${item.productName}`}
                  />
                  <span className="cart-panel__item-total">{formatCurrency(item.totalPrice)}</span>
                  <button
                    onClick={() => onRemoveItem(item.productId)}
                    aria-label={`Remove ${item.productName} from cart`}
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
            <div className="cart-panel__summary">
              <p>Subtotal: {formatCurrency(subtotal)}</p>
              <button onClick={onClearCart}>Clear cart</button>
            </div>
          </>
        )}
      </aside>
    </div>
  );
}
