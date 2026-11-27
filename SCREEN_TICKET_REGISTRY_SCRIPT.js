const registryTitle = document.getElementById("registry-title");
const windowTitle = document.getElementById("window-title");
const navRegistry = document.getElementById("nav-registry");
const searchInput = document.getElementById("search-input");
const statusFilter = document.getElementById("status-filter");
const priorityFilter = document.getElementById("priority-filter");
const assigneeFilter = document.getElementById("assignee-filter");
const tbody = document.getElementById("tickets-tbody");
const resultCount = document.getElementById("result-count");

const createTicketDialog = document.getElementById("create-ticket-dialog");
const createTicketForm = document.getElementById("create-ticket-form");
const createTicketError = document.getElementById("create-ticket-error");

let currentUser = null;
let currentProjectId = null;
let loadedTickets = [];

bindDialog(
    createTicketDialog,
    document.getElementById("open-create-ticket"),
    [
        document.getElementById("close-create-ticket"),
        document.getElementById("cancel-create-ticket")
    ]
);

function renderAssigneeOptions(members) {
    const fixedOptions = `
        <option value="">Исполнитель: Все</option>
        <option value="unassigned">Не назначен</option>
    `;
    const memberOptions = members
        .map((m) => `<option value="${escapeHtml(m.userId)}">${escapeHtml(m.fullName)} (@${escapeHtml(m.username)})</option>`)
        .join("");
    assigneeFilter.innerHTML = fixedOptions + memberOptions;
}

function renderTickets() {
    const search = searchInput.value.trim().toLowerCase();
    const onlyUnassigned = assigneeFilter.value === "unassigned";

    const filtered = loadedTickets.filter((ticket) => {
        if (onlyUnassigned && ticket.assigneeId) {
            return false;
        }
        if (search) {
            const numberText = `#bug-${ticket.ticketNumber}`;
            const titleText = (ticket.title || "").toLowerCase();
            return numberText.includes(search) || titleText.includes(search);
        }
        return true;
    });

    resultCount.textContent = `Показано ${filtered.length} из ${loadedTickets.length} тикетов`;

    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="response-status">Тикеты по выбранным фильтрам не найдены.</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map((ticket, index) => {
        const rowClass = index % 2 === 1 ? "record alternate" : "record";
        const statusLabel = STATUS_LABELS[ticket.status] || ticket.status;
        const cardHref = `SCREEN_TICKET_CARD.html?ticketId=${encodeURIComponent(ticket.id)}&projectId=${encodeURIComponent(currentProjectId)}`;
        const takeBtn = !ticket.assigneeId && ticket.status === "New"
            ? `<button class="button row-button" type="button" data-take-id="${escapeHtml(ticket.id)}">Взять в работу</button>`
            : "";

        return `
            <tr class="${rowClass}">
                <td class="cell number-cell"><b>#BUG-${escapeHtml(ticket.ticketNumber)}</b></td>
                <td class="cell">
                    <strong class="row-title">${escapeHtml(ticket.title)}</strong>
                    <small class="row-subtitle">Версия: ${escapeHtml(ticket.discoveryVersion)}</small>
                </td>
                <td class="cell"><span class="badge">${escapeHtml(statusLabel)}</span></td>
                <td class="cell">${escapeHtml(ticket.priority)}</td>
                <td class="cell">${escapeHtml(ticket.assigneeName || "Не назначен")}</td>
                <td class="cell">${escapeHtml(formatDate(ticket.deadline))}</td>
                <td class="cell">
                    <div class="row-actions">
                        ${takeBtn}
                        <a class="button row-button dark" href="${cardHref}">Открыть →</a>
                    </div>
                </td>
            </tr>
        `;
    }).join("");
}

async function fetchTickets() {
    const params = new URLSearchParams();
    if (statusFilter.value) {
        params.set("status", statusFilter.value);
    }
    if (priorityFilter.value) {
        params.set("priority", priorityFilter.value);
    }
    if (assigneeFilter.value && assigneeFilter.value !== "unassigned") {
        params.set("assigneeId", assigneeFilter.value);
    }

    const query = params.toString();
    const path = `/projects/${encodeURIComponent(currentProjectId)}/tickets${query ? `?${query}` : ""}`;
    loadedTickets = (await apiRequest(path)) || [];
    renderTickets();
}

tbody.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-take-id]");
    if (!button || !currentUser) {
        return;
    }

    button.disabled = true;
    try {
        const ticketId = button.dataset.takeId;
        await apiRequest(`/tickets/${encodeURIComponent(ticketId)}/assign`, {
            method: "PATCH",
            body: JSON.stringify({
                assigneeId: currentUser.id,
                reason: "Взял в работу из реестра"
            })
        });
        await fetchTickets();
    } catch (error) {
        alert(error.message);
        button.disabled = false;
    }
});

for (const filterEl of [statusFilter, priorityFilter, assigneeFilter]) {
    filterEl.addEventListener("change", () => {
        fetchTickets().catch((error) => alert(error.message));
    });
}

searchInput.addEventListener("input", renderTickets);

createTicketForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    createTicketError.hidden = true;

    const formData = new FormData(createTicketForm);
    const deadlineRaw = formData.get("deadline")?.toString();
    const payload = {
        title: formData.get("title")?.toString().trim(),
        reproductionSteps: formData.get("reproductionSteps")?.toString().trim(),
        priority: formData.get("priority")?.toString(),
        discoveryVersion: formData.get("discoveryVersion")?.toString().trim(),
        deadline: deadlineRaw ? new Date(deadlineRaw).toISOString() : null,
        attachments: []
    };

    try {
        await apiRequest(`/projects/${encodeURIComponent(currentProjectId)}/tickets`, {
            method: "POST",
            body: JSON.stringify(payload)
        });
        createTicketForm.reset();
        createTicketDialog.close();
        await fetchTickets();
    } catch (error) {
        createTicketError.textContent = error.message;
        createTicketError.hidden = false;
    }
});

async function resolveProjectId() {
    const fromUrl = getQueryParam("projectId");
    if (fromUrl) {
        return fromUrl;
    }
    const projects = await apiRequest("/projects");
    return projects?.[0]?.id || null;
}

async function init() {
    try {
        currentUser = await loadCurrentUser();
        currentProjectId = await resolveProjectId();

        if (!currentProjectId) {
            tbody.innerHTML = '<tr><td colspan="7" class="response-status">Нет доступных проектов. Создайте проект на главном экране.</td></tr>';
            return;
        }

        navRegistry.href = `SCREEN_TICKET_REGISTRY.html?projectId=${encodeURIComponent(currentProjectId)}`;

        const [project, members] = await Promise.all([
            apiRequest(`/projects/${encodeURIComponent(currentProjectId)}`),
            apiRequest(`/projects/${encodeURIComponent(currentProjectId)}/members`)
        ]);

        if (project) {
            registryTitle.textContent = `Реестр тикетов: ${project.name}`;
            windowTitle.textContent = `Реестр тикетов — ${project.name}`;
        }

        renderAssigneeOptions(members || []);
        await fetchTickets();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="7" class="response-status">Ошибка загрузки: ${escapeHtml(error.message)}</td></tr>`;
    }
}

init();
