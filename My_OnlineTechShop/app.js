const apiBase = "https://localhost:7150/api";

let token = null;
let role = null;
let productsData = [];
let cart = {};

// --- HEADER & CONTAINERS ---
const loginContainer = document.getElementById("login-container");
const registerContainer = document.getElementById("register-container");
const btnLoginShow = document.getElementById("btn-login-show");
const btnRegisterShow = document.getElementById("btn-register-show");
const btnLogout = document.getElementById("btn-logout");

// --- HEADER BUTTONS ---
btnLoginShow.addEventListener("click", () => {
    loginContainer.classList.remove("hidden");
    registerContainer.classList.add("hidden");
});
btnRegisterShow.addEventListener("click", () => {
    registerContainer.classList.remove("hidden");
    loginContainer.classList.add("hidden");
});
btnLogout.addEventListener("click", () => {
    token = null;
    role = null;
    cart = {};
    localStorage.removeItem("token");

    document.getElementById("user-container").classList.add("hidden");
    document.getElementById("admin-container").classList.add("hidden");

    document.getElementById("btn-cart")?.remove();
    document.getElementById("btn-history")?.remove();
    localStorage.removeItem("cart");

    document.getElementById("btn-admin-catalog")?.classList.add("hidden");
    document.getElementById("btn-admin-panel")?.classList.add("hidden");

    loginContainer.classList.remove("hidden");
    btnLogout.classList.add("hidden");
    btnLoginShow.classList.remove("hidden");
    btnRegisterShow.classList.remove("hidden");
});

// --- REGISTER ---
document.getElementById("btn-register").addEventListener("click", async () => {
    const login = document.getElementById("reg-login").value;
    const password = document.getElementById("reg-password").value;
    try {
        const res = await fetch(`${apiBase}/auth/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ login, password })
        });
        if (!res.ok) throw new Error("Помилка реєстрації");
        showMessage("Реєстрація успішна! Увійдіть у систему.");
        registerContainer.classList.add("hidden");
        loginContainer.classList.remove("hidden");
    } catch (e) {
        document.getElementById("register-error").textContent = e.message;
    }
});

// --- LOGIN ---
document.getElementById("btn-login").addEventListener("click", async () => {
    const login = document.getElementById("login").value;
    const password = document.getElementById("password").value;
    try {
        const res = await fetch(`${apiBase}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ login, password })
        });
        if (!res.ok) throw new Error("Невірний логін або пароль");
        const data = await res.json();
        token = data.token;
        const payload = JSON.parse(atob(token.split(".")[1]));
        role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        showAppropriatePanel();
    } catch (e) {
        document.getElementById("login-error").textContent = e.message;
    }
});

// --- AFTER LOGIN ---
function showAppropriatePanel() {
    loginContainer.classList.add("hidden");
    registerContainer.classList.add("hidden");

    btnLogout.classList.remove("hidden");
    btnLoginShow.classList.add("hidden");
    btnRegisterShow.classList.add("hidden");

    if (role === "Admin") {
        document.getElementById("admin-container").classList.remove("hidden");

        document.getElementById("btn-admin-catalog").classList.remove("hidden");
        document.getElementById("btn-admin-panel").classList.remove("hidden");

        loadAdminData();
    } else {
        document.getElementById("user-container").classList.remove("hidden");
        createUserHeaderButtons();
        loadProducts();
    }
}

// --- USER PANEL ---
const savedCart = localStorage.getItem("cart");
if (savedCart) cart = JSON.parse(savedCart);

async function loadProducts() {
    const res = await fetch(`${apiBase}/products`, { headers: { "Authorization": "Bearer " + token } });
    productsData = await res.json();
    renderPaginatedProducts(productsData);
}

// --- PRODUCT RENDER & HOVER ---
function renderProducts(products) {
    const container = document.getElementById("products");
    container.innerHTML = "";

    products.forEach(p => {
        const imgSrc = p.imageName
            ? `Images/${p.imageName}`
            : `Images/${p.name.toLowerCase().replace(/\s+/g, "_")}.png`;

        const div = document.createElement("div");
        div.className = "product-card";
        div.innerHTML = `
            <img src="${imgSrc}" alt="${p.name}">
            <h4>${p.name}</h4>
            <p>${p.price} ₴</p>
            <p>${p.stockQuantity > 0 ? "В наявності" : "Немає"}</p>
            <button ${p.stockQuantity === 0 ? "disabled" : ""} data-id="${p.id}">Додати у корзину</button>
        `;
        container.appendChild(div);

        // Hover preview (активація елементу інтерфейсу при наведенні курсора миші)
        const img = div.querySelector("img");

        //помилка фото - показується за замовчуванням замість поломаної картинки - placeholder
        img.addEventListener("error", () => {
            img.src = "Images/placeholder.png";
        })

        img.addEventListener("mouseenter", () => img.style.transform = "scale(1.5)");
        img.addEventListener("mouseleave", () => img.style.transform = "scale(1)");
    });

    // Додавання у корзину
    document.querySelectorAll(".product-card button").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.getAttribute("data-id");
            if (!cart[id]) cart[id] = 1;
            else cart[id]++;

            localStorage.setItem("cart", JSON.stringify(cart));

            showMessage(`Товар додано у корзину. Кількість: ${cart[id]}`);
        });
    });
}

// --- FILTER ---
document.getElementById("btn-filter").addEventListener("click", () => {
    const nameFilter = document.getElementById("filter-name").value.toLowerCase();
    const minPrice = parseFloat(document.getElementById("filter-min-price").value) || 0;
    const maxPrice = parseFloat(document.getElementById("filter-max-price").value) || Infinity;

    const filtered = productsData.filter(p =>
        p.name.toLowerCase().includes(nameFilter) &&
        p.price >= minPrice &&
        p.price <= maxPrice
    );
    currentPage = 1;
    renderPaginatedProducts(filtered);
});

// --- USER HEADER BUTTONS (Корзина + Історія замовлень) ---
function createUserHeaderButtons() {
    const header = document.getElementById("header-buttons");

    // Кошик
    if (!document.getElementById("btn-cart")) {
        const cartBtn = document.createElement("button");
        cartBtn.id = "btn-cart";
        cartBtn.textContent = "Кошик";
        header.insertBefore(cartBtn, btnLogout);
        cartBtn.addEventListener("click", () => {
            showCartModal();
        });
    }

    // Історія замовлень
    if (!document.getElementById("btn-history")) {
        const historyBtn = document.createElement("button");
        historyBtn.id = "btn-history";
        historyBtn.textContent = "Історія замовлень";
        header.insertBefore(historyBtn, btnLogout);
        historyBtn.addEventListener("click", showOrderHistory);
    }
}

// --- CART MODAL ---
function showCartModal() {
    const modal = document.createElement("div");
    modal.id = "cart-modal";
    modal.style.position = "fixed";
    modal.style.top = "0";
    modal.style.left = "0";
    modal.style.width = "100%";
    modal.style.height = "100%";
    modal.style.background = "rgba(0,0,0,0.5)";
    modal.style.display = "flex";
    modal.style.justifyContent = "center";
    modal.style.alignItems = "center";
    modal.style.zIndex = "1000";

    const content = document.createElement("div");
    content.style.background = "#fff";
    content.style.padding = "20px";
    content.style.borderRadius = "5px";
    content.style.minWidth = "300px";

    let total = 0;
    content.innerHTML = `<h3>Ваш кошик</h3>`;
    for (const id in cart) {
        const p = productsData.find(x => x.id == id);
        const qty = cart[id];
        total += p.price * qty;

        const itemDiv = document.createElement("div");

        itemDiv.innerHTML = `
            ${p.name} - ${p.price} ₴
            <button class="qty-minus" data-id="${id}">-</button>
            <span>${qty}</span>
            <button class="qty-plus" data-id="${id}">+</button>
            <button class="remove-item" data-id="${id}">Видалити</button>
        `;

        content.appendChild(itemDiv);
    }

    // Зміна кількості
    content.querySelectorAll(".qty-plus").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.dataset.id;
            cart[id]++;

            localStorage.setItem("cart", JSON.stringify(cart));

            modal.remove();
            showCartModal();
        });
    });

    content.querySelectorAll(".qty-minus").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.dataset.id;
            if (cart[id] > 1) cart[id]--;
            else delete cart[id];

            localStorage.setItem("cart", JSON.stringify(cart));

            modal.remove();
            showCartModal();
        });
    });

    // Видалення
    content.querySelectorAll(".remove-item").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.dataset.id;
            delete cart[id];

            localStorage.setItem("cart", JSON.stringify(cart));

            modal.remove();
            showCartModal();
        });
    });

    const totalDiv = document.createElement("div");
    totalDiv.textContent = `Разом: ${total} ₴`;
    content.appendChild(totalDiv);

    const checkoutBtn = document.createElement("button");
    checkoutBtn.textContent = "Оформити замовлення";
    checkoutBtn.addEventListener("click", checkoutCart);
    content.appendChild(checkoutBtn);

    const closeBtn = document.createElement("button");
    closeBtn.textContent = "Закрити";
    closeBtn.style.marginLeft = "10px";
    closeBtn.addEventListener("click", () => modal.remove());
    content.appendChild(closeBtn);

    modal.appendChild(content);
    document.body.appendChild(modal);
}

// --- CHECKOUT ---
async function checkoutCart() {
    try {
        const body = {};
        for (const id in cart) body[id] = cart[id];
        const res = await fetch(`${apiBase}/orders`, {
            method: "POST",
            headers: { "Content-Type": "application/json", "Authorization": "Bearer " + token },
            body: JSON.stringify(body)
        });
        if (!res.ok) throw new Error("Помилка оформлення замовлення");
        showMessage("Замовлення створено!");
        cart = {};

        localStorage.removeItem("cart");

        document.getElementById("cart-modal")?.remove();
    } catch (e) {
        showMessage(e.message);
    }
}

// --- ADMIN PANEL ---
async function loadAdminData() {
    // Users
    const usersRes = await fetch(`${apiBase}/admin/users`, { headers: { "Authorization": "Bearer " + token } });
    const users = await usersRes.json();
    const usersTbody = document.querySelector("#users-table tbody");
    usersTbody.innerHTML = "";
    users.forEach(u => {
        const tr = document.createElement("tr");
        tr.innerHTML = `<td>${u.id}</td><td>${u.login}</td><td>${u.role}</td><td>${u.createdAt}</td>`;
        usersTbody.appendChild(tr);
    });

    // Products
    const productsRes = await fetch(`${apiBase}/products`, { headers: { "Authorization": "Bearer " + token } });
    const products = await productsRes.json();
    productsData = products; // оновлюємо локально
    const productsTbody = document.querySelector("#products-table tbody");
    productsTbody.innerHTML = "";
    products.forEach(p => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${p.id}</td>
            <td>${p.name}</td>
            <td>${p.price}</td>
            <td>${p.stockQuantity}</td>
            <td>${p.isActive}</td>
            <td>
                <button class="edit-product" data-id="${p.id}">Редагувати</button>
                <button class="deactivate" data-id="${p.id}">Видалити з каталогу</button>
            </td>
        `;
        productsTbody.appendChild(tr);
    });

    document.querySelectorAll(".deactivate").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            await fetch(`${apiBase}/products/${id}`, {
                method: "DELETE",
                headers: { "Authorization": "Bearer " + token }
            });
            loadAdminData();
        });
    });

    document.querySelectorAll(".edit-product").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.dataset.id;
            const product = productsData.find(p => p.id == id);
            showEditProductModal(product);
        });
    });

    // Orders
    const ordersRes = await fetch(`${apiBase}/admin/orders`, { headers: { "Authorization": "Bearer " + token } });
    const orders = await ordersRes.json();
    const ordersTbody = document.querySelector("#orders-table tbody");
    ordersTbody.innerHTML = "";
    orders.forEach(o => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${o.id}</td>
            <td>${o.user.login}</td>
            <td>${o.totalPrice}</td>
            <td>${o.status}</td>
            <td>
                <button class="change-status" data-id="${o.id}">Change Status</button>
            </td>
        `;
        ordersTbody.appendChild(tr);
    });

    document.querySelectorAll(".change-status").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const newStatus = prompt("Введіть новий статус (Created, Paid, Shipped, Completed, Cancelled):");
            if (!newStatus) return;
            await fetch(`${apiBase}/orders/${id}/status?status=${newStatus}`, {
                method: "PUT",
                headers: { "Authorization": "Bearer " + token }
            });
            loadAdminData();
        });
    });
}

// --- Історія замовлень користувача ---
const orderHistoryModal = document.getElementById("order-history-modal");
const orderHistoryTableBody = document.querySelector("#order-history-table tbody");
const closeOrderHistoryBtn = document.getElementById("close-order-history");

async function showOrderHistory() {
    try {
        const res = await fetch(`${apiBase}/orders/my`, {
            headers: { "Authorization": "Bearer " + token }
        });

        if (!res.ok) 
            throw new Error("Не вдалося завантажити історію замовлень");

        const orders = await res.json();

        orderHistoryTableBody.innerHTML = "";

        orders.forEach(o => {

            const tr = document.createElement("tr");

            //формуємо список товарів
            const productsList = o.items
                .map(i => `${i.product?.name ?? "Unknown"} x${i.quantity}`) //уникаємо падіння, якщо раптом продукт буде null
                .join(", ");

            //загальна к-ть товарів у замовленні
            const totalQuantity = o.items
                .reduce((sum, i) => sum + i.quantity, 0);

            tr.innerHTML = `
                <td>${o.id}</td>
                <td>${productsList}</td>
                <td>${totalQuantity}</td>
                <td>${o.totalPrice}</td>
                <td>${o.status}</td>
            `;
            orderHistoryTableBody.appendChild(tr);
        });

        orderHistoryModal.classList.remove("hidden");

    } catch (e) {
        showMessage(e.message);
    }
}

closeOrderHistoryBtn.addEventListener("click", () => {
    orderHistoryModal.classList.add("hidden");
});


// --- Додавання нового товару (Admin) ---
document.getElementById("add-product-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const name = document.getElementById("new-product-name").value;
    const price = parseFloat(document.getElementById("new-product-price").value);
    const stock = parseInt(document.getElementById("new-product-stock").value);
    const imageFile = document.getElementById("new-product-image").files[0];

    if (!imageFile) {
        document.getElementById("add-product-error").textContent = "Будь ласка, оберіть зображення.";
        return;
    }

    const formData = new FormData();
    formData.append("name", name);
    formData.append("price", price);
    formData.append("stockQuantity", stock);
    formData.append("image", imageFile);

    try {
        const res = await fetch(`${apiBase}/products`, {
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "Authorization": "Bearer " + token 
            },
            body: JSON.stringify({
                name: name,
                price: price,
                stockQuantity: stock
            })
        });
        
        if (!res.ok) throw new Error("Не вдалося додати товар");
        showMessage("Товар додано!");
        document.getElementById("add-product-form").reset();
        loadAdminData(); // оновлюємо таблицю
    } catch (e) {
        document.getElementById("add-product-error").textContent = e.message;
    }
});

// --- ADMIN VIEW CATALOG (тільки перегляд) ---
document.addEventListener("DOMContentLoaded", () => {
    const viewCatalogBtn = document.getElementById("btn-view-catalog");
    if (!viewCatalogBtn) return;

    viewCatalogBtn.addEventListener("click", async () => {
        // показуємо каталог
        document.getElementById("user-container").classList.remove("hidden");

        // завантажуємо товари
        const res = await fetch(`${apiBase}/products`, {
            headers: { "Authorization": "Bearer " + token }
        });
        const products = await res.json();
        renderProducts(products);

        // вимикаємо кнопки додавання в корзину
        setTimeout(() => {
            document.querySelectorAll(".product-card button").forEach(btn => {
                btn.disabled = true;
                btn.textContent = "Тільки перегляд";
            });
        }, 100);
    });
});

document.getElementById("btn-admin-catalog")?.addEventListener("click", async () => {
    document.getElementById("admin-container").classList.add("hidden");
    document.getElementById("user-container").classList.remove("hidden");

    const res = await fetch(`${apiBase}/products`, {
        headers: { "Authorization": "Bearer " + token }
    });

    const products = await res.json();
    renderProducts(products);

    setTimeout(() => {
        document.querySelectorAll(".product-card button").forEach(btn => {
            btn.disabled = true;
            btn.textContent = "Тільки перегляд";
        });
    }, 100);
});

document.getElementById("btn-admin-panel")?.addEventListener("click", () => {
    document.getElementById("user-container").classList.add("hidden");
    document.getElementById("admin-container").classList.remove("hidden");
});

function showEditProductModal(product) {

    const modal = document.createElement("div");
    modal.style.position = "fixed";
    modal.style.top = "0";
    modal.style.left = "0";
    modal.style.width = "100%";
    modal.style.height = "100%";
    modal.style.background = "rgba(0,0,0,0.5)";
    modal.style.display = "flex";
    modal.style.justifyContent = "center";
    modal.style.alignItems = "center";
    modal.style.zIndex = "2000";

    const content = document.createElement("div");
    content.style.background = "#fff";
    content.style.padding = "20px";
    content.style.borderRadius = "5px";
    content.style.minWidth = "350px";

    content.innerHTML = `
        <h3>Редагувати товар</h3>
        <input type="text" id="edit-name" value="${product.name}">
        <input type="number" id="edit-price" value="${product.price}">
        <input type="number" id="edit-stock" value="${product.stockQuantity}">
        <input type="file" id="edit-image" accept="image/*">
        <br><br>
        <button id="save-product">Зберегти</button>
        <button id="cancel-edit">Скасувати</button>
    `;

    modal.appendChild(content);
    document.body.appendChild(modal);

    document.getElementById("cancel-edit").addEventListener("click", () => modal.remove());

    document.getElementById("save-product").addEventListener("click", async () => {

        const name = document.getElementById("edit-name").value;
        const price = parseFloat(document.getElementById("edit-price").value);
        const stock = parseInt(document.getElementById("edit-stock").value);
        const imageFile = document.getElementById("edit-image").files[0];

        const formData = new FormData();
        formData.append("name", name);
        formData.append("price", price);
        formData.append("stockQuantity", stock);

        if (imageFile) {
            formData.append("image", imageFile);
        }

        const res = await fetch(`${apiBase}/products/${product.id}`, {
            method: "PUT",
            headers: { 
                "Content-Type": "application/json",
                "Authorization": "Bearer " + token 
            },
            body: JSON.stringify({
                id: product.id,
                name: name,
                price: price,
                stockQuantity: stock, 
                isActive: true
            })
        });

        if (!res.ok) {
            showMessage("Не вдалося оновити товар", "error");
            return;
        }

        showMessage("Товар оновлено!");

        modal.remove();
        loadAdminData();
    });
}

// PAGINATION (конкретна к-ть товарів на сторінку. тоді інша сторінка з товарами)

let currentPage = 1;
const itemsPerPage = 6;

function renderPaginatedProducts(products) {
    const totalPages = Math.ceil(products.length / itemsPerPage);

    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;

    renderProducts(products.slice(start, end));
    renderPaginationControls(products, totalPages);
}

function renderPaginationControls(products, totalPages) {
    let pagination = document.getElementById("pagination");

    if (!pagination) {
        pagination = document.createElement("div");
        pagination.id = "pagination";
        pagination.style.marginTop = "20px";
        pagination.style.textAlign = "center";
        document.getElementById("user-container").appendChild(pagination);
    }

    pagination.innerHTML = "";

    for (let i = 1; i <= totalPages; i++) {
        const btn = document.createElement("button");
        btn.textContent = i;
        btn.style.margin = "5px";

        if (i === currentPage) {
            btn.style.fontWeight = "bold";
        }

        btn.addEventListener("click", () => {
            currentPage = i;
            renderPaginatedProducts(products);
        });

        pagination.appendChild(btn);
    }
}

// CUSTOM ALERT

function showMessage(text, type = "success") {
    const msg = document.createElement("div");
    msg.className = "custom-message " + type;
    msg.textContent = text;

    document.body.appendChild(msg);

    setTimeout(() => {
        msg.classList.add("show");
    }, 10);

    setTimeout(() => {
        msg.classList.remove("show");
        setTimeout(() => msg.remove(), 300);
    }, 3000);
}