const apiUrl = '/api/products';
let userToken = ""; // Глобальный токен

// Глобал. переменные для пагинации
let currentPage = 1;
const pageSize = 8;
let totalPages = 1;

// Корзина
let cart = [];

// 1. Загрузка корзины при старте
function loadCart() {
    const savedCart = localStorage.getItem('techStoreCart');
    if (savedCart) {
        cart = JSON.parse(savedCart);
        updateCartCount();
    }
}

// 2. Добавление товара
function addToCart(product) {
    // Проверка есть ли уже такой товар
    const existingItem = cart.find(item => item.id === product.id);
    
    if (existingItem) {
        existingItem.quantity++;
    } else {
        cart.push({
            id: product.id,
            name: product.name,
            price: product.price,
            imageUrl: product.imageUrl,
            quantity: 1
        });
    }
    saveCart();
    alert(`Товар "${product.name}" добавлен в корзину!`);
}

// 3. Сохранение корзины
function saveCart() {
    localStorage.setItem('techStoreCart', JSON.stringify(cart));
    updateCartCount();
}

// 4. Обновление счетчика корзины
function updateCartCount() {
    const count = cart.reduce((sum, item) => sum + item.quantity, 0);
    document.getElementById('cart-count').innerText = count;
}

// 5. Открыть корзину
function openCart() {
    renderCartItems(); // Перерисовать список перед открытием
    document.getElementById('cart-modal').style.display = 'block';
}

// 6. Закрыть корзину
function closeCart() {
    document.getElementById('cart-modal').style.display = 'none';
}

window.onclick = function(event) {
    const modal = document.getElementById('cart-modal');
    if (event.target == modal) closeCart();
    
    // Для логина (чтобы не сломать старое)
    const loginModal = document.getElementById('login-modal');
    if (event.target == loginModal) closeLoginModal();
}

// 7. Рендер товаров в корзине
function renderCartItems() {
    const container = document.getElementById('cart-items-container');
    const totalElem = document.getElementById('cart-total-price');
    
    container.innerHTML = ''; // Очищаем старое
    let totalPrice = 0;

    if (cart.length === 0) {
        container.innerHTML = '<p style="text-align:center; padding: 20px;">Ваша корзина пуста 😔</p>';
        totalElem.innerText = '0 ₴';
        return;
    }

    cart.forEach(item => {
        const itemTotal = item.price * item.quantity;
        totalPrice += itemTotal;

        const div = document.createElement('div');
        div.className = 'cart-item';
        div.innerHTML = `
            <img src="${item.imageUrl}" alt="photo">
            
            <div class="cart-item-info">
                <p class="cart-item-title">${item.name}</p>
                <small>${item.price} ₴ / шт.</small>
            </div>

            <div class="qty-controls">
                <button class="btn-qty" onclick="changeQty(${item.id}, -1)">-</button>
                <span>${item.quantity}</span>
                <button class="btn-qty" onclick="changeQty(${item.id}, 1)">+</button>
            </div>

            <div style="font-weight:bold; min-width: 80px; text-align:right;">
                ${itemTotal} ₴
            </div>

            <button class="btn-remove" onclick="removeFromCart(${item.id})" title="Удалить">🗑</button>
        `;
        container.appendChild(div);
    });

    totalElem.innerText = totalPrice + ' ₴';
}

// 8. Изменение количества
function changeQty(id, change) {
    const item = cart.find(x => x.id === id);
    if (!item) return;

    item.quantity += change;

    // Если стало 0 то удаляем
    if (item.quantity <= 0) {
        removeFromCart(id);
        return; 
    }

    saveCart(); // Сохраняем в localStorage
    renderCartItems(); // Перерисовываем (чтобы обновилась цена и цифра)
}

// 9. Удаление из корзины
function removeFromCart(id) {
    cart = cart.filter(x => x.id !== id);
    saveCart();
    renderCartItems();
}

// 10 Оформление заказа
async function checkout() {
    if (cart.length === 0) {
        alert("Корзина пуста!");
        return;
    }
    
    // Проверка авторизации
    if (!userToken) {
        alert("Для оформления заказа нужно войти!");
        closeCart();
        openLoginModal();
        return;
    }

    // Подготовка данных для сервера
    const orderData = {
        Items: cart.map(item => ({
            ProductId: item.id,
            Quantity: item.quantity
        }))
    };

    try {
        // Отправка запроса на сервер
        const response = await fetch('/api/orders', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${userToken}`
            },
            body: JSON.stringify(orderData)
        });

        if (response.ok) {
            const result = await response.json();
            
            // Успех!
            alert(`Заказ №${result.id} успешно оформлен!`);
            
            // Очистка корзины
            cart = [];
            saveCart();       // Очистит localStorage
            renderCartItems(); // Очистит вид
            closeCart();      // Закроет окно
            
        } else {
            const errorText = await response.text();
            console.error("Ошибка сервера:", errorText);
            alert("Ошибка оформления: " + errorText);
        }
    } catch (e) {
        console.error(e);
        alert("Ошибка сети. Проверьте консоль.");
    }
}

// ЗАГРУЗКА ТОВАРОВ 
async function loadProducts(page = 1) {
    const grid = document.getElementById('products-grid');
    const pageInfo = document.getElementById('page-info');
    const btnPrev = document.getElementById('btn-prev');
    const btnNext = document.getElementById('btn-next');

    const searchInput = document.getElementById('search-input');
    const categorySelect = document.getElementById('category-filter');
    
    const searchValue = searchInput ? searchInput.value : '';
    const categoryValue = categorySelect ? categorySelect.value : '';

    // Очистка перед загрузкой
    grid.innerHTML = '<p>Загрузка...</p>';

    try {

        let url = `${apiUrl}?page=${page}&size=${pageSize}`;
        
        if (searchValue) {
            url += `&search=${encodeURIComponent(searchValue)}`;
        }
        if (categoryValue) {
            url += `&categoryId=${categoryValue}`;
        }

        const response = await fetch(url);
        if (!response.ok) throw new Error('Ошибка загрузки');
        
        const data = await response.json();

        currentPage = data.currentPage;
        totalPages = data.totalPages;

        // Если товаров нет
        if (data.items.length === 0) {
            grid.innerHTML = '<p>Ничего не найдено 🤷‍♂️</p>';
            pageInfo.innerText = '-';
            btnPrev.disabled = true;
            btnNext.disabled = true;
            return;
        }

        renderProducts(data.items);
        pageInfo.innerText = `Стр. ${currentPage} из ${totalPages}`;
        btnPrev.disabled = (currentPage === 1);
        btnNext.disabled = (currentPage >= totalPages);

    } catch (error) {
        console.error(error);
        grid.innerHTML = '<p style="color:red">Не удалось загрузить товары.</p>';
    }
}

// Поиск
function applyFilters() {
    // При поиске всегда сброс на 1 страницу
    loadProducts(1);
}

// Переключение страниц
function changePage(direction){
    const newPage = currentPage + direction;

    if (newPage > 0 && newPage <= totalPages) {
        loadProducts(newPage);
        // Плавный скролл наверх
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function renderProducts(products) {
    const grid = document.getElementById('products-grid');
    grid.innerHTML = '';

    products.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';

        // Формируем путь к картинке
        let imageSrc = product.imageUrl;
        if (!imageSrc) {
            imageSrc = 'https://placehold.co/300x300/png?text=No+Image';
        }

        const name = product.name || 'Товар';
        const price = product.price || 0;

        const productData = JSON.stringify(product).replace(/"/g, '&quot;');

        card.innerHTML = `
            <img src="${imageSrc}" alt="${name}" class="product-image">
            <h3 class="product-title">${name}</h3>
            <div class="product-price">${price} ₴</div>

            <button class="btn-buy" onclick='addToCart(${productData})'>В корзину</button>
        `;
        grid.appendChild(card);
    });
}

// АВТОРИЗАЦИЯ

// 1. Открытие/Закрытие модального окна
function openLoginModal() {
    document.getElementById('login-modal').style.display = 'block';
}

function closeLoginModal() {
    document.getElementById('login-modal').style.display = 'none';
    document.getElementById('login-error').style.display = 'none';
}

// 2. Обработка формы входа
document.getElementById('login-form').addEventListener('submit', async function(e) {
    e.preventDefault();

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;
    const errorMsg = document.getElementById('login-error');

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (response.ok) {
            const data = await response.json();

            // Сохранение токена
            localStorage.setItem('techStoreToken', data.token);

            alert("Успешный вход!");
            closeLoginModal();
            checkAuthStatus();

        } else {
            errorMsg.innerText = "Неверный логин или пароль";
            errorMsg.style.display = 'block';
        }
    } catch (err) {
        console.error(err);
        errorMsg.innerText = "Ошибка сервера";
        errorMsg.style.display = 'block';
    }
});

function checkAuthStatus() {
    const token = localStorage.getItem('techStoreToken');
    const authBtnContainer = document.getElementById('auth-status');
    const adminPanel = document.getElementById('admin-panel');

    // Сброс (по умолчанию всё скрыто)
    if (adminPanel) {
        adminPanel.style.display = 'none';
        adminPanel.style.backgroundColor = '#f9f9f9';
        adminPanel.style.border = 'none';
        const title = adminPanel.querySelector('h2');
        if(title) title.innerText = "⚙️ Панель Администратора";
    }
    
    const logsBtn = document.getElementById('btn-tab-logs');
    if (logsBtn) logsBtn.style.display = 'none';

    if (token) {
        // БЛОК АВТОРИЗОВАННОГО ПОЛЬЗОВАТЕЛЯ
        const decodedToken = parseJwt(token);
        
        let roleData = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] 
                       || decodedToken["role"] 
                       || "User";

        const userRoles = Array.isArray(roleData) ? roleData : [roleData];
        
        // Получаем имя пользователя
        const username = decodedToken.sub || decodedToken.email || "User";
        
        // Кнопки профиля
        authBtnContainer.innerHTML = `
            <span style="color: white; margin-right: 10px;">Привет, ${username}!</span>
            <button class="btn-login" onclick="openMyOrdersModal()" style="background:#2196F3; margin-right:5px;">Мои заказы</button>
            <button class="btn-login" onclick="logout()">Выйти</button>
        `;

        const isAdmin = userRoles.includes("Admin");
        const isOwner = userRoles.includes("BusinessOwner");

        if ((isAdmin || isOwner) && adminPanel) {
            adminPanel.style.display = 'block';

            // ВИЗУАЛИЗАЦИЯ ДЛЯ ВЛАДЕЛЬЦА 
            if (isOwner) {
                adminPanel.style.backgroundColor = '#fffbea'; 
                adminPanel.style.border = '2px solid #d4af37'; 
                
                // Другой заголовок
                const title = adminPanel.querySelector('h2');
                if(title) title.innerText = "👑 Панель Владельца";
                
                // Показываем кнопку логов
                if (logsBtn) logsBtn.style.display = 'inline-block';
            }
        }
        
        userToken = token;
    } else {
        authBtnContainer.innerHTML = `
            <button class="btn-login" onclick="openLoginModal()">Войти</button>
            <button class="btn-login" onclick="openRegisterModal()" style="background:#00a046; margin-left:5px;">Регистрация</button>
        `;
        userToken = "";
    }
}



// 4. Выход
function logout() {
    localStorage.removeItem('techStoreToken');
    checkAuthStatus();
    alert("Вы вышли из системы");
}


// ПОКУПКА (Уже не нужна, есть addToCart)
function buyItem(id) {
    alert(`Товар ID ${id} добавлен в корзину!`);
}

// Парс JWT для получения роли
function parseJwt(token) {
    try {
        var base64Url = token.split('.')[1];
        var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

// АДМИН ПАНЕЛЬ: ВКЛАДКИ 

function showAdminTab(tabName) {
    // 1. Скрываем все вкладки
    document.querySelectorAll('.admin-tab-content').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));

    // 2. Показываем нужную
    const activeTab = document.getElementById(`tab-${tabName}`);
    if (activeTab) activeTab.style.display = 'block';
    
    // Загружаем данные для вкладки
    if (tabName === 'products') loadAdminProducts();
    if (tabName === 'orders') loadAdminOrders();
    if (tabName === 'users') loadAdminUsers();
    if (tabName === 'logs') loadSystemLogs();
}

function closeAdminPanel() {
    document.getElementById('admin-panel').style.display = 'none';
}

// АДМИН: ПОЛЬЗОВАТЕЛИ
async function loadAdminUsers() {
    const tbody = document.getElementById('admin-users-table');
    tbody.innerHTML = '<tr><td colspan="3">Загрузка...</td></tr>';

    try {
        const response = await fetch('/api/users', {
            headers: { 'Authorization': `Bearer ${userToken}` }
        });

        if (response.ok) {
            const users = await response.json();
            tbody.innerHTML = ''; // Очистить
            
            users.forEach(u => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td><small>${u.id}</small></td>
                    <td>${u.email}</td>
                    <td>${u.userName}</td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="3" style="color:red">Ошибка доступа</td></tr>';
        }
    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="3" style="color:red">Ошибка сети</td></tr>';
    }
}

// АДМИН: ТОВАРЫ
async function loadAdminProducts() {
    const tbody = document.getElementById('admin-products-table');
    tbody.innerHTML = '<tr><td colspan="6">Загрузка...</td></tr>';

    try {
        // Загружаем сразу много товаров (size=100), чтобы админ видел всё
        const response = await fetch('/api/products?size=100');
        const data = await response.json();
        
        tbody.innerHTML = ''; // Очистка

        data.items.forEach(p => {
            // Экранируем данные для передачи в кнопку
            const productJson = JSON.stringify(p).replace(/"/g, '&quot;');

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${p.id}</td>
                <td><img src="${p.imageUrl || ''}" alt="img" style="width:40px"></td>
                <td>${p.name}</td>
                <td>${p.price} ₴</td>
                <td>${p.stock}</td>
                <td>
                    <button class="btn-action btn-edit" onclick='openEditProductModal(${productJson})'>✏️</button>
                    <button class="btn-action btn-delete" onclick="deleteProduct(${p.id})">🗑</button>
                </td>
            `;
            tbody.appendChild(tr);
        });

    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="6" style="color:red">Ошибка сети</td></tr>';
    }
}

// АДМИН: УДАЛЕНИЕ
async function deleteProduct(id) {
    if (!confirm("Вы точно хотите удалить этот товар?")) return;

    try {
        const response = await fetch(`${apiUrl}/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${userToken}` }
        });

        if (response.ok) {
            // Удаляем строку из таблицы визуально
            loadAdminProducts();
        } else {
            alert("Ошибка удаления");
        }
    } catch (e) {
        alert("Ошибка сети");
    }
}

// АДМИН: РЕДАКТИРОВАНИЕ И СОЗДАНИЕ

// 1. Открыть модалку для СОЗДАНИЯ
function openAddProductModal() {
    document.getElementById('product-form').reset(); // Очистить форму
    document.getElementById('prod-id').value = ''; // Убрать ID
    document.getElementById('modal-product-title').innerText = "Новый товар";
    document.getElementById('product-modal').style.display = 'block';
}

// 2. Открыть модалку для РЕДАКТИРОВАНИЯ
function openEditProductModal(product) {
    // Заполняем поля данными из товара
    document.getElementById('prod-id').value = product.id;
    document.getElementById('prod-name').value = product.name;
    document.getElementById('prod-price').value = product.price;
    document.getElementById('prod-stock').value = product.stock;
    document.getElementById('prod-desc').value = product.description || "";
    document.getElementById('prod-cat').value = product.categoryId;
    
    document.getElementById('modal-product-title').innerText = "Редактирование товара";
    document.getElementById('product-modal').style.display = 'block';
}

function closeProductModal() {
    document.getElementById('product-modal').style.display = 'none';
}

// 3. ОБРАБОТКА СОХРАНЕНИЯ (ЕДИНАЯ ФУНКЦИЯ)
document.getElementById('product-form').addEventListener('submit', async function(e) {
    e.preventDefault();

    const id = document.getElementById('prod-id').value;
    const formData = new FormData(this); // Собирает файлы и поля

    // Определяем: это создание или обновление?
    const isEdit = !!id; 
    const url = isEdit ? `${apiUrl}/${id}` : apiUrl;
    const method = isEdit ? 'PUT' : 'POST';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': `Bearer ${userToken}`
            },
            body: formData
        });

        if (response.ok) {
            alert(isEdit ? "Товар обновлен!" : "Товар создан!");
            closeProductModal();
            loadAdminProducts(); // Обновить таблицу в админке
            loadProducts();      // Обновить каталог на главной
        } else {
            const err = await response.text();
            alert("Ошибка: " + err);
        }
    } catch (error) {
        console.error(error);
        alert("Ошибка сети");
    }
});

// АДМИН: ЗАКАЗЫ

async function loadAdminOrders() {
    const tbody = document.getElementById('admin-orders-table');
    tbody.innerHTML = '<tr><td colspan="6">Загрузка...</td></tr>';

    try {
        const response = await fetch('/api/orders', { // Этот метод только для админа
            headers: { 'Authorization': `Bearer ${userToken}` }
        });

        if (response.ok) {
            const orders = await response.json();
            tbody.innerHTML = ''; // Очистка

            if (orders.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6">Нет заказов</td></tr>';
                return;
            }

            orders.forEach(order => {
                const tr = document.createElement('tr');
                
                // Красивая дата
                const date = new Date(order.orderDate).toLocaleString();

                // Генерация выпадающего списка для статуса
                // Проверка, какой статус сейчас, чтобы добавить атрибут 'selected'
                const statuses = ['Created', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];
                let optionsHtml = '';
                
                statuses.forEach(s => {
                    const isSelected = (order.status === s) ? 'selected' : '';
                    optionsHtml += `<option value="${s}" ${isSelected}>${s}</option>`;
                });

                tr.innerHTML = `
                    <td>${order.id}</td>
                    <td>${date}</td>
                    <td><small>${order.userId || 'Гость'}</small></td>
                    <td><strong>${order.totalAmount} ₴</strong></td>
                    <td>
                        <select onchange="updateOrderStatus(${order.id}, this.value)" style="padding:5px;">
                            ${optionsHtml}
                        </select>
                    </td>
                    <td>
                        <button class="btn-action" onclick="alert('Детали заказа №${order.id}: ' + JSON.stringify('${order.items.length} поз.'))">ℹ️ Инфо</button>
                    </td>
                `;
                tbody.appendChild(tr);
            });

        } else {
            tbody.innerHTML = '<tr><td colspan="6" style="color:red">Ошибка доступа</td></tr>';
        }
    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="6" style="color:red">Ошибка сети</td></tr>';
    }
}

// АДМИН: ОБНОВЛЕНИЕ СТАТУСА
async function updateOrderStatus(orderId, newStatus) {
    
    try {
        const response = await fetch(`/api/orders/${orderId}/status`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${userToken}`
            },
            
            body: JSON.stringify(newStatus)
        });

        if (response.ok) {
            console.log(`Заказ ${orderId} обновлен на ${newStatus}`);
        } else {
            alert("Не удалось обновить статус. Возможно, заказ уже закрыт.");
            loadAdminOrders(); // Вернуть как было
        }
    } catch (e) {
        console.error(e);
        alert("Ошибка сети");
    }
}

// ВЛАДЕЛЕЦ: ЛОГИ
async function loadSystemLogs() {
    const tbody = document.getElementById('admin-logs-table');
    tbody.innerHTML = '<tr><td colspan="6">Загрузка...</td></tr>';

    try {
        const response = await fetch('/api/logs', {
            headers: { 'Authorization': `Bearer ${userToken}` }
        });

        if (response.ok) {
            const logs = await response.json();
            tbody.innerHTML = ''; 

            if (logs.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6">Логов пока нет</td></tr>';
                return;
            }

            logs.forEach(log => {
                const tr = document.createElement('tr');
                const date = new Date(log.timestamp).toLocaleString();
                
                let color = 'black';
                if (log.action === 'Deleted') color = 'red';
                if (log.action === 'Created') color = 'green';
                if (log.action === 'Updated') color = 'orange';

                tr.innerHTML = `
                    <td><small>${date}</small></td>
                    <td>${log.adminEmail}</td>
                    <td style="color:${color}; font-weight:bold;">${log.action}</td>
                    <td>${log.entityName}</td>
                    <td>${log.entityId}</td>
                    <td><small>${log.details}</small></td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="6" style="color:red">Доступ запрещен (Нужны права Владельца)</td></tr>';
        }
    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="6" style="color:red">Ошибка сети</td></tr>';
    }
}

// РЕГИСТРАЦИЯ

function openRegisterModal() {
    document.getElementById('register-modal').style.display = 'block';
}

document.getElementById('register-form').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const email = document.getElementById('reg-email').value;
    const password = document.getElementById('reg-password').value;
    const confirm = document.getElementById('reg-confirm').value;
    const errorMsg = document.getElementById('reg-error');

    if (password !== confirm) {
        errorMsg.innerText = "Пароли не совпадают!";
        errorMsg.style.display = 'block';
        return;
    }

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (response.ok) {
            alert("Регистрация успешна! Теперь войдите.");
            closeRegisterModal();
            openLoginModal(); // Сразу даем войти
        } else {
            const err = await response.text();
            errorMsg.innerText = "Ошибка: " + err;
            errorMsg.style.display = 'block';
        }
    } catch (e) {
        errorMsg.innerText = "Ошибка сети";
        errorMsg.style.display = 'block';
    }
});

function closeRegisterModal() {
    document.getElementById('register-modal').style.display = 'none';
    document.getElementById('reg-error').style.display = 'none';
}

// МОИ ЗАКАЗЫ (КЛИЕНТ)
function openMyOrdersModal() {
    document.getElementById('my-orders-modal').style.display = 'block';
    loadMyOrders();
}

function closeMyOrdersModal() {
    document.getElementById('my-orders-modal').style.display = 'none';
}

async function loadMyOrders() {
    const tbody = document.getElementById('my-orders-table');
    tbody.innerHTML = '<tr><td colspan="5">Загрузка...</td></tr>';

    try {
        const response = await fetch('/api/orders/my-orders', {
            headers: { 'Authorization': `Bearer ${userToken}` }
        });

        if (response.ok) {
            const orders = await response.json();
            tbody.innerHTML = '';

            if (orders.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5">Вы еще ничего не заказывали 🛍️</td></tr>';
                return;
            }

            orders.forEach(order => {
                const tr = document.createElement('tr');
                const date = new Date(order.orderDate).toLocaleDateString();
                
                // Статус на русском
                const statusMap = {
                    'Created': 'Создан',
                    'Processing': 'В обработке',
                    'Shipped': 'Отправлен',
                    'Delivered': 'Доставлен',
                    'Cancelled': 'Отменен'
                };
                const statusRu = statusMap[order.status] || order.status;

                // Список товаров в заказе
                const itemsSummary = order.items
                    .map(i => `${i.productName} x${i.quantity}`)
                    .join(', ');

                tr.innerHTML = `
                    <td>#${order.id}</td>
                    <td>${date}</td>
                    <td><strong>${order.totalAmount} ₴</strong></td>
                    <td>${statusRu}</td>
                    <td><small>${itemsSummary}</small></td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" style="color:red">Ошибка загрузки</td></tr>';
        }
    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="5" style="color:red">Ошибка сети</td></tr>';
    }
}

// Закрытие по клику
window.onclick = function(event) {
    // Старые проверки
    if (event.target == document.getElementById('cart-modal')) closeCart();
    if (event.target == document.getElementById('login-modal')) closeLoginModal();
    if (event.target == document.getElementById('product-modal')) closeProductModal();
    
    // Новые проверки
    if (event.target == document.getElementById('register-modal')) closeRegisterModal();
    if (event.target == document.getElementById('my-orders-modal')) closeMyOrdersModal();
}

// Старт
loadProducts();
checkAuthStatus();
loadCart();