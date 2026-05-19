# Cart Feature — Specification

## Overview

Users should be able to view the contents of their cart, see a cost breakdown, and manage their item selections before checkout. The cart is accessed by clicking the existing cart icon in the `Header`. Items can be added from the product listing, their quantities updated, or removed from within the cart view.

---

## Decisions & Ambiguities Resolved

The following points were not fully specified in the feature request and have been resolved here as the authoritative decisions for implementation.

| # | Ambiguity | Decision |
|---|-----------|----------|
| 1 | **Max-qty on `PUT` updates** | The 5-item cap applies to the new absolute value. `PUT` with `quantity > 5` → `400`. Quantity of exactly 5 is **valid**. The cap is not checked against the cart's existing value (since PUT sets, not adds). |
| 2 | **Increment scenario on `POST`** | `POST` is additive: `newTotal = existingQty + requestedQty`. If `newTotal > 5` the request is rejected with `400`, even if `requestedQty` alone is ≤ 5. Example: cart has 4 of product 1; `POST {productId:1, quantity:2}` → `400`. |
| 3 | **`PUT` semantics** | `PUT` is an **absolute set**, not a delta increment. Sending `quantity: 3` means the cart item will have exactly 3 units regardless of the prior value. |
| 4 | **Error response format** | All validation failures return `400 Bad Request` with an ASP.NET Core `ValidationProblem` body (`application/problem+json`). This is the default status code produced by `TypedResults.ValidationProblem`. Example: `{ "title": "One or more validation errors occurred.", "status": 400, "errors": { "quantity": ["Quantity must be between 1 and 5."] } }` |
| 5 | **UI approach** | The cart is shown as a slide-in side panel (drawer) anchored to the right side of the viewport, toggled by the existing cart icon button in `Header`. It is not a separate route/page. |
| 6 | **`PUT` on an item not in the cart** | If `productId` is valid in the catalogue but the product has not been added to the cart yet, `PUT` returns `404 Not Found`. `PUT` cannot create new cart entries — use `POST` for that. |

---

## Business Rules

1. **Maximum quantity per item**: No single product may have a quantity greater than 5 in the cart at any time.
   - Valid quantity range is **1 to 5 inclusive**. A quantity of exactly 5 is accepted.
   - A quantity of 0 or any negative number is rejected with `400 Bad Request`.
   - Attempts to `POST /api/cart` that would push a product's total quantity above 5 must be rejected with `400 Bad Request`.
   - Attempts to `PUT /api/cart/{productId}` with a quantity above 5 must be rejected with `400 Bad Request`.
2. **Product must exist**: Adding or updating a cart item for an unknown product ID must return `404 Not Found`.
3. **Cart is shared / session-less**: The existing singleton `InMemoryCartService` is retained; all users share one cart (no auth scope required for this exercise).

---

## Backend

### Endpoints

| Method | Route | Description | Success | Error codes |
|--------|-------|-------------|---------|-------------|
| GET | `/api/cart` | Return all cart items | `200 OK` — array of `CartItem` | — |
| POST | `/api/cart` | Add product or increment quantity | `201 Created` (new item) / `200 OK` (updated) — `CartItem` | `404`, `400` |
| PUT | `/api/cart/{productId}` | Set absolute quantity for an existing cart item | `200 OK` — `CartItem` | `404`, `400` |
| DELETE | `/api/cart/{productId}` | Remove a single item | `204 No Content` | `404` |
| DELETE | `/api/cart` | Clear the entire cart | `204 No Content` | — |

### New request record — `UpdateCartItemRequest`

```csharp
record UpdateCartItemRequest(int Quantity);
```

### `PUT /api/cart/{productId}` — behaviour

Checks are applied in this exact order:

1. If `Quantity < 1` (zero or negative) → `400 Bad Request` with `errors: { "quantity": ["Quantity must be at least 1."] }`.
2. If `Quantity > 5` → `400 Bad Request` with `errors: { "quantity": ["Quantity cannot exceed 5."] }`. **Quantity of exactly 5 is valid and must not be rejected.**
3. If product ID does not exist in the catalogue → `404 Not Found` with body `"Product not found"`.
4. If product ID is valid but not present in the cart → `404 Not Found` (empty body).
5. Otherwise set the item's quantity to the requested value → `200 OK` with the updated `CartItem`.

### `POST /api/cart` — updated behaviour

Checks are applied in this exact order:

1. If `Quantity < 1` (zero or negative) → `400 Bad Request` with `errors: { "quantity": ["Quantity must be at least 1."] }`.
2. If product ID does not exist in the catalogue → `404 Not Found` with body `"Product not found"`.
3. Compute `newTotal = existingQuantityInCart + request.Quantity` (0 if item not yet in cart).
4. If `newTotal > 5` → `400 Bad Request` with `errors: { "quantity": ["Cannot exceed 5 of any single item."] }`. **A newTotal of exactly 5 is valid.**
5. If item was not in the cart → insert it, return `201 Created` with the new `CartItem`.
6. If item was already in the cart → increment its quantity by `request.Quantity`, return `200 OK` with the updated `CartItem`.

### Error response format

All validation failures use `TypedResults.ValidationProblem(...)`, which returns `400 Bad Request` by default in ASP.NET Core:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "quantity": ["Quantity must be between 1 and 5."]
  }
}
```

`404` responses for a product not found in the catalogue use `TypedResults.NotFound("Product not found")` (string body). `404` for a cart item not found uses `TypedResults.NotFound()` (empty body). These two cases are intentionally distinguishable by body presence.

---

### `InMemoryCartService` — methods to implement

All methods currently throw `NotImplementedException`. Each must be implemented using the existing `_lock` (`Lock`) for thread safety:

| Method | Behaviour |
|--------|-----------|
| `GetAll()` | Inside lock, return `_cart.ToList()` (snapshot, not live reference) |
| `GetByProductId(int)` | Inside lock, return `_cart.FirstOrDefault(i => i.ProductId == productId)` or `null` |
| `Add(CartItem)` | Inside lock: if matching `ProductId` exists increment its `Quantity` by `item.Quantity`; otherwise append `item` to `_cart`. Return the affected item. |
| `Remove(int)` | Inside lock: find and remove item; return `true` if found and removed, `false` if not found |
| `Clear()` | Inside lock, call `_cart.Clear()` |

A new method is also required on the interface and implementation:

| Method | Signature | Behaviour |
|--------|-----------|-----------|
| `Update` | `CartItem? Update(int productId, int quantity)` | Set quantity on an existing item; return updated item, or `null` if not found |

---

## Frontend

### Cart panel

- A slide-in side panel (drawer) fixed to the right side of the viewport, rendered as a `<aside role="dialog" aria-modal="true" aria-label="Shopping cart">`.
- When `isOpen` is `false` the component returns `null` (unmounted, not CSS-hidden).
- Controlled by `isCartOpen: boolean` state in `App`.
- Three ways to close: click the explicit "Close" button (`×`), press `Escape` (via `useEffect` keydown listener), or click the semi-transparent backdrop overlay.

### Panel contents — non-empty cart

- A list of cart items, each showing:
  - Product name
  - Unit price formatted as `$XX.XX` using `Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })`
  - Quantity input: `<input type="number" min="1" max="5">` — on change, calls `onUpdateQuantity(productId, newValue)` immediately
  - Line total formatted the same way (`unitPrice × quantity`)
  - "Remove" button: calls `onRemoveItem(productId)`; labelled `aria-label="Remove {productName} from cart"`
- An order summary at the bottom:
  - **Subtotal** — sum of all `totalPrice` values from `cartItems`, formatted as currency
- A **Clear cart** button: calls `onClearCart()`; only rendered when cart is non-empty

### Panel contents — empty cart

- Render the static text: **"Your cart is empty."**

### State management (in `App`)

- `isCartOpen: boolean` — controls panel visibility; default `false`.
- `cartItems: CartItem[]` — fetched via `GET /api/cart` on panel open and after every mutation (add, update, remove, clear); default `[]`.
- `cartItemCount: number` — derived value, not stored in state: `cartItems.reduce((sum, i) => sum + i.quantity, 0)`. Replaces the manually-incremented counter that exists today.
- On any API error inside the cart panel (update, remove, clear), reload the cart from the server and surface the error via the existing `cartMessage` notification in `App`.

### API layer additions (`src/frontend/src/api/index.ts`)

| Function | HTTP call |
|----------|-----------|
| `fetchCart()` | `GET /api/cart` |
| `updateCartItem(productId, quantity)` | `PUT /api/cart/{productId}` |
| `removeFromCart(productId)` | `DELETE /api/cart/{productId}` |
| `clearCart()` | `DELETE /api/cart` |

### Type additions (`src/frontend/src/types/index.ts`)

```typescript
export interface CartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}
```

---

## Out of scope

- User authentication / per-user carts
- Persistent storage (database)
- Checkout / payment flow
- Stock validation against current stock levels
- Animations or transitions on the cart panel
