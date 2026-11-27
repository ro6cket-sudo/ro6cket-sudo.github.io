const ROLE_LABELS = {
    Leader: "Руководитель",
    Developer: "Разработчик",
    Tester: "Тестировщик"
};

const STATUS_LABELS = {
    New: "Новый",
    InProgress: "В работе",
    OnReview: "На ревью",
    Closed: "Закрыт"
};

async function apiRequest(path, options = {}) {
    const headers = { ...(options.headers || {}) };
    if (options.body && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    const response = await fetch(`/api${path}`, { ...options, headers });
    if (response.status === 204) {
        return null;
    }

    const text = await response.text();
    const data = text ? JSON.parse(text) : null;

    if (!response.ok) {
        const message = data?.title || data?.message || `Ошибка ${response.status}`;
        throw new Error(message);
    }

    return data;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

function formatDate(isoString) {
    if (!isoString) {
        return "Не задан";
    }
    return new Date(isoString).toLocaleDateString("ru-RU");
}

function formatDateTime(isoString) {
    if (!isoString) {
        return "";
    }
    return new Date(isoString).toLocaleString("ru-RU", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    });
}

function getQueryParam(name) {
    return new URLSearchParams(window.location.search).get(name);
}

async function loadCurrentUser(selector = "#profile-username") {
    const user = await apiRequest("/users/me");
    const el = document.querySelector(selector);
    if (el && user) {
        el.textContent = `@${user.username}`;
    }
    return user;
}

function bindDialog(dialog, openBtn, closeBtns) {
    if (!dialog) {
        return;
    }
    if (openBtn) {
        openBtn.addEventListener("click", () => dialog.showModal());
    }
    for (const btn of closeBtns) {
        if (btn) {
            btn.addEventListener("click", () => dialog.close());
        }
    }
}
