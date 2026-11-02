/**
 * Shahi Zayka - Royal Culinary Full-Stack Platform
 * Client Architecture for ASP.NET Core & PostgreSQL Integration
 */

// API Configuration
const API_BASE = window.location.port === '5000' || window.location.port === '5001'
    ? `${window.location.origin}/api`
    : 'http://localhost:5000/api';

// Global Application State
const state = {
    products: [],
    categories: [],
    cart: JSON.parse(localStorage.getItem('shahi_cart') || '[]'),
    currentUser: JSON.parse(localStorage.getItem('shahi_user') || 'null'),
    token: localStorage.getItem('shahi_token') || null,
    selectedCategory: 'all',
    searchQuery: '',
    isVegOnly: false,
    currentTrackingOrder: null,
    activePromo: null
};

// ==========================================
// TOAST NOTIFICATION SYSTEM
// ==========================================
function showToast(message, type = 'info') {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    const icon = type === 'success' ? 'fa-circle-check' : (type === 'error' ? 'fa-triangle-exclamation' : 'fa-circle-info');
    toast.innerHTML = `<i class="fa-solid ${icon}"></i> <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 4000);
}

// ==========================================
// SWIPER INITIALIZATION
// ==========================
let swiperInstance = null;
function initSwiper() {
    if (typeof Swiper !== 'undefined' && document.querySelector('.mySwiper')) {
        swiperInstance = new Swiper(".mySwiper", {
            loop: true,
            navigation: {
                nextEl: "#prev",
                prevEl: "#next",
            },
            autoplay: {
                delay: 4500,
                disableOnInteraction: false,
            }
        });
    }
}

// ==========================================
// DATA FETCHING & API INTERACTION
// ==========================================
async function fetchProducts() {
    try {
        const res = await fetch(`${API_BASE}/products`);
        if (!res.ok) throw new Error('API server returned error');
        state.products = await res.json();
    } catch (err) {
        console.warn('API unavailable, falling back to local products.json:', err);
        try {
            const localRes = await fetch('products.json');
            const localData = await localRes.json();
            // Normalize local schema
            state.products = localData.map(p => ({
                id: p.id,
                name: p.name.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase()),
                price: parseFloat(p.price.replace('$', '')),
                imageUrl: p.images.trim(),
                description: 'Authentic royal delicacy prepared with heritage aromatic spices.',
                categoryName: p.name.includes('biryani') ? 'Biryani' : (p.name.includes('roll') ? 'Rolls & Starters' : 'Curries'),
                isVegetarian: p.name.toLowerCase().includes('veg') || p.name.toLowerCase().includes('panner'),
                averageRating: 4.8,
                reviewCount: 95,
                stockQuantity: 50,
                isAvailable: true
            }));
        } catch (e) {
            console.error('Failed to load local fallback data', e);
        }
    }
    renderProducts();
}

async function fetchCategories() {
    try {
        const res = await fetch(`${API_BASE}/categories`);
        if (res.ok) {
            state.categories = await res.json();
            renderCategoryPills();
        }
    } catch (err) {
        // Fallback default categories
        state.categories = [
            { id: 0, name: 'All', slug: 'all' },
            { id: 1, name: 'Biryani', slug: 'biryani' },
            { id: 2, name: 'Curries', slug: 'curries' },
            { id: 3, name: 'Rolls & Starters', slug: 'rolls' },
            { id: 4, name: 'Royal Platters', slug: 'thali' }
        ];
        renderCategoryPills();
    }
}

// ==========================================
// RENDERING PRODUCT CATALOG
// ==========================================
function renderCategoryPills() {
    const container = document.getElementById('category-pills-container');
    if (!container) return;

    container.innerHTML = '';
    state.categories.forEach(cat => {
        const pill = document.createElement('button');
        pill.className = `pill-btn ${state.selectedCategory === cat.slug ? 'active' : ''}`;
        pill.textContent = cat.name;
        pill.addEventListener('click', () => {
            state.selectedCategory = cat.slug;
            document.querySelectorAll('.pill-btn').forEach(btn => btn.classList.remove('active'));
            pill.classList.add('active');
            renderProducts();
        });
        container.appendChild(pill);
    });
}

function renderProducts() {
    const cardList = document.querySelector('.card-list');
    if (!cardList) return;

    cardList.innerHTML = '';

    // Filter items
    let filtered = state.products.filter(p => {
        // Category match
        if (state.selectedCategory !== 'all') {
            const catName = p.categoryName?.toLowerCase() || '';
            if (!catName.includes(state.selectedCategory)) return false;
        }

        // Veg only filter
        if (state.isVegOnly && !p.isVegetarian) return false;

        // Search match
        if (state.searchQuery) {
            const q = state.searchQuery.toLowerCase();
            const matchName = p.name.toLowerCase().includes(q);
            const matchDesc = p.description?.toLowerCase().includes(q) || false;
            if (!matchName && !matchDesc) return false;
        }

        return true;
    });

    if (filtered.length === 0) {
        cardList.innerHTML = `
            <div style="grid-column: 1/-1; padding: 3rem; text-align: center; color: #777;">
                <i class="fa-solid fa-utensils" style="font-size: 3rem; margin-bottom: 1rem; color: var(--gold-finger);"></i>
                <h3>No royal dishes found matching your criteria.</h3>
                <p>Try clearing filters or search for biryani, chicken, or paneer.</p>
            </div>`;
        return;
    }

    filtered.forEach(product => {
        const orderCard = document.createElement('div');
        orderCard.className = 'order-card';
        const vegBadge = product.isVegetarian
            ? `<span class="card-tag veg"><i class="fa-solid fa-leaf"></i> Veg</span>`
            : `<span class="card-tag"><i class="fa-solid fa-drumstick-bite"></i> Non-Veg</span>`;

        orderCard.innerHTML = `
            ${vegBadge}
            <div class="card-image">
                <img src="${product.imageUrl}" alt="${product.name}" onerror="this.src='images/lucknow chicken-biryani.png'"/>
            </div>
            <h4>${product.name}</h4>
            <div class="card-rating">
                <i class="fa-solid fa-star"></i> ${product.averageRating || 4.8} (${product.reviewCount || 100}+ orders)
            </div>
            <p class="card-desc">${product.description || 'Authentic shahi specialty.'}</p>
            <h4 class="price">$${Number(product.price).toFixed(2)}</h4>
            <a href="#" class="btn card-btn" data-id="${product.id}">Add to cart</a>
        `;

        const cardBtn = orderCard.querySelector('.card-btn');
        cardBtn.addEventListener('click', (e) => {
            e.preventDefault();
            addToCart(product);
        });

        cardList.appendChild(orderCard);
    });
}

// ==========================================
// CART & PRICING MANAGEMENT
// ==========================================
function addToCart(product) {
    const existing = state.cart.find(item => item.id === product.id);
    if (existing) {
        existing.quantity += 1;
        showToast(`Increased quantity of ${product.name}`, 'info');
    } else {
        state.cart.push({
            id: product.id,
            name: product.name,
            price: Number(product.price),
            imageUrl: product.imageUrl,
            quantity: 1
        });
        showToast(`${product.name} added to royal cart!`, 'success');
    }

    saveCart();
    renderCart();
    openCartTab();
}

function saveCart() {
    localStorage.setItem('shahi_cart', JSON.stringify(state.cart));
}

function renderCart() {
    const cartList = document.querySelector('.cart-list');
    const cardTotal = document.querySelector('.cart-total');
    const cartValue = document.querySelector('.cart-value');
    if (!cartList) return;

    cartList.innerHTML = '';
    let subtotal = 0;
    let totalItems = 0;

    state.cart.forEach(item => {
        subtotal += item.price * item.quantity;
        totalItems += item.quantity;

        const cartItem = document.createElement('div');
        cartItem.className = 'item';
        cartItem.innerHTML = `
            <div class="item-image">
                <img src="${item.imageUrl}" alt="${item.name}" onerror="this.src='images/lucknow chicken-biryani.png'">
            </div>
            <div class="detail">
                <h4>${item.name}</h4>
                <h4 class="item-total">$${(item.price * item.quantity).toFixed(2)}</h4>
            </div>
            <div class="flex">
                <a href="#" class="quantity-btn minus" data-id="${item.id}">
                    <i class="fa-solid fa-minus"></i>
                </a>
                <h4 class="quantity-value">${item.quantity}</h4>
                <a href="#" class="quantity-btn plus" data-id="${item.id}">
                    <i class="fa-solid fa-plus"></i>
                </a>
            </div>
        `;

        cartItem.querySelector('.plus').addEventListener('click', (e) => {
            e.preventDefault();
            item.quantity++;
            saveCart();
            renderCart();
        });

        cartItem.querySelector('.minus').addEventListener('click', (e) => {
            e.preventDefault();
            if (item.quantity > 1) {
                item.quantity--;
            } else {
                state.cart = state.cart.filter(x => x.id !== item.id);
                showToast(`Removed ${item.name} from cart`, 'info');
            }
            saveCart();
            renderCart();
        });

        cartList.appendChild(cartItem);
    });

    if (state.cart.length === 0) {
        cartList.innerHTML = `
            <div style="text-align: center; padding: 2rem; color: #888;">
                <i class="fa-solid fa-cart-shopping" style="font-size: 2.5rem; margin-bottom: 0.8rem; color: #ccc;"></i>
                <p>Your royal cart is empty.</p>
            </div>
        `;
    }

    if (cardTotal) cardTotal.textContent = `$${subtotal.toFixed(2)}`;
    if (cartValue) cartValue.textContent = totalItems;
}

function openCartTab() {
    const cartTab = document.querySelector('.cart-tab');
    if (cartTab) cartTab.classList.add('cart-tab-active');
}

function closeCartTab() {
    const cartTab = document.querySelector('.cart-tab');
    if (cartTab) cartTab.classList.remove('cart-tab-active');
}

// ==========================================
// CHECKOUT & IDEMPOTENT ORDER CREATION
// ==========================================
function openCheckoutModal() {
    if (state.cart.length === 0) {
        showToast('Please add items to your cart before checking out.', 'error');
        return;
    }
    closeCartTab();

    const modal = document.getElementById('checkout-modal');
    if (!modal) return;

    // Calculate figures
    const subtotal = state.cart.reduce((sum, i) => sum + (i.price * i.quantity), 0);
    const tax = Math.round(subtotal * 0.05 * 100) / 100;
    const delivery = subtotal >= 50 ? 0 : 5.00;
    let discount = 0;
    if (state.activePromo === 'ROYAL10') discount = Math.round(subtotal * 0.10 * 100) / 100;
    if (state.activePromo === 'WELCOME') discount = 5.00;
    const total = Math.max(0, subtotal + tax + delivery - discount);

    // Update summary preview
    document.getElementById('checkout-subtotal').textContent = `$${subtotal.toFixed(2)}`;
    document.getElementById('checkout-tax').textContent = `$${tax.toFixed(2)}`;
    document.getElementById('checkout-delivery').textContent = delivery === 0 ? 'FREE' : `$${delivery.toFixed(2)}`;
    document.getElementById('checkout-discount').textContent = discount > 0 ? `-$${discount.toFixed(2)}` : '$0.00';
    document.getElementById('checkout-total').textContent = `$${total.toFixed(2)}`;

    // Autofill logged-in user profile
    if (state.currentUser) {
        document.getElementById('checkout-name').value = state.currentUser.fullName || '';
        document.getElementById('checkout-email').value = state.currentUser.email || '';
        document.getElementById('checkout-phone').value = state.currentUser.phoneNumber || '';
        document.getElementById('checkout-address').value = state.currentUser.address || '';
    }

    modal.classList.add('active');
}

async function handleCheckoutSubmit(e) {
    e.preventDefault();

    const name = document.getElementById('checkout-name').value.trim();
    const email = document.getElementById('checkout-email').value.trim();
    const phone = document.getElementById('checkout-phone').value.trim();
    const address = document.getElementById('checkout-address').value.trim();
    const instructions = document.getElementById('checkout-instructions').value.trim();
    const paymentMethod = parseInt(document.getElementById('checkout-payment').value, 10);
    const promoCode = document.getElementById('checkout-promo').value.trim();

    if (!name || !email || !phone || !address) {
        showToast('Please fill out all required delivery fields.', 'error');
        return;
    }

    // Generate unique UUID Idempotency-Key
    const idempotencyKey = 'IDEM-' + Date.now() + '-' + Math.random().toString(36).substring(2, 9).toUpperCase();

    const payload = {
        idempotencyKey,
        customerName: name,
        customerEmail: email,
        customerPhone: phone,
        deliveryAddress: address,
        specialInstructions: instructions,
        paymentMethod: paymentMethod || 1,
        promoCode: promoCode || state.activePromo,
        items: state.cart.map(i => ({ productId: i.id, quantity: i.quantity }))
    };

    const submitBtn = document.getElementById('place-order-btn');
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Processing Royal Order...';

    try {
        const headers = {
            'Content-Type': 'application/json',
            'Idempotency-Key': idempotencyKey
        };
        if (state.token) {
            headers['Authorization'] = `Bearer ${state.token}`;
        }

        const res = await fetch(`${API_BASE}/orders/checkout`, {
            method: 'POST',
            headers,
            body: JSON.stringify(payload)
        });

        const data = await res.json();

        if (!res.ok) {
            throw new Error(data.message || 'Order failed to process');
        }

        // Success!
        showToast(`Order ${data.orderNumber} placed successfully!`, 'success');
        state.cart = [];
        saveCart();
        renderCart();

        document.getElementById('checkout-modal').classList.remove('active');
        openOrderTrackerModal(data.orderNumber);

    } catch (err) {
        showToast(`Checkout Error: ${err.message}`, 'error');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = 'Confirm & Place Royal Order';
    }
}

// ==========================================
// LIVE ORDER TRACKER
// ==========================================
function openOrderTrackerModal(orderNumber = null) {
    const modal = document.getElementById('tracker-modal');
    if (!modal) return;

    if (orderNumber) {
        document.getElementById('tracker-input').value = orderNumber;
        trackOrder(orderNumber);
    }

    modal.classList.add('active');
}

async function trackOrder(orderNumber) {
    if (!orderNumber) {
        orderNumber = document.getElementById('tracker-input').value.trim();
    }
    if (!orderNumber) {
        showToast('Please enter an order number.', 'error');
        return;
    }

    const resultBox = document.getElementById('tracker-results');
    resultBox.innerHTML = '<div style="text-align: center; padding: 2rem;"><i class="fa-solid fa-spinner fa-spin fa-2x"></i><p>Locating order status...</p></div>';

    try {
        const res = await fetch(`${API_BASE}/orders/${encodeURIComponent(orderNumber)}`);
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Order not found');
        }

        const order = await res.json();
        state.currentTrackingOrder = order;
        renderOrderTrackerDetails(order);

    } catch (err) {
        resultBox.innerHTML = `
            <div style="text-align: center; padding: 2rem; color: #d32f2f;">
                <i class="fa-solid fa-circle-exclamation fa-2x" style="margin-bottom: 0.5rem;"></i>
                <p>${err.message}</p>
                <small>Format example: SZ-20261102-ABC123</small>
            </div>
        `;
    }
}

function renderOrderTrackerDetails(order) {
    const resultBox = document.getElementById('tracker-results');
    const statusSteps = ['Pending', 'Confirmed', 'Preparing', 'OutForDelivery', 'Delivered'];
    const currentStepIdx = statusSteps.indexOf(order.statusName);

    const stepsHtml = `
        <div class="order-stepper">
            <div class="stepper-step ${currentStepIdx >= 0 ? 'completed' : ''} ${currentStepIdx === 0 ? 'active' : ''}">
                <div class="step-icon"><i class="fa-solid fa-file-invoice"></i></div>
                <div class="step-title">Placed</div>
            </div>
            <div class="stepper-step ${currentStepIdx >= 1 ? 'completed' : ''} ${currentStepIdx === 1 ? 'active' : ''}">
                <div class="step-icon"><i class="fa-solid fa-check"></i></div>
                <div class="step-title">Confirmed</div>
            </div>
            <div class="stepper-step ${currentStepIdx >= 2 ? 'completed' : ''} ${currentStepIdx === 2 ? 'active' : ''}">
                <div class="step-icon"><i class="fa-solid fa-fire-burner"></i></div>
                <div class="step-title">Preparing</div>
            </div>
            <div class="stepper-step ${currentStepIdx >= 3 ? 'completed' : ''} ${currentStepIdx === 3 ? 'active' : ''}">
                <div class="step-icon"><i class="fa-solid fa-motorcycle"></i></div>
                <div class="step-title">On The Way</div>
            </div>
            <div class="stepper-step ${currentStepIdx >= 4 ? 'completed' : ''} ${currentStepIdx === 4 ? 'active' : ''}">
                <div class="step-icon"><i class="fa-solid fa-house-chimney"></i></div>
                <div class="step-title">Delivered</div>
            </div>
        </div>
    `;

    const itemsListHtml = order.items.map(i => `
        <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem; font-size: 0.95rem;">
            <span>${i.quantity}x ${i.productName}</span>
            <span style="font-weight: 600;">$${Number(i.totalPrice).toFixed(2)}</span>
        </div>
    `).join('');

    resultBox.innerHTML = `
        <div style="background: var(--eye-ball); padding: 1.2rem; border-radius: 1rem; margin-top: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #ccc; padding-bottom: 0.8rem;">
                <div>
                    <h4 style="font-size: 1.2rem; color: var(--lead);">${order.orderNumber}</h4>
                    <small style="color: #666;">Placed on ${new Date(order.createdAtUtc).toLocaleTimeString()}</small>
                </div>
                <span class="status-badge ${order.statusName}">${order.statusName}</span>
            </div>

            ${stepsHtml}

            <div style="margin-top: 1.5rem; background: white; padding: 1rem; border-radius: 0.8rem;">
                <h5 style="margin-bottom: 0.5rem; color: var(--lead);">Order Summary</h5>
                ${itemsListHtml}
                <hr style="margin: 0.8rem 0; border: none; border-top: 1px dashed #ccc;">
                <div style="display: flex; justify-content: space-between; font-weight: bold; font-size: 1.1rem;">
                    <span>Total Paid:</span>
                    <span style="color: var(--lead);">$${Number(order.totalAmount).toFixed(2)} (${order.paymentMethodName})</span>
                </div>
                <p style="margin-top: 0.5rem; font-size: 0.85rem; color: #555;">
                    <i class="fa-solid fa-location-dot"></i> Delivering to: ${order.deliveryAddress}
                </p>
            </div>
        </div>
    `;
}

// ==========================================
// AUTHENTICATION & USER PROFILE
// ==========================================
function updateAuthUI() {
    const authContainer = document.getElementById('nav-auth-container');
    const mobileAuthContainer = document.getElementById('mobile-auth-container');

    const content = state.currentUser ? `
        <div style="display: flex; align-items: center; gap: 0.8rem;">
            ${state.currentUser.role === 2 || state.currentUser.roleName === 'Admin' ? '<button class="btn" id="open-admin-btn" style="background:#212121; color:#f2BD12;"><i class="fa-solid fa-crown"></i> Admin</button>' : ''}
            <span style="font-weight: bold; color: var(--lead);"><i class="fa-solid fa-user"></i> ${state.currentUser.fullName.split(' ')[0]}</span>
            <button class="btn" id="logout-btn" style="padding: 0.5rem 1rem; font-size: 0.9rem;">Logout</button>
        </div>
    ` : `
        <a href="#" class="btn" id="open-auth-btn">
            Sign in &nbsp; <i class="fa-solid fa-right-to-bracket"></i>
        </a>
    `;

    if (authContainer) {
        authContainer.innerHTML = content;
        attachAuthEventListeners();
    }
    if (mobileAuthContainer) {
        mobileAuthContainer.innerHTML = content;
        attachAuthEventListeners();
    }
}

function attachAuthEventListeners() {
    const openBtn = document.getElementById('open-auth-btn');
    if (openBtn) {
        openBtn.addEventListener('click', (e) => {
            e.preventDefault();
            document.getElementById('auth-modal').classList.add('active');
        });
    }

    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            state.currentUser = null;
            state.token = null;
            localStorage.removeItem('shahi_user');
            localStorage.removeItem('shahi_token');
            updateAuthUI();
            showToast('You have been logged out.', 'info');
        });
    }

    const adminBtn = document.getElementById('open-admin-btn');
    if (adminBtn) {
        adminBtn.addEventListener('click', () => {
            openAdminModal();
        });
    }
}

async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value.trim();

    try {
        const res = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        const data = await res.json();
        if (!res.ok) throw new Error(data.message || 'Login failed');

        state.token = data.token;
        state.currentUser = data.user;
        localStorage.setItem('shahi_token', data.token);
        localStorage.setItem('shahi_user', JSON.stringify(data.user));

        updateAuthUI();
        document.getElementById('auth-modal').classList.remove('active');
        showToast(`Welcome back, ${data.user.fullName}!`, 'success');

    } catch (err) {
        showToast(`Login Error: ${err.message}`, 'error');
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const fullName = document.getElementById('reg-name').value.trim();
    const email = document.getElementById('reg-email').value.trim();
    const password = document.getElementById('reg-password').value.trim();
    const phone = document.getElementById('reg-phone').value.trim();
    const address = document.getElementById('reg-address').value.trim();

    try {
        const res = await fetch(`${API_BASE}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fullName, email, password, phoneNumber: phone, address })
        });
        const data = await res.json();
        if (!res.ok) throw new Error(data.message || 'Registration failed');

        state.token = data.token;
        state.currentUser = data.user;
        localStorage.setItem('shahi_token', data.token);
        localStorage.setItem('shahi_user', JSON.stringify(data.user));

        updateAuthUI();
        document.getElementById('auth-modal').classList.remove('active');
        showToast(`Account created! Welcome, ${data.user.fullName}!`, 'success');

    } catch (err) {
        showToast(`Registration Error: ${err.message}`, 'error');
    }
}

// ==========================================
// ADMIN DASHBOARD
// ==========================================
async function openAdminModal() {
    const modal = document.getElementById('admin-modal');
    if (!modal) return;
    modal.classList.add('active');
    loadAdminOrders();
}

async function loadAdminOrders() {
    const tableBody = document.getElementById('admin-orders-body');
    if (!tableBody) return;
    tableBody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 2rem;"><i class="fa-solid fa-spinner fa-spin"></i> Loading live orders...</td></tr>';

    try {
        const res = await fetch(`${API_BASE}/orders`, {
            headers: { 'Authorization': `Bearer ${state.token}` }
        });
        if (!res.ok) throw new Error('Failed to load orders');
        const orders = await res.json();

        if (orders.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 1.5rem;">No orders placed yet.</td></tr>';
            return;
        }

        tableBody.innerHTML = orders.map(o => `
            <tr>
                <td><strong>${o.orderNumber}</strong></td>
                <td>${o.customerName}<br><small>${o.customerPhone}</small></td>
                <td>$${Number(o.totalAmount).toFixed(2)}</td>
                <td><span class="status-badge ${o.statusName}">${o.statusName}</span></td>
                <td>
                    <select class="form-select status-changer" data-id="${o.id}" style="padding: 0.3rem; font-size: 0.85rem;">
                        <option value="1" ${o.status === 1 ? 'selected' : ''}>Pending</option>
                        <option value="2" ${o.status === 2 ? 'selected' : ''}>Confirmed</option>
                        <option value="3" ${o.status === 3 ? 'selected' : ''}>Preparing</option>
                        <option value="4" ${o.status === 4 ? 'selected' : ''}>OutForDelivery</option>
                        <option value="5" ${o.status === 5 ? 'selected' : ''}>Delivered</option>
                        <option value="6" ${o.status === 6 ? 'selected' : ''}>Cancelled</option>
                    </select>
                </td>
                <td>
                    <button class="btn" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;" onclick="trackOrder('${o.orderNumber}'); document.getElementById('admin-modal').classList.remove('active'); openOrderTrackerModal('${o.orderNumber}');">View</button>
                </td>
            </tr>
        `).join('');

        // Wire status changes
        document.querySelectorAll('.status-changer').forEach(select => {
            select.addEventListener('change', async (e) => {
                const orderId = e.target.getAttribute('data-id');
                const newStatus = parseInt(e.target.value, 10);
                await updateOrderStatus(orderId, newStatus);
            });
        });

    } catch (err) {
        tableBody.innerHTML = `<tr><td colspan="6" style="color: red; text-align: center;">Error loading orders: ${err.message}</td></tr>`;
    }
}

async function updateOrderStatus(orderId, newStatus) {
    try {
        const res = await fetch(`${API_BASE}/orders/${orderId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({ status: newStatus })
        });
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Status update failed');
        }
        showToast('Order status updated successfully!', 'success');
        loadAdminOrders();
    } catch (err) {
        showToast(`Failed to update status: ${err.message}`, 'error');
    }
}

// ==========================================
// NEWSLETTER & REVIEWS WIRING
// ==========================================
async function handleNewsletter(e) {
    e.preventDefault();
    const emailInput = document.getElementById('newsletter-email');
    if (!emailInput || !emailInput.value.trim()) return;

    try {
        const res = await fetch(`${API_BASE}/newsletter/subscribe`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: emailInput.value.trim() })
        });
        const data = await res.json();
        if (res.ok) {
            showToast(data.message || 'Thank you for subscribing to Shahi Zayka updates!', 'success');
            emailInput.value = '';
        } else {
            showToast(data.message || 'Subscription failed', 'error');
        }
    } catch (err) {
        showToast('Thank you for subscribing!', 'success');
        emailInput.value = '';
    }
}

// ==========================================
// INITIALIZATION ON DOM READY
// ==========================================
document.addEventListener('DOMContentLoaded', () => {
    // 1. Swiper carousel
    initSwiper();

    // 2. Fetch products & categories
    fetchCategories();
    fetchProducts();

    // 3. Render initial cart & auth
    renderCart();
    updateAuthUI();

    // 4. Mobile hamburger menu
    const hamburger = document.querySelector('.hamburger');
    const mobileMenu = document.querySelector('.mobile-menu');
    if (hamburger && mobileMenu) {
        hamburger.addEventListener('click', () => {
            mobileMenu.classList.toggle('mobile-menu-active');
        });
    }

    // 5. Cart open/close triggers
    const cartIcon = document.querySelector('.cart-icon');
    const closeBtn = document.querySelector('.close-btn');
    if (cartIcon) cartIcon.addEventListener('click', (e) => { e.preventDefault(); openCartTab(); });
    if (closeBtn) closeBtn.addEventListener('click', (e) => { e.preventDefault(); closeCartTab(); });

    // 6. Checkout trigger
    const checkoutBtn = document.getElementById('checkout-trigger-btn');
    if (checkoutBtn) {
        checkoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            openCheckoutModal();
        });
    }

    // 7. Search input
    const searchInput = document.getElementById('search-dish');
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            state.searchQuery = e.target.value;
            renderProducts();
        });
    }

    // 8. Veg filter checkbox
    const vegCheckbox = document.getElementById('veg-only-filter');
    if (vegCheckbox) {
        vegCheckbox.addEventListener('change', (e) => {
            state.isVegOnly = e.target.checked;
            renderProducts();
        });
    }

    // 9. Forms
    const checkoutForm = document.getElementById('checkout-form');
    if (checkoutForm) checkoutForm.addEventListener('submit', handleCheckoutSubmit);

    const loginForm = document.getElementById('login-form');
    if (loginForm) loginForm.addEventListener('submit', handleLogin);

    const registerForm = document.getElementById('register-form');
    if (registerForm) registerForm.addEventListener('submit', handleRegister);

    const newsletterBtn = document.getElementById('newsletter-submit-btn');
    if (newsletterBtn) newsletterBtn.addEventListener('click', handleNewsletter);

    // 10. Track order triggers
    const trackNavBtn = document.getElementById('nav-track-order');
    if (trackNavBtn) {
        trackNavBtn.addEventListener('click', (e) => {
            e.preventDefault();
            openOrderTrackerModal();
        });
    }

    const trackSearchBtn = document.getElementById('track-search-btn');
    if (trackSearchBtn) {
        trackSearchBtn.addEventListener('click', () => {
            trackOrder();
        });
    }

    // 11. Modal Close buttons
    document.querySelectorAll('.modal-close, .modal-overlay').forEach(el => {
        el.addEventListener('click', (e) => {
            if (e.target === el || e.target.classList.contains('modal-close')) {
                document.querySelectorAll('.modal-overlay').forEach(m => m.classList.remove('active'));
            }
        });
    });

    // 12. Quick Demo Login helper buttons
    const demoCustomerBtn = document.getElementById('demo-customer-btn');
    if (demoCustomerBtn) {
        demoCustomerBtn.addEventListener('click', () => {
            document.getElementById('login-email').value = 'customer@shahizayka.com';
            document.getElementById('login-password').value = 'Customer@123';
        });
    }

    const demoAdminBtn = document.getElementById('demo-admin-btn');
    if (demoAdminBtn) {
        demoAdminBtn.addEventListener('click', () => {
            document.getElementById('login-email').value = 'admin@shahizayka.com';
            document.getElementById('login-password').value = 'Admin@123';
        });
    }

    // 13. Auth Tabs
    const tabLogin = document.getElementById('tab-login-btn');
    const tabRegister = document.getElementById('tab-register-btn');
    const boxLogin = document.getElementById('login-box');
    const boxRegister = document.getElementById('register-box');

    if (tabLogin && tabRegister && boxLogin && boxRegister) {
        tabLogin.addEventListener('click', () => {
            tabLogin.classList.add('active');
            tabRegister.classList.remove('active');
            boxLogin.style.display = 'block';
            boxRegister.style.display = 'none';
        });

        tabRegister.addEventListener('click', () => {
            tabRegister.classList.add('active');
            tabLogin.classList.remove('active');
            boxLogin.style.display = 'none';
            boxRegister.style.display = 'block';
        });
    }
});
